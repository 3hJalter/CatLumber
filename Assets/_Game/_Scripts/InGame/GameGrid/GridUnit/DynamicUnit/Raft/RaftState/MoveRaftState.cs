using System.Collections.Generic;
using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.DynamicUnit.Interface;
using _Game.Managers;
using DG.Tweening;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Raft.RaftState
{
    public class MoveRaftState : IState<Raft>
    {
        private Direction _inverseDirection;
        private Tween moveTween;
        private bool _isMove;

        public StateEnum Id => StateEnum.Move;

        public void OnEnter(Raft t)
        {
            moveTween = null;
            if (t.blockDirectionUnits.Count > 0)
            {
                t.player.ChangeAnim(Constants.PUSH_ANIM);
                DOVirtual.DelayedCall(Constants.PUSH_TIME, () =>
                {
                    t.player.ChangeAnim(Constants.IDLE_ANIM);
                    for (int i = 0; i < t.blockDirectionUnits.Count; i++)
                        t.blockDirectionUnits[i].OnBePushed(t.rideRaftDirection, t.player);
                    t.blockDirectionUnits.Clear();
                    _inverseDirection = GridUnitFunc.InvertDirection(t.rideRaftDirection);
                    t.MovingData.SetData(_inverseDirection);
                    _isMove = t.ConditionMergeOnBePushed.IsApplicable(t.MovingData);
                    OnExecute(t);
                });
            }
            else
            {
                _inverseDirection = GridUnitFunc.InvertDirection(t.rideRaftDirection);
                t.MovingData.SetData(_inverseDirection);
                _isMove = t.ConditionMergeOnBePushed.IsApplicable(t.MovingData);
                OnExecute(t);
            }
        }

        public void OnExecute(Raft t)
        {
            if (!_isMove)
            {
                if (t.MovingData.blockDynamicUnits.Count > 0) t.OnPush(t.MovingData.inputDirection, t.MovingData);
                t.StateMachine.ChangeState(StateEnum.Idle);
                t.player.isRideVehicle = false;
                GameplayManager.Ins.IsCanUndo = true;
                //DEV: May be error here, old parent may not be null
                foreach (GridUnit unit in t.CarryUnits) unit.Tf.SetParent(null);              
            }
            else
            {
                foreach (GridUnit unit in t.CarryUnits) unit.Tf.SetParent(t.Tf);
                t.SetEnterCellData(t.MovingData.inputDirection, t.MovingData.enterMainCell, t.UnitTypeY, false,
                    t.MovingData.enterCells);
                t.OnOutCells();
                t.OnEnterCells(t.MovingData.enterMainCell, t.MovingData.enterCells);
                moveTween = t.Tf.DOMove(t.EnterPosData.finalPos, Constants.MOVING_TIME)
                    .SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed).OnComplete(() =>
                    {
                        foreach (GridUnit unit in t.CarryUnits)
                        {
                            unit.Tf.SetParent(null);
                            GameGridCell nextMainCellIn = unit.MainCell.GetNeighborCell(t.MovingData.inputDirection);
                            List<GameGridCell> nextCellsIn = unit.GetUnitNeighborCells(t.MovingData.inputDirection);
                            unit.SetEnterCellData(t.MovingData.inputDirection, nextMainCellIn, unit.UnitTypeY, false,
                                nextCellsIn);
                            unit.OnOutCells();
                            unit.OnEnterCells(nextMainCellIn, nextCellsIn);
                            unit.OnEnterTrigger(unit);
                        }

                        t.OnEnterTrigger(t);
                        t.StateMachine.ChangeState(StateEnum.Idle);
                        t.Ride(t.rideRaftDirection, t);
                    });
            }
        }

        public void OnExit(Raft t)
        {
            moveTween?.Kill();
        }
    }
}
