using UnityEngine;

public class CardSlot : MonoBehaviour
{
    public Card CurrentCard { get; protected set; }

    private void Awake()
    {
        CurrentCard = GetComponentInChildren<Card>(true);
    }

    public void PlaceCard(Card card)
    {
        CurrentCard = card;
        card.PlaceIn(transform);
    }

    public void RemoveCard(Card card)
    {
        if (CurrentCard == card)
            CurrentCard = null;
    }

}
