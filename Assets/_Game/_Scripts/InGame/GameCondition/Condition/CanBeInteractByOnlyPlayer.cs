using _Game._Scripts.InGame.GameCondition.Data;
using _Game.DesignPattern.ConditionRule;
using _Game.GameGrid.Unit.DynamicUnit.Player;
using UnityEngine;

namespace _Game._Scripts.InGame.GameCondition.Condition
{
    [CreateAssetMenu(fileName = "CanBeInteractByOnlyPlayer", menuName = "ConditionSO/Interact/CanBeInteractByPlayer", order = 1)]
    public class CanBeInteractByOnlyPlayer : ScriptableObject, ICondition
    {
        public bool IsApplicable(ConditionData dataIn)
        {
            if (dataIn is not BeInteractedData data) return false;
            return data.pushUnit is Player;
        }
    }
}
