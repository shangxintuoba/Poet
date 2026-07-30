using System;
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

    private readonly List<GameObject> choiceObjects = new List<GameObject>();
    private TextMeshProUGUI currentTextBlock;

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
            currentTextBlock.text = text;
        }
        else
        {
            currentTextBlock.text += "\n\n" + text;
        }

        ScrollToBottom();
    }

    public void ShowChoices(List<Choice> choices, Action<Choice> onChoiceSelected)
    {
        ClearChoices();

        if (content == null || choiceButtonPrefab == null)
            return;

        foreach (Choice choice in choices)
        {
            Choice selectedChoice = choice;
            Button button = Instantiate(choiceButtonPrefab, content);
            button.gameObject.SetActive(true);
            button.name = "Choice";

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = selectedChoice.text;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onChoiceSelected?.Invoke(selectedChoice));
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

    private void ScrollToBottom()
    {
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
