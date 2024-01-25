using _Game.DesignPattern;
using _Game.DesignPattern.StateMachine;
using _Game.Utilities.Timer;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using _Game._Scripts.InGame.GameCondition.Data;
using UnityEngine;
using _Game.Utilities;
using _Game.Managers;

namespace _Game.GameGrid.Unit.DynamicUnit.Chump.ChumpState
{
    public class RollBlockChumpState : IState<Chump>
    {
        private int DEGREE = 60;
        Vector3 axis;
        float lastAngle = 0;
        Direction blockDirection;
        Tween moveTween;

        List<GridUnit> blockObjects = new();

        public StateEnum Id => StateEnum.RollBlock;

        public void OnEnter(Chump t)
        {
            blockObjects.Clear();
            GameplayManager.Ins.IsCanUndo = false;
            OnExecute(t);
        }

        public void OnExecute(Chump t)
        {
            switch (t.UnitTypeY)
            {
                case UnitTypeY.Up:
                    GetBlockObjects(t.MainMovingData);
                    //NOTE: Blocking when chump is up
                    blockDirection = t.MainMovingData.inputDirection;
                    axis = Vector3.Cross(Vector3.up, Constants.DirVector3[blockDirection]);
                    lastAngle = 0;
                    DEGREE = 90 - (blockObjects[0].Size.y + 1) * 15;

                    moveTween = DOVirtual.Float(0, DEGREE * 2, Constants.MOVING_TIME * 1f, i =>
                    {
                        i = i <= DEGREE ? i : 2 * DEGREE - i;
                        t.skin.RotateAround(t.anchor.Tf.position, axis, i - lastAngle);
                        lastAngle = i;
                    }).SetUpdate(UpdateType.Fixed)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => t.StateMachine.ChangeState(StateEnum.Idle));


                    if (blockObjects.Count > 0)
                        TimerManager.Inst.WaitForTime(Constants.MOVING_TIME * 0.5f, ObjectBlocking);
                    break;
                case UnitTypeY.Down:
                    //NOTE: Blocking when chump is down
                    GetBlockObjects(t.MainMovingData);
                    blockDirection = t.MainMovingData.inputDirection;
                    if (!IsSamePushDirection())
                    {
                        Vector3 originPos = t.Tf.position;
                        moveTween = t.Tf.DOMove(originPos + Constants.DirVector3F[blockDirection] * Constants.CELL_SIZE / 2f, Constants.MOVING_TIME * 0.5f).SetEase(Ease.InQuad)
                            .OnComplete(() => t.Tf.DOMove(originPos, Constants.MOVING_TIME * 0.5f).SetEase(Ease.OutQuad).OnComplete(ChangeToIdle));
                    }
                    else
                    {
                        Vector3 originPos = t.Tf.position;
                        moveTween = t.Tf.DOShakePosition(Constants.MOVING_TIME * 0.8f, 0.1f, 60, 0, false, true, ShakeRandomnessMode.Harmonic).SetEase(Ease.InQuad)
                            .OnComplete(ChangeToIdle);
                    }
                    if (blockObjects.Count > 0)
                        TimerManager.Inst.WaitForTime(Constants.MOVING_TIME * 0.5f, ObjectBlocking);

                    break;
            }

            return;

            void GetBlockObjects(MovingData data)
            {
                blockObjects.AddRange(data.blockStaticUnits);
                blockObjects.AddRange(data.blockDynamicUnits);
            }

            void ObjectBlocking()
            {
                string objectBlocking = "BLOCKING - ";
                for (int i = 0; i < blockObjects.Count; i++)
                {
                    blockObjects[i].OnBePushed(blockDirection);
                    objectBlocking += $"{blockObjects[i]} ";
                }
                if (t.MainMovingData.blockDynamicUnits.Count > 0) t.OnPush(t.MainMovingData.inputDirection, t.MainMovingData);
                DevLog.Log(DevId.Hung, $"{objectBlocking} || DYNAMIC - {t.MainMovingData.blockDynamicUnits.Count}");
                //NOTE: Checking if push dynamic object does not create any change in grid -> discard the newest save.
                if (!LevelManager.Ins.CurrentLevel.GridMap.IsChange)
                    LevelManager.Ins.DiscardSaveState();
            }

            bool IsSamePushDirection()
            {
                if ((t.UnitTypeXZ == UnitTypeXZ.Horizontal && (t.MainMovingData.inputDirection == Direction.Left || t.MainMovingData.inputDirection == Direction.Right))
                    || (t.UnitTypeXZ == UnitTypeXZ.Vertical && (t.MainMovingData.inputDirection == Direction.Forward || t.MainMovingData.inputDirection == Direction.Back)))
                {
                    return true;
                }
                return false;
            }

            void ChangeToIdle()
            {
                t.StateMachine.ChangeState(StateEnum.Idle);
            }
        }

        public void OnExit(Chump t)
        {
            GameplayManager.Ins.IsCanUndo = true;
            moveTween.Kill();
        }

    }
}