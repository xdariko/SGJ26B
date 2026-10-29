namespace GridSkillTree
{
    public enum CostFormulaType
    {
        Constant,
        Linear,
        Exponential
    }

    public enum GrowthFormulaType
    {
        Constant,
        Linear,
        PercentLinear,
        Exponential
    }

    public enum SkillValueMode
    {
        Additive,
        Percent,
        Set,
        Unlock
    }

    public enum SkillNodeVisualState
    {
        Locked,
        Available,
        Maxed
    }
}
