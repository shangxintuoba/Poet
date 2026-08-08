using System;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextPanelUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private TextMeshProUGUI textBlockPrefab;
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField, Min(1f)] private float charactersPerSecond = 30f;
    [SerializeField, Min(0f)] private float choiceDelay = 0.4f;

    private readonly List<GameObject> choiceObjects = new List<GameObject>();
    private readonly Queue<string> textQueue = new Queue<string>();
    private TextMeshProUGUI currentTextBlock;
    private List<Choice> pendingChoices;
    private Action<Choice> pendingChoiceSelected;
    private bool isTyping;
    private Coroutine choiceDelayCoroutine;



    private void Awake()
    {
        if (textBlockPrefab != null)
            textBlockPrefab.gameObject.SetActive(false);

        if (choiceButtonPrefab != null)
            choiceButtonPrefab.gameObject.SetActive(false);
    }

    public void ShowDialogueUI(string text)
    {
        if (content == null || textBlockPrefab == null || string.IsNullOrWhiteSpace(text))
            return;

        if (currentTextBlock == null)
        {
            currentTextBlock = Instantiate(textBlockPrefab, content);
            currentTextBlock.gameObject.SetActive(true);
            currentTextBlock.name = "TextBlock";
            currentTextBlock.text = string.Empty;
        }

        textQueue.Enqueue(text);

        if (!isTyping)
        {
            isTyping = true;
            StartCoroutine(TypeQueuedText());
        }
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
        StartChoiceDelay();
    }

    public void ShowChoices(List<Choice> choices, Action<Choice> onChoiceSelected)
    {
        ClearChoices();
        pendingChoices = new List<Choice>(choices);
        pendingChoiceSelected = onChoiceSelected;

        if (!isTyping)
            StartChoiceDelay();
    }

    private void StartChoiceDelay()
    {
        if (choiceDelayCoroutine != null)
            StopCoroutine(choiceDelayCoroutine);

        choiceDelayCoroutine = StartCoroutine(ShowChoicesAfterDelay());
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

        Action<Choice> choiceSelected = pendingChoiceSelected;

        foreach (Choice choice in pendingChoices)
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

        pendingChoices = null;
        pendingChoiceSelected = null;
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

    private void ScrollToBottom()
    {
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
