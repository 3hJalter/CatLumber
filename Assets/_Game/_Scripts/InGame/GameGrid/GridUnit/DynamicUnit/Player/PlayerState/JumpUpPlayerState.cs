using _Game.DesignPattern;
using _Game.DesignPattern.StateMachine;
using _Game.Managers;
using DG.Tweening;
using GameGridEnum;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Player.PlayerState
{
    public class JumpUpPlayerState : IState<Player>
    {
        private bool _isExecuted;
        private bool _firstTime;
        Direction direction;

        public StateEnum Id => StateEnum.JumpUp;

        public void OnEnter(Player t)
        {
            t.ChangeAnim(Constants.JUMP_UP_ANIM);
            direction = Direction.None;
            _firstTime = true;
            ParticlePool.Play(DataManager.Ins.VFXData.GetParticleSystem(VFXType.Dust),
                t.transform.position - Vector3.up * 0.5f);
        }

        public void OnExecute(Player t)
        {
            if (!_firstTime && t.InputDetection.InputAction == InputAction.ButtonDown)
            {
                direction = t.Direction;
            }
            if (_isExecuted) return;
            _isExecuted = true;
            _firstTime = false;
            t.Tf.DOMove(t.EnterPosData.finalPos, Constants.MOVING_TIME)
                .SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed).OnComplete(() =>
                {
                    t.InputCache.Enqueue(direction);
                    t.OnEnterTrigger(t);
                    t.StateMachine.ChangeState(StateEnum.Idle);
                });
        }

        public void OnExit(Player t)
        {
            _isExecuted = false;
        }
    }
}