using System;
using System.Collections.Generic;
using UnityEngine;

// Поломки мониторов: время от времени одна из камер теряет сигнал, лента под ней едет вслепую
// (узел fault_stop «Песочные часы» замедляет её по уровням, на последнем она стоит). Игрок чинит монитор ударами кулака. Про UI ничего не знает, шлёт события.
public class CameraFaults : ManagedBehaviour
{
    private class FaultState
    {
        public bool broken;
        public int hitsLeft;
        public float cooldown;
    }

    private readonly Dictionary<Belt, FaultState> states = new();
    private readonly List<Belt> candidates = new();
    private float timer;
    private bool active;

    public event Action<Belt> OnBroken;
    // Удар по сломанному монитору: сколько ещё осталось
    public event Action<Belt, int> OnHit;
    public event Action<Belt> OnRepaired;

    private void Awake()
    {
        G.faults = this;
    }

    private void Start()
    {
        G.shift.OnShiftStarted += HandleShiftStarted;
        G.shift.OnShiftEnded += HandleShiftEnded;
        if (G.shift.Current == ShiftController.State.Running)
            HandleShiftStarted();
    }

    private void OnDestroy()
    {
        if (G.faults == this)
            G.faults = null;
        if (G.shift != null)
        {
            G.shift.OnShiftStarted -= HandleShiftStarted;
            G.shift.OnShiftEnded -= HandleShiftEnded;
        }
    }

    public bool IsBroken(Belt belt) => belt != null && states.TryGetValue(belt, out FaultState s) && s.broken;

    public int HitsLeft(Belt belt) => belt != null && states.TryGetValue(belt, out FaultState s) && s.broken ? s.hitsLeft : 0;

    // Удар по монитору. true = монитор был сломан и удар засчитан
    public bool Hit(Belt belt)
    {
        if (belt == null || !states.TryGetValue(belt, out FaultState s) || !s.broken)
            return false;

        s.hitsLeft--;
        OnHit?.Invoke(belt, s.hitsLeft);
        if (s.hitsLeft <= 0)
            Repair(belt, s);
        return true;
    }

    // Сломать конкретную ленту сейчас (читы, тесты)
    public void Break(Belt belt)
    {
        if (belt == null || !belt.IsRunning)
            return;

        FaultState s = Get(belt);
        if (s.broken)
            return;

        s.broken = true;
        s.hitsLeft = Mathf.Max(1, G.stats.faultHits);
        belt.SpeedMultiplier = G.stats.faultBeltSpeed;
        OnBroken?.Invoke(belt);
    }

    private void Repair(Belt belt, FaultState s)
    {
        s.broken = false;
        s.hitsLeft = 0;
        s.cooldown = G.config.faultCooldown;
        belt.SpeedMultiplier = 1f;
        OnRepaired?.Invoke(belt);
    }

    private FaultState Get(Belt belt)
    {
        if (!states.TryGetValue(belt, out FaultState s))
        {
            s = new FaultState();
            states[belt] = s;
        }
        return s;
    }

    private void HandleShiftStarted()
    {
        RepairAllSilently();
        active = G.shift.Night >= G.config.faultFromNight;
        timer = G.config.faultGrace + NextInterval();
    }

    private void HandleShiftEnded(ShiftScore score)
    {
        RepairAllSilently();
        active = false;
    }

    private void RepairAllSilently()
    {
        foreach (KeyValuePair<Belt, FaultState> pair in states)
        {
            bool wasBroken = pair.Value.broken;
            pair.Value.broken = false;
            pair.Value.hitsLeft = 0;
            pair.Value.cooldown = 0f;
            if (!wasBroken)
                continue;

            pair.Key.SpeedMultiplier = 1f;
            OnRepaired?.Invoke(pair.Key);
        }
    }

    private float NextInterval()
    {
        float jitter = G.config.faultIntervalJitter;
        return G.stats.faultInterval * (1f + UnityEngine.Random.Range(-jitter, jitter));
    }

    protected override void PausableUpdate()
    {
        Tick(Time.deltaTime);
    }

    // Шаг таймера поломок. Отдельно от Update, чтобы гонять с фиксированным dt (тесты)
    public void Tick(float dt)
    {
        if (!active || G.shift.Current != ShiftController.State.Running)
            return;

        foreach (FaultState s in states.Values)
            s.cooldown = Mathf.Max(0f, s.cooldown - dt);

        timer -= dt;
        if (timer > 0f)
            return;

        // Ломаем случайный рабочий монитор; если все сломаны или только что починены, попробуем чуть позже
        candidates.Clear();
        foreach (Belt belt in G.shift.Belts)
        {
            if (belt == null || !belt.IsRunning || !belt.gameObject.activeInHierarchy)
                continue;
            FaultState s = Get(belt);
            if (!s.broken && s.cooldown <= 0f)
                candidates.Add(belt);
        }

        if (candidates.Count == 0)
        {
            timer = 2f;
            return;
        }

        Break(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
        timer = NextInterval();
    }
}
