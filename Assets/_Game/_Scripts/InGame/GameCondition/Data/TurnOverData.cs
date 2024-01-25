using _Game.GameGrid;
using _Game.GameGrid.Unit;
using GameGridEnum;
using UnityEngine;

namespace _Game._Scripts.InGame.GameCondition.Data
{
    public class TurnOverData : MovingData
    {
        public HeightLevel nextEndHeight;

        public Vector3Int nextSize;

        // Constructor
        public TurnOverData(GridUnit runRuleUnit) : base(runRuleUnit)
        {
        }

        // Set Data Method
        public override void SetData(Direction direction)
        {
            //DEV: Refactor
            UpdateUsingId();
            //NOTE: Set Data
            inputDirection = direction;
            enterCells.Clear();
            blockDynamicUnits.Clear();
            blockStaticUnits.Clear();
            nextSize = owner.Size;
            nextEndHeight = owner.EndHeight;
            SetNextMainCell();
            if (enterMainCell is null) return;
            int xAxisLoop = direction is Direction.Left or Direction.Right
                ? owner.Size.y
                : owner.Size.x;
            int zAxisLoop = direction is Direction.Left or Direction.Right
                ? owner.Size.z
                : owner.Size.y;
            Vector2Int nexMainCellPos = enterMainCell.GetCellPosition();
            nextSize = GridUnitFunc.RotateSize(direction, owner.Size);
            nextEndHeight = owner.CalculateEndHeight(owner.StartHeight, nextSize);
            HeightLevel endHeightForChecking = nextEndHeight > owner.EndHeight ? nextEndHeight : owner.EndHeight;
            for (int i = 0; i < xAxisLoop; i++)
            for (int j = 0; j < zAxisLoop; j++)
            {
                Vector2Int cellPos = nexMainCellPos + new Vector2Int(i, j);
                GameGridCell cell = LevelManager.Ins.CurrentLevel.GetCell(cellPos);
                if (cell is null) continue;
                for (HeightLevel k = owner.StartHeight; k <= endHeightForChecking; k++)
                {
                    GridUnit unit = cell.GetGridUnitAtHeight(k);
                    if (unit is null || unit == owner) continue;
                    switch (unit)
                    {
                        case GridUnitDynamic dynamicUnit when !blockDynamicUnits.Contains(dynamicUnit):
                            blockDynamicUnits.Add(dynamicUnit);
                            break;
                        case GridUnitStatic staticUnit when !blockStaticUnits.Contains(staticUnit):
                            blockStaticUnits.Add(staticUnit);
                            break;
                    }
                }

                enterCells.Add(cell);
            }

            return;

            void SetNextMainCell()
            {
                Vector2Int nextMainCellPos = owner.MainCell.GetCellPosition() + GetOffset();
                enterMainCell = LevelManager.Ins.CurrentLevel.GetCell(nextMainCellPos);
                return;

                Vector2Int GetOffset()
                {
                    switch (direction)
                    {
                        case Direction.Left:
                            return new Vector2Int(-owner.Size.y, 0);
                        case Direction.Right:
                            return new Vector2Int(owner.Size.x, 0);
                        case Direction.Forward:
                            return new Vector2Int(0, owner.Size.z);
                        case Direction.Back:
                            return new Vector2Int(0, -owner.Size.y);
                        case Direction.None:
                        default:
                            return Vector2Int.zero;
                    }
                }
            }
        }
    }
}
