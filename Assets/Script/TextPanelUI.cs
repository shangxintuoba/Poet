using System;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;

public class TextPanelUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private TextMeshProUGUI textBlockPrefab;
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private GameObject choiceSlotPrefab;
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
    private List<string> pendingTextChoices;
    private Action<int> pendingTextChoiceSelected;
    private List<string> visibleTextChoices;
    private Action<int> visibleTextChoiceSelected;
    private List<string> savedTextChoices;
    private Action<int> savedTextChoiceSelected;
    private bool isTyping;
    private Coroutine typingCoroutine;
    private Coroutine choiceDelayCoroutine;
    private bool isShowingCardDescription;
    private string savedDialogueText;
    private Coroutine cardChoiceCoroutine;
    public CardSlot EventCardChoiceSlot { get; private set; }
    public EventCard ActiveEventCard { get; private set; }
    private Vector2 paperInitialPosition;
    private bool hasResolvedPaperInitialPosition;
    private bool isPaperRaised;
    private Tween paperTween;

    public bool IsTyping => isTyping;
    public string DisplayedText => currentTextBlock != null ? currentTextBlock.text : string.Empty;
    public bool HasDisplayedText => !string.IsNullOrWhiteSpace(DisplayedText);
    public bool IsExpanded => isPaperRaised;

    public void SetDisplayedText(string text)
    {
        StopTyping();
        StopChoiceDelay();
        if (cardChoiceCoroutine != null)
            StopCoroutine(cardChoiceCoroutine);

        cardChoiceCoroutine = null;
        ClearChoices();
        pendingChoices = null;
        pendingChoiceSelected = null;
        visibleChoices = null;
        visibleChoiceSelected = null;
        savedChoices = null;
        savedChoiceSelected = null;
        savedDialogueText = string.Empty;
        isShowingCardDescription = false;
        EnsureTextBlock();
        currentTextBlock.text = text ?? string.Empty;
        currentTextBlock.maxVisibleCharacters = int.MaxValue;
        SetPaperRaised(!string.IsNullOrWhiteSpace(currentTextBlock.text));
        ScrollToBottom();
    }

    private void Awake()
    {
        currentTextBlock = textBlockPrefab;
        if (currentTextBlock != null)
            currentTextBlock.gameObject.SetActive(true);

        if (choiceButtonPrefab != null)
            choiceButtonPrefab.gameObject.SetActive(false);

        ResolvePaper();
    }

    private void Update()
    {
        if (!isTyping || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || !IsPointerOverTyper())
            return;

        SkipTyping();
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

    public void ShowTextWithChoices(string text, List<string> choices, Action<int> onSelected)
    {
        StopTyping();
        StopChoiceDelay();
        if (cardChoiceCoroutine != null)
            StopCoroutine(cardChoiceCoroutine);

        cardChoiceCoroutine = null;
        ClearChoices();
        pendingChoices = null;
        pendingChoiceSelected = null;
        visibleChoices = null;
        visibleChoiceSelected = null;
        pendingTextChoices = choices != null ? new List<string>(choices) : null;
        pendingTextChoiceSelected = onSelected;
        visibleTextChoices = null;
        visibleTextChoiceSelected = null;
        isShowingCardDescription = false;
        EnsureTextBlock();
        currentTextBlock.text = string.Empty;
        currentTextBlock.maxVisibleCharacters = int.MaxValue;

        if (string.IsNullOrWhiteSpace(text))
        {
            SetPaperRaised(pendingTextChoices != null && pendingTextChoices.Count > 0);
            StartTextChoiceDelay();
            return;
        }

        ShowDialogueUI(text);
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

    public void ShowEventCardChoiceSlot(EventCard eventCard)
    {
        if (cardChoiceCoroutine != null)
            StopCoroutine(cardChoiceCoroutine);

        cardChoiceCoroutine = StartCoroutine(ShowEventCardChoiceSlotAfterTyping(eventCard));
    }

    private IEnumerator ShowEventCardChoiceSlotAfterTyping(EventCard eventCard)
    {
        while (isTyping)
            yield return null;

        GameObject slotObject = Instantiate(choiceSlotPrefab, content);
        slotObject.SetActive(true);
        slotObject.name = "EventCardChoiceSlot";
        EventCardChoiceSlot = slotObject.GetComponent<CardSlot>();
        ActiveEventCard = eventCard;
        eventCard.ChoiceSlot = slotObject;
        choiceObjects.Add(slotObject);
        cardChoiceCoroutine = null;
        ScrollToBottom();
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
        currentTextBlock.maxVisibleCharacters = int.MaxValue;
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
        savedTextChoices = null;
        savedTextChoiceSelected = null;

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
        else if (visibleTextChoices != null)
        {
            savedTextChoices = new List<string>(visibleTextChoices);
            savedTextChoiceSelected = visibleTextChoiceSelected;
        }
        else if (pendingTextChoices != null)
        {
            savedTextChoices = new List<string>(pendingTextChoices);
            savedTextChoiceSelected = pendingTextChoiceSelected;
        }

        ClearChoices();
        pendingChoices = null;
        pendingChoiceSelected = null;
        visibleChoices = null;
        visibleChoiceSelected = null;
        visibleTextChoices = null;
        visibleTextChoiceSelected = null;
    }

    private void RestoreSavedChoices()
    {
        if (savedChoices == null && savedTextChoices == null)
            return;

        if (savedChoices != null)
        {
            pendingChoices = savedChoices;
            pendingChoiceSelected = savedChoiceSelected;
            savedChoices = null;
            savedChoiceSelected = null;
            ShowPendingChoices();
        }
        else
        {
            pendingTextChoices = savedTextChoices;
            pendingTextChoiceSelected = savedTextChoiceSelected;
            savedTextChoices = null;
            savedTextChoiceSelected = null;
            ShowPendingTextChoices();
        }
    }

    private void EnsureTextBlock()
    {
        if (currentTextBlock == null)
            currentTextBlock = textBlockPrefab;

        if (currentTextBlock != null)
            currentTextBlock.gameObject.SetActive(true);
    }
    private IEnumerator TypeQueuedText()
    {
        while (textQueue.Count > 0)
        {
            string nextText = textQueue.Dequeue();
            string textToType = currentTextBlock.text.Length > 0 ? "\n\n" + nextText : nextText;

            currentTextBlock.maxVisibleCharacters = int.MaxValue;
            currentTextBlock.ForceMeshUpdate();
            int previousCharacterCount = currentTextBlock.textInfo.characterCount;

            currentTextBlock.text += textToType;
            currentTextBlock.ForceMeshUpdate();
            int totalCharacterCount = currentTextBlock.textInfo.characterCount;
            currentTextBlock.maxVisibleCharacters = previousCharacterCount;

            if (content is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            ScrollToBottom();

            for (int visibleCharacterCount = previousCharacterCount + 1;
                 visibleCharacterCount <= totalCharacterCount;
                 visibleCharacterCount++)
            {
                currentTextBlock.maxVisibleCharacters = visibleCharacterCount;
                yield return new WaitForSeconds(1f / charactersPerSecond);
            }
        }

        currentTextBlock.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
        typingCoroutine = null;
        StartChoiceDelay();
        StartTextChoiceDelay();
    }

    private void SkipTyping()
    {
        if (currentTextBlock == null)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        while (textQueue.Count > 0)
        {
            string nextText = textQueue.Dequeue();
            currentTextBlock.text += currentTextBlock.text.Length > 0 ? "\n\n" + nextText : nextText;
        }

        currentTextBlock.maxVisibleCharacters = int.MaxValue;
        currentTextBlock.ForceMeshUpdate();
        if (content is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        ScrollToBottom();

        typingCoroutine = null;
        isTyping = false;
        StartChoiceDelay();
        StartTextChoiceDelay();
    }

    private bool IsPointerOverTyper()
    {
        ResolvePaper();
        if (paper == null)
            return false;

        Canvas canvas = paper.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        return RectTransformUtility.RectangleContainsScreenPoint(paper, Mouse.current.position.ReadValue(), eventCamera);
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (currentTextBlock != null)
            currentTextBlock.maxVisibleCharacters = int.MaxValue;

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

    private void StartTextChoiceDelay()
    {
        if (pendingTextChoices == null || pendingTextChoices.Count == 0 || isShowingCardDescription)
            return;

        StopChoiceDelay();
        choiceDelayCoroutine = StartCoroutine(ShowTextChoicesAfterDelay());
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

    private IEnumerator ShowTextChoicesAfterDelay()
    {
        yield return new WaitForSeconds(choiceDelay);
        choiceDelayCoroutine = null;
        ShowPendingTextChoices();
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

    private void ShowPendingTextChoices()
    {
        if (content == null || choiceButtonPrefab == null || pendingTextChoices == null)
            return;

        List<string> choicesToShow = pendingTextChoices;
        Action<int> choiceSelected = pendingTextChoiceSelected;
        pendingTextChoices = null;
        pendingTextChoiceSelected = null;
        visibleTextChoices = new List<string>(choicesToShow);
        visibleTextChoiceSelected = choiceSelected;

        for (int index = 0; index < choicesToShow.Count; index++)
        {
            int selectedIndex = index;
            Button button = Instantiate(choiceButtonPrefab, content);
            button.gameObject.SetActive(true);
            button.name = "NodeChoice";

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = choicesToShow[selectedIndex];

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => choiceSelected?.Invoke(selectedIndex));
            choiceObjects.Add(button.gameObject);
        }

        ScrollToBottom();
    }

    public void ClearChoices()
    {
        EventCardChoiceSlot = null;
        ActiveEventCard = null;

        foreach (GameObject choiceObject in choiceObjects)
        {
            if (choiceObject != null)
                Destroy(choiceObject);
        }

        choiceObjects.Clear();
        pendingTextChoices = null;
        pendingTextChoiceSelected = null;
    }


    private void ResolvePaper()
    {
        if (paper == null)
        {
            GameObject paperObject = GameObject.Find("Canvas/Typer/Paper");
            if (paperObject != null)
                paper = paperObject.GetComponent<RectTransform>();
        }

        if (paper != null && !hasResolvedPaperInitialPosition)
        {
            paperInitialPosition = paper.anchoredPosition;
            hasResolvedPaperInitialPosition = true;
        }
    }

    private void SetPaperRaised(bool hasText)
    {
        ResolvePaper();
        if (paper == null)
            return;

        isPaperRaised = hasText;
        paperTween?.Kill();
        Vector2 targetPosition = hasText
            ? paperInitialPosition + Vector2.up * paperMoveUpDistance
            : paperInitialPosition;
        paperTween = paper.DOAnchorPos(targetPosition, paperMoveDuration).SetEase(Ease.OutQuad);
    }

    public float SetExpandedWithoutClearingText(bool expanded)
    {
        SetPaperRaised(expanded);
        return paperMoveDuration;
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
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
