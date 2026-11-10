using GridSkillTree;
using UnityEngine;

// Звуки сцены Game: слушает события логики и только играет звук. Создаётся AudioHub'ом при загрузке сцены.
public class GameAudio : MonoBehaviour
{
    [Tooltip("За сколько секунд до конца смены тикают часы")]
    [SerializeField] private int tickSeconds = 5;
    [Tooltip("Насколько повышается тон снятия на каждый шаг серии")]
    [SerializeField] private float comboPitchStep = 0.03f;
    [SerializeField] private float comboPitchMax = 1.5f;

    private int lastTickSecond = -1;
    private bool subscribed;

    private void Start()
    {
        if (G.shift == null || G.main == null)
            return;

        G.shift.OnShiftStarted += HandleShiftStarted;
        G.shift.OnShiftEnded += HandleShiftEnded;
        G.shift.OnParcelResolved += HandleResolved;
        G.main.OnPhaseChanged += HandlePhase;
        if (G.familiars != null)
            G.familiars.OnPounce += HandlePounce;
        if (G.tree != null)
            G.tree.OnNodePurchased += HandlePurchased;
        if (G.faults != null)
        {
            G.faults.OnBroken += HandleMonitorBroken;
            G.faults.OnHit += HandleMonitorHit;
            G.faults.OnRepaired += HandleMonitorRepaired;
        }
        subscribed = true;

        // Смена могла начаться раньше нашего Start (Main.Start идёт первым)
        if (G.shift.Current == ShiftController.State.Running)
            HandleShiftStarted();
    }

    private void OnDestroy()
    {
        if (!subscribed)
            return;

        if (G.shift != null)
        {
            G.shift.OnShiftStarted -= HandleShiftStarted;
            G.shift.OnShiftEnded -= HandleShiftEnded;
            G.shift.OnParcelResolved -= HandleResolved;
        }
        if (G.main != null)
            G.main.OnPhaseChanged -= HandlePhase;
        if (G.familiars != null)
            G.familiars.OnPounce -= HandlePounce;
        if (G.tree != null)
            G.tree.OnNodePurchased -= HandlePurchased;
        if (G.faults != null)
        {
            G.faults.OnBroken -= HandleMonitorBroken;
            G.faults.OnHit -= HandleMonitorHit;
            G.faults.OnRepaired -= HandleMonitorRepaired;
        }
    }

    private void HandleShiftStarted()
    {
        lastTickSecond = -1;
        AudioHub.Play(SfxId.NightStart);
        AudioHub.PlayMusic(G.stats != null && G.stats.finalShift ? MusicId.Final : MusicId.Shift);
    }

    private void HandleShiftEnded(ShiftScore score)
    {
        AudioHub.Play(SfxId.ShiftEnd);
    }

    private void HandlePhase(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Report:
                AudioHub.PlayMusic(MusicId.Book);
                break;
            case GamePhase.Book:
                AudioHub.Play(SfxId.BookOpen);
                AudioHub.PlayMusic(MusicId.Book);
                break;
            case GamePhase.Ending:
                AudioHub.PlayMusic(MusicId.Ending);
                break;
        }
    }

    private void HandleResolved(ParcelOutcome o)
    {
        if (o.ecto > 0)
            AudioHub.Play(SfxId.Ecto);

        switch (o.action)
        {
            case ParcelAction.GhostReleased:
                AudioHub.Play(SfxId.GhostRelease);
                break;

            case ParcelAction.Removed:
                if (o.familiar != null)
                    break; // фамильяр уже прозвучал прыжком
                if (o.correct)
                {
                    // Чем длиннее серия, тем выше звук снятия
                    int streak = G.shift.Score != null ? G.shift.Score.streak : 0;
                    float pitch = Mathf.Min(comboPitchMax, 1f + streak * comboPitchStep);
                    AudioHub.Play(SfxId.Snatch, pitch);
                }
                else
                {
                    AudioHub.Play(SfxId.WrongRemove);
                }
                break;

            case ParcelAction.Delivered:
                AudioHub.Play(o.correct ? SfxId.Deliver : SfxId.Complaint);
                break;
        }
    }

    private void HandleMonitorBroken(Belt belt)
    {
        AudioHub.Play(SfxId.MonitorBreak);
    }

    private void HandleMonitorHit(Belt belt, int hitsLeft)
    {
        AudioHub.Play(SfxId.MonitorHit);
    }

    private void HandleMonitorRepaired(Belt belt)
    {
        // Конец смены чинит всё молча
        if (G.shift.Current == ShiftController.State.Running)
            AudioHub.Play(SfxId.MonitorFix);
    }

    private void HandlePounce(Familiar f, Parcel target)
    {
        AudioHub.Play(SfxId.FamiliarPounce, f.def.pounceClips);
    }

    private void HandlePurchased(SkillNodeData node, int level, System.Collections.Generic.IReadOnlyList<SkillEffect> effects)
    {
        // Корневой узел покупается сам при старте ночи — молчим
        if (G.main.Phase == GamePhase.Book)
            AudioHub.Play(SfxId.Buy);
    }

    private void Update()
    {
        if (G.shift == null || G.shift.Current != ShiftController.State.Running || G.IsPaused)
            return;

        int sec = Mathf.CeilToInt(G.shift.Remaining);
        if (sec <= tickSeconds && sec > 0 && sec != lastTickSecond)
        {
            lastTickSecond = sec;
            AudioHub.Play(SfxId.ClockTick, sec % 2 == 0 ? 1f : 0.8f);
        }
    }
}
