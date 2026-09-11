using UnityEngine;
using UnityEngine.EventSystems;

public class HintPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform panelRect;
    private Canvas parentCanvas;
    private bool isDragging;

    private void Awake()
    {
        panelRect = transform as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || panelRect == null)
            return;

        float scaleFactor = parentCanvas != null ? parentCanvas.scaleFactor : 1f;
        panelRect.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            isDragging = false;
    }

}
