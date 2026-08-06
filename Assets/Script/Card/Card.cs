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
    [SerializeField] private RectTransform cardContainer;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (cardContainer == null)
        {
            GameObject container = GameObject.Find("CardContainer");
            if (container != null)
                cardContainer = container.GetComponent<RectTransform>();
        }
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

        if (rootCanvas == null || transform.parent != rootCanvas.transform)
            return;

        if (IsOverCardContainer(eventData))
        {
            Transform nearestSlot = FindNearestTaggedSlot(eventData);
            if (nearestSlot != null)
            {
                PlaceIn(nearestSlot);
                return;
            }
        }

        ReturnToPreviousParent();
    }

    private bool IsOverCardContainer(PointerEventData eventData)
    {
        return cardContainer != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                cardContainer,
                eventData.position,
                eventData.pressEventCamera);
    }

    private Transform FindNearestTaggedSlot(PointerEventData eventData)
    {
        if (cardContainer == null)
            return null;

        Transform nearestSlot = null;
        float shortestDistance = float.PositiveInfinity;

        foreach (Transform slot in cardContainer.GetComponentsInChildren<Transform>(true))
        {
            if (!slot.CompareTag("Slot") || IsOccupied(slot))
                continue;

            Vector2 slotScreenPosition = RectTransformUtility.WorldToScreenPoint(
                eventData.pressEventCamera,
                slot.position);
            float distance = (slotScreenPosition - eventData.position).sqrMagnitude;

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestSlot = slot;
            }
        }

        return nearestSlot;
    }

    private bool IsOccupied(Transform slot)
    {
        return slot.GetComponentInChildren<Card>(true) != null;
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
