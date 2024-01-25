using _Game.DesignPattern.StateMachine;
using _Game.Utilities;
using DG.Tweening;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Chump.ChumpState
{
    public class RollChumpState : IState<Chump>
    {
        private bool _isRoll;
        Tween moveTween;
        public StateEnum Id => StateEnum.Roll;

        public void OnEnter(Chump t)
        {
            t.MovingData.SetData(t.BeInteractedData.inputDirection);
            _isRoll = t.ConditionMergeOnBePushed.IsApplicable(t.MovingData);
            OnExecute(t);
        }

        public void OnExecute(Chump t)
        {
            if (!_isRoll)
            {
                t.StateMachine.ChangeState(StateEnum.RollBlock);
            }
            else
            {

                t.SetEnterCellData(t.MovingData.inputDirection, t.MovingData.enterMainCell, t.UnitTypeY, false,
                    t.MovingData.enterCells);
                t.OnOutCells();
                t.OnEnterCells(t.MovingData.enterMainCell, t.MovingData.enterCells);
                // Animation and complete
                // TODO: rotate in place animation for skin
                moveTween = t.Tf.DOMove(t.EnterPosData.initialPos,
                        t.EnterPosData.isFalling ? Constants.MOVING_TIME / 2 : Constants.MOVING_TIME)
                    .SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed).OnComplete(() =>
                    {
                        if (t.EnterPosData.isFalling)
                        {
                            //Falling to water
                            t.StateMachine.ChangeState(StateEnum.Fall);
                        }
                        else
                        {
                            t.OnEnterTrigger(t);
                            t.StateMachine.ChangeState(StateEnum.Idle);
                            if (t.gameObject.activeSelf)
                                t.OnBePushed(t.BeInteractedData.inputDirection, t.BeInteractedData.pushUnit);
                        }
                    });
            }
        }

        public void OnExit(Chump t)
        {
            moveTween.Kill();
        }

        private Vector3 GetRotateAxis(GridUnit t, Direction direction)
        {
            Vector3 axis = t.skin.localRotation.eulerAngles;
            if (direction is Direction.Back or Direction.Forward) axis.x += 359;
            else if (direction is Direction.Left or Direction.Right) axis.z += 359;
            return axis;
        }
    }
}
