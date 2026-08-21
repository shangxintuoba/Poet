using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField] private CardLibrary cardLibrary;
    [SerializeField] private Card universalCardPrefab;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private Transform emotionContainer;
    [SerializeField, Min(1)] private int emotionCapacity = 5;

    public List<Card> CardsOwned = new List<Card>();
    public Queue<Card> EmotionsOwned = new Queue<Card>();

    public void CreateCards(List<string> cardReferences)
    {
        if (cardReferences == null)
            return;

        foreach (string cardReference in cardReferences)
            CreateCardByReference(cardReference);
    }

    public void CreateCardsFromPrefabs(List<Card> cardPrefabs)
    {
        if (cardPrefabs == null)
            return;

        foreach (Card prefab in cardPrefabs)
        {
            if (prefab == null)
                continue;

            string cardReference = prefab.Data != null && !string.IsNullOrWhiteSpace(prefab.Data.id)
                ? prefab.Data.id
                : !string.IsNullOrWhiteSpace(prefab.Name)
                    ? prefab.Name
                    : prefab.gameObject.name;

            CreateCardByReference(cardReference);
        }
    }

    public void CreateCardById(string cardId)
    {
        CreateCardByReference(cardId);
    }

    public void CreateCardByName(string cardName)
    {
        CreateCardByReference(cardName);
    }

    public void CreateCardByReference(string cardReference)
    {
        ResolveCardLibrary();
        if (cardLibrary == null || !cardLibrary.LoadJson() && cardLibrary.Data == null)
        {
            Debug.LogWarning("Card Library JSON was not found.");
            return;
        }

        if (universalCardPrefab == null)
        {
            Debug.LogWarning("Card Manager has no Universal Card Prefab assigned.");
            return;
        }

        CardLibrary.CardData data = cardLibrary.FindCardData(cardReference);
        if (data == null)
        {
            Debug.LogWarning("Card Library JSON does not contain: " + cardReference);
            return;
        }

        if (data.type == "Emotion")
            CreateEmotion(data);
        else
            CreateRegularCard(data);
    }

    public void DestroyCards(List<Card> cards)
    {
        if (cards == null)
            return;

        foreach (Card card in new List<Card>(cards))
        {
            if (card == null)
                continue;

            if (card.IsEmotionCard)
            {
                RemoveEmotionFromQueue(card);
                DestroyEmotion(card);
            }
            else
            {
                RemoveFromSlot(card);
                CardsOwned.Remove(card);
                Destroy(card.gameObject);
            }
        }
    }

    public void DestroyCardsByDataIds(List<string> cardReferences)
    {
        if (cardReferences == null || cardReferences.Count == 0)
            return;

        List<Card> cardsToDestroy = new List<Card>();
        foreach (Card card in CardsOwned)
        {
            if (card != null && MatchesAnyReference(card, cardReferences))
                cardsToDestroy.Add(card);
        }

        foreach (Card emotion in EmotionsOwned)
        {
            if (emotion != null && MatchesAnyReference(emotion, cardReferences))
                cardsToDestroy.Add(emotion);
        }

        DestroyCards(cardsToDestroy);
    }

    private void CreateRegularCard(CardLibrary.CardData data)
    {
        Transform parent = cardContainer != null ? cardContainer : transform;
        Card card = Instantiate(universalCardPrefab, parent);
        card.Initialize(data);
        PlaceAtBottomLeft(card, parent);
        CardsOwned.Add(card);

        Transform firstFreeSlot = FindFirstFreeSlot(cardContainer);
        if (firstFreeSlot != null)
            PlaceInSlot(card, firstFreeSlot);
    }

    private void CreateEmotion(CardLibrary.CardData data)
    {
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

        Card emotion = Instantiate(universalCardPrefab, emotionContainer);
        emotion.Initialize(data);
        PlaceAtBottomLeft(emotion, emotionContainer);
        EmotionsOwned.Enqueue(emotion);
        PlaceInSlot(emotion, slot);
    }

    private void DestroyEmotion(Card emotion)
    {
        if (emotion == null)
            return;

        GameManager gameManager = GameManager.Instance != null
            ? GameManager.Instance
            : FindFirstObjectByType<GameManager>();

        if (gameManager != null && gameManager.WillPower != null && emotion.Data != null)
            gameManager.WillPower.ChangeValue(emotion.Data.willPowerDelta);

        RemoveFromSlot(emotion);
        Destroy(emotion.gameObject);
    }

    private void RemoveEmotionFromQueue(Card emotion)
    {
        if (emotion == null || EmotionsOwned.Count == 0)
            return;

        Queue<Card> remaining = new Queue<Card>();
        while (EmotionsOwned.Count > 0)
        {
            Card queuedEmotion = EmotionsOwned.Dequeue();
            if (queuedEmotion != emotion)
                remaining.Enqueue(queuedEmotion);
        }
        EmotionsOwned = remaining;
    }

    private bool MatchesAnyReference(Card card, List<string> references)
    {
        if (card.Data == null)
            return false;

        foreach (string reference in references)
        {
            if (reference == card.Data.id ||
                reference == card.Data.name)
                return true;
        }

        return false;
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

    private void ResolveCardLibrary()
    {
        if (cardLibrary == null)
            cardLibrary = FindFirstObjectByType<CardLibrary>();
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
