using System;
using _Game._Scripts.Managers;
using _Game._Scripts.Tutorial.ObjectTutorial;
using _Game.GameGrid;
using _Game.GameGrid.Unit;
using _Game.GameGrid.Unit.DynamicUnit.Chump;
using _Game.GameGrid.Unit.DynamicUnit.Player;
using UnityEngine;

namespace _Game._Scripts.Tutorial.ConditionTutorial
{
    [CreateAssetMenu(fileName = "TutorialLevel2", menuName = "ScriptableObjects/TutorialData/Lvl2", order = 1)]
    public class TutorialConditionLevel2 : BaseTutorialData, ITutorialCondition
    {
        private BaseObjectTutorial glowSpot;
        private ArrowDirection arrowDirection;
        public void HandleShowTutorial(GameGridCell cell, GridUnit triggerUnit)
        {
            if (currentTutIndex == 0)
            {
                if (triggerUnit is not Player) return;
                // if player at cell 13,7
                if (!(Math.Abs(cell.WorldX - 13) < Constants.TOLERANCE) ||
                    !(Math.Abs(cell.WorldY - 7) < Constants.TOLERANCE)) return;
                glowSpot = Instantiate(TutorialManager.Ins.TutorialObjList[TutorialObj.LightSpot],
                    new Vector3(15,0,15), Quaternion.identity);
                currentTutIndex++;
                return;
            }
            if (currentTutIndex == 1)
            {   
                if (triggerUnit is not Player) return;
                // if player at cell 15,15
                if (!(Math.Abs(cell.WorldX - 15) < Constants.TOLERANCE) ||
                    !(Math.Abs(cell.WorldY - 15) < Constants.TOLERANCE)) return;
                // Turn off glow spot
                Destroy(glowSpot.gameObject);
                arrowDirection = (ArrowDirection) Instantiate(TutorialManager.Ins.TutorialObjList[TutorialObj.Arrow],
                    new Vector3(16,0,16.5f), Quaternion.identity);
                arrowDirection.PointerToHeight(3, Direction.Left, true);
                currentTutIndex++;
                return;
            }
            if (currentTutIndex == 2)
            {
                if (triggerUnit is not Chump) return;
                Destroy(arrowDirection.gameObject);
                currentTutIndex++;
            }
        }

        public void OnForceShowTutorial(int index, bool isIncrement = true)
        { }
        
        public void HandleShowTutorial(GridUnit triggerUnit, GridUnit targetUnit)
        { }
    }
}
