using _Game.DesignPattern.ConditionRule;
using _Game.DesignPattern.StateMachine;
using UnityEngine;

namespace _Game._Scripts.InGame.GameCondition.Condition
{
    [CreateAssetMenu(fileName = "IsIdleState", menuName = "ConditionSO/Interact/IsIdleState", order = 1)]
    public class IsIdleState : ScriptableObject, ICondition
    {
        public bool IsApplicable(ConditionData data)
        {
            return data.owner.IsCurrentStateIs(StateEnum.Idle);
        }
    }
}
