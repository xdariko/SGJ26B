using UnityEngine;

// Живой фамильяр на смену: таймер отдыха и текущая цель.
public class Familiar
{
    public readonly FamiliarDefinition def;
    public readonly float cooldown;

    public float Timer { get; private set; }
    public Parcel Target { get; private set; }
    public float PounceLeft { get; private set; }

    public bool IsReady => Target == null && Timer <= 0f;
    public bool IsBusy => Target != null;
    // 0 = только что отработал, 1 = готов
    public float Readiness01 => cooldown > 0f ? 1f - Mathf.Clamp01(Timer / cooldown) : 1f;

    public Familiar(FamiliarDefinition def, float rateMult)
    {
        this.def = def;
        cooldown = def.cooldown / Mathf.Max(0.1f, rateMult);
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

    public void Finish()
    {
        Target = null;
        Timer = cooldown;
    }
}
