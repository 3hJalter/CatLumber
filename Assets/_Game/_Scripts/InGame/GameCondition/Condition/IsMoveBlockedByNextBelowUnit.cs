using System.Collections.Generic;
using _Game._Scripts.InGame.GameCondition.Data;
using _Game.DesignPattern.ConditionRule;
using _Game.GameGrid.Unit;
using GameGridEnum;
using UnityEngine;

namespace _Game._Scripts.InGame.GameCondition.Condition
{
    [CreateAssetMenu(fileName = "IsMoveBlockedByNextBelowUnit",
        menuName = "ConditionSO/Moving/IsMoveBlockedByNextBelowUnit",
        order = 1)]
    public class IsMoveBlockedByNextBelowUnit : ScriptableObject, ICondition
    {
        public bool IsApplicable(ConditionData dataIn)
        {
            if (dataIn is not MovingData data) return false;
            if (data.owner is not GridUnitDynamic player) return false;
            if (data.enterMainCell is null) return false;
            bool canMoveDirectly = data.enterMainCell.Data.canMovingDirectly;
            HeightLevel belowSurfaceStartHeight = Constants.DirFirstHeightOfSurface[data.enterMainCell.SurfaceType];
            // Get all objects in next cell from below this (Player) to Surface Start Height
            List<GridUnit> unitsInNextCell = new();
            for (HeightLevel i = player.StartHeight - 1; i >= belowSurfaceStartHeight; i--)
            {
                GridUnit unit = data.enterMainCell.GetGridUnitAtHeight(i);
                if (unit is null || unitsInNextCell.Contains(unit)) continue;
                unitsInNextCell.Add(unit);
            }

            // if no objects
            if (unitsInNextCell.Count == 0) return player.StartHeight - 2 <= belowSurfaceStartHeight && canMoveDirectly;

            GridUnit firstUnit = unitsInNextCell[0];
            HeightLevel firstObjectEndHeight = firstUnit.EndHeight;
            // If the distance = this (Player) sH - first object of the list End Height (eH) > 2 -> Rule Reject
            int distance = player.StartHeight - firstObjectEndHeight;
            switch (distance)
            {
                case > 2:
                    return false;
                case 2:
                    return firstUnit.UnitTypeXZ switch
                    {
                        UnitTypeXZ.None => false,
                        UnitTypeXZ.Both => true,
                        UnitTypeXZ.Vertical => data.inputDirection is Direction.Forward or Direction.Back,
                        UnitTypeXZ.Horizontal => data.inputDirection is Direction.Left or Direction.Right,
                        _ => true
                    };
                default:
                    switch (firstUnit.UnitTypeXZ)
                    {
                        case UnitTypeXZ.None:
                            if (firstUnit.StartHeight == belowSurfaceStartHeight  && !canMoveDirectly)
                                return false; // TEMP
                            switch (firstUnit)
                            {
                                case GridUnitDynamic dynamicUnit when !data.blockDynamicUnits.Contains(dynamicUnit):
                                    data.blockDynamicUnits.Add(dynamicUnit);
                                    break;
                                case GridUnitStatic staticUnit when !data.blockStaticUnits.Contains(staticUnit):
                                    data.blockStaticUnits.Add(staticUnit);
                                    break;
                            }

                            return false;
                        case UnitTypeXZ.Both:
                        case UnitTypeXZ.Vertical when data.inputDirection is Direction.Forward or Direction.Back:
                            return true;
                        case UnitTypeXZ.Vertical:
                            if (firstUnit.StartHeight == belowSurfaceStartHeight 
                                + (data.enterMainCell.Data.canFloating ? firstUnit.FloatingHeightOffset : 0) 
                                && !canMoveDirectly)
                                return false; // TEMP
                            switch (firstUnit)
                            {
                                case GridUnitDynamic dynamicUnit when !data.blockDynamicUnits.Contains(dynamicUnit):
                                    data.blockDynamicUnits.Add(dynamicUnit);
                                    break;
                                case GridUnitStatic staticUnit when !data.blockStaticUnits.Contains(staticUnit):
                                    data.blockStaticUnits.Add(staticUnit);
                                    break;
                            }

                            return false;
                        case UnitTypeXZ.Horizontal when data.inputDirection is Direction.Left or Direction.Right:
                            return true;
                        case UnitTypeXZ.Horizontal:
                            if (firstUnit.StartHeight == belowSurfaceStartHeight 
                                + (data.enterMainCell.Data.canFloating ? firstUnit.FloatingHeightOffset : 0) 
                                && !canMoveDirectly)
                                return false; // TEMP
                            switch (firstUnit)
                            {
                                case GridUnitDynamic dynamicUnit when !data.blockDynamicUnits.Contains(dynamicUnit):
                                    data.blockDynamicUnits.Add(dynamicUnit);
                                    break;
                                case GridUnitStatic staticUnit when !data.blockStaticUnits.Contains(staticUnit):
                                    data.blockStaticUnits.Add(staticUnit);
                                    break;
                            }

                            return false;
                        default:
                            return true;
                    }
            }

        }
    }
}
