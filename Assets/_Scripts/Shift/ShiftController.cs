using System;
using System.Collections.Generic;
using UnityEngine;

// Смена: таймер, ленты, счёт. Про UI ничего не знает, только шлёт события.
public class ShiftController : ManagedBehaviour
{
    public enum State
    {
        Idle,
        Running,
        Ended
    }

    [SerializeField] private List<Belt> belts = new();

    // Адресат каждой линии по порядку (ГДД §5). Линия 1 до покупки цвета принимает всё.
    private static readonly Addressee[] LineAddressees =
        { Addressee.Vampires, Addressee.Witches, Addressee.Ghosts, Addressee.Pumpkinheads };

    public State Current { get; private set; } = State.Idle;
    public int Night { get; private set; }
    public float Duration { get; private set; }
    public float Elapsed { get; private set; }
    public float Remaining => Mathf.Max(0f, Duration - Elapsed);
    public float Progress01 => Duration > 0f ? Mathf.Clamp01(Elapsed / Duration) : 0f;
    public ShiftScore Score { get; private set; }
    public IReadOnlyList<Belt> Belts => belts;

    public event Action OnShiftStarted;
    public event Action<ShiftScore> OnShiftEnded;
    public event Action<ParcelOutcome> OnParcelResolved;

    private void Awake()
    {
        G.shift = this;

        foreach (Belt belt in belts)
            belt.OnParcelReachedEnd += HandleReachedEnd;
    }

    private void OnDestroy()
    {
        foreach (Belt belt in belts)
            if (belt != null)
                belt.OnParcelReachedEnd -= HandleReachedEnd;
    }

    public void StartShift()
    {
        Night = SaveData.Night;
        Duration = G.stats.shiftDuration;
        Elapsed = 0f;
        Score = new ShiftScore(Night, G.config, G.stats);

        for (int i = 0; i < belts.Count; i++)
        {
            Belt belt = belts[i];
            belt.Stop();
            bool active = i < G.stats.lines;
            belt.gameObject.SetActive(active);
            if (!active)
                continue;

            belt.Cam.Setup(G.stats.camLevel);
            belt.Begin(GetLineAddressee(i), Night);
        }

        Current = State.Running;
        OnShiftStarted?.Invoke();
    }

    public void EndShift()
    {
        if (Current != State.Running)
            return;

        foreach (Belt belt in belts)
            belt.Stop();

        Current = State.Ended;
        SaveData.Night = Night + 1;
        OnShiftEnded?.Invoke(Score);
    }

    // Клик игрока (или фамильяра) по посылке
    public void HandleClick(Parcel parcel)
    {
        if (Current != State.Running || parcel == null || parcel.IsResolved)
            return;

        if (parcel.HasGhost && G.stats.camLevel >= 3)
        {
            Resolve(parcel, ParcelAction.GhostReleased);
            parcel.ReleaseGhost();
            return;
        }

        Resolve(parcel, ParcelAction.Removed);
        parcel.Remove();
    }

    // Фамильяр снимает посылку (или выпускает призрака). Комбо игрока не трогает.
    public void HandleFamiliar(Parcel parcel, FamiliarDefinition familiar)
    {
        if (Current != State.Running || parcel == null || parcel.IsResolved)
            return;

        if (familiar.releasesGhosts && parcel.HasGhost && G.stats.camLevel >= 3)
        {
            Resolve(parcel, ParcelAction.GhostReleased, familiar);
            parcel.ReleaseGhost();
            return;
        }

        Resolve(parcel, ParcelAction.Removed, familiar);
        parcel.Remove();
    }

    // Пересчитать видимость, если уровень камеры поменялся посреди смены (читы, события)
    public void RefreshVision()
    {
        foreach (Belt belt in belts)
            if (belt.gameObject.activeInHierarchy)
                belt.Cam.Setup(G.stats.camLevel);
    }

    private void HandleReachedEnd(Parcel parcel)
    {
        Resolve(parcel, ParcelAction.Delivered);
        parcel.Deliver();
    }

    private void Resolve(Parcel parcel, ParcelAction action, FamiliarDefinition familiar = null)
    {
        ParcelOutcome outcome = ParcelJudge.Evaluate(parcel, action, G.config, G.stats);
        outcome.familiar = familiar;
        Score.Register(ref outcome);

        if (G.wallet != null)
        {
            G.wallet.Add(G.config.coin, outcome.coins - outcome.penalty);
            G.wallet.Add(G.config.ecto, outcome.ecto);
        }

        OnParcelResolved?.Invoke(outcome);
    }

    private Addressee GetLineAddressee(int lineIndex)
    {
        if (lineIndex == 0 && G.stats.camLevel < 1)
            return Addressee.Any;

        return LineAddressees[Mathf.Clamp(lineIndex, 0, LineAddressees.Length - 1)];
    }

    protected override void PausableUpdate()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        if (Current != State.Running)
            return;

        Elapsed += dt;
        if (Elapsed >= Duration)
            EndShift();
    }
}
