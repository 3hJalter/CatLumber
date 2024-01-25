using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.StaticUnit;
using _Game.Managers;
using DG.Tweening;
using GameGridEnum;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace _Game.GameGrid.Unit.DynamicUnit.Chump.ChumpState
{
    public class TurnOverChumpState : IState<Chump>
    {
        private bool _isTurnOver;
        private bool isContinueTurnOver = false;
        private Vector3 anchorAdd;

        public StateEnum Id => StateEnum.TurnOver;

        public void OnEnter(Chump t)
        {
            t.TurnOverData.SetData(t.BeInteractedData.inputDirection);
            _isTurnOver = t.ConditionMergeOnBePushed.IsApplicable(t.TurnOverData);
            anchorAdd.Set(0, 0, 0);
            GameplayManager.Ins.IsCanUndo = false;
            OnExecute(t);
        }

        public void OnExecute(Chump t)
        {
            if (!_isTurnOver)
            {
                t.StateMachine.ChangeState(StateEnum.RollBlock);
            }
            else
            {
                #region Specific Conditions
                switch (t.TurnOverData.Condition)
                {
                    case CONDITION.BE_BLOCKED_BY_TREE_ROOT:
                        t.TurnOverData.blockStaticUnits[0].UpHeight(t, t.TurnOverData.inputDirection);
                        break;
                    case CONDITION.ROLL_AROUND_BLOCK_CHUMP:
                        isContinueTurnOver = true;
                        t.TurnOverData.blockDynamicUnits[0].UpHeight(t, t.TurnOverData.inputDirection);
                        break;
                }
                #endregion

                #region Handle Cell Data

                t.anchor.ChangeAnchorPos(t, t.TurnOverData.inputDirection);
                t.Size = t.TurnOverData.nextSize;
                Vector3 skinOffset = t.TurnOverData.enterMainCell.WorldPos - t.MainCell.WorldPos;
                UnitTypeY switchType = t.UnitTypeY == UnitTypeY.Up ? UnitTypeY.Down : UnitTypeY.Up;
                t.SetEnterCellData(t.TurnOverData.inputDirection, t.TurnOverData.enterMainCell, switchType, false,
                    t.TurnOverData.enterCells);
                t.OnOutCells();

                if (t.UnitTypeY is UnitTypeY.Up ||
                    (!t.TurnOverData.enterMainCell.Data.canFloating &&
                     t.UnitTypeY is UnitTypeY.Down))
                    t.SwitchType(t.TurnOverData.inputDirection);
                else if (t.TurnOverData.enterMainCell.Data.canFloating
                         && t.TurnOverData.enterMainCell.GetGridUnitAtHeight(
                             Constants.DirFirstHeightOfSurface[GridSurfaceType.Water] + t.FloatingHeightOffset) is not null)
                    t.SwitchType(t.TurnOverData.inputDirection);

                t.OnEnterCells(t.TurnOverData.enterMainCell, t.TurnOverData.enterCells);

                #endregion

                #region Animation and complete

                Vector3 axis = Vector3.Cross(Vector3.up, Constants.DirVector3[t.TurnOverData.inputDirection]);
                // Roll 90 degree around anchor in 0.18 second using Tween
                float lastAngle = 0;
                switch(t.TurnOverData.Condition)
                {
                    case CONDITION.BE_BLOCKED_BY_TREE_ROOT:
                    Vector3 position = t.Tf.position;
                    t.Tf.DOMove(new Vector3(position.x, t.EnterPosData.finalPos.y, position.z), Constants.MOVING_TIME)
                        .SetEase(Ease.Linear)
                        .SetUpdate(UpdateType.Fixed);
                        break;
                    case CONDITION.ROLL_AROUND_BLOCK_CHUMP:
                        anchorAdd = (Constants.DirVector3F[t.TurnOverData.inputDirection] + Vector3.up) * Constants.CELL_SIZE / 8f;
                        skinOffset += Vector3.up * Constants.CELL_SIZE / 4f;
                        break;
                }

                // TEMPORARY FIX: anchor position is not correct when Player Turn Over Chump at Raft
                
                DOVirtual.Float(0, 90, Constants.MOVING_TIME, i =>
                {
                    t.skin.RotateAround(t.anchor.Tf.position + anchorAdd, axis, i - lastAngle);
                    lastAngle = i;
                }).SetUpdate(UpdateType.Fixed).SetEase(Ease.Linear).OnComplete(() =>
                {
                    t.skin.localPosition -= skinOffset;
                    t.Tf.position = t.EnterPosData.initialPos;
                    if (isContinueTurnOver)
                    {
                        t.OnEnterTrigger(t);
                        t.StateMachine.ChangeState(StateEnum.TurnOver);
                    }
                    else if (!t.EnterPosData.isFalling)
                    {
                        t.OnEnterTrigger(t);
                        t.StateMachine.ChangeState(StateEnum.Idle);
                        
                    }                    
                    else
                    {
                        t.StateMachine.ChangeState(StateEnum.Fall);
                    }
                });

                #endregion

            }
        }

        public void OnExit(Chump t)
        {
            if (!isContinueTurnOver)
            {
                GameplayManager.Ins.IsCanUndo = true;
            }
            isContinueTurnOver = false;
        }
    }
}
