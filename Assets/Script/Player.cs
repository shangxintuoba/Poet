using UnityEngine;

public class Player : MonoBehaviour
{
    public int Perception;
    public int Penetration;
    public int tenacity;
    public int Caliber;

    public int Money;
    public int WillPower;

    public Organ Gland;
    public Organ Receptor;
    public Organ Annus;
    public Organ Septum;
    public Organ Heart;

    public void ApplyCardStats(Card card)
    {
        ChangeCardStats(card, 1);
    }

    public void RemoveCardStats(Card card)
    {
        ChangeCardStats(card, -1);
    }

    private void ChangeCardStats(Card card, int multiplier)
    {
        if (card == null)
            return;

    }
}