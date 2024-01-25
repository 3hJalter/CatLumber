using System;
using System.Collections.Generic;
using _Game._Scripts.Managers;
using _Game._Scripts.Tutorial.ObjectTutorial;
using _Game.GameGrid;
using _Game.GameGrid.Unit;
using _Game.GameGrid.Unit.DynamicUnit.Player;
using _Game.Managers;
using UnityEngine;

namespace _Game._Scripts.Tutorial.ConditionTutorial
{
    [CreateAssetMenu(fileName = "TutorialLevel1", menuName = "ScriptableObjects/TutorialData/Lvl1", order = 1)]
    public class TutorialConditionLevel1 : BaseTutorialData, ITutorialCondition
    {
        // private BaseObjectTutorial glowSpot;
        // private ArrowDirection arrowDirection;
        
        public void OnForceShowTutorial(int index, bool isIncrement = true)
        {
            if (index < 0 || index >= tutorialScreens.Count) return;
            currentTutIndex = index;
            currentScreen = UIManager.Ins.OpenUIDirectly(tutorialScreens[currentTutIndex]);
            if (isIncrement) currentTutIndex++;
        }
        
        public void HandleShowTutorial(GameGridCell cell, GridUnit triggerUnit)
        {
            if (triggerUnit is not Player) return;
            switch (currentTutIndex)
            {
                // Case 0 already handle when the cutscene is playing
                case 1:
                {
                    // if player at cell 7, 11
                    if (Math.Abs(cell.WorldX - 7) < Constants.TOLERANCE && Math.Abs(cell.WorldY - 11) < Constants.TOLERANCE) 
                    {
                        MoveInputManager.Ins.OnForceResetMove();
                        currentScreen = UIManager.Ins.OpenUIDirectly(tutorialScreens[currentTutIndex]);
                        currentTutIndex++;
                        FXManager.Ins.TrailHint.OnCancel();
                        // Destroy(glowSpot.gameObject);
                        // Destroy(arrowDirection.gameObject);
                    }
                    else
                    {
                        if (currentScreen is not null)
                        {
                            // currentScreen.CloseDirectly();
                            currentScreen = null;
                            FXManager.Ins.TrailHint.OnPlay(new List<Vector3>()
                            {
                                new(7,3,7),
                                new(7,3,13),
                            }, 8f, true);
                            // glowSpot = Instantiate(TutorialManager.Ins.TutorialObjList[TutorialObj.LightSpot],
                            //     new Vector3(7,0,11), Quaternion.identity);
                            // arrowDirection = (ArrowDirection) Instantiate(TutorialManager.Ins.TutorialObjList[TutorialObj.Arrow],
                            //     new Vector3(9,0,7), Quaternion.identity);
                            // arrowDirection.PointerToHeight(2, Direction.Forward, true);
                        }
                    }
                    break;
                }
            }
        }

        public void HandleShowTutorial(GridUnit triggerUnit, GridUnit targetUnit)
        {
            
        }
    }
}
