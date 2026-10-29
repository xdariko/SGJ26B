using System.Collections.Generic;
using GridSkillTree;
using UnityEngine;

// id эффектов из Ведьминой книги. Должны совпадать с SkillEffect.id в WitchBook.asset.
public static class Effects
{
    public const string CamColor = "cam_color";
    public const string CamCurse = "cam_curse";
    public const string CamGhost = "cam_ghost";
    public const string CamNoise = "cam_noise";
    public const string CamZoom = "cam_zoom";

    public const string BeltSpeed = "belt_speed";
    public const string SpawnRate = "spawn_rate";
    public const string ParcelValue = "parcel_value";
    public const string Line2 = "line_2";
    public const string Line3 = "line_3";
    public const string Line4 = "line_4";

    public const string ShiftTime = "shift_time";
    public const string ComboMax = "combo_max";
    public const string ComplaintCut = "complaint_cut";
    public const string GhostChance = "ghost_chance";
    public const string GoldenChance = "golden_chance";
    public const string TrashCut = "trash_cut";

    public const string FamCat = "fam_cat";
    public const string FamCatRate = "fam_cat_rate";
    public const string FamRaven = "fam_raven";
    public const string FamOwl = "fam_owl";
    public const string FamBat = "fam_bat";
    public const string FamRate = "fam_rate";

    public const string FinalOrder = "final_order";
}

// Снимок статов на смену. Пересобирается перед каждой сменой из дерева.
//
// Договорённость по узлам дерева: числовые эффекты делаем в режиме Additive + Linear,
// baseValue пишем долей (0.15 = +15% за уровень). Режим Percent в тулзе умножает на 100, его не используем.
public class GameStats
{
    public int camLevel;
    public float camNoise = 1f;
    public float camZoom;

    public float beltSpeed;
    public float spawnInterval;
    public float trashChance;
    public float parcelValueMult = 1f;
    public int lines = 1;

    public float shiftDuration;
    public float comboMax;
    public float penaltyMult = 1f;
    public float ghostChanceMult = 1f;
    public float goldenChanceMult = 1f;

    public bool famCat, famRaven, famOwl, famBat;
    public float famCatRateMult = 1f;
    public float famRateMult = 1f;

    public bool finalShift;

    // Все купленные Unlock-эффекты (новые предметы и прочее)
    private readonly HashSet<string> unlocks = new();

    public bool HasUnlock(string effectId) => unlocks.Contains(effectId);

    public static GameStats Build(SkillTreeRuntime tree, GameConfig cfg)
    {
        GameStats s = new();

        float Value(string id) => tree != null ? tree.GetEffectValue(id) : 0f;
        bool Has(string id) => tree != null && tree.HasUnlock(id);

        s.camLevel = (Has(Effects.CamColor) ? 1 : 0) + (Has(Effects.CamCurse) ? 1 : 0) + (Has(Effects.CamGhost) ? 1 : 0);
        s.camNoise = Mathf.Max(0f, 1f - Value(Effects.CamNoise));
        s.camZoom = Value(Effects.CamZoom);

        s.beltSpeed = cfg.beltSpeed * (1f + Value(Effects.BeltSpeed));
        s.spawnInterval = cfg.spawnInterval / (1f + Value(Effects.SpawnRate));
        s.trashChance = Mathf.Max(cfg.minTrashChance, cfg.trashChance * (1f - Value(Effects.TrashCut)));
        s.parcelValueMult = 1f + Value(Effects.ParcelValue);
        s.lines = 1 + (Has(Effects.Line2) ? 1 : 0) + (Has(Effects.Line3) ? 1 : 0) + (Has(Effects.Line4) ? 1 : 0);
        // С каждой новой линией поток на одну ленту реже, чтобы 4 монитора не давали 4x нагрузки
        s.spawnInterval *= 1f + cfg.lineSpawnSlowdown * (s.lines - 1);

        s.shiftDuration = cfg.shiftDuration + Value(Effects.ShiftTime);
        s.comboMax = cfg.comboMax + Value(Effects.ComboMax);
        s.penaltyMult = Mathf.Max(0.1f, 1f - Value(Effects.ComplaintCut));
        s.ghostChanceMult = 1f + Value(Effects.GhostChance);
        s.goldenChanceMult = Has(Effects.GoldenChance) ? 1f + Value(Effects.GoldenChance) : 0f;

        s.famCat = Has(Effects.FamCat);
        s.famRaven = Has(Effects.FamRaven);
        s.famOwl = Has(Effects.FamOwl);
        s.famBat = Has(Effects.FamBat);
        s.famCatRateMult = 1f + Value(Effects.FamCatRate);
        s.famRateMult = 1f + Value(Effects.FamRate);

        s.finalShift = Has(Effects.FinalOrder);

        if (tree != null && tree.TreeData != null)
            foreach (SkillNodeData node in tree.TreeData.nodes)
                foreach (SkillEffect e in node.effects)
                    if (e.valueMode == SkillValueMode.Unlock && tree.HasUnlock(e.id))
                        s.unlocks.Add(e.id);
        return s;
    }
}
