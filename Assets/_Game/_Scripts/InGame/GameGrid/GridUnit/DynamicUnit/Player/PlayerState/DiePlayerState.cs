using System;
using _Game.DesignPattern.StateMachine;
using _Game.Managers;
using _Game.Utilities.Timer;

namespace _Game.GameGrid.Unit.DynamicUnit.Player.PlayerState
{
    public class DiePlayerState : IState<Player>
    {
        private const float DIE_TIME = 1.08f;
        public StateEnum Id => StateEnum.Die;

        public void OnEnter(Player t)
        {
            GameplayManager.Ins.IsCanUndo = false;
            GameplayManager.Ins.IsCanResetIsland = false;
            t.ChangeAnim(Constants.DIE_ANIM);
            TimerManager.Inst.WaitForTime(DIE_TIME, OnLoseGame);
            void OnLoseGame()
            {
                GameManager.Ins.PostEvent(DesignPattern.EventID.LoseGame);
            }
        }

        public void OnExecute(Player t)
        {
            
        }

        public void OnExit(Player t)
        {
            
        }
    }
}
