using System.Collections.Generic;
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
    public GameObject NaureOutline;
    public GameObject PoliticsOutline;
    public GameObject EmotionOutline;
    public GameObject UniversalOutline;
    public string Description;
    public string Name;

    public CardLibrary.CardData Data { get; private set; }
    public bool IsEmotionCard => Data != null && Data.type == "Emotion";

    private bool[] usedRawChoices;
    private int lockedRawChoiceIndex = -1;
    private CardManager cardManager;

    public enum CardOutlineType
    {
        OEmotion,
        OPolitics,
        ONature,
        ONone,
        OUniversal,
    }

    public CardOutlineType OutlineType;

    private RectTransform cardContainer;
    private RectTransform cardPanel;
    private RectTransform mapContent;
    private RectTransform mapViewport;
    private RectTransform emotionCardContainer;
    [SerializeField, Min(0f)] private float cardPanelDropPadding = 80f;
    protected TextPanelUI textPanel;
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
    private LiquidAmountIndicator liquidAmountIndicator;
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
        if (emotionCardContainer == null) emotionCardContainer = GameObject.Find("EmotionCardContainer")?.GetComponent<RectTransform>();
        if (shadow == null) shadow = transform.Find("Shadow") as RectTransform;
        liquidAmountIndicator = GetComponentInChildren<LiquidAmountIndicator>(true);
        restingScale = transform.localScale;
        if (shadow != null) restingShadowPosition = shadow.anchoredPosition;
        ResolveTextPanel();
        ResolveSelectedCardOverlay();
    }

    private void Start()
    {
        if (NameText != null)
            NameText.text = Name;

        AssignOutlineType();
    }

    public void Initialize(CardLibrary.CardData data)
    {
        Data = data;
        Name = data != null ? data.name : string.Empty;
        Description = data != null ? data.description : string.Empty;

        if (NameText != null)
            NameText.text = Name;

        ResetRawChoiceState();
        AssignOutlineType();
    }

    public void AssignOutlineType()
    {
        OutlineType = CardOutlineType.ONone;

        if (Data != null)
        {
            switch (Data.type)
            {
                case "Emotion":
                    OutlineType = CardOutlineType.OEmotion;
                    break;

                case "Material":
                    switch (Data.materialType)
                    {
                        case "Nature":
                            OutlineType = CardOutlineType.ONature;
                            break;

                        case "Politics":
                            OutlineType = CardOutlineType.OPolitics;
                            break;

                        case "Universal":
                            OutlineType = CardOutlineType.OUniversal;
                            break;
                    }
                    break;
            }
        }

        SetActiveOutline();
    }

    private void SetActiveOutline()
    {
        if (NaureOutline != null)
            NaureOutline.SetActive(OutlineType == CardOutlineType.ONature);

        if (PoliticsOutline != null)
            PoliticsOutline.SetActive(OutlineType == CardOutlineType.OPolitics);

        if (EmotionOutline != null)
            EmotionOutline.SetActive(OutlineType == CardOutlineType.OEmotion);

        if (UniversalOutline != null)
            UniversalOutline.SetActive(OutlineType == CardOutlineType.OUniversal);
    }

    private void Update()
    {
        if (selectedCard != this || Mouse.current == null) return;

        // Only a right click away from this card closes its selected state.
        if (Mouse.current.rightButton.wasPressedThisFrame && !IsPointerOverThisCard(Mouse.current.position.ReadValue()))
            DeselectCard();
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
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (selectedCard == this)
        {
            DeselectCard();
            return;
        }
        if (selectedCard != null) selectedCard.DeselectCard();

        selectedCard = this;
        MoveToSelectedOverlay();
        ApplySelectedFeedback();
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
        ShowRawChoices();
    }

    private void ShowRawChoices()
    {
        if (Data == null || Data.type != "Raw" || !Data.useable ||
            Data.choices == null || Data.choices.Length == 0 || textPanel == null)
            return;

        ResetRawChoiceState();

        List<string> labels = new List<string>();
        List<int> choiceIndices = new List<int>();

        if (lockedRawChoiceIndex >= 0 && lockedRawChoiceIndex < Data.choices.Length)
        {
            labels.Add(Data.choices[lockedRawChoiceIndex].choiceText);
            choiceIndices.Add(lockedRawChoiceIndex);
        }
        else
        {
            for (int i = 0; i < Data.choices.Length; i++)
            {
                if (usedRawChoices[i])
                    continue;

                labels.Add(Data.choices[i].choiceText);
                choiceIndices.Add(i);
            }
        }

        textPanel.ClearChoices();
        if (labels.Count == 0)
            return;

        textPanel.ShowCardChoices(labels, visibleIndex =>
        {
            if (visibleIndex >= 0 && visibleIndex < choiceIndices.Count)
                UseRawChoiceAt(choiceIndices[visibleIndex]);
        });
    }

    private void UseRawChoiceAt(int index)
    {
        if (Data == null || Data.choices == null)
            return;

        ResetRawChoiceState();
        if (index < 0 || index >= Data.choices.Length || usedRawChoices[index])
            return;

        CardLibrary.RawChoiceData choice = Data.choices[index];
        usedRawChoices[index] = true;

        if (choice.hideOtherChoices)
            lockedRawChoiceIndex = index;

        textPanel?.ShowCardDescription(string.IsNullOrWhiteSpace(choice.usedText) ? Description : choice.usedText);

        ResolveCardManager();
        if (cardManager != null)
        {
            cardManager.DestroyCardsByDataIds(new List<string>(choice.cardsDestroyed ?? new string[0]));
            cardManager.CreateCards(new List<string>(choice.cardsAdded ?? new string[0]));
        }

        if (choice.destroyWhenUsed)
        {
            cardManager?.DestroyCards(new List<Card> { this });
            return;
        }

        DeselectCard();
    }

    private void ResetRawChoiceState()
    {
        int choiceCount = Data != null && Data.choices != null ? Data.choices.Length : 0;
        if (usedRawChoices == null || usedRawChoices.Length != choiceCount)
        {
            usedRawChoices = new bool[choiceCount];
            lockedRawChoiceIndex = -1;
        }
    }

    private void ResolveCardManager()
    {
        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (selectedCard == this) ReturnFromSelectedOverlay();

        isDragging = true;
        previousParent = transform.parent;
        previousPosition = ((RectTransform)transform).anchoredPosition;
        previousSlot = previousParent.GetComponent<CardSlot>();
        if (previousSlot != null) previousSlot.RemoveCard(this);
        transform.SetParent(rootCanvas.transform, true);
        canvasGroup.blocksRaycasts = false;
        StartDragFeedback();
        liquidAmountIndicator?.BeginSway();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData.button != PointerEventData.InputButton.Left) return;
        ((RectTransform)transform).anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
        liquidAmountIndicator?.SetDragDelta(eventData.delta, rootCanvas != null ? rootCanvas.scaleFactor : 1f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData.button != PointerEventData.InputButton.Left) return;
        canvasGroup.blocksRaycasts = true;
        isDragging = false;
        if (rootCanvas == null || transform.parent != rootCanvas.transform) return;

        if (IsEmotionCard)
        {
            HandleEmotionDrop(eventData.position);
            return;
        }

        if (IsOverEmotionCardContainer(eventData.position))
        {
            ReturnToPreviousPosition();
            return;
        }

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


    private void HandleEmotionDrop(Vector2 screenPosition)
    {
        RectTransform nearestEmotionSlot = FindNearestFreeEmotionSlot();
        if (nearestEmotionSlot == null)
        {
            ReturnToPreviousPosition();
            return;
        }

        CardSlot slot = nearestEmotionSlot.GetComponent<CardSlot>();
        if (slot != null)
            slot.TryPlace(this);
        else
            PlaceInSlot(nearestEmotionSlot);
    }

    private RectTransform FindNearestFreeEmotionSlot()
    {
        if (emotionCardContainer == null)
            return null;

        RectTransform nearestSlot = null;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < emotionCardContainer.childCount; i++)
        {
            Transform child = emotionCardContainer.GetChild(i);
            if (!child.CompareTag("Slot"))
                continue;

            GameObject slotObject = child.gameObject;
            if (IsOccupied(slotObject))
                continue;

            RectTransform slot = child as RectTransform;
            if (slot == null)
                continue;

            float distance = Vector3.Distance(transform.position, slot.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSlot = slot;
            }
        }

        return nearestSlot;
    }

    private bool IsEmotionSlot(Transform slot)
    {
        return emotionCardContainer != null && slot.IsChildOf(emotionCardContainer);
    }

    private bool IsOverEmotionCardContainer(Vector2 screenPosition)
    {
        if (emotionCardContainer == null)
            return false;

        Camera camera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;
        return RectTransformUtility.RectangleContainsScreenPoint(emotionCardContainer, screenPosition, camera);
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
            if (slotRect == null || IsEmotionSlot(slot.transform) || IsOccupied(slot)) continue;
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
        liquidAmountIndicator?.StopSway();
        if (selectedCard == this)
        {
            ApplySelectedFeedback();
            return;
        }

        ScaleTo(restingScale);
        if (shadow == null) return;
        shadowTween?.Kill();
        shadowTween = shadow.DOAnchorPos(restingShadowPosition, scaleDuration).SetEase(Ease.OutQuad);
    }

    private void ApplySelectedFeedback()
    {
        ScaleTo(restingScale * selectionScale);
        if (shadow == null) return;
        shadowTween?.Kill();
        shadowTween = shadow.DOAnchorPos(restingShadowPosition + dragShadowOffset, scaleDuration).SetEase(Ease.OutQuad);
    }

    protected void DeselectCard()
    {
        if (selectedCard != this) return;
        ResolveTextPanel();
        textPanel?.RestoreDialogueAfterCardDescription();
        ReturnFromSelectedOverlay();
        selectedCard = null;
        ResetDragFeedback();
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
        if (selectedCard == this)
        {
            ResolveTextPanel();
            textPanel?.RestoreDialogueAfterCardDescription();
            selectedCard = null;
        }
    }
}
