using _Game.DesignPattern.StateMachine;

namespace _Game.GameGrid.Unit.DynamicUnit.ButtonUnitState
{
    public class IdleButtonState : IState<ButtonUnit>
    {
        public StateEnum Id => StateEnum.Idle;
        public void OnEnter(ButtonUnit t)
        {
        }

        public void OnExecute(ButtonUnit t)
        {
        }

        public void OnExit(ButtonUnit t)
        {
        }
    }
}
