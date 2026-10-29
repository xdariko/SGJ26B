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

            float own = string.IsNullOrEmpty(def.rateEffect) ? 0f : G.tree.GetEffectValue(def.rateEffect);
            active.Add(new Familiar(def, (1f + own) * G.stats.famRateMult));
        }

        OnRosterChanged?.Invoke();
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
                    G.shift.HandleFamiliar(target, f.def);
                f.Finish();
            }
            else if (f.IsReady)
            {
                Parcel target = FindTarget(f.def);
                if (target == null)
                    continue;

                claimed.Add(target);
                f.Pounce(target);
                OnPounce?.Invoke(f, target);
            }
        }
    }

    // Самая срочная видимая цель: та, что ближе всех к концу ленты
    private Parcel FindTarget(FamiliarDefinition def)
    {
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
                if (progress < def.zoneStart || progress > def.zoneEnd || progress <= bestProgress)
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

        if (def.releasesGhosts && p.HasGhost && camLevel >= 3)
            return true;

        return def.targets.Contains(p.Kind) && G.config.GetRule(p.Kind).requiredCamLevel <= camLevel;
    }
}
