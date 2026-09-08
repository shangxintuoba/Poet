using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    private Card card;
    private CardLibrary.CardData data;
    private GameManager.CharacterState state;
    private TextPanelUI textPanel;
    private CardManager cardManager;

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
        if (progressData == null || progressData.choices == null || progressData.choices.Length == 0)
            return;

        List<string> labels = new List<string>();
        foreach (CardLibrary.CharacterChoiceData choice in progressData.choices)
            labels.Add(choice.text);

        textPanel.ShowCardChoices(labels, UseChoice);
    }

    private void UseChoice(int choiceIndex)
    {
        CardLibrary.CharacterProgressData progressData = GetCurrentProgressData();
        if (progressData == null || progressData.choices == null ||
            choiceIndex < 0 || choiceIndex >= progressData.choices.Length)
            return;

        CardLibrary.CharacterChoiceData choice = progressData.choices[choiceIndex];
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
