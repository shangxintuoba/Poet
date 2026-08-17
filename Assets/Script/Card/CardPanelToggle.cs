using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardPanelToggle : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform cardPanel;
    [SerializeField, Min(0f)] private float slideDistance = 120f;
    [SerializeField, Min(0f)] private float duration = 0.18f;

    private Vector2 openPosition;
    private Tween moveTween;
    private bool isOpen = true;

    private void Awake()
    {
        if (cardPanel == null)
            cardPanel = transform.parent as RectTransform;
        if (cardPanel != null)
            openPosition = cardPanel.anchoredPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            Toggle();
    }

    public void Toggle()
    {
        if (cardPanel == null) return;
        isOpen = !isOpen;
        Vector2 target = isOpen ? openPosition : openPosition + Vector2.down * slideDistance;
        moveTween?.Kill();
        moveTween = cardPanel.DOAnchorPos(target, duration).SetEase(Ease.OutQuad);
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
    }
}