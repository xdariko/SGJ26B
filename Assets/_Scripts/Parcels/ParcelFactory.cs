using System.Collections.Generic;
using UnityEngine;

// Бросок «что заспавнить». Чистая логика, без сцены.
public static class ParcelFactory
{
    public static ParcelSpec Roll(int night, Addressee beltAddress, GameConfig cfg, GameStats stats)
    {
        ParcelSpec spec = new()
        {
            kind = ParcelKind.Normal,
            address = beltAddress == Addressee.Any ? RandomAddressee(cfg, Addressee.Any) : beltAddress
        };

        if (Random.value < stats.trashChance)
        {
            spec.shape = PickShape(cfg.shapes, true, night, stats);
            spec.kind = ParcelKind.Trash;
            spec.address = Addressee.Any;
            return spec;
        }

        spec.shape = PickShape(cfg.shapes, false, night, stats);

        foreach (TraitChance tc in cfg.traitChances)
        {
            if (night < tc.minNight || stats.camLevel < tc.minCamLevel)
                continue;

            if (tc.trait == ParcelTrait.WrongAddress && beltAddress == Addressee.Any)
                continue;

            if (tc.trait == ParcelTrait.Golden && cfg.goldenShape == null)
                continue;

            float chance = tc.chance * tc.trait switch
            {
                ParcelTrait.Ghost => stats.ghostChanceMult,
                ParcelTrait.Golden => stats.goldenChanceMult,
                _ => 1f
            };

            if (Random.value >= chance)
                continue;

            ApplyTrait(ref spec, tc.trait, beltAddress, cfg);
            break;
        }

        return spec;
    }

    private static void ApplyTrait(ref ParcelSpec spec, ParcelTrait trait, Addressee beltAddress, GameConfig cfg)
    {
        switch (trait)
        {
            case ParcelTrait.WrongAddress:
                spec.kind = ParcelKind.WrongAddress;
                spec.address = RandomAddressee(cfg, beltAddress);
                break;
            case ParcelTrait.Cursed:
                spec.kind = ParcelKind.Cursed;
                break;
            case ParcelTrait.Mimic:
                spec.kind = ParcelKind.Mimic;
                break;
            case ParcelTrait.Ghost:
                spec.hasGhost = true;
                break;
            case ParcelTrait.Golden:
                spec.kind = ParcelKind.Golden;
                spec.shape = cfg.goldenShape;
                break;
        }
    }

    private static ParcelDefinition PickShape(List<ParcelDefinition> shapes, bool trash, int night, GameStats stats)
    {
        float total = 0f;
        foreach (ParcelDefinition s in shapes)
            if (s != null && s.isTrash == trash && s.IsAvailable(night, stats))
                total += s.weight;

        float roll = Random.value * total;
        ParcelDefinition last = null;

        foreach (ParcelDefinition s in shapes)
        {
            if (s == null || s.isTrash != trash || !s.IsAvailable(night, stats))
                continue;

            last = s;
            roll -= s.weight;
            if (roll <= 0f)
                return s;
        }

        return last;
    }

    private static Addressee RandomAddressee(GameConfig cfg, Addressee exclude)
    {
        List<Addressee> options = new();
        foreach (AddresseeInfo info in cfg.addressees)
            if (info.id != Addressee.Any && info.id != exclude)
                options.Add(info.id);

        return options.Count == 0 ? Addressee.Any : options[Random.Range(0, options.Count)];
    }
}
