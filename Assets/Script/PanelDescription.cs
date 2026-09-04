using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PanelDescription : MonoBehaviour
{
    public TextManager textManager;
    public string PanelName;
    [TextArea] public string Description;
    public GameObject arrow;


    private static PanelDescription activePanel;
    private RectTransform panelRect;
    private RectTransform arrowRect;
    private Vector2 arrowRestingPosition;
    private Tween arrowTween;
    private Coroutine showCoroutine;

    private void Awake()
    {
        panelRect = transform as RectTransform;
        arrowRect = arrow != null ? arrow.transform as RectTransform : null;
        if (arrowRect != null)
            arrowRestingPosition = arrowRect.anchoredPosition;
        arrow?.SetActive(false);
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        bool leftPressed = Mouse.current.leftButton.wasPressedThisFrame;
        bool rightPressed = Mouse.current.rightButton.wasPressedThisFrame;
        bool middlePressed = Mouse.current.middleButton.wasPressedThisFrame;
        if (!leftPressed && !rightPressed && !middlePressed)
            return;

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        bool pointerIsOverPanel = IsPointerOverPanel(pointerPosition);

        if (activePanel == this && !pointerIsOverPanel)
            HideDescription();

        if (rightPressed && pointerIsOverPanel &&
            !IsPointerOverCard(pointerPosition) &&
            !IsPointerOverNestedPanel(pointerPosition))
        {
            if (activePanel == this)
            {
                HideDescription();
                return;
            }

            if (showCoroutine != null)
                StopCoroutine(showCoroutine);
            showCoroutine = StartCoroutine(ShowDescriptionAtEndOfFrame());
        }
    }

    private IEnumerator ShowDescriptionAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        showCoroutine = null;

        if (activePanel != null && activePanel != this)
            activePanel.HideDescription();

        string details = string.IsNullOrWhiteSpace(PanelName)
            ? Description
            : string.IsNullOrWhiteSpace(Description)
                ? PanelName
                : PanelName + "\n\n" + Description;

        if (string.IsNullOrWhiteSpace(details))
            yield break;

        activePanel = this;
        arrow?.SetActive(true);
        StartArrowFloat();
        textManager?.ShowDescription(details);
    }

    private void HideDescription()
    {
        StopArrowFloat();
        arrow?.SetActive(false);

        if (activePanel != this)
            return;

        activePanel = null;
        textManager?.HideDescription();
    }

    private void StartArrowFloat()
    {
        if (arrowRect == null)
            return;

        arrowTween?.Kill();
        arrowRect.anchoredPosition = arrowRestingPosition;
        arrowTween = arrowRect
            .DOAnchorPosY(arrowRestingPosition.y + 6f, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopArrowFloat()
    {
        arrowTween?.Kill();
        arrowTween = null;
        if (arrowRect != null)
            arrowRect.anchoredPosition = arrowRestingPosition;
    }

    private bool IsPointerOverPanel(Vector2 screenPosition)
    {
        if (panelRect == null)
            return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, eventCamera);
    }

    private bool IsPointerOverCard(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject.GetComponentInParent<Card>() != null)
                return true;
        }

        return false;
    }

    private bool IsPointerOverNestedPanel(Vector2 screenPosition)
    {
        PanelDescription[] nestedPanels = GetComponentsInChildren<PanelDescription>(false);
        for (int i = 0; i < nestedPanels.Length; i++)
        {
            PanelDescription nestedPanel = nestedPanels[i];
            if (nestedPanel != this && nestedPanel.IsPointerOverPanel(screenPosition))
                return true;
        }

        return false;
    }

    private void OnDisable()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        HideDescription();
    }

    private void OnDestroy()
    {
        arrowTween?.Kill();
    }

}
