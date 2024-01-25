using System.Linq;
using _Game.GameGrid.Unit.DynamicUnit.Player;
using _Game.Utilities;
using DG.Tweening;
using GameGridEnum;
using UnityEngine;

namespace _Game.GameGrid.Unit.StaticUnit
{
    public class FloatingChest : GridUnitStatic
    {
        [SerializeField] private Animator chestAnimator;
        [SerializeField] private GameObject chestModel;
        
        private bool isInteracted = false;

        private Vector3 originTransform;
        private Tween floatingTween;
        private const float MOVE_Y_VALUE = 0.06f;
        private const float MOVE_Y_TIME = 2f;
        
        public override void OnInit(GameGridCell mainCellIn, HeightLevel startHeightIn = HeightLevel.One, bool isUseInitData = true,
            Direction skinDirection = Direction.None, bool hasSetPosAndRot = false)
        {
            base.OnInit(mainCellIn, startHeightIn, isUseInitData, skinDirection, hasSetPosAndRot);
            SetFloatingTween();
        }

        private void SetFloatingTween()
        {
            #region ANIM
            //DEV: Refactor anim system
            if (IsInWater())
            {
                originTransform = Tf.transform.position;
                floatingTween = DOVirtual.Float(0 ,MOVE_Y_TIME, MOVE_Y_TIME, SetSinWavePosition).SetLoops(-1).SetEase(Ease.Linear);
            }
            else
            {
                if (floatingTween is null) return;
                floatingTween.Kill();
            }

            return;
            
            void SetSinWavePosition(float time)
            {
                float value = Mathf.Sin(2 * time * Mathf.PI / MOVE_Y_TIME) * MOVE_Y_VALUE;
                Tf.transform.position = originTransform + Vector3.up * value;
            }
            #endregion
        }

        public override void OnBePushed(Direction direction = Direction.None, GridUnit pushUnit = null)
        {
            if (isInteracted) return;
            isInteracted = true;
            base.OnBePushed(direction, pushUnit);
            if (pushUnit is not Player) return;
            ShowAnim(true);
            DOVirtual.DelayedCall(1f, () =>
            {
                DevLog.Log(DevId.Hoang, "Loot something");
            });
        }

        public override void OnDespawn()
        {
            ShowAnim(false);
            isInteracted = false;
            floatingTween?.Kill();
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
        
        private bool IsInWater()
        {
            return startHeight <= Constants.DirFirstHeightOfSurface[GridSurfaceType.Water] + FloatingHeightOffset &&
                   cellInUnits.All(t => t.SurfaceType is GridSurfaceType.Water);
        }
    }
}
