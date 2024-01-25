using System.Collections.Generic;
using _Game.DesignPattern;
using _Game.DesignPattern.StateMachine;
using _Game.Managers;
using _Game.Utilities;
using DG.Tweening;
using GameGridEnum;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Chump.ChumpState
{
    public class FallChumpState : IState<Chump>
    {
        private readonly Vector3 WATER_SPLASH_OFFSET = Vector3.up * 0.18f;
        private Tween moveTween;
        public StateEnum Id => StateEnum.Fall;

        public void OnEnter(Chump t)
        {
            // We know that when OnOutCell, the mainCell and cellInUnits will be cleared
            // That why we need to save it to use for OnEnterCell because all the mainCell and cellInUnits still the same as before
            GameGridCell mainCell = t.MainCell;
            //DEV: Can be optimize
            List<GameGridCell> cellInUnits = new(t.cellInUnits);
            t.SetEnterCellData(Direction.None, t.MainCell, t.UnitTypeY, false, t.cellInUnits);
            t.OnOutCells();
            t.OnEnterCells(mainCell, cellInUnits);

            if (t.MainMovingData.enterMainCell.SurfaceType == GridSurfaceType.Water)
            {
                //NOTE: Fall into water and checking if it is have object in water
                GridUnit unitInCells = null;

                for (int i = 0; i < cellInUnits.Count; i++)
                {
                    unitInCells = cellInUnits[i].GetGridUnitAtHeight(
                        Constants.DirFirstHeightOfSurface[GridSurfaceType.Water]
                        + t.FloatingHeightOffset); // Check the floating unit, also we know water is floatSurface, no need to check its bool
                    if (unitInCells != null) break;
                }

                //NOTE: Water do not have anything
                if (unitInCells == t || unitInCells == null)
                {
                    // Tween to final position
                    DevLog.Log(DevId.Hung, "Fall into water cell that do not have anything");
                    Sequence s = DOTween.Sequence();
                    // Kill the other tween that currently running on the object
                    moveTween = s;
                    s.Append(t.Tf.DOMove(t.EnterPosData.finalPos, Constants.MOVING_TIME * 0.6f).SetEase(Ease.Linear).OnComplete(
                        () => ParticlePool.Play(DataManager.Ins.VFXData.GetParticleSystem(VFXType.WaterSplash), t.Tf.position + WATER_SPLASH_OFFSET)))
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
                }
                else //NOTE: Water have something
                {
                    DevLog.Log(DevId.Hung, "Fall into water cell that has object");
                    // t.StateMachine.ChangeState(StateEnum.FormRaft);
                    moveTween = t.Tf.DOMove(t.EnterPosData.finalPos, Constants.MOVING_TIME * 0.6f)
                        .SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed).OnComplete(() =>
                        {
                            t.OnEnterTrigger(t);
                            t.StateMachine.ChangeState(StateEnum.Idle);
                        });
                }

            }
            else
            {
                moveTween = t.Tf.DOMove(t.EnterPosData.finalPos, Constants.MOVING_TIME * 0.6f)
                    .SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed).OnComplete(() =>
                    {
                        t.OnEnterTrigger(t);
                        t.StateMachine.ChangeState(StateEnum.Idle);
                    });
            }
        }

        public void OnExecute(Chump t)
        {

        }

        public void OnExit(Chump t)
        {
            moveTween.Kill();
        }
    }
}
