using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.DynamicUnit.Bomb;
using DG.Tweening;
using UnityEngine;

namespace a
{
    public class RollBombState : IState<Bomb>
    {
        private bool _isRoll;
        private Tween moveTween;
        public StateEnum Id => StateEnum.Roll;

        public void OnEnter(Bomb t)
        {
            t.MovingData.SetData(t.BeInteractedData.inputDirection);
            _isRoll = t.ConditionMergeOnBePushed.IsApplicable(t.MovingData);
            t.StartWaitForExplode();
            OnExecute(t);
        }

        public void OnExecute(Bomb t)
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
                Sequence s = DOTween.Sequence();
                moveTween = s;
                s.Append(t.Tf.DOMove(t.EnterPosData.initialPos,
                        t.EnterPosData.isFalling ? Constants.MOVING_TIME / 2 : Constants.MOVING_TIME))
                    .Join(t.Tf.DORotate(GetRotation(t.MovingData.inputDirection),
                        t.EnterPosData.isFalling ? Constants.MOVING_TIME / 2 : Constants.MOVING_TIME,
                        RotateMode.FastBeyond360))
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

        public void OnExit(Bomb t)
        {
            moveTween.Kill();
        }

        private static Vector3 GetRotation(Direction direction)
        {
            return direction switch
            {
                Direction.Left => new Vector3(0, 0, 360),
                Direction.Right => new Vector3(0, 0, -360),
                Direction.Forward => new Vector3(360, 0, 0),
                Direction.Back => new Vector3(-360, 0, -0),
                _ => Vector3.zero
            };
        }
    }
}
