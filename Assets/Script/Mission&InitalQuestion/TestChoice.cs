using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TestChoice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI ChoiceText;
    public GameObject Outline;
    public int Index;

    private void Awake()
    {
        Outline.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Outline.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Outline.SetActive(false);
    }
}
