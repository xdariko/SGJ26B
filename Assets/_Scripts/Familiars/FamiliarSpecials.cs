using UnityEngine;

// Особые умения фамильяров, которые меняют награду. Ночное зрение совы — в FamiliarManager.CurseSight.
public static class FamiliarSpecials
{
    public static void Apply(ref ParcelOutcome o, FamiliarDefinition def)
    {
        if (!o.correct || Random.value >= def.specialChance)
            return;

        switch (def.special)
        {
            case FamiliarSpecial.FindCoin:
                o.coins += def.specialAmount;
                o.familiarBonus = true;
                break;
            case FamiliarSpecial.Redirect when o.kind == ParcelKind.WrongAddress && o.action == ParcelAction.Removed:
                o.coins += def.specialAmount;
                o.familiarBonus = true;
                break;
            case FamiliarSpecial.MoreEcto when o.action == ParcelAction.GhostReleased:
                o.ecto += def.specialAmount;
                o.familiarBonus = true;
                break;
        }
    }
}
