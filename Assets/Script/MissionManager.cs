using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public CardSlot Slot1;
    public CardSlot Slot2;
    public TextMeshProUGUI Slot1text;
    public TextMeshProUGUI Slot2text;

    public class Mission
    {
        public string Name;
        public string Descrition;
        public List<Card> RequiredCards;
        public int MoneyReward;
    }

    public void ChooseRequiredCard()
    {


    }

    public void InstantiateMission()
    {
        //set slot1text and slot2text as the name of the RequiredCards. 

    }


    public void TryCalculateResult()
    {
        //calculate if the 
    }
}
