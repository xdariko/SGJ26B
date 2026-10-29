using UnityEngine;

public enum ParcelAction
{
    Delivered,
    Removed,
    GhostReleased
}

// Итог одного решения игрока (или фамильяра). На него подписываются звуки, всплывашки, HUD.
public struct ParcelOutcome
{
    public ParcelKind kind;
    public ParcelAction action;
    public bool correct;
    public int coins;
    public int ecto;
    public int penalty;
    // Жалоба из-за того, чего камера не видела
    public bool unseen;
    public string complaint;
    public float comboMult;
    public Vector3 position;
    public Belt belt;
    // Кто сделал: null = игрок или лента, иначе фамильяр
    public FamiliarDefinition familiar;
}

public static class ParcelJudge
{
    public static ParcelOutcome Evaluate(Parcel parcel, ParcelAction action, GameConfig cfg, GameStats stats)
    {
        ParcelOutcome o = new()
        {
            kind = parcel.Kind,
            action = action,
            position = parcel.transform.position,
            belt = parcel.Belt
        };

        if (action == ParcelAction.GhostReleased)
        {
            o.correct = true;
            o.ecto = cfg.ghostEcto;
            return o;
        }

        KindRule rule = cfg.GetRule(parcel.Kind);
        o.correct = action == ParcelAction.Removed ? rule.mustRemove : !rule.mustRemove;

        if (o.correct)
        {
            // Доставка платит цену предмета, снятие — награду из правила типа
            int reward = action == ParcelAction.Delivered && parcel.Spec.shape != null
                ? parcel.Spec.shape.value
                : rule.reward;
            o.coins = Mathf.RoundToInt(reward * stats.parcelValueMult);
        }
        else if (action == ParcelAction.Delivered)
        {
            o.unseen = rule.requiredCamLevel > stats.camLevel;
            // Камера не могла это показать: начальство ворчит, но штрафует мягче, чтобы не загнать в минус
            float unseenMult = o.unseen ? cfg.unseenPenaltyMult : 1f;
            o.penalty = Mathf.RoundToInt(rule.penalty * stats.penaltyMult * unseenMult);
            o.complaint = rule.complaint;
        }

        return o;
    }
}
