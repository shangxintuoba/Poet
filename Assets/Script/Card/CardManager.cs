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
            PlaceAtBottomLeft(createdCard, parent);
            CardsOwbned.Add(createdCard);

            Transform firstFreeSlot = FindFirstFreeSlot();
            if (firstFreeSlot == null)
                continue;

            CardSlot cardSlot = firstFreeSlot.GetComponent<CardSlot>();
            if (cardSlot != null)
                cardSlot.TryPlace(createdCard);
            else
                createdCard.PlaceIn(firstFreeSlot);
        }
    }

    public void DestroyCards(List<Card> cards)
    {
        if (cards == null)
            return;

        foreach (Card card in new List<Card>(cards))
        {
            if (card == null)
                continue;

            CardSlot cardSlot = card.GetComponentInParent<CardSlot>();
            if (cardSlot != null)
                cardSlot.RemoveCard(card);

            CardsOwbned.Remove(card);
            Destroy(card.gameObject);
        }
    }

    private void PlaceAtBottomLeft(Card card, Transform parent)
    {
        RectTransform containerRect = parent as RectTransform;
        RectTransform cardRect = card.transform as RectTransform;
        if (containerRect == null || cardRect == null)
            return;

        Vector3 bottomLeft = containerRect.TransformPoint(new Vector3(containerRect.rect.xMin, containerRect.rect.yMin, 0f));
        cardRect.position = bottomLeft;
    }

    private Transform FindFirstFreeSlot()
    {
        if (cardContainer == null)
            return null;

        // Direct child order matches the visible Grid Layout order.
        for (int i = 0; i < cardContainer.childCount; i++)
        {
            Transform slot = cardContainer.GetChild(i);
            if (!slot.CompareTag("Slot"))
                continue;

            CardSlot cardSlot = slot.GetComponent<CardSlot>();
            bool occupied = cardSlot != null
                ? cardSlot.CurrentCard != null
                : slot.GetComponentInChildren<Card>(true) != null;
            if (!occupied)
                return slot;
        }

        return null;
    }
}
