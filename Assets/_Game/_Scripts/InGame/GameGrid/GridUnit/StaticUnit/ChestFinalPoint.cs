using System;
using _Game.GameGrid.Unit.DynamicUnit.Player;
using DG.Tweening;
using UnityEngine;

namespace _Game.GameGrid.Unit.StaticUnit
{
    public class ChestFinalPoint : GridUnitStatic
    {
        [SerializeField] private Animator chestAnimator;
        [SerializeField] private GameObject chestModel;
        private bool isInteracted = false;

        public override void OnInteract()
        {
            ShowAnim(true);
            DOVirtual.DelayedCall(1f, () => LevelManager.Ins.OnWin());
        }

        public override void OnBePushed(Direction direction = Direction.None, GridUnit pushUnit = null)
        {
            if (isInteracted) return;
            isInteracted = true;
            base.OnBePushed(direction, pushUnit);
            if (pushUnit is not Player) return;
            ShowAnim(true);
            DOVirtual.DelayedCall(1f, () => LevelManager.Ins.OnWin());
        }

        public override void OnDespawn()
        {
            ShowAnim(false);
            isInteracted = false;
            base.OnDespawn();
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
    }
}
