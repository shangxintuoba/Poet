using System.Collections;
using DG.Tweening;
using UnityEngine;

public class SettingPanel : MonoBehaviour
{
    public GameObject StartButton;
    public GameObject RestartButton;
    public GameObject QuitButton;
    public GameObject BackButton;
    public GameObject Title;
    public GameObject ToggleButton;
    private bool Opened;

    public GameObject Typer;
    public GameObject CardPanel;
    public GameObject MissionPanel;
    public GameObject ForgePanel;
    public GameObject MapPanel;
    public GameObject StartScene;
    public GameObject InitialTest;
    public GameObject StartSceneBg;
    [SerializeField] private TextPanelUI textPanelUI;

    [SerializeField, Min(0f)] private float otherUIHideDistance = 650f;
    [SerializeField, Min(0f)] private float otherUIMoveDuration = 0.25f;
    [SerializeField, Min(0f)] private float initialTestMoveDistance = 700f;
    [SerializeField, Min(0f)] private float initialTestMoveDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float startSceneBackgroundSpeed = 20f;

    private Vector2 typerPositionBeforeSettings;
    private Vector2 cardPanelPositionBeforeSettings;
    private Vector2 missionPanelPositionBeforeSettings;
    private Vector2 forgePanelPositionBeforeSettings;
    private Tween typerTween;
    private Tween cardPanelTween;
    private Tween missionPanelTween;
    private Tween forgePanelTween;
    private bool hasRecordedOtherUIPositions;
    private bool restoreExpandedTextPanel;
    private Coroutine otherUIRoutine;
    private Vector2 initialTestRestingPosition;
    private Vector2 startSceneBackgroundPosition;
    private bool hasInitialTestRestingPosition;
    private bool hasStartSceneBackgroundPosition;
    private Tween initialTestTween;
    private Tween startSceneBackgroundTween;

    private void Start()
    {
        StartSceneBackGroundScroll();
    }


    public void TogglePanel()
    {
        Opened = !Opened;
        RestartButton.SetActive(Opened);
        QuitButton.SetActive(Opened);
        BackButton.SetActive(Opened);
        ToggleButton.SetActive(!Opened);
        ShowOtherUI();
    }

    public void ShowOtherUI()
    {
        if (otherUIRoutine != null)
            StopCoroutine(otherUIRoutine);

        typerTween?.Kill();
        cardPanelTween?.Kill();
        missionPanelTween?.Kill();
        forgePanelTween?.Kill();

        otherUIRoutine = StartCoroutine(MoveOtherUI());
    }

    private IEnumerator MoveOtherUI()
    {
        RectTransform typerRect = Typer.GetComponent<RectTransform>();
        RectTransform cardPanelRect = CardPanel.GetComponent<RectTransform>();
        RectTransform missionPanelRect = MissionPanel.GetComponent<RectTransform>();
        RectTransform forgePanelRect = ForgePanel.GetComponent<RectTransform>();

        if (Opened)
        {
            if (!hasRecordedOtherUIPositions)
            {
                typerPositionBeforeSettings = typerRect.anchoredPosition;
                cardPanelPositionBeforeSettings = cardPanelRect.anchoredPosition;
                missionPanelPositionBeforeSettings = missionPanelRect.anchoredPosition;
                forgePanelPositionBeforeSettings = forgePanelRect.anchoredPosition;
                restoreExpandedTextPanel = textPanelUI.HasDisplayedText && textPanelUI.IsExpanded;
                hasRecordedOtherUIPositions = true;
            }

            if (restoreExpandedTextPanel && textPanelUI.IsExpanded)
                textPanelUI.SetExpandedWithoutClearingText(false);

            typerTween = typerRect
                .DOAnchorPos(typerPositionBeforeSettings + Vector2.down * otherUIHideDistance,
                    otherUIMoveDuration)
                .SetEase(Ease.OutQuad);
            cardPanelTween = cardPanelRect
                .DOAnchorPos(cardPanelPositionBeforeSettings + Vector2.down * otherUIHideDistance,
                    otherUIMoveDuration)
                .SetEase(Ease.OutQuad);
            missionPanelTween = missionPanelRect
                .DOAnchorPos(missionPanelPositionBeforeSettings + Vector2.up * otherUIHideDistance,
                    otherUIMoveDuration)
                .SetEase(Ease.OutQuad);
            forgePanelTween = forgePanelRect
                .DOAnchorPos(forgePanelPositionBeforeSettings + Vector2.left * otherUIHideDistance,
                    otherUIMoveDuration)
                .SetEase(Ease.OutQuad);
            MapPanel.SetActive(false);
        }
        else
        {
            typerTween = typerRect
                .DOAnchorPos(typerPositionBeforeSettings, otherUIMoveDuration)
                .SetEase(Ease.OutQuad);
            cardPanelTween = cardPanelRect
                .DOAnchorPos(cardPanelPositionBeforeSettings, otherUIMoveDuration)
                .SetEase(Ease.OutQuad);
            missionPanelTween = missionPanelRect
                .DOAnchorPos(missionPanelPositionBeforeSettings, otherUIMoveDuration)
                .SetEase(Ease.OutQuad);
            forgePanelTween = forgePanelRect
                .DOAnchorPos(forgePanelPositionBeforeSettings, otherUIMoveDuration)
                .SetEase(Ease.OutQuad);
            MapPanel.SetActive(true);

            yield return new WaitForSeconds(otherUIMoveDuration);

            if (Opened)
                yield break;

            if (restoreExpandedTextPanel)
                textPanelUI.SetExpandedWithoutClearingText(true);

            hasRecordedOtherUIPositions = false;
        }

        otherUIRoutine = null;
    }

    private void OnDestroy()
    {
        typerTween?.Kill();
        cardPanelTween?.Kill();
        missionPanelTween?.Kill();
        forgePanelTween?.Kill();
        initialTestTween?.Kill();
        startSceneBackgroundTween?.Kill();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Restart()
    {
        GameManager.Instance.RestartGame();
    }

    public void Startgame()
    {
        startSceneBackgroundTween?.Kill();
        StartScene.SetActive(false);
        ShowInitialTest();
    }

    public void ShowInitialTest()
    {
        RectTransform initialTestRect = InitialTest.GetComponent<RectTransform>();
        if (!hasInitialTestRestingPosition)
        {
            initialTestRestingPosition = initialTestRect.anchoredPosition;
            hasInitialTestRestingPosition = true;
        }

        initialTestTween?.Kill();
        initialTestRect.anchoredPosition = initialTestRestingPosition + Vector2.up * initialTestMoveDistance;
        InitialTest.SetActive(true);
        initialTestTween = initialTestRect
            .DOAnchorPos(initialTestRestingPosition, initialTestMoveDuration)
            .SetEase(Ease.OutQuad);
    }

    public void StartSceneBackGroundScroll()
    {
        RectTransform backgroundRect = StartSceneBg.GetComponent<RectTransform>();
        if (!hasStartSceneBackgroundPosition)
        {
            startSceneBackgroundPosition = backgroundRect.anchoredPosition;
            hasStartSceneBackgroundPosition = true;
        }

        float repeatDistance = StartSceneBg.transform.GetChild(0).GetComponent<RectTransform>().rect.width;
        float duration = repeatDistance / startSceneBackgroundSpeed;

        startSceneBackgroundTween?.Kill();
        backgroundRect.anchoredPosition = startSceneBackgroundPosition;
        startSceneBackgroundTween = backgroundRect
            .DOAnchorPosX(startSceneBackgroundPosition.x - repeatDistance, duration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

}
