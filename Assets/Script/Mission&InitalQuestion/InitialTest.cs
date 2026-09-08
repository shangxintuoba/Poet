using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InitialTest : MonoBehaviour
{
    public TextMeshProUGUI TextArea;
    public TestChoice testChoicePrefab;
    [Min(0f)] public float CharacterDelay = 0.03f;
    private int currentQuestionIndex;
    private int[] selectedAnswers = new int[7];
    private Transform choicesContainer;
    private readonly List<TestChoice> spawnedChoices = new List<TestChoice>();

    public List<string> FinalMissionRequiredCardIDs { get; private set; } = new List<string>();

    [Serializable]
    public class TemperatureResultRow
    {
        [Tooltip("寒带结果。填写 Card ID；多个 ID 使用逗号分隔。")]
        public string Cold;

        [Tooltip("温带结果。填写 Card ID；多个 ID 使用逗号分隔。")]
        public string Temperate;

        [Tooltip("热带结果。填写 Card ID；多个 ID 使用逗号分隔。")]
        public string Tropical;
    }

    [Serializable]
    public class EnvironmentResultMatrix
    {
        [Tooltip("湿度：泡在水里")]
        public TemperatureResultRow Submerged = new TemperatureResultRow();

        [Tooltip("湿度：小雨")]
        public TemperatureResultRow LightRain = new TemperatureResultRow();

        [Tooltip("湿度：干旱")]
        public TemperatureResultRow Drought = new TemperatureResultRow();
    }

    [Serializable]
    public class RealityResultRow
    {
        [Tooltip("现实位于你的内部。填写 Card ID；多个 ID 使用逗号分隔。")]
        public string Inside;

        [Tooltip("现实位于你的周围。填写 Card ID；多个 ID 使用逗号分隔。")]
        public string Around;

        [Tooltip("现实位于遥远的地方。填写 Card ID；多个 ID 使用逗号分隔。")]
        public string Distant;
    }

    [Serializable]
    public class WorldResultMatrix
    {
        public RealityResultRow BC7000 = new RealityResultRow();
        public RealityResultRow AD0 = new RealityResultRow();
        [FormerlySerializedAs("AD1971")]
        public RealityResultRow AD1871 = new RealityResultRow();
        public RealityResultRow AD3027 = new RealityResultRow();
    }

    [Header("湿度 × 温度：Final Mission Card IDs")]
    public EnvironmentResultMatrix EnvironmentMatrix = new EnvironmentResultMatrix();

    [Header("时代 × 现实位置：Final Mission Card IDs")]
    public WorldResultMatrix WorldMatrix = new WorldResultMatrix();

    [Serializable]
    public class LanguageMaterialResults
    {
        public string Fire;
        public string Glass;
        public string Soil;
        public string Stone;
    }

    [Serializable]
    public class ThemeResults
    {
        public string ethics;
        public string Love;
        public string Truth;
    }

    [Header("Final Mission 所需 Card IDs")]
    public LanguageMaterialResults LanguageCards = new LanguageMaterialResults();
    public ThemeResults ThemeCards = new ThemeResults();


    [Serializable]
    public class Questions
    {
        public string id;
        public string questionText;
        public string[] Choices;
    }

    public Questions[] QuestionList =
    {
        new Questions
        {
            id = "location",
            questionText = "The story takes place in:",
            Choices = new[]
            {
                "A tropical zone",
                "A temperate zone",
                "A frigid zone"
            }
        },
        new Questions
        {
            id = "humidity",
            questionText = "The world's humidity:",
            Choices = new[]
            {
                "Submerged in water",
                "Light rain",
                "drought"
            }
        },
        new Questions
        {
            id = "time",
            questionText = "The story happens in:",
            Choices = new[]
            {
                "7000 BC",
                "0 AD",
                "1871",
                "3027"
            }
        },
        new Questions
        {
            id = "languageMaterial",
            questionText = "The material of your language:",
            Choices = new[]
            {
                "Fire",
                "Glass",
                "Soil",
                "Stone"
            }
        },
        new Questions
        {
            id = "reality",
            questionText = "Reality is located:",
            Choices = new[]
            {
                "Within you",
                "Around you",
                "Far away from you"
            }
        },
        new Questions
        {
            id = "willpower",
            questionText = "Your willpower comes from:",
            Choices = new[]
            {
                "Your mind",
                "Your muscles",
                "Your stomach"
            }
        },
        new Questions
        {
            id = "theme",
            questionText = "Your most important theme:",
            Choices = new[]
            {
                "Sin",
                "Love",
                "Truth"
            }
        }
    };

    private void Awake()
    {
        choicesContainer = transform.Find("ChoicesCointainer");
    }

    private void Start()
    {
        StartTest();
    }

    public void StartTest()
    {
        StopAllCoroutines();
        currentQuestionIndex = 0;
        selectedAnswers = new int[QuestionList.Length];
        SpawnQuestion();
    }

    public void SpawnQuestion()
    {
        ClearQuestionChoices();

        Questions question = QuestionList[currentQuestionIndex];
        TextArea.text = question.questionText;
        TextArea.maxVisibleCharacters = 0;
        StartCoroutine(TypeQuestion(question));
    }

    private IEnumerator TypeQuestion(Questions question)
    {
        for (int i = 1; i <= question.questionText.Length; i++)
        {
            TextArea.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(CharacterDelay);
        }

        TextArea.maxVisibleCharacters = int.MaxValue;
        SpawnChoices(question);
    }

    private void SpawnChoices(Questions question)
    {
        for (int i = 0; i < question.Choices.Length; i++)
        {
            int answerIndex = i;
            TestChoice choice = Instantiate(testChoicePrefab, choicesContainer);
            choice.Index = answerIndex;
            choice.ChoiceText.text = question.Choices[answerIndex];
            choice.GetComponent<Button>().onClick.AddListener(() => SelectAnswer(answerIndex));
            spawnedChoices.Add(choice);
        }
    }

    public void SelectAnswer(int answerIndex)
    {
        selectedAnswers[currentQuestionIndex] = answerIndex;
        currentQuestionIndex++;

        if (currentQuestionIndex < QuestionList.Length)
        {
            SpawnQuestion();
            return;
        }

        ClearQuestionChoices();
        CalculateResult();
        gameObject.SetActive(false);
    }

    public void CalculateResult()
    {
        FinalMissionRequiredCardIDs.Clear();

        TemperatureResultRow environmentRow = selectedAnswers[1] switch
        {
            0 => EnvironmentMatrix.Submerged,
            1 => EnvironmentMatrix.LightRain,
            _ => EnvironmentMatrix.Drought
        };

        string environmentCards = selectedAnswers[0] switch
        {
            0 => environmentRow.Tropical,
            1 => environmentRow.Temperate,
            _ => environmentRow.Cold
        };
        AddRequiredCardIDs(environmentCards);

        RealityResultRow worldRow = selectedAnswers[2] switch
        {
            0 => WorldMatrix.BC7000,
            1 => WorldMatrix.AD0,
            2 => WorldMatrix.AD1871,
            _ => WorldMatrix.AD3027
        };

        string worldCards = selectedAnswers[4] switch
        {
            0 => worldRow.Inside,
            1 => worldRow.Around,
            _ => worldRow.Distant
        };
        AddRequiredCardIDs(worldCards);

        string languageCard = selectedAnswers[3] switch
        {
            0 => LanguageCards.Fire,
            1 => LanguageCards.Glass,
            2 => LanguageCards.Soil,
            _ => LanguageCards.Stone
        };
        AddRequiredCardIDs(languageCard);

        string themeCard = selectedAnswers[6] switch
        {
            0 => ThemeCards.ethics,
            1 => ThemeCards.Love,
            _ => ThemeCards.Truth
        };
        AddRequiredCardIDs(themeCard);
    }

    private void AddRequiredCardIDs(string cardIDs)
    {
        if (string.IsNullOrWhiteSpace(cardIDs))
            return;

        string[] ids = cardIDs.Split(',');
        for (int i = 0; i < ids.Length; i++)
            FinalMissionRequiredCardIDs.Add(ids[i].Trim());
    }

    private void ClearQuestionChoices()
    {
        for (int i = 0; i < spawnedChoices.Count; i++)
            Destroy(spawnedChoices[i].gameObject);

        spawnedChoices.Clear();
    }

}
