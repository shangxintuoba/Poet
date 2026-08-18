using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public List<Card> CardsOwned = new List<Card>();
    public Queue<Emotion> EmotionsOwned = new Queue<Emotion>();

    [SerializeField] private Transform cardContainer;
    [SerializeField] private Transform emotionContainer;
    [SerializeField, Min(1)] private int emotionCapacity = 5;

    public void CreateCards(List<Card> cards)
    {
        if (cards == null)
            return;

        foreach (Card card in cards)
        {
            if (card == null)
                continue;

            if (card is Emotion emotion)
                CreateEmotion(emotion);
            else
                CreateCard(card);
        }
    }

    public void CreateEmotion(Emotion emotionPrefab)
    {
        if (emotionPrefab == null)
            return;

        ResolveEmotionContainer();
        if (emotionContainer == null)
        {
            Debug.LogWarning("Emotion Card Container was not found.");
            return;
        }

        while (EmotionsOwned.Count >= emotionCapacity)
            DestroyEmotion(EmotionsOwned.Dequeue());

        Transform slot = FindFirstFreeSlot(emotionContainer);
        if (slot == null)
        {
            Debug.LogWarning("Emotion Card Container has no free Slot.");
            return;
        }

        Emotion createdEmotion = Instantiate(emotionPrefab, emotionContainer);
        PlaceAtBottomLeft(createdEmotion, emotionContainer);
        EmotionsOwned.Enqueue(createdEmotion);
        PlaceInSlot(createdEmotion, slot);
    }

    public void DestroyCards(List<Card> cards)
    {
        if (cards == null)
            return;

        foreach (Card card in new List<Card>(cards))
        {
            if (card == null)
                continue;

            if (card is Emotion emotion)
            {
                RemoveEmotionFromQueue(emotion);
                DestroyEmotion(emotion);
                continue;
            }

            RemoveFromSlot(card);
            CardsOwned.Remove(card);
            Destroy(card.gameObject);
        }
    }

    private void CreateCard(Card cardPrefab)
    {
        Transform parent = cardContainer != null ? cardContainer : transform;
        Card createdCard = Instantiate(cardPrefab, parent);
        PlaceAtBottomLeft(createdCard, parent);
        CardsOwned.Add(createdCard);

        Transform firstFreeSlot = FindFirstFreeSlot(cardContainer);
        if (firstFreeSlot != null)
            PlaceInSlot(createdCard, firstFreeSlot);
    }

    private void DestroyEmotion(Emotion emotion)
    {
        if (emotion == null)
            return;

        RemoveFromSlot(emotion);
        Destroy(emotion.gameObject);
    }

    private void RemoveEmotionFromQueue(Emotion emotion)
    {
        if (emotion == null || EmotionsOwned.Count == 0)
            return;

        Queue<Emotion> remaining = new Queue<Emotion>();
        while (EmotionsOwned.Count > 0)
        {
            Emotion queuedEmotion = EmotionsOwned.Dequeue();
            if (queuedEmotion != emotion)
                remaining.Enqueue(queuedEmotion);
        }
        EmotionsOwned = remaining;
    }

    private void RemoveFromSlot(Card card)
    {
        CardSlot cardSlot = card.GetComponentInParent<CardSlot>();
        if (cardSlot != null)
            cardSlot.RemoveCard(card);
    }

    private void PlaceAtBottomLeft(Card card, Transform parent)
    {
        RectTransform containerRect = parent as RectTransform;
        RectTransform cardRect = card.transform as RectTransform;
        if (containerRect == null || cardRect == null)
            return;

        cardRect.position = containerRect.TransformPoint(new Vector3(containerRect.rect.xMin, containerRect.rect.yMin, 0f));
    }

    private void PlaceInSlot(Card card, Transform slot)
    {
        CardSlot cardSlot = slot.GetComponent<CardSlot>();
        if (cardSlot != null)
            cardSlot.TryPlace(card);
        else
            card.PlaceIn(slot);
    }

    private Transform FindFirstFreeSlot(Transform container)
    {
        if (container == null)
            return null;

        for (int i = 0; i < container.childCount; i++)
        {
            Transform slot = container.GetChild(i);
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

    private void ResolveEmotionContainer()
    {
        if (emotionContainer != null)
            return;

        GameObject container = GameObject.Find("EmotionCardContainer");
        if (container != null)
            emotionContainer = container.transform;
    }
}
