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
    [Tooltip("等待 InitialTest 结束后显示的 Card ID。")]
    public List<string> InitialCards = new List<string>();
    [Tooltip("调用 ShowInitialCard 时一并生成的初始卡牌预制体。")]
    public List<Card> InitialCardPrefabs = new List<Card>();

    [HideInInspector]public bool HasInitialized;

    public void CreateCards(List<string> cardReferences)
    {
        if (cardReferences == null)
            return;

        foreach (string cardReference in cardReferences)
            CreateCardByReference(cardReference);
    }

    /// <summary>Queues card IDs awarded by the initial test without creating them yet.</summary>
    public void AddInitialCards(List<string> cardReferences)
    {
        if (cardReferences == null)
            return;

        foreach (string cardReference in cardReferences)
        {
            if (!string.IsNullOrWhiteSpace(cardReference))
                InitialCards.Add(cardReference);
        }
    }

    public void CreateRandomCard(List<string> cardReferences)
    {
        if (cardReferences == null || cardReferences.Count == 0)
            return;

        List<string> validReferences = new List<string>();
        foreach (string cardReference in cardReferences)
        {
            if (!string.IsNullOrWhiteSpace(cardReference))
                validReferences.Add(cardReference);
        }

        if (validReferences.Count == 0)
            return;

        CreateCardByReference(validReferences[Random.Range(0, validReferences.Count)]);
    }

    public void CreateCardsFromPrefabs(List<Card> cardPrefabs)
    {
        if (cardPrefabs == null)
            return;

        foreach (Card prefab in cardPrefabs)
            CreateCardFromPrefab(prefab);
    }

    public Card CreateCardFromPrefab(Card prefab)
    {
        if (prefab == null)
            return null;

        Transform parent = cardContainer != null ? cardContainer : transform;
        Card card = Instantiate(prefab, parent);
        PlaceAtBottomLeft(card, parent);
        CardsOwned.Add(card);

        Transform firstFreeSlot = FindFirstFreeSlot(cardContainer);
        if (firstFreeSlot != null)
            PlaceInSlot(card, firstFreeSlot);

        return card;
    }

    public void ClearAllCards()
    {
        List<Card> allCards = new List<Card>(CardsOwned);
        allCards.AddRange(EmotionsOwned);
        DestroyCards(allCards);
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

        if (IsCardType(data, "Emotion"))
            CreateEmotion(data);
        else if (data.type == "Character")
        {
            GameManager.Instance.GetCharacterState(data);
            Map map = FindFirstObjectByType<Map>();
            if (map != null && map.CurrentNode != null)
                RefreshCharactersAtNode(map.CurrentNode);
        }
        else
            CreateRegularCard(data);
    }

    public void RefreshCharactersAtNode(Node currentNode)
    {
        ResolveCardLibrary();
        if (cardLibrary.Data == null)
            cardLibrary.LoadJson();

        foreach (Card ownedCard in new List<Card>(CardsOwned))
        {
            if (ownedCard.Data != null && ownedCard.Data.type == "Character")
            {
                GameManager.CharacterState state = GameManager.Instance.GetCharacterState(ownedCard.Data);
                if (state.CurrentNodeIndex != currentNode.Index)
                    DestroyCards(new List<Card> { ownedCard });
            }
        }

        foreach (CardLibrary.CardData data in cardLibrary.Cards)
        {
            if (data.type != "Character")
                continue;

            GameManager.CharacterState state = GameManager.Instance.GetCharacterState(data);
            if (state.CurrentNodeIndex == currentNode.Index && !HasCharacterCard(data.id))
                CreateRegularCard(data);
        }
    }

    public Card CreateCardInSlot(string cardReference, CardSlot targetSlot)
    {
        if (targetSlot == null || targetSlot.CurrentCard != null)
            return null;

        ResolveCardLibrary();
        if (cardLibrary == null || !cardLibrary.LoadJson() && cardLibrary.Data == null || universalCardPrefab == null)
            return null;

        CardLibrary.CardData data = cardLibrary.FindCardData(cardReference);
        if (data == null)
            return null;

        Card card = Instantiate(universalCardPrefab, targetSlot.transform);
        card.Initialize(data);
        targetSlot.PlaceCard(card);

        if (IsCardType(data, "Emotion"))
        {
            while (EmotionsOwned.Count >= emotionCapacity)
                DestroyEmotion(EmotionsOwned.Dequeue());
            EmotionsOwned.Enqueue(card);
            ApplyEmotionWillPower(data);
        }
        else
        {
            CardsOwned.Add(card);
        }

        return card;
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

    private bool HasCharacterCard(string cardId)
    {
        foreach (Card card in CardsOwned)
        {
            if (card.Data != null && card.Data.type == "Character" && card.Data.id == cardId)
                return true;
        }

        return false;
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
        ApplyEmotionWillPower(data);
    }

    private void DestroyEmotion(Card emotion)
    {
        if (emotion == null)
            return;

        RemoveFromSlot(emotion);
        Destroy(emotion.gameObject);
    }

    private void ApplyEmotionWillPower(CardLibrary.CardData data)
    {
        GameManager.Instance.WillPower.ChangeValue(data.willPowerDelta);
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
            cardSlot.PlaceCard(card);
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

    private static bool IsCardType(CardLibrary.CardData data, string type)
    {
        return data != null &&
               string.Equals(data.type, type, System.StringComparison.OrdinalIgnoreCase);
    }

    private void ResolveEmotionContainer()
    {
        if (emotionContainer != null)
            return;

        GameObject container = GameObject.Find("EmotionCardContainer");
        if (container != null)
            emotionContainer = container.transform;
    }

    public void ShowInitialCard()
    {
        if (HasInitialized) return;

        List<string> initialCardReferences = InitialCards != null
            ? new List<string>(InitialCards)
            : new List<string>();
        bool initialCardsAssigned = initialCardReferences.Count == 0;

        // Initial setup only creates the configured prefabs (Your Body, Time, Willpower).
        // The IDs selected by InitialTest become rewards of Your Body's first Raw choice.
        if (InitialCardPrefabs != null)
        {
            foreach (Card prefab in InitialCardPrefabs)
            {
                Card card = CreateCardFromPrefab(prefab);
                HandleInitialCard initialCardHandler = card != null
                    ? card.GetComponent<HandleInitialCard>()
                    : null;

                if (initialCardHandler != null)
                    initialCardsAssigned |= initialCardHandler.AddInitialCards(initialCardReferences);
            }
        }

        if (initialCardsAssigned)
            InitialCards.Clear();
        else if (initialCardReferences.Count > 0)
            Debug.LogWarning("Initial cards could not be assigned because no generated prefab has HandleInitialCard.");

        SettingPanel settingPanel = FindFirstObjectByType<SettingPanel>();
        settingPanel?.ShowMapPanelAfterInitialCards();
        HasInitialized = true;
    }


}
