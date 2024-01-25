using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Game.DesignPattern.ConditionRule
{
    [CreateAssetMenu(fileName = "ConditionRuleMerge", menuName = "ConditionRuleSO/ConditionRuleMerge", order = 1)]
    public class ConditionMerge : SerializedScriptableObject
    {
        // ReSharper disable once Unity.RedundantSerializeFieldAttribute
        // ReSharper disable once CollectionNeverUpdated.Local
        [SerializeField] private readonly List<ICondition> rules = new();

        public bool IsApplicable(ConditionData data)
        {
            return rules.All(rule => rule.IsApplicable(data));
        }
    }
}
