using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    private Card card;
    private CardLibrary.CardData data;
    private GameManager.CharacterState state;
    private TextPanelUI textPanel;
    private CardManager cardManager;
    private readonly List<CardLibrary.CharacterChoiceData> visibleChoices = new();

    public int Progress => state.Progress;
    public string CurrentNodeIndex => state.CurrentNodeIndex;

    public void Initialize(Card owner, CardLibrary.CardData characterData)
    {
        card = owner;
        data = characterData;
        state = GameManager.Instance.GetCharacterState(data);
        textPanel = FindFirstObjectByType<TextPanelUI>();
        cardManager = FindFirstObjectByType<CardManager>();
    }

    public void ShowDetails()
    {
        CardLibrary.CharacterProgressData progressData = GetCurrentProgressData();
        string cardName = string.IsNullOrWhiteSpace(card.Name)
            ? (card.NameText != null ? card.NameText.text : card.gameObject.name)
            : card.Name;
        string description = progressData != null && !string.IsNullOrWhiteSpace(progressData.text)
            ? progressData.text
            : card.Description;
        string details = string.IsNullOrWhiteSpace(description)
            ? cardName
            : cardName + "\n\n" + description;

        textPanel.ShowCardDescription(details);
        ShowChoices(progressData);
    }

    private void ShowChoices(CardLibrary.CharacterProgressData progressData)
    {
        visibleChoices.Clear();

        if (progressData == null || progressData.choices == null || progressData.choices.Length == 0)
            return;

        List<string> labels = new List<string>();
        foreach (CardLibrary.CharacterChoiceData choice in progressData.choices)
        {
            if (choice.reusable && !GameManager.Instance.CanUseOnce(GetChoiceUsageKey(choice)))
                continue;

            visibleChoices.Add(choice);
            labels.Add(choice.text);
        }

        textPanel.ShowCardChoices(labels, UseChoice);
    }

    private void UseChoice(int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= visibleChoices.Count)
            return;

        CardLibrary.CharacterChoiceData choice = visibleChoices[choiceIndex];
        if (choice.reusable)
            GameManager.Instance.MarkUsedOnce(GetChoiceUsageKey(choice));
        else
            GameManager.Instance.SetCharacterProgress(data, choice.targetProgress);

        cardManager.DestroyCardsByDataIds(new List<string>(choice.cardsRemoved));
        cardManager.CreateCards(new List<string>(choice.cardsAdded));
        GameManager.Instance.WillPower.ChangeValue(choice.deltaWillPower);
        GameManager.Instance.Money.ChangeValue(choice.deltaMoney);

        Map map = FindFirstObjectByType<Map>();
        cardManager.RefreshCharactersAtNode(map.CurrentNode);

        if (gameObject.activeInHierarchy)
            ShowDetails();
    }

    private string GetChoiceUsageKey(CardLibrary.CharacterChoiceData choice)
    {
        return $"CharacterChoice:{data.id}:{choice.id}";
    }

    private CardLibrary.CharacterProgressData GetCurrentProgressData()
    {
        if (data.characterProgress == null)
            return null;

        foreach (CardLibrary.CharacterProgressData progressData in data.characterProgress)
        {
            if (progressData.progress == state.Progress)
                return progressData;
        }

        return null;
    }
}
