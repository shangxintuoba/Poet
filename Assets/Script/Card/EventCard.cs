using UnityEngine;

public class EventCard : Card
{
    public GameObject ChoiceSlot;

    public class EventSlotChoices
    {

        public Card[] CardsAddedPrefabs;
        public Card[] CardsDestroyed;
        [Min(0)] public int TimeConsumed;
        public string[] NodesUnlocked;
        public bool HideOtherChoices;
        public bool DestroyWhenUsed;


    }




}
