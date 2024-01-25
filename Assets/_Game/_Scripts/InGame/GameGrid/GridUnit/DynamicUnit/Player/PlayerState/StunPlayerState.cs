using _Game.DesignPattern.StateMachine;
using _Game.Managers;

namespace _Game.GameGrid.Unit.DynamicUnit.Player.PlayerState
{
    public class StunPlayerState : IState<Player>
    {
        public StateEnum Id => StateEnum.Stun;

        public void OnEnter(Player t)
        {
            GameplayManager.Ins.IsCanUndo = false;
            GameplayManager.Ins.IsCanResetIsland = false;
        }

        public void OnExecute(Player t)
        {
            if (t.IsDead)
            {
                t.StateMachine.ChangeState(StateEnum.Die);
                return;
            }
        }

        public void OnExit(Player t)
        {
            GameplayManager.Ins.IsCanUndo = true;
            GameplayManager.Ins.IsCanResetIsland = true;
        }
    }
}