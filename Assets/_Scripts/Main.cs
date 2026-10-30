using System;
using GridSkillTree;
using UnityEngine;
using UnityEngine.InputSystem;

// Фазы игрового цикла: смена → отчёт → книга → смена...
public enum GamePhase
{
    Shift,
    Report,
    Book,
    Ending
}

public class Main : MonoBehaviour
{
    [SerializeField] private SkillTreeRuntime tree;
    [SerializeField] private CurrencyWallet wallet;
    [Tooltip("Комикс перед первой ночью новой игры")]
    [SerializeField] private ComicCutsceneUI introComic;

    public GamePhase Phase { get; private set; }

    public event Action<GamePhase> OnPhaseChanged;

    private void Awake()
    {
        G.main = this;
        G.config = GameConfig.Instance;
        G.tree = tree != null ? tree : FindAnyObjectByType<SkillTreeRuntime>();
        G.wallet = wallet != null ? wallet : FindAnyObjectByType<CurrencyWallet>();
    }

    private void Start()
    {
        G.shift.OnShiftEnded += HandleShiftEnded;

        if (SaveData.NewGameRequested)
        {
            SaveData.NewGameRequested = false;
            SaveData.ResetAll();
        }

        StartGame();
    }

    // Новая игра начинается с комикса, продолжение — сразу со смены
    private void StartGame()
    {
        if (!SaveData.IntroSeen && introComic != null)
        {
            introComic.Play(() =>
            {
                SaveData.IntroSeen = true;
                BeginNight();
            });
            return;
        }

        BeginNight();
    }

    private void OnDestroy()
    {
        if (G.shift != null)
            G.shift.OnShiftEnded -= HandleShiftEnded;
    }

    private void HandleShiftEnded(ShiftScore score)
    {
        // После финальной смены вместо отчёта концовка
        SetPhase(G.stats.finalShift ? GamePhase.Ending : GamePhase.Report);
    }

    public void OpenBook()
    {
        SetPhase(GamePhase.Book);
    }

    private void SetPhase(GamePhase phase)
    {
        Phase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
            && !ComicCutsceneUI.BlocksPause && Phase != GamePhase.Ending)
        {
            TogglePause();
        }
    }

    // Пересобрать статы из книги и начать смену
    public void BeginNight(bool forceFinal = false)
    {
        EnsureRootNode();
        G.stats = GameStats.Build(G.tree, G.config, forceFinal);
        G.shift.StartShift();
        SetPhase(GamePhase.Shift);
    }

    private void EnsureRootNode()
    {
        SkillNodeData root = G.tree.TreeData.GetNodeById(G.config.rootNodeId);
        if (root != null && G.tree.GetLevel(root.id) == 0)
            G.tree.Buy(root);
    }

    public void NewGame()
    {
        SaveData.ResetAll();
        StartGame();
    }

    private void TogglePause()
    {
        SetPause(!G.IsPaused);
    }

    private void SetPause(bool paused)
    {
        G.IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (G.ui != null)
            G.ui.SetPausePanel(paused);
    }

    public void ResumeGame()
    {
        SetPause(false);
    }
}
