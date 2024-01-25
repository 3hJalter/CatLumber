using System;
using System.Collections.Generic;
using _Game.GameGrid;
using _Game.GameGrid.Unit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Game._Scripts.Tutorial
{
    public interface ITutorialCondition
    {
        public void OnForceShowTutorial(int index, bool isIncrement = true);
        
        void HandleShowTutorial(GameGridCell cell, GridUnit triggerUnit);
        
        void HandleShowTutorial(GridUnit triggerUnit, GridUnit targetUnit);
        
        // void HandleShowTutorial();
        
        void ResetTutorial();
    }
    
    public abstract class BaseTutorialData : SerializedScriptableObject
    {
        // ReSharper disable once Unity.RedundantSerializeFieldAttribute
        [SerializeField] protected List<TutorialScreen> tutorialScreens = new();
        protected int currentTutIndex;

        protected UICanvas currentScreen;
        
        [ContextMenu("Reset Tutorial")]
        public void ResetTutorial()
        {
            currentTutIndex = 0;
        }
    }
}
