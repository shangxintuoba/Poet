using System;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TextPanelUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private TextMeshProUGUI textBlockPrefab;
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField, Min(1f)] private float charactersPerSecond = 30f;
    [SerializeField, Min(0f)] private float choiceDelay = 0.4f;
    [SerializeField] private RectTransform paper;
    [SerializeField, Min(0f)] private float paperMoveUpDistance = 120f;
    [SerializeField, Min(0f)] private float paperMoveDuration = 0.35f;

    private readonly List<GameObject> choiceObjects = new List<GameObject>();
    private readonly Queue<string> textQueue = new Queue<string>();
    private TextMeshProUGUI currentTextBlock;
    private List<Choice> pendingChoices;
    private Action<Choice> pendingChoiceSelected;
    private List<Choice> visibleChoices;
    private Action<Choice> visibleChoiceSelected;
    private List<Choice> savedChoices;
    private Action<Choice> savedChoiceSelected;
    private bool isTyping;
    private Coroutine typingCoroutine;
    private Coroutine choiceDelayCoroutine;
    private bool isShowingCardDescription;
    private string savedDialogueText;
    private Coroutine cardChoiceCoroutine;
    private Vector2 paperInitialPosition;
    private Tween paperTween;

    public bool IsTyping => isTyping;
    public string DisplayedText => currentTextBlock != null ? currentTextBlock.text : string.Empty;

    public void SetDisplayedText(string text)
    {
        StopTyping();
        StopChoiceDelay();
        ClearChoices();
        EnsureTextBlock();
        currentTextBlock.text = text ?? string.Empty;
        SetPaperRaised(!string.IsNullOrWhiteSpace(currentTextBlock.text));
        ScrollToBottom();
    }

    private void Awake()
    {
        if (textBlockPrefab != null)
            textBlockPrefab.gameObject.SetActive(false);
        if (choiceButtonPrefab != null)
            choiceButtonPrefab.gameObject.SetActive(false);
        ResolvePaper();
    }

    public void ShowDialogueUI(string text)
    {
        if (content == null || textBlockPrefab == null || string.IsNullOrWhiteSpace(text))
            return;

        EnsureTextBlock();
        textQueue.Enqueue(text);
        SetPaperRaised(true);

        if (!isTyping)
        {
            isTyping = true;
            typingCoroutine = StartCoroutine(TypeQueuedText());
        }
    }

    public void ShowCardDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return;

        EnsureTextBlock();

        if (!isShowingCardDescription)
        {
            savedDialogueText = currentTextBlock.text;
            SaveAndHideChoices();
            isShowingCardDescription = true;
        }

        StopTyping();
        StopChoiceDelay();
        ClearChoices();
        currentTextBlock.text = string.Empty;
        ShowDialogueUI(description);
    }

    public void ShowCardChoices(List<string> choices, Action<int> onSelected)
    {
        if (!isShowingCardDescription || choices == null || choices.Count == 0)
            return;

        if (cardChoiceCoroutine != null)
            StopCoroutine(cardChoiceCoroutine);
        cardChoiceCoroutine = StartCoroutine(ShowCardChoicesAfterTyping(choices, onSelected));
    }

    private IEnumerator ShowCardChoicesAfterTyping(List<string> choices, Action<int> onSelected)
    {
        while (isTyping)
            yield return null;

        yield return new WaitForSeconds(choiceDelay);
        if (!isShowingCardDescription)
            yield break;

        foreach (string choiceText in choices)
        {
            int choiceIndex = choices.IndexOf(choiceText);
            Button button = Instantiate(choiceButtonPrefab, content);
            button.gameObject.SetActive(true);
            button.name = "CardChoice";
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = choiceText;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke(choiceIndex));
            choiceObjects.Add(button.gameObject);
        }
        cardChoiceCoroutine = null;
        ScrollToBottom();
    }

    public void RestoreDialogueAfterCardDescription()
    {
        if (!isShowingCardDescription)
            return;

        StopTyping();
        if (cardChoiceCoroutine != null) StopCoroutine(cardChoiceCoroutine);
        cardChoiceCoroutine = null;
        ClearChoices();
        currentTextBlock.text = savedDialogueText;
        savedDialogueText = string.Empty;
        isShowingCardDescription = false;
        SetPaperRaised(!string.IsNullOrWhiteSpace(currentTextBlock.text));

        RestoreSavedChoices();
        ScrollToBottom();
    }

    public void ShowChoices(List<Choice> choices, Action<Choice> onChoiceSelected)
    {
        ClearChoices();
        pendingChoices = new List<Choice>(choices);
        pendingChoiceSelected = onChoiceSelected;
        visibleChoices = null;
        visibleChoiceSelected = null;

        if (!isTyping && !isShowingCardDescription)
            StartChoiceDelay();
    }

    private void SaveAndHideChoices()
    {
        savedChoices = null;
        savedChoiceSelected = null;

        if (visibleChoices != null)
        {
            savedChoices = new List<Choice>(visibleChoices);
            savedChoiceSelected = visibleChoiceSelected;
        }
        else if (pendingChoices != null)
        {
            savedChoices = new List<Choice>(pendingChoices);
            savedChoiceSelected = pendingChoiceSelected;
        }

        ClearChoices();
        pendingChoices = null;
        pendingChoiceSelected = null;
        visibleChoices = null;
        visibleChoiceSelected = null;
    }

    private void RestoreSavedChoices()
    {
        if (savedChoices == null)
            return;

        pendingChoices = savedChoices;
        pendingChoiceSelected = savedChoiceSelected;
        savedChoices = null;
        savedChoiceSelected = null;
        ShowPendingChoices();
    }

    private void EnsureTextBlock()
    {
        if (currentTextBlock != null)
            return;

        currentTextBlock = Instantiate(textBlockPrefab, content);
        currentTextBlock.gameObject.SetActive(true);
        currentTextBlock.name = "TextBlock";
        currentTextBlock.text = string.Empty;
    }

    private IEnumerator TypeQueuedText()
    {
        while (textQueue.Count > 0)
        {
            string nextText = textQueue.Dequeue();
            string textToType = currentTextBlock.text.Length > 0 ? "\n\n" + nextText : nextText;

            foreach (char character in textToType)
            {
                currentTextBlock.text += character;
                ScrollToBottom();
                yield return new WaitForSeconds(1f / charactersPerSecond);
            }
        }

        isTyping = false;
        typingCoroutine = null;
        StartChoiceDelay();
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = null;
        isTyping = false;
        textQueue.Clear();
    }

    private void StartChoiceDelay()
    {
        if (pendingChoices == null || isShowingCardDescription)
            return;

        StopChoiceDelay();
        choiceDelayCoroutine = StartCoroutine(ShowChoicesAfterDelay());
    }

    private void StopChoiceDelay()
    {
        if (choiceDelayCoroutine != null)
            StopCoroutine(choiceDelayCoroutine);

        choiceDelayCoroutine = null;
    }

    private IEnumerator ShowChoicesAfterDelay()
    {
        yield return new WaitForSeconds(choiceDelay);
        choiceDelayCoroutine = null;
        ShowPendingChoices();
    }

    private void ShowPendingChoices()
    {
        if (content == null || choiceButtonPrefab == null || pendingChoices == null)
            return;

        List<Choice> choicesToShow = pendingChoices;
        Action<Choice> choiceSelected = pendingChoiceSelected;
        pendingChoices = null;
        pendingChoiceSelected = null;
        visibleChoices = new List<Choice>(choicesToShow);
        visibleChoiceSelected = choiceSelected;

        foreach (Choice choice in choicesToShow)
        {
            Choice selectedChoice = choice;
            Button button = Instantiate(choiceButtonPrefab, content);
            button.gameObject.SetActive(true);
            button.name = "Choice";

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = selectedChoice.text;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => choiceSelected?.Invoke(selectedChoice));
            choiceObjects.Add(button.gameObject);
        }

        ScrollToBottom();
    }

    public void ClearChoices()
    {
        foreach (GameObject choiceObject in choiceObjects)
        {
            if (choiceObject != null)
                Destroy(choiceObject);
        }

        choiceObjects.Clear();
    }


    private void ResolvePaper()
    {
        if (paper != null)
            return;

        GameObject paperObject = GameObject.Find("Canvas/Typer/Paper");
        if (paperObject == null)
            return;

        paper = paperObject.GetComponent<RectTransform>();
        if (paper != null)
            paperInitialPosition = paper.anchoredPosition;
    }

    private void SetPaperRaised(bool hasText)
    {
        ResolvePaper();
        if (paper == null)
            return;

        paperTween?.Kill();
        Vector2 targetPosition = hasText
            ? paperInitialPosition + Vector2.up * paperMoveUpDistance
            : paperInitialPosition;
        paperTween = paper.DOAnchorPos(targetPosition, paperMoveDuration).SetEase(Ease.OutQuad);
    }

    private void OnDestroy()
    {
        paperTween?.Kill();
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
