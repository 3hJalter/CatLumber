using _Game._Scripts.InGame.GameCondition.Data;
using _Game.DesignPattern.ConditionRule;
using _Game.GameGrid.Unit;
using _Game.GameGrid.Unit.DynamicUnit.Chump;
using _Game.GameGrid.Unit.StaticUnit;
using UnityEngine;
namespace _Game._Scripts.InGame.GameCondition.Condition
{
    [CreateAssetMenu(fileName = "IsMoveBlockedByNextUnit", menuName = "ConditionSO/Moving/IsMoveBlockedByNextUnit",
        order = 1)]
    public class IsMoveBlockedByNextUnit : ScriptableObject, ICondition
    {
        public bool IsApplicable(ConditionData data)
        {

            #region verify data

            if (data is not MovingData movingData) return false;
            data.Condition = CONDITION.NONE;

            #endregion
            switch (movingData.blockDynamicUnits.Count)
            {
                case 0:
                    switch (movingData.blockStaticUnits.Count)
                    {
                        case 0:
                            return true;
                        case 1 when movingData.blockStaticUnits[0] is TreeRoot
                        && movingData.owner is IJumpTreeRootUnit jumpTreeRootUnit && jumpTreeRootUnit.CanJumpOnTreeRoot(movingData.inputDirection):
                            data.Condition = CONDITION.BE_BLOCKED_BY_TREE_ROOT;
                            return true;
                        default:
                            return false;
                    }
                case 1 when (movingData.blockDynamicUnits[0] is Chump blockChump && blockChump.UnitTypeY == UnitTypeY.Down)
                && (movingData.owner is Chump pushChump && pushChump.UnitTypeY == UnitTypeY.Up) && IsDifferentDirection(movingData.inputDirection, blockChump.UnitTypeXZ):
                    data.Condition = CONDITION.ROLL_AROUND_BLOCK_CHUMP;
                    return true;
                default: 
                    return false;
            }

            //return movingData.blockDynamicUnits.Count switch
            //{
            //    0 when movingData.blockStaticUnits.Count == 0 => true,
            //    0 when movingData.blockStaticUnits.Count == 1 && movingData.blockStaticUnits[0] is TreeRoot =>
            //        movingData.owner is IJumpTreeRootUnit jumpTreeRootUnit && jumpTreeRootUnit.CanJumpOnTreeRoot(movingData.inputDirection),             
            //    1 when (movingData.blockDynamicUnits[0] is Chump blockChump && blockChump.UnitTypeY == UnitTypeY.Down) 
            //    && (movingData.owner is Chump pushChump && pushChump.UnitTypeY == UnitTypeY.Up) && IsDifferentDirection(movingData.inputDirection, blockChump.UnitTypeXZ) => true,
            //    _ => false,
            //};
            // Temporary for handle TreeRoot
            // for (int i = 0; i < movingData.blockDynamicUnits.Count; i++)
            // {
            //     GridUnit unit = movingData.blockDynamicUnits[i];
            //     if (!unit.CanMoveFromUnit(movingData.inputDirection, movingData.owner)) return false;
            // }
            // Temporary for handle Tree Root

        }

        private bool IsDifferentDirection(Direction direction, UnitTypeXZ type)
        {
            switch (direction)
            {
                case Direction.Left:
                case Direction.Right:
                    switch(type)
                    {
                        case UnitTypeXZ.Vertical:
                            return true;
                        default:
                            return false;
                    }
                case Direction.Forward:
                case Direction.Back:
                    switch (type)
                    {
                        case UnitTypeXZ.Horizontal:
                            return true;
                        default:
                            return false;
                    }
            }
            return false;
        }
    }
}
