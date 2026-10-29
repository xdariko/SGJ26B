using System;
using UnityEngine;

namespace GridSkillTree
{
    [Serializable]
    public class SkillCost
    {
        public CurrencyDefinition currency;
        public int baseAmount = 1;
        public CostFormulaType formula = CostFormulaType.Constant;

        public int GetAmount(int currentLevel)
        {
            int amount = formula switch
            {
                CostFormulaType.Constant => baseAmount,
                CostFormulaType.Linear => baseAmount * (currentLevel + 1),
                CostFormulaType.Exponential => Mathf.RoundToInt(baseAmount * Mathf.Pow(1.5f, currentLevel)),
                _ => baseAmount
            };

            return Mathf.Max(0, amount);
        }
    }
}
