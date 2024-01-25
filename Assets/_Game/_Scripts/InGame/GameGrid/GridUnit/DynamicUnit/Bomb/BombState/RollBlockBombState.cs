using System.Collections.Generic;
using _Game._Scripts.InGame.GameCondition.Data;
using _Game.DesignPattern.StateMachine;
using _Game.Managers;
using _Game.Utilities;
using _Game.Utilities.Timer;
using DG.Tweening;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Bomb.BombState
{
    public class RollBlockBombState : IState<Bomb>
    {
        private int degree = 60;
        private Vector3 axis;
        private float lastAngle;
        private Direction blockDirection;
        private Tween moveTween;

        private readonly List<GridUnit> blockObjects = new();

        public StateEnum Id => StateEnum.RollBlock;

        public void OnEnter(Bomb t)
        {
            blockObjects.Clear();
            GameplayManager.Ins.IsCanUndo = false;
            OnExecute(t);
        }

        public void OnExecute(Bomb t)
        {
            switch (t.UnitTypeY)
            {
                case UnitTypeY.Up:
                    GetBlockObjects(t.MovingData);
                    //NOTE: Blocking when chump is up
                    blockDirection = t.MovingData.inputDirection;
                    axis = Vector3.Cross(Vector3.up, Constants.DirVector3[blockDirection]);
                    lastAngle = 0;
                    degree = 90 - (blockObjects[0].Size.y + 1) * 15;

                    moveTween = DOVirtual.Float(0, degree * 2, Constants.MOVING_TIME * 1f, i =>
                    {
                        i = i <= degree ? i : 2 * degree - i;
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
                    GetBlockObjects(t.MovingData);
                    blockDirection = t.MovingData.inputDirection;
                    if (!IsSamePushDirection())
                    {
                        Vector3 originPos = t.Tf.position;
                        moveTween = t.Tf.DOMove(originPos + Constants.DirVector3F[blockDirection] * Constants.CELL_SIZE / 2f, Constants.MOVING_TIME * 0.5f).SetEase(Ease.InQuad)
                            .OnComplete(() => t.Tf.DOMove(originPos, Constants.MOVING_TIME * 0.5f).SetEase(Ease.OutQuad).OnComplete(ChangeToIdle));
                    }
                    else
                    {
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
                if (t.MovingData.blockDynamicUnits.Count > 0) t.OnPush(t.MovingData.inputDirection, t.MovingData);
                DevLog.Log(DevId.Hung, $"{objectBlocking} || DYNAMIC - {t.MovingData.blockDynamicUnits.Count}");
                //NOTE: Checking if push dynamic object does not create any change in grid -> discard the newest save.
                if (!LevelManager.Ins.CurrentLevel.GridMap.IsChange)
                    LevelManager.Ins.DiscardSaveState();
            }

            bool IsSamePushDirection()
            {
                if ((t.UnitTypeXZ == UnitTypeXZ.Horizontal && (t.MovingData.inputDirection == Direction.Left || t.MovingData.inputDirection == Direction.Right))
                    || (t.UnitTypeXZ == UnitTypeXZ.Vertical && (t.MovingData.inputDirection == Direction.Forward || t.MovingData.inputDirection == Direction.Back)))
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

        public void OnExit(Bomb t)
        {
            GameplayManager.Ins.IsCanUndo = true;
            moveTween.Kill();
        }

    }
}
