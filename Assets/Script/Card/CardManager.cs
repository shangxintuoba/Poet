using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public List<Card> CardsOwbned = new List<Card>();
    [SerializeField] private Transform cardContainer;

    public void CreateCards(List<Card> cards)
    {
        if (cards == null)
            return;

        Transform parent = cardContainer != null ? cardContainer : transform;
        foreach (Card card in cards)
        {
            if (card == null)
                continue;

            Card createdCard = Instantiate(card, parent);
            CardsOwbned.Add(createdCard);
        }
    }

    public void DestroyCards(List<Card> cards)
    {
        if (cards == null)
            return;

        foreach (Card card in new List<Card>(cards))
        {
            if (card != null && CardsOwbned.Remove(card))
                Destroy(card.gameObject);
        }
    }
}