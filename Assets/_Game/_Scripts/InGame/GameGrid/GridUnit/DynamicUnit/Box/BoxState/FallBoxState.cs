using System.Collections.Generic;
using _Game.DesignPattern;
using _Game.DesignPattern.StateMachine;
using _Game.Managers;
using _Game.Utilities;
using DG.Tweening;
using GameGridEnum;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Box.BoxState
{
    public class FallBoxState : IState<Box>
    {
        private readonly Vector3 WATER_SPLASH_OFFSET = Vector3.up * 0.18f;
        private Tween moveTween;
        public StateEnum Id => StateEnum.Fall;
        public void OnEnter(Box t)
        {
            // We know that when OnOutCell, the mainCell and cellInUnits will be cleared
            // That why we need to save it to use for OnEnterCell because all the mainCell and cellInUnits still the same as before
            GameGridCell mainCell = t.MainCell;
            //DEV: Can be optimize
            List<GameGridCell> cellInUnits = new(t.cellInUnits);
            t.SetEnterCellData(Direction.None, t.MainCell, t.UnitTypeY, false, t.cellInUnits);
            t.OnOutCells();
            t.OnEnterCells(mainCell, cellInUnits);
            
            // Tween to final position
            if (t.MovingData.enterMainCell.Data.canFloating)
            {
                //NOTE: Fall into water and checking if it is have object in water
                GridUnit unitInCells = null;
                // Check from the bottom of water + floating height offset to the bottom of the box
                HeightLevel floatingHeight = Constants.DirFirstHeightOfSurface[GridSurfaceType.Water] + t.FloatingHeightOffset;
                for (int i = 0; i < cellInUnits.Count; i++)
                {
                    for (HeightLevel h = floatingHeight; h <= t.BelowStartHeight; h++)
                    {
                        unitInCells = cellInUnits[i].GetGridUnitAtHeight(h);
                        if (unitInCells is not null) break;
                    }
                }
                
                //NOTE: Water do not have anything
                if (unitInCells == t || unitInCells is null)
                {
                    Sequence s = DOTween.Sequence();
                    moveTween = s;
                    s.Append(t.Tf.DOMove(t.EnterPosData.finalPos, Constants.MOVING_TIME * 0.6f).SetEase(Ease.Linear)
                        .OnComplete(() => ParticlePool.Play(DataManager.Ins.VFXData.GetParticleSystem(VFXType.WaterSplash), t.transform.position + WATER_SPLASH_OFFSET)))
                        .Append(t.Tf.DOMoveY(Constants.POS_Y_BOTTOM, Constants.MOVING_TIME * 0.6f).SetEase(Ease.Linear))
                        .OnComplete(() =>
                        {
                            t.OnEnterTrigger(t);
                            t.StateMachine.ChangeState(StateEnum.Emerge);
                        });

                }
                else
                {
                    //NOTE: Water have something
                    // Can be change to animation later
                    t.skin.localRotation =
                        Quaternion.Euler(t.UnitTypeXZ is UnitTypeXZ.Horizontal
                            ? Constants.HorizontalSkinRotation
                            : Constants.VerticalSkinRotation);
                    DevLog.Log(DevId.Hoang, "Box Fall into water cell that has object");
                    t.StateMachine.ChangeState(StateEnum.Emerge);
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

        public void OnExecute(Box t)
        {
            
        }

        public void OnExit(Box t)
        {
            moveTween.Kill();
        }
    }
}
