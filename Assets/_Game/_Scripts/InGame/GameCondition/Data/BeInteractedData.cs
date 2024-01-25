using _Game.DesignPattern.ConditionRule;
using _Game.GameGrid.Unit;

namespace _Game._Scripts.InGame.GameCondition.Data
{
    public class BeInteractedData : ConditionData
    {
        // InputData
        public Direction inputDirection;

        public GridUnit pushUnit;

        // Constructor
        public BeInteractedData(GridUnit owner)
        {
            this.owner = owner;
        }

        // Set Data Method
        public void SetData(Direction direction, GridUnit interactedUnitIn)
        {
            inputDirection = direction;
            pushUnit = interactedUnitIn;
        }
    }
}
