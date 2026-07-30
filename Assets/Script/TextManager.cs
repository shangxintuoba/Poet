using Ink.Runtime;
using Ink.UnityIntegration;
using UnityEngine;

public class TextManager : MonoBehaviour
{
    [SerializeField] private InkFile inkFile;
    [SerializeField] private TextPanelUI textPanel;

    private Story story;

    private void Start()
    {
        if (inkFile == null || !inkFile.isCompiled || textPanel == null)
        {
            Debug.LogError("TextManager requires a compiled InkFile and TextPanelUI reference.");
            return;
        }

        story = new Story(inkFile.storyJson);
        ContinueStory();
    }

    private void ContinueStory()
    {
        while (story.canContinue)
        {
            string text = story.Continue().Trim();
            if (!string.IsNullOrWhiteSpace(text))
                textPanel.ShowDialogueUI(text);
        }

        if (story.currentChoices.Count > 0)
            textPanel.ShowChoices(story.currentChoices, SelectChoice);
    }

    private void SelectChoice(Choice choice)
    {
        textPanel.ClearChoices();
        textPanel.ShowDialogueUI(choice.text);
        story.ChooseChoiceIndex(choice.index);
        ContinueStory();
    }
}
