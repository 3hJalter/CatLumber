using System;
using _Game._Scripts.Managers;
using _Game.DesignPattern;
using _Game.GameGrid.Unit.DynamicUnit.Player;
using _Game.Utilities;
using DG.Tweening;
using GameGridEnum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace _Game.GameGrid.Unit.StaticUnit
{
    /* Description:
     * This class is used for listen from button unit
     * The chest will be opened when all button units are entered
     */
    public class ChestButtonUnit : GridUnitStatic
    {
        private int numberOfButtonInLevel;
        private int numberOfButtonEntered;

        private bool isLocked;
        private bool isInteracted;
        
        [Title("Canvas")]
        [SerializeField] private GameObject canvas;
        [SerializeField] private TextMeshProUGUI textMeshPro;
        
        [Title("Chest")]
        [SerializeField] private Animator chestAnimator;
        [SerializeField] private GameObject chestModel;
        [SerializeField] private GameObject lockedChestModel;
        [SerializeField] private ParticleSystem chestUnlockParticle;
        public override void OnInit(GameGridCell mainCellIn, HeightLevel startHeightIn = HeightLevel.One, bool isUseInitData = true,
            Direction skinDirection = Direction.None, bool hasSetPosAndRot = false)
        {
            base.OnInit(mainCellIn, startHeightIn, isUseInitData, skinDirection, hasSetPosAndRot);
            SetUpButtonUnit();
            OnLockChest();
        }
        
        public override void OnDespawn()
        {
            DeSetUpButtonUnit();
            base.OnDespawn();
        }
        
        private Action<bool> OnButtonUnitEnter()
        {
            // if dispatch true, button unit is entered
            return isEntered =>
            {
                if (isEntered) numberOfButtonEntered++;
                else numberOfButtonEntered--;
                SetText();
                if (numberOfButtonEntered == numberOfButtonInLevel && isLocked) OnUnlockChest();
            };
        }

        private void OnLockChest()
        {
            isLocked = true;
            chestAnimator.gameObject.SetActive(false);
            chestModel.SetActive(false);
            lockedChestModel.SetActive(true);
            canvas.SetActive(true);
            SetText();
        }
        
        private void OnUnlockChest()
        {
            isLocked = false;
            chestAnimator.gameObject.SetActive(true);
            chestModel.SetActive(true);
            lockedChestModel.SetActive(false);
            canvas.SetActive(false); 
            chestUnlockParticle.Play();
        }

        private void SetText()
        {
            textMeshPro.text = $"{numberOfButtonEntered}/{numberOfButtonInLevel}";
        }
        
        public override void OnBePushed(Direction direction = Direction.None, GridUnit pushUnit = null)
        {
            if (isInteracted || isLocked) return;
            isInteracted = true;
            base.OnBePushed(direction, pushUnit);
            if (pushUnit is not Player) return;
            ShowAnim(true);
            DOVirtual.DelayedCall(1f, () =>
            {
                DevLog.Log(DevId.Hoang, "Loot something");
            });
        }
        
        private void ShowAnim(bool isShow)
        {
            chestAnimator.gameObject.SetActive(isShow);
            chestModel.SetActive(!isShow);
            if (isShow)
            {
                chestAnimator.SetTrigger(Constants.OPEN_ANIM);
                btnCanvas.gameObject.SetActive(false);
            }
        }
        
        private void SetUpButtonUnit()
        {
            // Get number of button in level
            numberOfButtonInLevel = 0;
            numberOfButtonEntered = 0;
            LevelManager.Ins.CurrentLevel.UnitDataList.ForEach(unitData =>
            {
                if (unitData.unit.PoolType == PoolType.ButtonUnit) numberOfButtonInLevel++;
            });
            // Add listener
            EventGlobalManager.Ins.OnButtonUnitEnter.AddListener(OnButtonUnitEnter(), true);
        }
        
        private void DeSetUpButtonUnit()
        {
            EventGlobalManager.Ins.OnButtonUnitEnter.RemoveListener(OnButtonUnitEnter());
            numberOfButtonEntered = 0;
            numberOfButtonInLevel = 0;
        }
    }
}
