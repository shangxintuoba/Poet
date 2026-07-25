using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Description;
    public Image Icon;

    public enum Type
    {
        Raw,
        Material,
        Work,
    }

    public bool canEquiped;
    //Attribute when Equiped
    public int Perception;
    public int Penetration;
    public int tenacity;
    public int Caliber;


}
