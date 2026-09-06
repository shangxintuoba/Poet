using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

public sealed class CardLibraryJsonConverterWindow : EditorWindow
{
    private static readonly string[] BaseSheets = { "FullLibrary", "Raw_Choice", "Node", "DailyMission" };
    private static readonly string[] ForgeSheets =
    {
        "ForgeLibrary_Universal", "ForgeLibrary_Nature", "ForgeLibrary_Politics", "ForgeLibrary_Emotion"
    };
    private static readonly string[] RequiredSheets = BaseSheets.Concat(ForgeSheets).ToArray();

    private const string DefaultWorkbookPath = "Assets/CardLibrary/CardLibrary.xlsx";
    private const string OutputJsonPath = "Assets/CardLibrary/CardLibrary.json";

    private DefaultAsset workbookAsset;
    private string status;
    private MessageType statusType = MessageType.Info;

    [MenuItem("Tools/Card Library/Convert Excel to JSON")]
    private static void Open()
    {
        GetWindow<CardLibraryJsonConverterWindow>("Card Library Converter");
    }

    private void OnEnable()
    {
        if (workbookAsset == null)
            workbookAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultWorkbookPath);
    }

    private void OnGUI()
    {
        GUILayout.Label("Card Library XLSX → JSON", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Reads the CardLibrary workbook and writes CardLibrary.json to Assets/CardLibrary. " +
            "The generated format matches CardLibrary.cs.", MessageType.Info);

        workbookAsset = (DefaultAsset)EditorGUILayout.ObjectField(
            "Workbook", workbookAsset, typeof(DefaultAsset), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Default Workbook"))
                workbookAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultWorkbookPath);

            if (GUILayout.Button("Select Workbook…"))
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select CardLibrary.xlsx", Application.dataPath, "xlsx");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    string projectPath = ToProjectPath(selectedPath);
                    if (string.IsNullOrEmpty(projectPath))
                    {
                        SetStatus("Please select an .xlsx file inside this Unity project's Assets folder.", MessageType.Warning);
                    }
                    else
                    {
                        workbookAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(projectPath);
                    }
                }
            }
        }

        GUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(workbookAsset == null))
        {
            if (GUILayout.Button("Convert and Save JSON", GUILayout.Height(30f)))
                ConvertWorkbook();
        }

        GUILayout.Space(8f);
        EditorGUILayout.LabelField("Output", OutputJsonPath);
        if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, statusType);
    }

    public void ConvertWorkbook()
    {
        string assetPath = AssetDatabase.GetAssetPath(workbookAsset);
        if (!assetPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            SetStatus("The selected asset must be an .xlsx workbook.", MessageType.Error);
            return;
        }

        try
        {
            string absoluteWorkbookPath = Path.GetFullPath(assetPath);
            Dictionary<string, List<List<string>>> sheets = XlsxReader.Read(absoluteWorkbookPath);
            CardLibrary.CardLibraryData library = BuildCardLibrary(sheets);
            string json = JsonUtility.ToJson(library, true);

            string absoluteOutputPath = Path.GetFullPath(OutputJsonPath);
            File.WriteAllText(absoluteOutputPath, json, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(OutputJsonPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();

            int rawChoiceCount = library.cards
                .Where(card => card != null && card.type == "Raw" && card.choices != null)
                .Sum(card => card.choices.Length);
            SetStatus($"Created {OutputJsonPath}: {library.cards.Length} cards, {rawChoiceCount} Raw choices, {library.nodes.Length} nodes, {library.dailyMissions.Length} daily missions.", MessageType.Info);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(OutputJsonPath);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, MessageType.Error);
            Debug.LogException(exception);
        }
    }

    private static CardLibrary.CardLibraryData BuildCardLibrary(Dictionary<string, List<List<string>>> sheets)
    {
        List<string> missingSheets = RequiredSheets.Where(sheet => !sheets.ContainsKey(sheet)).ToList();
        if (missingSheets.Count > 0)
            throw new InvalidDataException("Missing required sheet: " + string.Join(", ", missingSheets));

        List<Dictionary<string, string>> fullLibrary = AsRows(sheets["FullLibrary"], new[]
        {
            "Index", "Name", "type", "Description", "WillPowerDelta", "MaterialType", "Useable", "CanBeDropped", "Choices"
        });
        List<Dictionary<string, string>> rawChoices = AsRows(sheets["Raw_Choice"], new[]
        {
            "Index", "ChoiceText", "UsedText", "CardsAdded", "CardsDestroyed", "RandomCardList", "RandomCardNumber",
            "TimeConsumed", "UnolockNode", "HideOtherChoices", "DestroyWhenUsed"
        });
        List<Dictionary<string, string>> nodeRows = AsRows(sheets["Node"], new[] { "Index", "NodeName" });
        List<Dictionary<string, string>> dailyMissionRows = AsRows(sheets["DailyMission"], new[]
        {
            "Index", "Name", "MoneyReward", "RequiredCards"
        });

        Dictionary<string, Dictionary<string, string>> choicesById = rawChoices.ToDictionary(
            row => Value(row, "Index"), row => row);

        List<CardLibrary.CardData> cards = new List<CardLibrary.CardData>();
        foreach (Dictionary<string, string> row in fullLibrary)
        {
            CardLibrary.CardData card = new CardLibrary.CardData
            {
                id = Value(row, "Index"),
                name = Value(row, "Name"),
                type = Value(row, "type"),
                description = Value(row, "Description"),
                canBeDropped = BoolValue(Value(row, "CanBeDropped"))
            };

            if (card.type == "Emotion")
            {
                card.willPowerDelta = IntValue(Value(row, "WillPowerDelta"));
            }
            else if (card.type == "Material")
            {
                card.materialType = Value(row, "MaterialType");
            }
            else if (card.type == "Raw")
            {
                card.useable = BoolValue(Value(row, "Useable"));
                List<CardLibrary.RawChoiceData> choices = new List<CardLibrary.RawChoiceData>();
                foreach (string choiceId in SplitIds(Value(row, "Choices")))
                {
                    if (!choicesById.TryGetValue(choiceId, out Dictionary<string, string> choice))
                        throw new InvalidDataException($"Raw card '{card.id}' references missing choice '{choiceId}'.");

                    choices.Add(new CardLibrary.RawChoiceData
                    {
                        id = choiceId,
                        choiceText = Value(choice, "ChoiceText"),
                        usedText = Value(choice, "UsedText"),
                        cardsAdded = SplitIds(Value(choice, "CardsAdded")).ToArray(),
                        cardsDestroyed = SplitIds(Value(choice, "CardsDestroyed")).ToArray(),
                        randomCardList = SplitIds(Value(choice, "RandomCardList")).ToArray(),
                        randomCardNumber = IntValue(Value(choice, "RandomCardNumber")),
                        timeConsumed = IntValue(Value(choice, "TimeConsumed")),
                        unlockNodes = SplitIds(Value(choice, "UnolockNode")).ToArray(),
                        hideOtherChoices = BoolValue(Value(choice, "HideOtherChoices")),
                        destroyWhenUsed = BoolValue(Value(choice, "DestroyWhenUsed"))
                    });
                }
                card.choices = choices.ToArray();
            }

            cards.Add(card);
        }

        CardLibrary.NodeData[] nodes = nodeRows.Select(row => new CardLibrary.NodeData
        {
            id = Value(row, "Index"),
            name = Value(row, "NodeName")
        }).ToArray();

        CardLibrary.DailyMissionData[] dailyMissions = dailyMissionRows.Select(row => new CardLibrary.DailyMissionData
        {
            id = Value(row, "Index"),
            name = Value(row, "Name"),
            moneyReward = IntValue(Value(row, "MoneyReward")),
            requiredCards = SplitIds(Value(row, "RequiredCards")).ToArray()
        }).ToArray();

        CardLibrary.ForgeLibraryData[] forgeLibraries = ForgeSheets
            .Select(sheetName => BuildForgeLibrary(sheetName, sheets[sheetName]))
            .ToArray();

        return new CardLibrary.CardLibraryData
        {
            schemaVersion = 7,
            sourceSheets = RequiredSheets,
            cards = cards.ToArray(),
            nodes = nodes,
            dailyMissions = dailyMissions,
            forgeLibraries = forgeLibraries
        };
    }

    private static CardLibrary.ForgeLibraryData BuildForgeLibrary(string sheetName, List<List<string>> rows)
    {
        if (rows.Count < 2)
            throw new InvalidDataException($"Forge sheet '{sheetName}' must contain an index row and a name row.");

        List<string> ingredientIds = RowFrom(rows, 0).Skip(2).Select(Clean).ToList();
        List<string> ingredientNames = RowFrom(rows, 1).Skip(2).Select(Clean).ToList();
        List<CardLibrary.ForgeIngredientData> ingredients = new List<CardLibrary.ForgeIngredientData>();
        for (int index = 0; index < ingredientIds.Count; index++)
        {
            string id = ingredientIds[index];
            string name = index < ingredientNames.Count ? ingredientNames[index] : string.Empty;
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                ingredients.Add(new CardLibrary.ForgeIngredientData { id = id, name = name });
        }

        List<CardLibrary.ForgeFormulaData> formulas = new List<CardLibrary.ForgeFormulaData>();
        for (int rowIndex = 2; rowIndex < rows.Count; rowIndex++)
        {
            List<string> row = RowFrom(rows, rowIndex);
            string secondId = Cell(row, 0);
            string secondName = Cell(row, 1);
            if (string.IsNullOrEmpty(secondId) || string.IsNullOrEmpty(secondName))
                continue;

            for (int columnIndex = 0; columnIndex < ingredientIds.Count; columnIndex++)
            {
                string firstId = ingredientIds[columnIndex];
                string firstName = columnIndex < ingredientNames.Count ? ingredientNames[columnIndex] : string.Empty;
                string resultName = Cell(row, columnIndex + 2);
                if (string.IsNullOrEmpty(firstId) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(resultName))
                    continue;

                formulas.Add(new CardLibrary.ForgeFormulaData
                {
                    firstIngredientId = firstId,
                    firstIngredientName = firstName,
                    secondIngredientId = secondId,
                    secondIngredientName = secondName,
                    resultCardName = resultName
                });
            }
        }

        return new CardLibrary.ForgeLibraryData
        {
            type = sheetName.Replace("ForgeLibrary_", string.Empty),
            sourceSheet = sheetName,
            ingredients = ingredients.ToArray(),
            formulas = formulas.ToArray()
        };
    }

    private static List<Dictionary<string, string>> AsRows(List<List<string>> rows, string[] requiredHeaders)
    {
        if (rows.Count == 0)
            throw new InvalidDataException("A required sheet is empty.");

        List<string> headers = RowFrom(rows, 0).Select(Clean).ToList();
        List<string> missingHeaders = requiredHeaders.Where(header => !headers.Contains(header)).ToList();
        if (missingHeaders.Count > 0)
            throw new InvalidDataException("Missing required column: " + string.Join(", ", missingHeaders));

        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> source = RowFrom(rows, rowIndex);
            if (source.All(string.IsNullOrWhiteSpace))
                continue;

            Dictionary<string, string> row = new Dictionary<string, string>();
            for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                row[headers[columnIndex]] = Cell(source, columnIndex, false);
            result.Add(row);
        }
        return result;
    }

    private static string Value(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out string value) ? value : string.Empty;
    }

    private static List<string> SplitIds(string value)
    {
        return value.Split(',').Select(Clean).Where(item => !string.IsNullOrEmpty(item)).ToList();
    }

    private static int IntValue(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return result;
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number)
            ? Mathf.RoundToInt(number)
            : 0;
    }

    private static bool BoolValue(string value)
    {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    private static List<string> RowFrom(List<List<string>> rows, int index)
    {
        return index >= 0 && index < rows.Count ? rows[index] : new List<string>();
    }

    private static string Cell(List<string> row, int index, bool trim = true)
    {
        string value = index >= 0 && index < row.Count ? row[index] ?? string.Empty : string.Empty;
        return trim ? Clean(value) : value;
    }

    private static string Clean(string value)
    {
        return value == null ? string.Empty : value.Trim();
    }

    private static string ToProjectPath(string absolutePath)
    {
        string normalizedPath = absolutePath.Replace('\\', '/');
        string normalizedAssetsPath = Application.dataPath.Replace('\\', '/');
        return normalizedPath.StartsWith(normalizedAssetsPath + "/", StringComparison.OrdinalIgnoreCase)
            ? "Assets" + normalizedPath.Substring(normalizedAssetsPath.Length)
            : null;
    }

    private void SetStatus(string message, MessageType type)
    {
        status = message;
        statusType = type;
    }

    private static class XlsxReader
    {
        private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace DocumentRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        public static Dictionary<string, List<List<string>>> Read(string workbookPath)
        {
            using (FileStream stream = new FileStream(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                List<string> sharedStrings = ReadSharedStrings(archive);
                XDocument workbook = ReadXml(archive, "xl/workbook.xml");
                XDocument relationships = ReadXml(archive, "xl/_rels/workbook.xml.rels");
                Dictionary<string, string> targets = relationships.Descendants()
                    .Where(element => element.Name.LocalName == "Relationship")
                    .ToDictionary(
                        element => (string)element.Attribute("Id"),
                        element => (string)element.Attribute("Target"));

                Dictionary<string, List<List<string>>> result = new Dictionary<string, List<List<string>>>();
                foreach (XElement sheet in workbook.Descendants(Spreadsheet + "sheet"))
                {
                    string name = (string)sheet.Attribute("name");
                    string relationId = (string)sheet.Attribute(DocumentRelationships + "id");
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(relationId) || !targets.TryGetValue(relationId, out string target))
                        continue;

                    result[name] = ReadSheet(ReadXml(archive, NormalizePath(target)), sharedStrings);
                }
                return result;
            }
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return new List<string>();

            using (Stream stream = entry.Open())
            {
                XDocument document = XDocument.Load(stream);
                return document.Descendants(Spreadsheet + "si")
                    .Select(item => string.Concat(item.Descendants(Spreadsheet + "t").Select(text => text.Value)))
                    .ToList();
            }
        }

        private static List<List<string>> ReadSheet(XDocument document, List<string> sharedStrings)
        {
            List<List<string>> rows = new List<List<string>>();
            foreach (XElement rowNode in document.Descendants(Spreadsheet + "row"))
            {
                int rowIndex = int.TryParse((string)rowNode.Attribute("r"), out int parsedIndex)
                    ? parsedIndex - 1
                    : rows.Count;
                while (rows.Count <= rowIndex)
                    rows.Add(new List<string>());

                List<string> row = rows[rowIndex];
                foreach (XElement cell in rowNode.Elements(Spreadsheet + "c"))
                {
                    int columnIndex = ColumnIndex((string)cell.Attribute("r"));
                    while (row.Count <= columnIndex)
                        row.Add(string.Empty);
                    row[columnIndex] = ReadCell(cell, sharedStrings);
                }
            }
            return rows;
        }

        private static string ReadCell(XElement cell, List<string> sharedStrings)
        {
            string type = (string)cell.Attribute("t");
            if (type == "inlineStr")
                return string.Concat(cell.Descendants(Spreadsheet + "t").Select(text => text.Value));

            string value = (string)cell.Element(Spreadsheet + "v") ?? string.Empty;
            if (type == "s" && int.TryParse(value, out int index) && index >= 0 && index < sharedStrings.Count)
                return sharedStrings[index];
            if (type == "b")
                return value == "1" ? "true" : "false";
            return value;
        }

        private static int ColumnIndex(string reference)
        {
            int result = 0;
            foreach (char character in reference ?? "A1")
            {
                if (!char.IsLetter(character))
                    break;
                result = result * 26 + char.ToUpperInvariant(character) - 'A' + 1;
            }
            return Mathf.Max(0, result - 1);
        }

        private static string NormalizePath(string target)
        {
            string path = target.Replace('\\', '/');
            if (path.StartsWith("/", StringComparison.Ordinal))
                return path.TrimStart('/');
            while (path.StartsWith("../", StringComparison.Ordinal))
                path = path.Substring(3);
            return "xl/" + path.TrimStart('/');
        }

        private static XDocument ReadXml(ZipArchive archive, string path)
        {
            ZipArchiveEntry entry = archive.GetEntry(path);
            if (entry == null)
                throw new InvalidDataException("Workbook is missing '" + path + "'.");
            using (Stream stream = entry.Open())
                return XDocument.Load(stream);
        }
    }
}
