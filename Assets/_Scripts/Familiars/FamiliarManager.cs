using System;
using System.Collections.Generic;
using UnityEngine;

// Все фамильяры смены. Общие на все линии: ищут цель по всем лентам и прыгают к ней.
public class FamiliarManager : ManagedBehaviour
{
    [SerializeField] private List<FamiliarDefinition> definitions = new();

    private readonly List<Familiar> active = new();
    private readonly HashSet<Parcel> claimed = new();

    public IReadOnlyList<Familiar> Active => active;

    public event Action OnRosterChanged;
    // Фамильяр выбрал цель и прыгнул (посылка снимется через pounceTime)
    public event Action<Familiar, Parcel> OnPounce;
    // Сработал двойной прыжок: фамильяр не отдыхает
    public event Action<Familiar> OnDoubleJump;

    // Умение совы: пока она отдыхает (или летит), проклятия светятся ярче
    public bool CurseSight
    {
        get
        {
            foreach (Familiar f in active)
                if (f.hasSpecial && f.def.special == FamiliarSpecial.CurseSight && !f.IsReady)
                    return true;
            return false;
        }
    }

    private void Awake()
    {
        G.familiars = this;
    }

    private void Start()
    {
        G.shift.OnShiftStarted += Rebuild;
        if (G.shift.Current == ShiftController.State.Running)
            Rebuild();
    }

    private void OnDestroy()
    {
        if (G.shift != null)
            G.shift.OnShiftStarted -= Rebuild;
    }

    private void Rebuild()
    {
        active.Clear();
        claimed.Clear();

        foreach (FamiliarDefinition def in definitions)
        {
            if (def == null || !G.stats.HasUnlock(def.unlockEffect))
                continue;

            active.Add(new Familiar(def,
                1f + Value(def.rateEffect),
                Value(def.zoneEffect),
                Value(def.doubleEffect),
                def.special != FamiliarSpecial.None && G.stats.HasUnlock(def.specialEffect)));
        }

        OnRosterChanged?.Invoke();
    }

    private static float Value(string effectId)
    {
        return string.IsNullOrEmpty(effectId) || G.tree == null ? 0f : G.tree.GetEffectValue(effectId);
    }

    protected override void PausableUpdate()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        if (G.shift.Current != ShiftController.State.Running)
            return;

        foreach (Familiar f in active)
        {
            f.Tick(dt);

            if (f.IsBusy && f.PounceLeft <= 0f)
            {
                Parcel target = f.Target;
                claimed.Remove(target);
                if (target != null && !target.IsResolved)
                    G.shift.HandleFamiliar(target, f);
                if (f.Finish())
                    OnDoubleJump?.Invoke(f);
            }
            else if (f.IsReady)
            {
                Parcel target = FindTarget(f);
                if (target == null)
                    continue;

                claimed.Add(target);
                f.Pounce(target);
                OnPounce?.Invoke(f, target);
            }
        }
    }

    // Самая срочная видимая цель: та, что ближе всех к концу ленты
    private Parcel FindTarget(Familiar f)
    {
        FamiliarDefinition def = f.def;
        Parcel best = null;
        float bestProgress = -1f;

        foreach (Belt belt in G.shift.Belts)
        {
            if (!belt.IsRunning)
                continue;

            float length = belt.Length;
            foreach (Parcel p in belt.Parcels)
            {
                if (p == null || p.IsResolved || claimed.Contains(p) || !CanTake(def, p))
                    continue;

                float progress = p.Distance / length;
                if (progress < f.zoneStart || progress > def.zoneEnd || progress <= bestProgress)
                    continue;

                best = p;
                bestProgress = progress;
            }
        }

        return best;
    }

    // Фамильяр видит только то, что видит камера
    private static bool CanTake(FamiliarDefinition def, Parcel p)
    {
        int camLevel = G.stats.camLevel;

        if (def.releasesGhosts && p.HasGhost)
            return true;

        return def.targets.Contains(p.Kind) && G.config.GetRule(p.Kind).requiredCamLevel <= camLevel;
    }
}
