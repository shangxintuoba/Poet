using UnityEngine;

public class CardSlot : MonoBehaviour
{
    public Card CurrentCard { get; protected set; }

    private void Awake()
    {
        CurrentCard = GetComponentInChildren<Card>(true);
    }

    public virtual bool TryPlace(Card card)
    {
        Forge forge = GetComponentInParent<Forge>();
        if (forge != null)
        {
            if (!forge.TryPlaceComponentCard(this, card))
                return false;

            CurrentCard = card;
            card.PlaceIn(transform);
            return true;
        }

        if (card == null || CurrentCard != null || !Accepts(card))
            return false;

        CurrentCard = card;
        card.PlaceIn(transform);
        return true;
    }

    public bool ForcePlace(Card card)
    {
        if (card == null || CurrentCard != null)
            return false;

        CurrentCard = card;
        card.PlaceIn(transform);
        return true;
    }

    public void RemoveCard(Card card)
    {
        if (CurrentCard == card)
            CurrentCard = null;
    }

    private bool Accepts(Card card)
    {
        bool isEmotionSlot = IsInsideEmotionContainer();
        if (card.IsEmotionCard)
            return isEmotionSlot;

        return !isEmotionSlot;
    }

    private bool IsInsideEmotionContainer()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "EmotionCardContainer")
                return true;
            current = current.parent;
        }
        return false;
    }
}
