using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    public List<Card> CardsOwbned;


    public void CreateCards(List<Card> cards)
    {
        foreach (var card in cards)
        {

            //generate a card and add it to the List of CardsOwbned
        }
    }


    public void DestroyCards(List<Card> cards)
    {
        foreach (var card in cards)
        {

            //destroy the card and remove it from the List of CardsOwbned
        }
    }

}
