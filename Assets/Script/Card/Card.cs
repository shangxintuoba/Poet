using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Card : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    [SerializeField, Min(1f)] private float dragScale = 1.08f;
    [SerializeField, Min(0f)] private float scaleDuration = 0.12f;
    [SerializeField] private RectTransform shadow;
    [SerializeField] private Vector2 dragShadowOffset = new Vector2(-8f, -8f);

    private Vector3 restingScale;
    private Vector2 restingShadowPosition;
    private Tween scaleTween;
    private Tween shadowTween;
    private bool isDragging;

    private void Awake()
    {
        restingScale = transform.localScale;
        if (shadow == null)
            shadow = transform.Find("Shadow") as RectTransform;
        if (shadow != null)
            restingShadowPosition = shadow.anchoredPosition;

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

    public void OnPointerDown(PointerEventData eventData)
    {
        StartDragFeedback();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
            ResetDragFeedback();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        previousParent = transform.parent;
        previousPosition = ((RectTransform)transform).anchoredPosition;
        canvasGroup.blocksRaycasts = false;
        StartDragFeedback();

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
        isDragging = false;

        if (rootCanvas == null || transform.parent != rootCanvas.transform)
            return;

 
            Transform nearestSlot = FindNearestTaggedSlot(eventData);
            if (nearestSlot != null)
            {
                PlaceIn(nearestSlot);
                return;
            }


        //ReturnToPreviousParent();
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
        ResetDragFeedback();
    }

    public void ReturnToPreviousParent()
    {
        if (previousParent == null)
            return;

        transform.SetParent(previousParent, false);
        ((RectTransform)transform).anchoredPosition = previousPosition;
        ResetDragFeedback();
    }

    private void StartDragFeedback()
    {
        ScaleTo(restingScale * dragScale);
        MoveShadowTo(restingShadowPosition + dragShadowOffset);
    }

    private void ResetDragFeedback()
    {
        ScaleTo(restingScale);
        MoveShadowTo(restingShadowPosition);
    }

    private void ScaleTo(Vector3 targetScale)
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(targetScale, scaleDuration).SetEase(Ease.OutQuad);
    }

    private void MoveShadowTo(Vector2 targetPosition)
    {
        if (shadow == null)
            return;

        shadowTween?.Kill();
        shadowTween = shadow.DOAnchorPos(targetPosition, scaleDuration).SetEase(Ease.OutQuad);
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
        shadowTween?.Kill();
    }
}
