using UnityEngine;

public class Organ : CardSlot
{
    public Card cardEquiped;
    public bool HasEquiped;
    [SerializeField] private Player player;

    public override bool TryPlace(Card card)
    {
        if (card == null || !card.canEquiped || HasEquiped)
            return false;

        if (!base.TryPlace(card))
            return false;

        cardEquiped = card;
        HasEquiped = true;

        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (player != null)
            player.ApplyCardStats(card);

        return true;
    }

    public void Unequip()
    {
        if (!HasEquiped || cardEquiped == null)
            return;

        if (player != null)
            player.RemoveCardStats(cardEquiped);

        Card card = cardEquiped;
        cardEquiped = null;
        HasEquiped = false;
        ClearCard();
        card.ReturnToPreviousParent();
    }
}