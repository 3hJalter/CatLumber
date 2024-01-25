using _Game._Scripts.InGame.GameCondition.Data;
using _Game.DesignPattern.ConditionRule;
using _Game.GameGrid;
using _Game.GameGrid.Unit;
using GameGridEnum;
using UnityEngine;

namespace _Game._Scripts.InGame.GameCondition.Condition
{
    [CreateAssetMenu(fileName = "IsMoveBlockedByBelowUnit", menuName = "ConditionSO/Moving/IsMoveBlockedByBelowUnit",
        order = 1)]
    public class IsMoveBlockedByBelowUnit : ScriptableObject, ICondition
    {
        public bool IsApplicable(ConditionData dataIn)
        {
            if (dataIn is not MovingData data) return false;
            if (data.owner is not GridUnitDynamic player) return false;
            // Get Below Player Information
            GameGridCell currentCell = player.MainCell;
            bool canMoveDirectly = currentCell.Data.canMovingDirectly;
            HeightLevel belowSurfaceStartHeight = Constants.DirFirstHeightOfSurface[currentCell.SurfaceType];
            // Condition
            if (canMoveDirectly) return true;
            if (player.StartHeight <= belowSurfaceStartHeight) return false;
            GridUnit belowUnit = player.GetBelowUnitAtMainCell();
            if (!belowUnit) return false;
            // Only Consider the first unit (MainCell)
            return belowUnit.UnitTypeXZ switch
            {
                UnitTypeXZ.None => false,
                UnitTypeXZ.Both => true,
                UnitTypeXZ.Vertical => data.inputDirection is Direction.Forward or Direction.Back,
                UnitTypeXZ.Horizontal => data.inputDirection is Direction.Left or Direction.Right,
                _ => true
            };
        }
    }
}
