using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Card : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TextMeshProUGUI NameText;
    public Image Icon;
    public string Description;
    public string Name;

    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private RectTransform cardPanel;
    [SerializeField] private RectTransform mapContent;
    [SerializeField] private RectTransform mapViewport;
    [SerializeField, Min(0f)] private float cardPanelDropPadding = 80f;
    [SerializeField] protected TextPanelUI textPanel;
    [SerializeField, Min(1f)] private float dragScale = 1.08f;
    [SerializeField, Min(1f)] private float selectionScale = 1.08f;
    [SerializeField, Min(0f)] private float scaleDuration = 0.12f;
    [SerializeField, Min(0f)] private float snapDuration = 0.15f;
    [SerializeField] private RectTransform shadow;
    [SerializeField] private Vector2 dragShadowOffset = new Vector2(-8f, -8f);

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Transform previousParent;
    private Vector2 previousPosition;
    private CardSlot previousSlot;
    private Vector3 restingScale;
    private Vector2 restingShadowPosition;
    private Tween scaleTween;
    private Tween shadowTween;
    private Tween positionTween;
    private bool isDragging;
    private RectTransform selectedCardOverlay;
    private Transform selectionOriginalParent;
    private Vector2 selectionOriginalPosition;

    private static Card selectedCard;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (cardContainer == null)
        {
            GameObject container = GameObject.Find("CardContainer");
            if (container != null) cardContainer = container.GetComponent<RectTransform>();
        }
        if (cardPanel == null) cardPanel = GameObject.Find("CardPanel")?.GetComponent<RectTransform>();
        if (mapContent == null) mapContent = GameObject.Find("Canvas/MapPanel/Scroll View/Viewport/Content")?.GetComponent<RectTransform>();
        if (mapViewport == null) mapViewport = GameObject.Find("Canvas/MapPanel/Scroll View/Viewport")?.GetComponent<RectTransform>();
        if (shadow == null) shadow = transform.Find("Shadow") as RectTransform;
        restingScale = transform.localScale;
        if (shadow != null) restingShadowPosition = shadow.anchoredPosition;
        ResolveTextPanel();
        ResolveSelectedCardOverlay();
    }

    private void Update()
    {
        if (selectedCard != this || Mouse.current == null) return;
        bool clicked = Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame;
        if (clicked && !IsPointerOverThisCard(Mouse.current.position.ReadValue())) DeselectCard();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) StartDragFeedback();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !isDragging) ResetDragFeedback();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && selectedCard == this)
        {
            DeselectCard();
            return;
        }
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (selectedCard == this)
        {
            DeselectCard();
            return;
        }
        if (selectedCard != null) selectedCard.DeselectCard();

        selectedCard = this;
        MoveToSelectedOverlay();
        ScaleTo(restingScale * selectionScale);
        ShowCardDetails();
    }

    protected virtual void ShowCardDetails()
    {
        ResolveTextPanel();
        string cardName = string.IsNullOrWhiteSpace(Name)
            ? (NameText != null ? NameText.text : gameObject.name)
            : Name;
        string details = string.IsNullOrWhiteSpace(Description)
            ? cardName
            : cardName + "\n\n" + Description;
        textPanel?.ShowCardDescription(details);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (selectedCard == this) DeselectCard();

        isDragging = true;
        previousParent = transform.parent;
        previousPosition = ((RectTransform)transform).anchoredPosition;
        previousSlot = previousParent.GetComponent<CardSlot>();
        if (previousSlot != null) previousSlot.RemoveCard(this);
        transform.SetParent(rootCanvas.transform, true);
        canvasGroup.blocksRaycasts = false;
        StartDragFeedback();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData.button != PointerEventData.InputButton.Left) return;
        ((RectTransform)transform).anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData.button != PointerEventData.InputButton.Left) return;
        canvasGroup.blocksRaycasts = true;
        isDragging = false;
        if (rootCanvas == null || transform.parent != rootCanvas.transform) return;

        if (IsOverCardPanel(eventData.position))
        {
            RectTransform nearestSlot = FindNearestFreeSlot();
            if (nearestSlot != null)
            {
                CardSlot slot = nearestSlot.GetComponent<CardSlot>();
                if (slot != null) slot.TryPlace(this);
                else PlaceInSlot(nearestSlot);
                return;
            }
        }

        if (IsOverMapContent(eventData.position))
        {
            transform.SetParent(mapContent, true);
            transform.SetAsLastSibling();
            ResetDragFeedback();
            return;
        }

        ReturnToPreviousPosition();
    }

    private bool IsOverMapContent(Vector2 screenPosition)
    {
        if (mapViewport == null) return false;
        Camera camera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(mapViewport, screenPosition, camera);
    }

    private bool IsOverCardPanel(Vector2 screenPosition)
    {
        if (cardPanel == null) return false;

        Camera camera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(cardPanel, screenPosition, camera, out Vector2 localPoint))
            return false;

        Rect rect = cardPanel.rect;
        rect.xMin -= cardPanelDropPadding;
        rect.xMax += cardPanelDropPadding;
        rect.yMin -= cardPanelDropPadding;
        rect.yMax += cardPanelDropPadding;
        return rect.Contains(localPoint);
    }

    private RectTransform FindNearestFreeSlot()
    {
        GameObject[] slots = GameObject.FindGameObjectsWithTag("Slot");
        RectTransform nearestSlot = null;
        float nearestDistance = float.MaxValue;
        foreach (GameObject slot in slots)
        {
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (slotRect == null || IsOccupied(slot)) continue;
            float distance = Vector3.Distance(transform.position, slotRect.position);
            if (distance < nearestDistance) { nearestDistance = distance; nearestSlot = slotRect; }
        }
        return nearestSlot;
    }

    private bool IsOccupied(GameObject slot)
    {
        CardSlot cardSlot = slot.GetComponent<CardSlot>();
        if (cardSlot != null && cardSlot.CurrentCard != null && cardSlot.CurrentCard != this) return true;
        Card cardInSlot = slot.GetComponentInChildren<Card>(true);
        return cardInSlot != null && cardInSlot != this;
    }

    private void PlaceInSlot(RectTransform slot)
    {
        positionTween?.Kill();
        transform.SetParent(slot, true);
        positionTween = ((RectTransform)transform).DOAnchorPos(Vector2.zero, snapDuration).SetEase(Ease.OutQuad);
        ResetDragFeedback();
    }

    public void PlaceIn(Transform targetParent)
    {
        RectTransform slot = targetParent as RectTransform;
        if (slot != null) PlaceInSlot(slot);
    }

    public void PlaceIn(CardSlot slot)
    {
        if (slot != null) PlaceIn(slot.transform);
    }

    private void ReturnToPreviousPosition()
    {
        positionTween?.Kill();
        transform.SetParent(previousParent, false);
        ((RectTransform)transform).anchoredPosition = previousPosition;
        ResetDragFeedback();
    }

    private void StartDragFeedback()
    {
        ScaleTo(restingScale * dragScale);
        if (shadow == null) return;
        shadowTween?.Kill();
        shadowTween = shadow.DOAnchorPos(restingShadowPosition + dragShadowOffset, scaleDuration).SetEase(Ease.OutQuad);
    }

    private void ResetDragFeedback()
    {
        ScaleTo(restingScale);
        if (shadow == null) return;
        shadowTween?.Kill();
        shadowTween = shadow.DOAnchorPos(restingShadowPosition, scaleDuration).SetEase(Ease.OutQuad);
    }

    private void DeselectCard()
    {
        if (selectedCard != this) return;
        ResolveTextPanel();
        textPanel?.RestoreDialogueAfterCardDescription();
        ReturnFromSelectedOverlay();
        ScaleTo(restingScale);
        selectedCard = null;
    }

    private void MoveToSelectedOverlay()
    {
        ResolveSelectedCardOverlay();
        if (selectedCardOverlay == null) return;
        selectionOriginalParent = transform.parent;
        selectionOriginalPosition = ((RectTransform)transform).anchoredPosition;
        transform.SetParent(selectedCardOverlay, true);
    }

    private void ReturnFromSelectedOverlay()
    {
        if (selectionOriginalParent == null) return;
        transform.SetParent(selectionOriginalParent, false);
        ((RectTransform)transform).anchoredPosition = selectionOriginalPosition;
        selectionOriginalParent = null;
    }

    private void ResolveSelectedCardOverlay()
    {
        if (selectedCardOverlay != null) return;
        GameObject overlay = GameObject.Find("SelectedCardOverlay");
        if (overlay != null) selectedCardOverlay = overlay.GetComponent<RectTransform>();
    }

    private void ScaleTo(Vector3 targetScale)
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(targetScale, scaleDuration).SetEase(Ease.OutQuad);
    }

    private bool IsPointerOverThisCard(Vector2 screenPosition)
    {
        Camera eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPosition, eventCamera);
    }

    protected void ResolveTextPanel()
    {
        if (textPanel != null) return;
        GameObject manager = GameObject.Find("TextManager");
        if (manager != null) textPanel = manager.GetComponent<TextPanelUI>();
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
        shadowTween?.Kill();
        positionTween?.Kill();
        if (selectedCard == this) selectedCard = null;
    }
}