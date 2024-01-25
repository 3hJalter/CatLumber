using System;
using _Game._Scripts.Managers;
using _Game._Scripts.Tutorial.ObjectTutorial;
using _Game.GameGrid;
using _Game.GameGrid.Unit;
using _Game.GameGrid.Unit.DynamicUnit.Player;
using _Game.Managers;
using DG.Tweening;
using UnityEngine;

namespace _Game._Scripts.Tutorial.ConditionTutorial
{
    [CreateAssetMenu(fileName = "TutorialLevel9", menuName = "ScriptableObjects/TutorialData/Lvl9", order = 1)]
    public class TutorialConditionLevel9 : BaseTutorialData, ITutorialCondition
    {
        private BaseObjectTutorial glowSpot;
        public void OnForceShowTutorial(int index, bool isIncrement = true)
        {
            
        }

        public void HandleShowTutorial(GameGridCell cell, GridUnit triggerUnit)
        {
            if (currentTutIndex == 0)
            {
                if (triggerUnit is not Player) return;
                // if player at cell 9,7
                if (!(Math.Abs(cell.WorldX - 9) < Constants.TOLERANCE) ||
                    !(Math.Abs(cell.WorldY - 7) < Constants.TOLERANCE)) return;
                glowSpot = Instantiate(TutorialManager.Ins.TutorialObjList[TutorialObj.LightSpot],
                    new Vector3(11,0,15), Quaternion.identity);
                currentTutIndex++;
            }
            
            if (currentTutIndex == 1)
            {
                if (triggerUnit is not Player) return;
                // if player at cell 13,13 or 11,15 or 11,17
                if ((Math.Abs(cell.WorldX - 13) < Constants.TOLERANCE &&
                     Math.Abs(cell.WorldY - 13) < Constants.TOLERANCE) ||
                    (Math.Abs(cell.WorldX - 11) < Constants.TOLERANCE &&
                     Math.Abs(cell.WorldY - 15) < Constants.TOLERANCE) ||
                    (Math.Abs(cell.WorldX - 11) < Constants.TOLERANCE &&
                     Math.Abs(cell.WorldY - 17) < Constants.TOLERANCE))
                {
                    Destroy(glowSpot.gameObject);
                    currentScreen = UIManager.Ins.OpenUIDirectly(tutorialScreens[0]);
                    currentTutIndex++;
                    return;
                }
            }
            if (currentTutIndex == 2)
            {
                LevelManager.Ins.CurrentLevel.ChangeShadowUnitAlpha(true);
                currentTutIndex++;
            }
        }

        public void HandleShowTutorial(GridUnit triggerUnit, GridUnit targetUnit)
        {
            
        }
    }
}
