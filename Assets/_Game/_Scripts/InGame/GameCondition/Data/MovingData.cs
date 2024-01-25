using System.Collections.Generic;
using _Game.DesignPattern.ConditionRule;
using _Game.GameGrid;
using _Game.GameGrid.Unit;
using GameGridEnum;

namespace _Game._Scripts.InGame.GameCondition.Data
{
    public class MovingData : ConditionData
    {
        protected static int USING_TURN_COUNT = 0;
        protected int usingTurnId = 0;
        public int UsingTurnId => usingTurnId;

        public readonly List<GridUnitDynamic> blockDynamicUnits;
        public readonly List<GridUnitStatic> blockStaticUnits;

        public readonly List<GameGridCell> enterCells;

        // Generate after SetData
        public GameGridCell enterMainCell;

        // Input Data
        public Direction inputDirection;

        // Constructor
        public MovingData(GridUnit owner)
        {
            this.owner = owner;
            enterCells = new List<GameGridCell>();
            blockDynamicUnits = new List<GridUnitDynamic>();
            blockStaticUnits = new List<GridUnitStatic>();
        }

        // Set Data Method
        public virtual void SetData(Direction direction)
        {
            //DEV: Refactor
            UpdateUsingId();
            //NOTE: Set Data
            inputDirection = direction;
            enterCells.Clear();
            blockDynamicUnits.Clear();
            blockStaticUnits.Clear();
            enterMainCell = owner.MainCell.GetNeighborCell(direction);
            for (int i = 0; i < owner.cellInUnits.Count; i++)
            {
                GameGridCell neighbour = owner.cellInUnits[i].GetNeighborCell(direction);
                if (neighbour is null) continue;
                for (HeightLevel j = owner.StartHeight; j <= owner.EndHeight; j++)
                {
                    GridUnit unit = neighbour.GetGridUnitAtHeight(j);
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

                if (!enterCells.Contains(neighbour)) enterCells.Add(neighbour);
            }
        }

        protected void UpdateUsingId()
        {
            USING_TURN_COUNT++;
            usingTurnId = USING_TURN_COUNT;
        }
    }
}
