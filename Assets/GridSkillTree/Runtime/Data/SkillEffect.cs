using System;
using UnityEngine;

namespace GridSkillTree
{
    [Serializable]
    public class SkillEffect
    {
        [Tooltip("Game-specific key, for example: damage, move_speed, crit_chance, unlock_dash.")]
        public string id = "effect";

        public SkillValueMode valueMode = SkillValueMode.Additive;
        public float baseValue = 1f;
        public GrowthFormulaType growthFormula = GrowthFormulaType.Constant;

        public float GetValue(int level)
        {
            return growthFormula switch
            {
                GrowthFormulaType.Constant => baseValue,
                GrowthFormulaType.Linear => baseValue * level,
                GrowthFormulaType.PercentLinear => baseValue * level / 100f,
                GrowthFormulaType.Exponential => baseValue * Mathf.Pow(1.25f, level - 1),
                _ => baseValue
            };
        }
    }
}
