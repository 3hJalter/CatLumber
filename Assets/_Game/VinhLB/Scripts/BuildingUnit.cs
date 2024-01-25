using _Game.GameGrid.Unit;
using _Game.Utilities;

namespace VinhLB
{
    public class BuildingUnit : GridUnitStatic
    {
        public override void OnInteract()
        {
            DevLog.Log(DevId.Vinh, $"Interact with {gameObject.name}");
        }
    }
}
