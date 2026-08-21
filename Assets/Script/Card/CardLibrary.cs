using System;
using System.Collections.Generic;
using UnityEngine;

public class CardLibrary : MonoBehaviour
{
    [Serializable]
    public class CardLibraryData
    {
        public int schemaVersion;
        public string[] sourceSheets;
        public CardData[] cards;
    }

    [Serializable]
    public class CardData
    {
        public string id;
        public string name;
        public string type;
        [TextArea] public string description;
        public int willPowerDelta;
        public string materialType;
        public bool useable;
        public RawChoiceData[] choices;
    }

    [Serializable]
    public class RawChoiceData
    {
        public string id;
        public string choiceText;
        [TextArea] public string usedText;
        public string[] cardsAdded;
        public string[] cardsDestroyed;
        public bool hideOtherChoices;
        public bool destroyWhenUsed;
    }

    [SerializeField] private TextAsset cardLibraryJson;

    public CardLibraryData Data { get; private set; }
    public IReadOnlyList<CardData> Cards => Data != null && Data.cards != null
        ? Data.cards
        : Array.Empty<CardData>();

    private readonly Dictionary<string, CardData> cardsByReference = new Dictionary<string, CardData>();

    private void Awake()
    {
        LoadJson();
    }

    public bool LoadJson()
    {
        Data = null;
        cardsByReference.Clear();

        if (cardLibraryJson == null)
        {
            Debug.LogWarning("CardLibrary has no JSON asset assigned.", this);
            return false;
        }

        Data = JsonUtility.FromJson<CardLibraryData>(cardLibraryJson.text);
        if (Data == null || Data.cards == null)
        {
            Debug.LogError("CardLibrary JSON could not be read or contains no cards.", this);
            return false;
        }

        foreach (CardData card in Data.cards)
        {
            if (card == null)
                continue;

            AddReference(card.id, card);
            AddReference(card.name, card);
        }

        return true;
    }

    // Accepts a FullLibrary Index or a card Name.
    public CardData FindCardData(string cardReference)
    {
        if (string.IsNullOrWhiteSpace(cardReference))
            return null;

        cardsByReference.TryGetValue(cardReference, out CardData card);
        return card;
    }

    public CardData FindCardDataById(string cardId)
    {
        return FindCardData(cardId);
    }

    public CardData FindCardDataByName(string cardName)
    {
        return FindCardData(cardName);
    }

    private void AddReference(string reference, CardData card)
    {
        if (!string.IsNullOrWhiteSpace(reference))
            cardsByReference[reference] = card;
    }
}
