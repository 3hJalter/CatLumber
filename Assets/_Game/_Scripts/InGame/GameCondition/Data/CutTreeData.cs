using _Game.DesignPattern.ConditionRule;
using _Game.GameGrid.Unit;
using _Game.GameGrid.Unit.StaticUnit;

namespace _Game._Scripts.InGame.GameCondition.Data
{
    public class CutTreeData : ConditionData
    {
        public Direction inputDirection;

        public Tree tree;

        // Constructor
        public CutTreeData(GridUnit owner)
        {
            this.owner = owner;
        }

        // Set Data Method
        public void SetData(Direction direction, Tree treeIn)
        {
            inputDirection = direction;
            tree = treeIn;
        }
    }
}
