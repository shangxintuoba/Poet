using System;
using System.Collections.Generic;
using UnityEngine;

public class CardLibrary : MonoBehaviour
{
    [Serializable]
    public class CardLibraryData
    {
        public int schemaVersion;
        public string[] sourceSheets;
        public CardData[] cards;
        public NodeData[] nodes;
        public NodeChoiceData[] nodeChoices;
        public DailyMissionData[] dailyMissions;
        public ForgeLibraryData[] forgeLibraries;
    }

    [Serializable]
    public class CardData
    {
        public string id;
        public string name;
        public string type;
        [TextArea] public string description;
        public int willPowerDelta;
        public string materialType;
        public string fuelType;
        public bool useable;
        public bool canBeDropped;
        public bool refreshChoice;
        public int maximumUse;
        public RawChoiceData[] choices;
        public CharacterProgressData[] characterProgress;
    }

    [Serializable]
    public class RawChoiceData
    {
        public string id;
        public string choiceText;
        [TextArea] public string usedText;
        public string[] cardsAdded;
        public string[] cardsDestroyed;
        public string[] randomCardList;
        public int randomCardNumber;
        public int timeConsumed;
        public string[] unlockNodes;
        public bool hideOtherChoices;
        public bool destroyWhenUsed;
    }

    [Serializable]
    public class CharacterProgressData
    {
        public int progress;
        [TextArea] public string text;
        public string node;
        public CharacterChoiceData[] choices;
    }

    [Serializable]
    public class CharacterChoiceData
    {
        public string id;
        public string text;
        public bool reusable;
        public int targetProgress;
        public string[] cardsAdded;
        public string[] cardsRemoved;
        public int deltaWillPower;
        public int deltaMoney;
    }

    [Serializable]
    public class NodeData
    {
        public string id;
        public string name;
        [TextArea] public string textMorning;
        [TextArea] public string textAfternoon;
        [TextArea] public string textSunset;
        [TextArea] public string textNight;
        [TextArea] public string textMidnight;
        public string[] choicesMorning;
        public string[] choicesAfternoon;
        public string[] choicesSunset;
        public string[] choicesNight;
        public string[] choicesMidnight;
    }

    [Serializable]
    public class NodeChoiceData
    {
        public string id;
        public string text;
        public string[] cardsAdded;
        public string[] cardsRemoved;
        public int deltaWillPower;
        public int deltaMoney;
        public bool oncePerDay;
        public bool useOnlyOnce;
        public string goToFarNode;
    }

    [Serializable]
    public class DailyMissionData
    {
        public string id;
        public string name;
        [TextArea] public string description;
        public int moneyReward;
        public string[] requiredCards;
    }

    [System.Serializable]
    public class ForgeLibraryData
    {
        public string type;
        public string sourceSheet;
        public ForgeIngredientData[] ingredients;
        public ForgeFormulaData[] formulas;
    }

    [System.Serializable]
    public class ForgeIngredientData
    {
        public string id;
        public string name;
    }

    [System.Serializable]
    public class ForgeFormulaData
    {
        public string firstIngredientId;
        public string secondIngredientId;
        public string resultCardId;
    }

    [SerializeField] private TextAsset cardLibraryJson;

    public CardLibraryData Data { get; private set; }
    public IReadOnlyList<CardData> Cards => Data != null && Data.cards != null
        ? Data.cards
        : Array.Empty<CardData>();

    public IReadOnlyList<ForgeLibraryData> ForgeLibraries => Data != null && Data.forgeLibraries != null
        ? Data.forgeLibraries
        : Array.Empty<ForgeLibraryData>();

    public IReadOnlyList<DailyMissionData> DailyMissions => Data.dailyMissions;
    public IReadOnlyList<NodeData> Nodes => Data != null && Data.nodes != null
        ? Data.nodes
        : Array.Empty<NodeData>();
    public IReadOnlyList<NodeChoiceData> NodeChoices => Data != null && Data.nodeChoices != null
        ? Data.nodeChoices
        : Array.Empty<NodeChoiceData>();

    private readonly Dictionary<string, CardData> cardsByReference = new Dictionary<string, CardData>();
    private readonly Dictionary<string, NodeData> nodesById = new Dictionary<string, NodeData>();
    private readonly Dictionary<string, NodeChoiceData> nodeChoicesById = new Dictionary<string, NodeChoiceData>();

    private void Awake()
    {
        LoadJson();
    }

    public bool LoadJson()
    {
        Data = null;
        cardsByReference.Clear();
        nodesById.Clear();
        nodeChoicesById.Clear();

        if (cardLibraryJson == null)
        {
            Debug.LogWarning("CardLibrary has no JSON asset assigned.", this);
            return false;
        }

        Data = JsonUtility.FromJson<CardLibraryData>(cardLibraryJson.text);
        if (Data == null || Data.cards == null)
        {
            Debug.LogError("CardLibrary JSON could not be read or contains no cards.", this);
            return false;
        }

        foreach (CardData card in Data.cards)
        {
            if (card == null)
                continue;

            AddReference(card.id, card);
            AddReference(card.name, card);
        }

        foreach (NodeData node in Data.nodes ?? Array.Empty<NodeData>())
        {
            if (node != null && !string.IsNullOrWhiteSpace(node.id))
                nodesById[node.id] = node;
        }

        foreach (NodeChoiceData choice in Data.nodeChoices ?? Array.Empty<NodeChoiceData>())
        {
            if (choice != null && !string.IsNullOrWhiteSpace(choice.id))
                nodeChoicesById[choice.id] = choice;
        }

        return true;
    }

    // Accepts a FullLibrary Index or a card Name.
    public CardData FindCardData(string cardReference)
    {
        if (string.IsNullOrWhiteSpace(cardReference))
            return null;

        cardsByReference.TryGetValue(cardReference, out CardData card);
        return card;
    }

    public CardData FindCardDataById(string cardId)
    {
        return FindCardData(cardId);
    }

    public CardData FindCardDataByName(string cardName)
    {
        return FindCardData(cardName);
    }

    public NodeData FindNodeData(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return null;

        nodesById.TryGetValue(nodeId, out NodeData node);
        return node;
    }

    public NodeChoiceData FindNodeChoiceData(string choiceId)
    {
        if (string.IsNullOrWhiteSpace(choiceId))
            return null;

        nodeChoicesById.TryGetValue(choiceId, out NodeChoiceData choice);
        return choice;
    }

    private void AddReference(string reference, CardData card)
    {
        if (!string.IsNullOrWhiteSpace(reference))
            cardsByReference[reference] = card;
    }
}
