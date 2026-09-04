using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class ForgePanelUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private float moveRightDistance = 420f;
    [SerializeField, Min(0f)] private float moveDuration = 0.25f;

    private RectTransform panelRect;
    private Vector2 closedPosition;
    private Tween moveTween;
    private bool isOpen;

    private void Awake()
    {
        panelRect = transform as RectTransform;
        if (panelRect != null)
            closedPosition = panelRect.anchoredPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || panelRect == null)
            return;

        isOpen = !isOpen;
        float targetX = closedPosition.x + (isOpen ? moveRightDistance : 0f);
        moveTween?.Kill();
        moveTween = panelRect.DOAnchorPosX(targetX, moveDuration)
            .SetEase(Ease.OutQuad);
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
    }
}
