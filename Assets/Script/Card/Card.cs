using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Card : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Description;
    public Image Icon;

    public bool canEquiped;
    public int Perception;
    public int Penetration;
    public int tenacity;
    public int Caliber;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Transform previousParent;
    private Vector2 previousPosition;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        previousParent = transform.parent;
        previousPosition = ((RectTransform)transform).anchoredPosition;
        canvasGroup.blocksRaycasts = false;

        if (rootCanvas != null)
            transform.SetParent(rootCanvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas != null)
            ((RectTransform)transform).anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (rootCanvas != null && transform.parent == rootCanvas.transform && previousParent != null)
            transform.SetParent(previousParent, true);
    }

    public void PlaceIn(Transform parent)
    {
        transform.SetParent(parent, false);
        ((RectTransform)transform).anchoredPosition = Vector2.zero;
    }

    public void ReturnToPreviousParent()
    {
        if (previousParent == null)
            return;

        transform.SetParent(previousParent, false);
        ((RectTransform)transform).anchoredPosition = previousPosition;
    }
}