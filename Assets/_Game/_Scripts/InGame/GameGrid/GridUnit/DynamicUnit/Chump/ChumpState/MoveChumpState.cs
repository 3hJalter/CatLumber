using _Game.DesignPattern;
using _Game.DesignPattern.StateMachine;
using _Game.Managers;
using _Game.Utilities;
using DG.Tweening;
using GameGridEnum;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Chump.ChumpState
{
    public class MoveChumpState : IState<Chump>
    {
        private readonly Vector3 WATER_SPLASH_OFFSET = Vector3.up * 0.18f;
        private bool _isMove;
        Tween moveTween;
        public StateEnum Id => StateEnum.Move;

        public void OnEnter(Chump t)
        {
            t.MovingData.SetData(t.BeInteractedData.inputDirection);
            _isMove = t.ConditionMergeOnBePushed.IsApplicable(t.MovingData);
            OnExecute(t);
        }

        public void OnExecute(Chump t)
        {
            if (!_isMove)
            {
                if (t.MovingData.blockDynamicUnits.Count > 0) t.OnPush(t.MovingData.inputDirection, t.MovingData);
                t.StateMachine.ChangeState(StateEnum.Idle);
            }
            else
            {
                t.SetEnterCellData(t.MovingData.inputDirection, t.MovingData.enterMainCell, t.UnitTypeY, false,
                    t.MovingData.enterCells);
                t.OnOutCells();
                t.OnEnterCells(t.MovingData.enterMainCell, t.MovingData.enterCells);

                if (t.IsInWater())
                {
                    Sequence s = DOTween.Sequence();
                    moveTween = s;
                    s.Append(t.Tf.DOMove(t.EnterPosData.finalPos, Constants.MOVING_TIME * 0.6f).SetEase(Ease.Linear)
                            .OnComplete(() => ParticlePool.Play(DataManager.Ins.VFXData.GetParticleSystem(VFXType.WaterSplash), t.transform.position + WATER_SPLASH_OFFSET)))
                        .Append(t.Tf.DOMoveY(Constants.POS_Y_BOTTOM, Constants.MOVING_TIME * 0.6f).SetEase(Ease.Linear))
                        .OnComplete(() =>
                        {
                            t.OnEnterTrigger(t);
                            // Can be change to animation later
                            t.skin.localRotation =
                                Quaternion.Euler(t.UnitTypeXZ is UnitTypeXZ.Horizontal
                                    ? Constants.HorizontalSkinRotation
                                    : Constants.VerticalSkinRotation);
                            t.StateMachine.ChangeState(StateEnum.Emerge);
                        });
                    return;
                }
                
                // Animation and complete
                moveTween = t.Tf.DOMove(t.EnterPosData.initialPos,
                        t.EnterPosData.isFalling ? Constants.MOVING_TIME / 2 : Constants.MOVING_TIME)
                    .SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed).OnComplete(() =>
                    {
                        if (t.EnterPosData.isFalling)
                        {
                            t.StateMachine.ChangeState(StateEnum.Fall);
                        }
                        else
                        {
                            t.OnEnterTrigger(t);
                            t.StateMachine.ChangeState(StateEnum.Idle);
                        }
                    });
            }
        }

        public void OnExit(Chump t)
        {
            moveTween.Kill();
        }
    }
}
