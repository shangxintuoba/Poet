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
    public GameObject DefaultBg;
    [SerializeField] private TextPanelUI textPanelUI;
    [SerializeField] private AudioManager audioManager;

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
    private Vector2 defaultBackgroundPosition;
    private bool hasInitialTestRestingPosition;
    private bool hasStartSceneBackgroundPosition;
    private bool hasDefaultBackgroundPosition;
    private Tween initialTestTween;
    private Tween startSceneBackgroundTween;
    private Tween defaultBackgroundTween;
    private Coroutine initialTestCompletionRoutine;
    private bool mapAwaitingInitialCardReveal;

    private void Start()
    {
        StartSceneBackGroundScroll();
        ConstantBgScrolliing();
        ResolveAudioManager();
        UpdateMainMusicState();
    }

    private void Update()
    {
        UpdateMainMusicState();
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
            MapPanel.SetActive(!mapAwaitingInitialCardReveal);

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
        defaultBackgroundTween?.Kill();
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
        // Keep the start panel visible behind the initial test until the test finishes.
        mapAwaitingInitialCardReveal = false;
        CollapseGameplayUIForInitialTest();
        ShowInitialTest();
        UpdateMainMusicState();
    }

    /// <summary>Finishes the initial-test transition and reveals the gameplay UI.</summary>
    public void CompleteInitialTest()
    {
        if (initialTestCompletionRoutine != null)
            return;

        initialTestCompletionRoutine = StartCoroutine(CompleteInitialTestRoutine());
    }

    /// <summary>Restores the map when the deferred initial cards are shown.</summary>
    public void ShowMapPanelAfterInitialCards()
    {
        mapAwaitingInitialCardReveal = false;
        MapPanel?.SetActive(true);
    }

    private IEnumerator CompleteInitialTestRoutine()
    {
        initialTestTween?.Kill();

        if (InitialTest != null)
        {
            RectTransform initialTestRect = InitialTest.GetComponent<RectTransform>();
            if (initialTestRect != null)
            {
                Vector2 targetPosition = initialTestRestingPosition + Vector2.up * initialTestMoveDistance;
                initialTestTween = initialTestRect
                    .DOAnchorPos(targetPosition, initialTestMoveDuration)
                    .SetEase(Ease.InQuad);
                yield return initialTestTween.WaitForCompletion();
            }

            InitialTest.SetActive(false);
        }

        StartScene?.SetActive(false);

        mapAwaitingInitialCardReveal = true;
        ExpandGameplayUIAfterInitialTest();

        yield return new WaitForSecondsRealtime(otherUIMoveDuration);

        initialTestCompletionRoutine = null;
        UpdateMainMusicState();
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

    private void CollapseGameplayUIForInitialTest()
    {
        if (otherUIRoutine != null)
            StopCoroutine(otherUIRoutine);

        RectTransform typerRect = Typer.GetComponent<RectTransform>();
        RectTransform cardPanelRect = CardPanel.GetComponent<RectTransform>();
        RectTransform missionPanelRect = MissionPanel.GetComponent<RectTransform>();
        RectTransform forgePanelRect = ForgePanel.GetComponent<RectTransform>();

        if (!hasRecordedOtherUIPositions)
        {
            typerPositionBeforeSettings = typerRect.anchoredPosition;
            cardPanelPositionBeforeSettings = cardPanelRect.anchoredPosition;
            missionPanelPositionBeforeSettings = missionPanelRect.anchoredPosition;
            forgePanelPositionBeforeSettings = forgePanelRect.anchoredPosition;
            restoreExpandedTextPanel = textPanelUI != null && textPanelUI.HasDisplayedText && textPanelUI.IsExpanded;
            hasRecordedOtherUIPositions = true;
        }

        if (restoreExpandedTextPanel && textPanelUI != null && textPanelUI.IsExpanded)
            textPanelUI.SetExpandedWithoutClearingText(false);

        typerTween?.Kill();
        cardPanelTween?.Kill();
        missionPanelTween?.Kill();
        forgePanelTween?.Kill();

        typerTween = typerRect.DOAnchorPos(typerPositionBeforeSettings + Vector2.down * otherUIHideDistance,
            otherUIMoveDuration).SetEase(Ease.OutQuad);
        cardPanelTween = cardPanelRect.DOAnchorPos(cardPanelPositionBeforeSettings + Vector2.down * otherUIHideDistance,
            otherUIMoveDuration).SetEase(Ease.OutQuad);
        missionPanelTween = missionPanelRect.DOAnchorPos(missionPanelPositionBeforeSettings + Vector2.up * otherUIHideDistance,
            otherUIMoveDuration).SetEase(Ease.OutQuad);
        forgePanelTween = forgePanelRect.DOAnchorPos(forgePanelPositionBeforeSettings + Vector2.left * otherUIHideDistance,
            otherUIMoveDuration).SetEase(Ease.OutQuad);
        MapPanel.SetActive(false);
    }

    private void ExpandGameplayUIAfterInitialTest()
    {
        if (!hasRecordedOtherUIPositions)
            return;

        typerTween?.Kill();
        cardPanelTween?.Kill();
        missionPanelTween?.Kill();
        forgePanelTween?.Kill();

        typerTween = Typer.GetComponent<RectTransform>().DOAnchorPos(typerPositionBeforeSettings,
            otherUIMoveDuration).SetEase(Ease.OutQuad);
        cardPanelTween = CardPanel.GetComponent<RectTransform>().DOAnchorPos(cardPanelPositionBeforeSettings,
            otherUIMoveDuration).SetEase(Ease.OutQuad);
        missionPanelTween = MissionPanel.GetComponent<RectTransform>().DOAnchorPos(missionPanelPositionBeforeSettings,
            otherUIMoveDuration).SetEase(Ease.OutQuad);
        forgePanelTween = ForgePanel.GetComponent<RectTransform>().DOAnchorPos(forgePanelPositionBeforeSettings,
            otherUIMoveDuration).SetEase(Ease.OutQuad);
        MapPanel.SetActive(false);

        if (restoreExpandedTextPanel && textPanelUI != null)
            textPanelUI.SetExpandedWithoutClearingText(true);

        hasRecordedOtherUIPositions = false;
        restoreExpandedTextPanel = false;
    }

    private void ResolveAudioManager()
    {
        if (audioManager == null)
            audioManager = FindFirstObjectByType<AudioManager>();
    }

    private void UpdateMainMusicState()
    {
        ResolveAudioManager();
        if (audioManager == null)
            return;

        bool shouldPlayMain = (StartScene != null && StartScene.activeInHierarchy) ||
                              (InitialTest != null && InitialTest.activeInHierarchy);
        audioManager.SetMainMusicActive(shouldPlayMain);
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


    public void ConstantBgScrolliing()
    {
        if (DefaultBg == null)
            return;

        RectTransform backgroundRect = DefaultBg.GetComponent<RectTransform>();
        if (backgroundRect == null || DefaultBg.transform.childCount == 0)
            return;

        if (!hasDefaultBackgroundPosition)
        {
            defaultBackgroundPosition = backgroundRect.anchoredPosition;
            hasDefaultBackgroundPosition = true;
        }

        RectTransform repeatingChild = DefaultBg.transform.GetChild(0).GetComponent<RectTransform>();
        if (repeatingChild == null)
            return;

        float repeatDistance = repeatingChild.rect.width;
        float duration = repeatDistance / startSceneBackgroundSpeed;

        defaultBackgroundTween?.Kill();
        backgroundRect.anchoredPosition = defaultBackgroundPosition;
        defaultBackgroundTween = backgroundRect
            .DOAnchorPosX(defaultBackgroundPosition.x - repeatDistance, duration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }
}
