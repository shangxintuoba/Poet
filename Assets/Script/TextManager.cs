using System.Collections.Generic;
using Ink.Runtime;
using Ink.UnityIntegration;
using UnityEngine;

public class TextManager : MonoBehaviour
{
    [SerializeField] private InkFile inkFile;
    [SerializeField] private TextPanelUI textPanel;

    private Story story;

    public bool IsTyping => textPanel != null && textPanel.IsTyping;

    private void Start()
    {
        LoadStory(inkFile, null, string.Empty);
    }

    public string SaveStoryState()
    {
        return story != null ? story.state.ToJson() : null;
    }

    public string SaveDisplayedText()
    {
        return textPanel != null ? textPanel.DisplayedText : string.Empty;
    }

    public void LoadStory(InkFile newInkFile, string savedState, string savedText)
    {
        if (newInkFile == null || !newInkFile.isCompiled || textPanel == null)
            return;

        inkFile = newInkFile;
        story = new Story(inkFile.storyJson);
        BindExternalFunctions();
        if (!string.IsNullOrEmpty(savedState))
            story.state.LoadJson(savedState);

        textPanel.SetDisplayedText(savedText);
        ContinueStory();
    }

    private void BindExternalFunctions()
    {
        story.BindExternalFunction("GetTime", () =>
        {
            return GameManager.Instance != null && GameManager.Instance.TimeCard != null
                ? GameManager.Instance.TimeCard.CurrentTime
                : 0;
        });

        story.BindExternalFunction("GetBradPitProgress", () =>
        {
            return GameManager.Instance != null
                ? GameManager.Instance.BradPit_progress
                : 0;
        });

        story.BindExternalFunction<string>("CreateCard", cardId =>
        {
            FindFirstObjectByType<CardManager>()?
                .CreateCards(new List<string> { cardId });
        });

        story.BindExternalFunction<int>("ChangeMoney", amount =>
        {
            GameManager.Instance?.Money?.ChangeValue(amount);
        });

        story.BindExternalFunction<int>("ChangeWillPower", amount =>
        {
            GameManager.Instance?.WillPower?.ChangeValue(amount);
        });
    }

    private void ContinueStory()
    {
        while (story != null && story.canContinue)
        {
            string text = story.Continue().Trim();
            if (!string.IsNullOrWhiteSpace(text))
                textPanel.ShowDialogueUI(text);
        }

        if (story != null && story.currentChoices.Count > 0)
            textPanel.ShowChoices(story.currentChoices, SelectChoice);
    }

    private void SelectChoice(Choice choice)
    {
        if (story == null || choice == null || !story.currentChoices.Contains(choice))
            return;

        textPanel.ClearChoices();
        story.ChooseChoiceIndex(choice.index);
        ContinueStory();
    }
}
