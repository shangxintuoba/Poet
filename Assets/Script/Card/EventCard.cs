using System;
using System.Collections.Generic;
using UnityEngine;

public class EventCard : Card
{
    public GameObject ChoiceSlot;
    public List<EventSlotChoices> Choices;

    [Serializable]
    public class EventSlotChoices
    {
        public string TargetCardID;
        public Card[] CardsAddedPrefabs;
        public Card[] CardsDestroyed;
        [Min(0)] public int TimeConsumed;
        public string[] NodesUnlocked;
        public bool HideOtherChoices;
        public bool DestroyWhenUsed;
    }

    private int lockedChoiceIndex = -1;
    public int LockedChoiceIndex => lockedChoiceIndex;

    protected override void ShowCardDetails()
    {
        base.ShowCardDetails();
        textPanel.ShowEventCardChoiceSlot(this);
    }

    public void ExecuteChoice(int index)
    {
        EventSlotChoices choice = Choices[index];
        if (choice.HideOtherChoices)
            lockedChoiceIndex = index;

        ResolveCardManager();
        cardManager.DestroyCards(new List<Card>(choice.CardsDestroyed));
        cardManager.CreateCardsFromPrefabs(new List<Card>(choice.CardsAddedPrefabs));
        ConsumeTime(choice.TimeConsumed);
        UnlockNodes(choice.NodesUnlocked);

        if (choice.DestroyWhenUsed)
        {
            cardManager.DestroyCards(new List<Card> { this });
            return;
        }

        DeselectCard();
    }
}
