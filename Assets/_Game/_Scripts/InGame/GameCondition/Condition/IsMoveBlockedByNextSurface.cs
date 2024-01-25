using _Game._Scripts.InGame.GameCondition.Data;
using _Game.DesignPattern.ConditionRule;
using _Game.GameGrid;
using _Game.GameGrid.Unit;
using GameGridEnum;
using UnityEngine;

namespace _Game._Scripts.InGame.GameCondition.Condition
{
    [CreateAssetMenu(fileName = "IsMoveBlockedByNextSurface",
        menuName = "ConditionSO/Moving/IsMoveBlockedByNextSurface", order = 1)]
    public class IsMoveBlockedByNextSurface : ScriptableObject, ICondition
    {
        public bool IsApplicable(ConditionData data)
        {
            HeightLevel minOwnerHeight = Constants.DirFirstHeightOfSurface[GridSurfaceType.Water];

            #region verify data

            if (data is not MovingData movingData) return false;

            #endregion

            for (int i = 0; i < movingData.owner.cellInUnits.Count; i++)
            {
                GameGridCell neighbour = movingData.owner.cellInUnits[i].GetNeighborCell(movingData.inputDirection);
                if (neighbour is null) return false;
                HeightLevel neighbourHeight = Constants.DirFirstHeightOfSurface[neighbour.SurfaceType];
                if (neighbourHeight > minOwnerHeight) minOwnerHeight = neighbourHeight;
            }

            return movingData.owner.StartHeight >= minOwnerHeight;
        }
    }
}
