using System;
using _Game.GameGrid;
using _Game.GameGrid.Unit;
using _Game.GameGrid.Unit.DynamicUnit.Player;
using DG.Tweening;
using UnityEngine;

namespace _Game._Scripts.Tutorial.ConditionTutorial
{
    [CreateAssetMenu(fileName = "TutorialLevel4", menuName = "ScriptableObjects/TutorialData/Lvl4", order = 1)]
    public class TutorialConditionLevel4 : BaseTutorialData, ITutorialCondition
    {
        public void HandleShowTutorial(GameGridCell cell, GridUnit triggerUnit)
        {
            if (currentTutIndex == 0)
            {
                if (triggerUnit is not Player) return;
                // if Player at cell 7,7
                if (!(Math.Abs(cell.WorldX - 7) < Constants.TOLERANCE) ||
                    !(Math.Abs(cell.WorldY - 7) < Constants.TOLERANCE)) return;
                LevelManager.Ins.CurrentLevel.ChangeShadowUnitAlpha(false, 1);
                currentTutIndex++;
                return;
            }
            if (currentTutIndex == 1)
            {
                if (triggerUnit is not Player) return;
                // if Player at cell 7,7, return
                if (Math.Abs(cell.WorldX - 7) < Constants.TOLERANCE &&
                    Math.Abs(cell.WorldY - 7) < Constants.TOLERANCE) return;
                LevelManager.Ins.CurrentLevel.ChangeShadowUnitAlpha(true, 1);
                currentTutIndex++;
            }
        }

        public void HandleShowTutorial(GridUnit triggerUnit, GridUnit targetUnit)
        {
            
        }
        
        public void OnForceShowTutorial(int index, bool isIncrement = true)
        {
            
        }

       
    }
}
