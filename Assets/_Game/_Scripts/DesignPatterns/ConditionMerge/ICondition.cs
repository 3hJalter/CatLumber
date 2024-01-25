namespace _Game.DesignPattern.ConditionRule
{
    /* summary
     * 1. RuleData
     * 2. ICondition
     * 3. Condition
     * 4. ConditionMerge
     */
    public interface ICondition
    {
        bool IsApplicable(ConditionData data);
    }
}
