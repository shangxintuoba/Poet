using DG.Tweening;
using Ink.UnityIntegration;
using UnityEngine;
using UnityEngine.EventSystems;

public class Node : MonoBehaviour, IPointerClickHandler
{
    public bool isUnlocked;
    public string NodeName;
    public Node[] NearbyNodes;
    [SerializeField] private InkFile inkFile;
    [SerializeField, Min(1f)] private float currentScale = 1.08f;
    [SerializeField, Min(0f)] private float scaleDuration = 0.12f;
    [SerializeField] private RectTransform shadow;
    [SerializeField] private Vector2 currentShadowOffset = new Vector2(-6f, -6f);

    private Vector3 restingScale;
    private Vector2 restingShadowPosition;
    private Tween scaleTween;
    private Tween shadowTween;

    public InkFile InkFile => inkFile;

    private void Awake()
    {
        restingScale = transform.localScale;
        if (shadow == null)
            shadow = transform.Find("Shadow") as RectTransform;
        if (shadow != null)
            restingShadowPosition = shadow.anchoredPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            GetComponentInParent<Map>()?.TryGoTo(this);
    }

    public void SetCurrent(bool current)
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(current ? restingScale * currentScale : restingScale, scaleDuration)
            .SetEase(Ease.OutQuad);

        if (shadow != null)
        {
            shadowTween?.Kill();
            shadowTween = shadow.DOAnchorPos(
                current ? restingShadowPosition + currentShadowOffset : restingShadowPosition,
                scaleDuration).SetEase(Ease.OutQuad);
        }
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
        shadowTween?.Kill();
    }
}