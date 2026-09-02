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
        [Tooltip("Enter JSON card IDs, subtype IDs, or card names.")]
        [TextArea] public string[] CardsAdded;
        [Tooltip("Optional card prefab references. Their Card Name is used to find JSON data.")]
        public Card[] CardsAddedPrefabs;
        public Card[] CardsDestroyed;
        public string[] RandomCardList;
        [Min(0)] public int RandomCardNumber;
        [Min(0)] public int TimeConsumed;
        public string[] NodesUnlocked;
        public bool HideOtherChoices;
        public bool DestroyWhenUsed;
    }

    public bool Useable;
    [SerializeField] private bool isUsed;
    [SerializeField] private UseChoice[] choices;
    [SerializeField] private bool[] usedChoices;
    [SerializeField] private int lockedChoiceIndex = -1;

    protected override void ShowCardDetails()
    {
        base.ShowCardDetails();
        if (!Useable || choices == null || choices.Length == 0 || textPanel == null)
            return;

        EnsureChoiceState();
        ShowAvailableChoices();
    }

    private void UseChoiceAt(int index)
    {
        EnsureChoiceState();
        if (index < 0 || index >= choices.Length || usedChoices[index])
            return;

        UseChoice choice = choices[index];
        usedChoices[index] = true;
        isUsed = true;

        if (choice.HideOtherChoices)
            lockedChoiceIndex = index;

        textPanel.ShowCardDescription(string.IsNullOrWhiteSpace(choice.UsedText) ? Description : choice.UsedText);

        ResolveCardManager();
        if (cardManager != null)
        {
            cardManager.DestroyCards(new List<Card>(choice.CardsDestroyed));
            cardManager.CreateCards(choice.CardsAdded != null
                ? new List<string>(choice.CardsAdded)
                : new List<string>());
            cardManager.CreateCardsFromPrefabs(choice.CardsAddedPrefabs != null
                ? new List<Card>(choice.CardsAddedPrefabs)
                : new List<Card>());

            for (int i = 0; i < Mathf.Max(0, choice.RandomCardNumber); i++)
                cardManager.CreateRandomCard(choice.RandomCardList != null
                    ? new List<string>(choice.RandomCardList)
                    : new List<string>());
        }

        ConsumeTime(choice.TimeConsumed);
        UnlockNodes(choice.NodesUnlocked);

        if (choice.DestroyWhenUsed)
        {
            cardManager?.DestroyCards(new List<Card> { this });
            return;
        }

        DeselectCard();
    }
    private void ShowAvailableChoices()
    {
        List<string> labels = new List<string>();
        List<int> choiceIndices = new List<int>();

        if (lockedChoiceIndex >= 0 && lockedChoiceIndex < choices.Length)
        {
            labels.Add(choices[lockedChoiceIndex].ChoiceText);
            choiceIndices.Add(lockedChoiceIndex);
        }
        else
        {
            for (int i = 0; i < choices.Length; i++)
            {
                if (usedChoices[i])
                    continue;

                labels.Add(choices[i].ChoiceText);
                choiceIndices.Add(i);
            }
        }

        textPanel.ClearChoices();
        if (labels.Count == 0)
            return;

        textPanel.ShowCardChoices(labels, visibleIndex =>
        {
            if (visibleIndex >= 0 && visibleIndex < choiceIndices.Count)
                UseChoiceAt(choiceIndices[visibleIndex]);
        });
    }

    private void EnsureChoiceState()
    {
        if (choices == null)
        {
            usedChoices = Array.Empty<bool>();
            return;
        }

        if (usedChoices != null && usedChoices.Length == choices.Length)
            return;

        bool[] previousState = usedChoices;
        usedChoices = new bool[choices.Length];
        if (previousState == null)
            return;

        Array.Copy(previousState, usedChoices, Mathf.Min(previousState.Length, usedChoices.Length));
    }


    private void OnValidate()
    {
        EnsureChoiceState();
        if (choices == null || lockedChoiceIndex >= choices.Length)
            lockedChoiceIndex = -1;
    }
}
