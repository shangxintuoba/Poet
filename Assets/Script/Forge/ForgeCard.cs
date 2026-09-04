using DG.Tweening;
using UnityEngine;

public class ForgeCard : Card
{
    public Forge ForgePanel;
    [SerializeField] private float moveRightDistance = 420f;
    [SerializeField, Min(0f)] private float moveDuration = 0.25f;

    private Tween moveTween;
    private Vector2 forgePanelStartPosition;
    private bool hasStoredForgePanelStartPosition;

    protected override void ShowCardDetails()
    {
        base.ShowCardDetails();

        if (ForgePanel == null)
            ForgePanel = FindFirstObjectByType<Forge>();

        RectTransform panel = ForgePanel != null ? ForgePanel.transform as RectTransform : null;
        if (panel == null)
            return;

        if (!hasStoredForgePanelStartPosition)
        {
            forgePanelStartPosition = panel.anchoredPosition;
            hasStoredForgePanelStartPosition = true;
        }

        moveTween?.Kill();
        moveTween = panel.DOAnchorPosX(forgePanelStartPosition.x + moveRightDistance, moveDuration)
            .SetEase(Ease.OutQuad);
    }

    protected override void OnCardDeselected()
    {
        if (!hasStoredForgePanelStartPosition || ForgePanel == null)
            return;

        RectTransform panel = ForgePanel.transform as RectTransform;
        if (panel == null)
            return;

        moveTween?.Kill();
        moveTween = panel.DOAnchorPosX(forgePanelStartPosition.x, moveDuration)
            .SetEase(Ease.OutQuad);
    }
}
