using System;

namespace GridSkillTree
{
    [Serializable]
    public class CurrencyAmount
    {
        public CurrencyDefinition currency;
        public int amount = 1;
    }
}
