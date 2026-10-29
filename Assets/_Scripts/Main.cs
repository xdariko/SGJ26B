using System;
using GridSkillTree;
using UnityEngine;
using UnityEngine.InputSystem;

// Фазы игрового цикла: смена → отчёт → книга → смена...
public enum GamePhase
{
    Shift,
    Report,
    Book
}

public class Main : MonoBehaviour
{
    [SerializeField] private SkillTreeRuntime tree;
    [SerializeField] private CurrencyWallet wallet;

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
        BeginNight();
    }

    private void OnDestroy()
    {
        if (G.shift != null)
            G.shift.OnShiftEnded -= HandleShiftEnded;
    }

    private void HandleShiftEnded(ShiftScore score)
    {
        SetPhase(GamePhase.Report);
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
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    // Пересобрать статы из книги и начать смену
    public void BeginNight()
    {
        EnsureRootNode();
        G.stats = GameStats.Build(G.tree, G.config);
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
        BeginNight();
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
