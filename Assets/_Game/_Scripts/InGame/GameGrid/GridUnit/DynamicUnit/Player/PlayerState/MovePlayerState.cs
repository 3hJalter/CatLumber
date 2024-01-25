using _Game.DesignPattern.StateMachine;
using DG.Tweening;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Player.PlayerState
{
    public class MovePlayerState : IState<Player>
    {
        private bool firstTime;
        private Direction direction;
        private float initAnimSpeed;
        private Tween moveTween;
        private float originMovingDistance;

        public StateEnum Id => StateEnum.Move;

        public void OnEnter(Player t)
        {
            t.ChangeAnim(Constants.MOVE_ANIM);
            initAnimSpeed = t.AnimSpeed;
            direction = t.Direction;
            firstTime = true;
        }
        public void OnExecute(Player t)
        {
            //NOTE: Cache input and speed up animation
            if (!firstTime && t.InputDetection.InputAction == InputAction.ButtonDown)
            {
                t.SetAnimSpeed(initAnimSpeed / Constants.MOVING_TIME_FAST_RATE);
                moveTween.Kill();
                direction = t.Direction;


                moveTween = t.Tf.DOMove(t.EnterPosData.finalPos, CalculateMoveRemainingTime())
                    .SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed).OnComplete(() =>
                    {
                        t.InputCache.Enqueue(direction);
                        t.LookDirection(t.Direction);
                        t.SetAnimSpeed(initAnimSpeed);

                        t.OnEnterTrigger(t);
                        t.StateMachine.ChangeState(StateEnum.Idle);
                    });
            }

            if (moveTween != null) return;
            firstTime = false;
            originMovingDistance = (t.EnterPosData.finalPos - t.Tf.position).sqrMagnitude;
            moveTween = t.Tf.DOMove(t.EnterPosData.finalPos, Constants.MOVING_TIME)
                .SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed).OnComplete(() =>
                {
                    t.OnEnterTrigger(t);
                    t.StateMachine.ChangeState(StateEnum.Idle);
                });


            float CalculateMoveRemainingTime()
            {
                float remainingDistance = (t.EnterPosData.finalPos - t.Tf.position).sqrMagnitude;
                return Constants.MOVING_TIME_FAST_RATE * Constants.MOVING_TIME *
                       Mathf.Sqrt(remainingDistance / originMovingDistance);
            }
        }
        public void OnExit(Player t)
        {
            moveTween.Kill();
            moveTween = null;
        }
    }
}
