using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ComicCutsceneUI : MonoBehaviour
{
    public enum EndBehaviour
    {
        HidePanelAndResumeGame,
        KeepLastFrameAndStayPaused
    }

    [Serializable]
    public class ComicPage
    {
        public string pageName;

        [Tooltip("Кадры этой страницы. Они будут появляться друг за другом.")]
        public Image[] frames;
    }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Pages")]
    [Tooltip("Страницы комикса. Сначала показываются кадры первой страницы, потом второй и так далее.")]
    [SerializeField] private ComicPage[] pages;

    [Header("Startup")]
    [Tooltip("Включи только для начального комикса. Панель и первый кадр появятся сразу в Awake, до первого кадра игры.")]
    [SerializeField] private bool showFirstFrameImmediatelyOnAwake = false;

    [Tooltip("Если true, при мгновенном показе в Awake игра сразу ставится на паузу.")]
    [SerializeField] private bool pauseImmediatelyOnAwake = true;

    [Header("Input")]
    [SerializeField] private bool allowInput = true;
    [SerializeField] private bool allowSpace = true;
    [SerializeField] private bool allowMouseClick = true;
    [SerializeField] private bool allowEnter = true;
    [Tooltip("Кнопка «Пропустить». Escape тоже пропускает")]
    [SerializeField] private Button skipButton;

    [Header("Auto Play")]
    [SerializeField] private bool autoPlay = false;
    [SerializeField] private float secondsPerFrame = 1.6f;

    [Header("Appearance")]
    [SerializeField] private float panelFadeDuration = 0.25f;
    [SerializeField] private float delayBeforeFirstFrame = 0.15f;
    [SerializeField] private float frameAppearDuration = 0.35f;
    [SerializeField] private float frameStartScale = 0.88f;
    [SerializeField] private float framePunchScale = 0.05f;
    [SerializeField] private Ease appearEase = Ease.OutBack;

    [Header("Pages Behaviour")]
    [Tooltip("Если true, при переходе на новую страницу кадры прошлой страницы выключаются.")]
    [SerializeField] private bool hidePreviousPageOnNextPage = true;

    [Header("Pause Game")]
    [SerializeField] private bool pauseGameWhileShowing = true;

    [Tooltip("Если true, после обычного завершения вернет Time.timeScale к значению до катсцены.")]
    [SerializeField] private bool restorePreviousTimeScale = true;

    [Header("End")]
    [SerializeField] private EndBehaviour endBehaviour = EndBehaviour.HidePanelAndResumeGame;

    [Tooltip("Если true, после последнего кадра нужно ещё раз нажать кнопку, чтобы завершить комикс. Для финального комикса можно выключить.")]
    [SerializeField] private bool waitInputAfterLastFrame = true;

    [Header("Events")]
    public UnityEvent OnCutsceneStarted;
    public UnityEvent OnCutsceneCompleted;

    private int currentPageIndex = -1;
    private int currentFrameIndex = -1;

    private bool playing;
    private bool isPlaying
    {
        get => playing;
        set
        {
            if (playing == value)
                return;
            playing = value;
            playingCount += value ? 1 : -1;
        }
    }

    private bool waitingForInput;
    private bool canAcceptInput;
    private bool preparedFirstFrameOnAwake;
    private bool keepingLastFramePaused;

    private bool previousPausedState;
    private float previousTimeScale = 1f;
    private bool originalWaitInputAfterLastFrame;

    private Action currentOnComplete;

    private Coroutine playRoutine;
    private Tween currentFrameTween;
    private Tween panelTween;

    public bool IsPlaying => isPlaying;

    // Комикс идёт (или только что пропущен по Escape) — Main не должен открывать паузу
    private static int playingCount;
    private static int lastSkipFrame = -1;
    public static bool BlocksPause => playingCount > 0 || Time.frameCount == lastSkipFrame;

    private void Awake()
    {
        originalWaitInputAfterLastFrame = waitInputAfterLastFrame;

        SetupPanel();

        if (skipButton != null)
            skipButton.onClick.AddListener(Skip);

        if (showFirstFrameImmediatelyOnAwake)
            PrepareFirstFrameImmediately();
        else
            HideImmediate();
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Skip();
            return;
        }

        if (!allowInput)
            return;

        if (!canAcceptInput)
            return;

        if (!waitingForInput)
            return;

        if (!InputPressed())
            return;

        Continue();
    }

    public bool IsPanelVisible()
    {
        if (panel == null)
            return false;

        if (!panel.activeInHierarchy)
            return false;

        if (panelCanvasGroup == null)
            return true;

        return panelCanvasGroup.alpha > 0.01f;
    }

    public bool IsKeepingLastFramePaused()
    {
        return keepingLastFramePaused && IsPanelVisible();
    }

    private void SetupPanel()
    {
        if (panel == null)
            panel = gameObject;

        if (panelCanvasGroup == null && panel != null)
        {
            panelCanvasGroup = panel.GetComponent<CanvasGroup>();

            if (panelCanvasGroup == null)
                panelCanvasGroup = panel.AddComponent<CanvasGroup>();
        }
    }

    private void PrepareFirstFrameImmediately()
    {
        if (panel == null)
            return;

        panel.SetActive(true);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = true;
        }

        HideAllPagesImmediate();

        currentPageIndex = 0;
        currentFrameIndex = 0;

        Image firstFrame = GetFrame(0, 0);

        if (firstFrame != null)
            ShowFrameImmediate(firstFrame);

        preparedFirstFrameOnAwake = true;
        keepingLastFramePaused = false;

        if (pauseImmediatelyOnAwake && pauseGameWhileShowing)
        {
            G.IsPaused = true;
            Time.timeScale = 0f;
        }
    }

    public void Play()
    {
        Play(null);
    }

    public void Play(Action onComplete)
    {
        if (isPlaying)
            StopCutscene(false);

        currentOnComplete = onComplete;
        waitInputAfterLastFrame = originalWaitInputAfterLastFrame;
        keepingLastFramePaused = false;

        playRoutine = StartCoroutine(PlayRoutine());
    }

    public void StopCutscene(bool invokeCompleteEvent = true)
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        KillTweens();

        isPlaying = false;
        waitingForInput = false;
        canAcceptInput = false;
        preparedFirstFrameOnAwake = false;
        keepingLastFramePaused = false;

        if (endBehaviour == EndBehaviour.HidePanelAndResumeGame)
        {
            RestoreGamePauseState();
            HideImmediate();
        }

        if (invokeCompleteEvent)
            OnCutsceneCompleted?.Invoke();

        currentOnComplete = null;
    }

    private IEnumerator PlayRoutine()
    {
        if (panel == null)
            yield break;

        isPlaying = true;
        waitingForInput = false;
        canAcceptInput = false;
        keepingLastFramePaused = false;

        PauseGame();

        panel.SetActive(true);

        if (preparedFirstFrameOnAwake)
        {
            currentPageIndex = 0;
            currentFrameIndex = 0;

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.blocksRaycasts = true;
                panelCanvasGroup.interactable = true;
            }

            preparedFirstFrameOnAwake = false;

            OnCutsceneStarted?.Invoke();

            waitingForInput = true;
            canAcceptInput = true;

            yield break;
        }

        currentPageIndex = -1;
        currentFrameIndex = -1;

        HideAllPagesImmediate();

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = true;

            panelTween = panelCanvasGroup
                .DOFade(1f, panelFadeDuration)
                .SetUpdate(true);

            // Не WaitForCompletion: после автокилла твин уходит в пул, и ожидание может зависнуть
            yield return new WaitForSecondsRealtime(panelFadeDuration);
        }

        OnCutsceneStarted?.Invoke();

        if (delayBeforeFirstFrame > 0f)
            yield return new WaitForSecondsRealtime(delayBeforeFirstFrame);

        ShowNextPage();

        if (autoPlay)
        {
            while (isPlaying && HasNextFrameOrPage())
            {
                yield return new WaitForSecondsRealtime(secondsPerFrame);
                Continue();
            }

            if (isPlaying)
            {
                if (waitInputAfterLastFrame)
                {
                    waitingForInput = true;
                    canAcceptInput = true;
                }
                else
                {
                    yield return new WaitForSecondsRealtime(secondsPerFrame);
                    CompleteCutscene();
                }
            }
        }
        else
        {
            waitingForInput = true;
            canAcceptInput = true;
        }
    }

    // Пропустить комикс целиком: завершить как обычно, с колбэком
    public void Skip()
    {
        if (!isPlaying)
            return;

        lastSkipFrame = Time.frameCount;

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        CompleteCutscene();
    }

    private void Continue()
    {
        if (!isPlaying)
            return;

        if (HasNextFrameOnCurrentPage())
        {
            ShowNextFrameOnCurrentPage();
            return;
        }

        if (HasNextPage())
        {
            ShowNextPage();
            return;
        }

        if (waitInputAfterLastFrame)
        {
            waitInputAfterLastFrame = false;
            return;
        }

        CompleteCutscene();
    }

    private bool HasNextFrameOrPage()
    {
        return HasNextFrameOnCurrentPage() || HasNextPage();
    }

    private bool HasNextFrameOnCurrentPage()
    {
        ComicPage page = GetCurrentPage();

        if (page == null || page.frames == null)
            return false;

        return currentFrameIndex < page.frames.Length - 1;
    }

    private bool HasNextPage()
    {
        if (pages == null)
            return false;

        return currentPageIndex < pages.Length - 1;
    }

    private ComicPage GetCurrentPage()
    {
        if (pages == null)
            return null;

        if (currentPageIndex < 0 || currentPageIndex >= pages.Length)
            return null;

        return pages[currentPageIndex];
    }

    private Image GetFrame(int pageIndex, int frameIndex)
    {
        if (pages == null)
            return null;

        if (pageIndex < 0 || pageIndex >= pages.Length)
            return null;

        ComicPage page = pages[pageIndex];

        if (page == null || page.frames == null)
            return null;

        if (frameIndex < 0 || frameIndex >= page.frames.Length)
            return null;

        return page.frames[frameIndex];
    }

    private void ShowNextPage()
    {
        if (pages == null || pages.Length == 0)
        {
            CompleteCutscene();
            return;
        }

        if (hidePreviousPageOnNextPage && currentPageIndex >= 0)
            HidePageImmediate(currentPageIndex);

        currentPageIndex++;
        currentFrameIndex = -1;

        if (currentPageIndex < 0 || currentPageIndex >= pages.Length)
        {
            CompleteCutscene();
            return;
        }

        HidePageImmediate(currentPageIndex);
        ShowNextFrameOnCurrentPage();
    }

    private void ShowNextFrameOnCurrentPage()
    {
        ComicPage page = GetCurrentPage();

        if (page == null || page.frames == null || page.frames.Length == 0)
        {
            if (HasNextPage())
                ShowNextPage();
            else
                CompleteCutscene();

            return;
        }

        currentFrameIndex++;

        if (currentFrameIndex < 0 || currentFrameIndex >= page.frames.Length)
        {
            if (HasNextPage())
                ShowNextPage();
            else
                CompleteCutscene();

            return;
        }

        Image frame = page.frames[currentFrameIndex];

        if (frame == null)
            return;

        ShowFrame(frame);
    }

    private void ShowFrame(Image frame)
    {
        RectTransform rect = frame.GetComponent<RectTransform>();

        frame.gameObject.SetActive(true);
        frame.raycastTarget = false;

        Color color = frame.color;
        color.a = 0f;
        frame.color = color;

        if (rect != null)
            rect.localScale = Vector3.one * frameStartScale;

        currentFrameTween?.Kill();

        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        sequence.Append(
            frame
                .DOFade(1f, frameAppearDuration)
                .SetEase(Ease.OutQuad)
        );

        if (rect != null)
        {
            sequence.Join(
                rect
                    .DOScale(1f, frameAppearDuration)
                    .SetEase(appearEase)
            );

            if (framePunchScale > 0f)
            {
                sequence.Append(
                    rect.DOPunchScale(
                        Vector3.one * framePunchScale,
                        0.18f,
                        6,
                        0.7f
                    )
                );
            }
        }

        currentFrameTween = sequence;
    }

    private void ShowFrameImmediate(Image frame)
    {
        if (frame == null)
            return;

        RectTransform rect = frame.GetComponent<RectTransform>();

        frame.gameObject.SetActive(true);
        frame.raycastTarget = false;

        Color color = frame.color;
        color.a = 1f;
        frame.color = color;

        if (rect != null)
            rect.localScale = Vector3.one;
    }

    private void CompleteCutscene()
    {
        if (!isPlaying)
            return;

        isPlaying = false;
        waitingForInput = false;
        canAcceptInput = false;

        if (endBehaviour == EndBehaviour.KeepLastFrameAndStayPaused)
        {
            KeepPanelVisibleAndPaused();

            OnCutsceneCompleted?.Invoke();
            currentOnComplete?.Invoke();
            currentOnComplete = null;

            return;
        }

        StartCoroutine(HidePanelRoutine());
    }

    private IEnumerator HidePanelRoutine()
    {
        KillTweens();

        RestoreGamePauseState();

        if (panelCanvasGroup != null)
        {
            panelTween = panelCanvasGroup
                .DOFade(0f, panelFadeDuration)
                .SetUpdate(true);

            // Не WaitForCompletion: после автокилла твин уходит в пул, и ожидание может зависнуть
            yield return new WaitForSecondsRealtime(panelFadeDuration);
        }

        HideImmediate();

        OnCutsceneCompleted?.Invoke();
        currentOnComplete?.Invoke();
        currentOnComplete = null;
    }

    private void KeepPanelVisibleAndPaused()
    {
        KillTweens();

        keepingLastFramePaused = true;

        if (panel != null)
            panel.SetActive(true);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = true;
        }

        if (pauseGameWhileShowing)
        {
            G.IsPaused = true;
            Time.timeScale = 0f;
        }
    }

    private bool InputPressed()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (allowSpace && keyboard.spaceKey.wasPressedThisFrame)
                return true;

            if (allowEnter &&
                (keyboard.enterKey.wasPressedThisFrame ||
                 keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                return true;
            }
        }

        Mouse mouse = Mouse.current;

        if (allowMouseClick && mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;

        return false;
    }

    private void PauseGame()
    {
        if (!pauseGameWhileShowing)
            return;

        previousPausedState = G.IsPaused;
        previousTimeScale = Time.timeScale;

        G.IsPaused = true;
        Time.timeScale = 0f;
    }

    private void RestoreGamePauseState()
    {
        if (!pauseGameWhileShowing)
            return;

        G.IsPaused = previousPausedState;
        Time.timeScale = restorePreviousTimeScale ? previousTimeScale : 1f;
    }

    private void HideImmediate()
    {
        keepingLastFramePaused = false;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable = false;
        }

        HideAllPagesImmediate();

        if (panel != null)
            panel.SetActive(false);
    }

    private void HideAllPagesImmediate()
    {
        if (pages == null)
            return;

        for (int i = 0; i < pages.Length; i++)
            HidePageImmediate(i);
    }

    private void HidePageImmediate(int pageIndex)
    {
        if (pages == null)
            return;

        if (pageIndex < 0 || pageIndex >= pages.Length)
            return;

        ComicPage page = pages[pageIndex];

        if (page == null || page.frames == null)
            return;

        foreach (Image frame in page.frames)
            HideFrameImmediate(frame);
    }

    private void HideFrameImmediate(Image frame)
    {
        if (frame == null)
            return;

        Color color = frame.color;
        color.a = 0f;
        frame.color = color;

        RectTransform rect = frame.GetComponent<RectTransform>();

        if (rect != null)
            rect.localScale = Vector3.one;

        frame.gameObject.SetActive(false);
    }

    private void KillTweens()
    {
        currentFrameTween?.Kill();
        panelTween?.Kill();

        currentFrameTween = null;
        panelTween = null;
    }

    private void OnDisable()
    {
        if (isPlaying)
            StopCutscene(false);
    }

    private void OnDestroy()
    {
        KillTweens();

        if (isPlaying && endBehaviour == EndBehaviour.HidePanelAndResumeGame)
            RestoreGamePauseState();
    }
}