using System;
using System.Collections.Generic;
using UnityEngine;

public class Raw : Card
{
    [Serializable]
    public class UseChoice
    {
        public string ChoiceText;
        [TextArea] public string UsedText;
        public bool DestroyWhenUsed;
        public Card[] CardsAdded;
        public Card[] CardsDestroyed;
    }

    public bool Useable;
    [SerializeField] private bool isUsed;
    [SerializeField] private UseChoice[] choices;
    [SerializeField] private CardManager cardManager;

    protected override void ShowCardDetails()
    {
        base.ShowCardDetails();
        if (!Useable || isUsed || choices == null || choices.Length == 0 || textPanel == null)
            return;

        List<string> choiceTexts = new List<string>();
        foreach (UseChoice choice in choices)
            choiceTexts.Add(choice.ChoiceText);

        textPanel.ShowCardChoices(choiceTexts, UseChoiceAt);
    }

    private void UseChoiceAt(int index)
    {
        if (isUsed || index < 0 || index >= choices.Length)
            return;

        UseChoice choice = choices[index];
        isUsed = true;
        textPanel.ShowCardDescription(string.IsNullOrWhiteSpace(choice.UsedText) ? Description : choice.UsedText);

        ResolveCardManager();
        if (cardManager == null)
            return;

        cardManager.DestroyCards(new List<Card>(choice.CardsDestroyed));
        cardManager.CreateCards(new List<Card>(choice.CardsAdded));

        if (choice.DestroyWhenUsed)
            cardManager.DestroyCards(new List<Card> { this });
    }

    private void ResolveCardManager()
    {
        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();
    }
}
