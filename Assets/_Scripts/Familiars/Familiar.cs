using UnityEngine;

// Живой фамильяр на смену: таймер отдыха, текущая цель и прокачка из книги на эту смену.
public class Familiar
{
    public readonly FamiliarDefinition def;
    public readonly float cooldown;
    // С какой доли ленты начинает ловить (с учётом прокачки дальности)
    public readonly float zoneStart;
    // Шанс сразу прыгнуть ещё раз без отдыха
    public readonly float doubleChance;
    // Особое умение открыто в книге
    public readonly bool hasSpecial;

    public float Timer { get; private set; }
    public Parcel Target { get; private set; }
    public float PounceLeft { get; private set; }

    public bool IsReady => Target == null && Timer <= 0f;
    public bool IsBusy => Target != null;
    public bool IsResting => Target == null && Timer > 0f;
    // 0 = только что отработал, 1 = готов
    public float Readiness01 => cooldown > 0f ? 1f - Mathf.Clamp01(Timer / cooldown) : 1f;

    public Familiar(FamiliarDefinition def, float rateMult, float zoneBonus, float doubleChance, bool hasSpecial)
    {
        this.def = def;
        cooldown = def.cooldown / Mathf.Max(0.1f, rateMult);
        zoneStart = Mathf.Max(Mathf.Min(def.minZoneStart, def.zoneStart), def.zoneStart - zoneBonus);
        this.doubleChance = Mathf.Clamp01(doubleChance);
        this.hasSpecial = hasSpecial;
        Timer = cooldown * 0.5f;
    }

    public void Tick(float dt)
    {
        if (Target != null)
            PounceLeft -= dt;
        else if (Timer > 0f)
            Timer -= dt;
    }

    public void Pounce(Parcel target)
    {
        Target = target;
        PounceLeft = def.pounceTime;
    }

    // true = двойной прыжок: отдыха нет, сразу ищет следующую цель
    public bool Finish()
    {
        Target = null;
        bool again = doubleChance > 0f && Random.value < doubleChance;
        Timer = again ? 0f : cooldown;
        return again;
    }
}
