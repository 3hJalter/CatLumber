using _Game.DesignPattern.StateMachine;

namespace _Game.GameGrid.Unit.DynamicUnit.Bomb.BombState
{
    public class IdleBombState : IState<Bomb>
    {
        public StateEnum Id => StateEnum.Idle;
        public void OnEnter(Bomb t)
        {
            
        }

        public void OnExecute(Bomb t)
        {
            
        }

        public void OnExit(Bomb t)
        {
            
        }
    }
}
