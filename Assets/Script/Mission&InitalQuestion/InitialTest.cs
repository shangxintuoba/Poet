using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InitialTest : MonoBehaviour
{
    [Serializable]
    public class QuestionOption
    {
        [TextArea] public string optionText;
        [Tooltip("选择此选项后，直接添加到玩家卡组的 Card ID。")]
        public string[] cardsToAdd = Array.Empty<string>();
        [Tooltip("选择此选项后，加入 Final Mission 需求的 Card ID；不会自动创建给玩家。")]
        public string[] finalMissionRequiredCards = Array.Empty<string>();
    }

    [Serializable]
    public class Question
    {
        public string id;
        [TextArea] public string questionText;
        public QuestionOption[] options = Array.Empty<QuestionOption>();
    }

    public TextMeshProUGUI TextArea;
    public TestChoice testChoicePrefab;
    [SerializeField] private CardManager cardManager;
    [Min(0f)] public float CharacterDelay = 0.03f;

    [Header("Initial Test Questions")]
    [Tooltip("每题的文本、选项、选项奖励卡牌，以及 Final Mission 需求卡牌都在此配置。")]
    public Question[] QuestionList = Array.Empty<Question>();

    private int currentQuestionIndex;
    private int[] selectedAnswers = Array.Empty<int>();
    private Transform choicesContainer;
    private readonly List<TestChoice> spawnedChoices = new List<TestChoice>();

    public List<string> ResultCardIDs { get; } = new List<string>();
    public List<string> FinalMissionRequiredCardIDs { get; } = new List<string>();

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
        ClearQuestionChoices();
        currentQuestionIndex = 0;
        selectedAnswers = new int[QuestionList?.Length ?? 0];

        if (QuestionList == null || QuestionList.Length == 0)
        {
            Debug.LogWarning("InitialTest has no questions configured.");
            return;
        }

        SpawnQuestion();
    }

    public void SpawnQuestion()
    {
        if (QuestionList == null || currentQuestionIndex < 0 || currentQuestionIndex >= QuestionList.Length)
            return;

        ClearQuestionChoices();
        Question question = QuestionList[currentQuestionIndex];
        if (question == null)
        {
            Debug.LogWarning($"InitialTest question {currentQuestionIndex} is missing.");
            return;
        }

        TextArea.text = question.questionText;
        TextArea.maxVisibleCharacters = 0;
        StartCoroutine(TypeQuestion(question));
    }

    private IEnumerator TypeQuestion(Question question)
    {
        string questionText = question.questionText ?? string.Empty;
        for (int i = 1; i <= questionText.Length; i++)
        {
            TextArea.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(CharacterDelay);
        }

        TextArea.maxVisibleCharacters = int.MaxValue;
        SpawnChoices(question);
    }

    private void SpawnChoices(Question question)
    {
        if (choicesContainer == null)
        {
            Debug.LogWarning("InitialTest could not find ChoicesCointainer.");
            return;
        }

        QuestionOption[] options = question.options ?? Array.Empty<QuestionOption>();
        for (int i = 0; i < options.Length; i++)
        {
            int answerIndex = i;
            QuestionOption option = options[answerIndex];
            TestChoice choice = Instantiate(testChoicePrefab, choicesContainer);
            choice.Index = answerIndex;
            choice.ChoiceText.text = option != null ? option.optionText : string.Empty;
            choice.GetComponent<Button>().onClick.AddListener(() => SelectAnswer(answerIndex));
            spawnedChoices.Add(choice);
        }
    }

    public void SelectAnswer(int answerIndex)
    {
        if (QuestionList == null || currentQuestionIndex >= QuestionList.Length)
            return;

        QuestionOption[] options = QuestionList[currentQuestionIndex]?.options;
        if (options == null || answerIndex < 0 || answerIndex >= options.Length)
            return;

        selectedAnswers[currentQuestionIndex] = answerIndex;
        currentQuestionIndex++;
        if (currentQuestionIndex < QuestionList.Length)
        {
            SpawnQuestion();
            return;
        }

        ClearQuestionChoices();
        CalculateResult();
        AddResultCards();
        UpdateFinalMissionRequirements();
        CompleteTest();
    }

    public void CalculateResult()
    {
        ResultCardIDs.Clear();
        FinalMissionRequiredCardIDs.Clear();
        if (QuestionList == null)
            return;

        for (int questionIndex = 0; questionIndex < QuestionList.Length; questionIndex++)
        {
            QuestionOption[] options = QuestionList[questionIndex]?.options;
            int answerIndex = questionIndex < selectedAnswers.Length ? selectedAnswers[questionIndex] : -1;
            if (options == null || answerIndex < 0 || answerIndex >= options.Length)
                continue;

            QuestionOption selectedOption = options[answerIndex];
            if (selectedOption == null)
                continue;

            AddCardIDs(ResultCardIDs, selectedOption.cardsToAdd);
            AddCardIDs(FinalMissionRequiredCardIDs, selectedOption.finalMissionRequiredCards);
        }
    }

    /// <summary>Queues the cards awarded by the selected initial-test options.</summary>
    public void AddResultCards()
    {
        if (ResultCardIDs.Count == 0)
            return;

        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();

        if (cardManager == null)
        {
            Debug.LogWarning("InitialTest could not find a CardManager to queue result cards.");
            return;
        }

        cardManager.AddInitialCards(ResultCardIDs);
    }

    private void UpdateFinalMissionRequirements()
    {
        MissionManager missionManager = FindFirstObjectByType<MissionManager>(FindObjectsInactive.Include);
        if (missionManager == null)
        {
            Debug.LogWarning("InitialTest could not find a MissionManager to update Final Mission requirements.");
            return;
        }

        missionManager.SetFinalMissionRequiredCards(FinalMissionRequiredCardIDs);
    }

    private static void AddCardIDs(List<string> destination, string[] cardIDs)
    {
        if (cardIDs == null)
            return;

        for (int i = 0; i < cardIDs.Length; i++)
        {
            string cardID = cardIDs[i]?.Trim();
            if (!string.IsNullOrWhiteSpace(cardID))
                destination.Add(cardID);
        }
    }

    private void CompleteTest()
    {
        SettingPanel settingPanel = FindFirstObjectByType<SettingPanel>();
        if (settingPanel != null)
        {
            settingPanel.CompleteInitialTest();
            return;
        }

        gameObject.SetActive(false);
    }

    private void ClearQuestionChoices()
    {
        for (int i = 0; i < spawnedChoices.Count; i++)
        {
            if (spawnedChoices[i] != null)
                Destroy(spawnedChoices[i].gameObject);
        }

        spawnedChoices.Clear();
    }
}
