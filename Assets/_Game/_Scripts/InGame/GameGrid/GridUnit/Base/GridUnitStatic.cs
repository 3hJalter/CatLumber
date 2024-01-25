using _Game.GameGrid.Unit.DynamicUnit.Player;
using _Game.Managers;
using GameGridEnum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Game.GameGrid.Unit
{
    public abstract class GridUnitStatic : GridUnit
    {
        [Title("Static Unit")]
        [SerializeField] protected GridUnitStaticType gridUnitStaticType;
        [SerializeField] protected Anchor anchor;


        [SerializeField] private bool hasInteractBtn;

        [ShowIf(nameof(hasInteractBtn))] [SerializeField]
        protected Canvas btnCanvas;

        [ShowIf(nameof(hasInteractBtn))] [SerializeField]
        private HButton interactBtn;

        private bool _isFirstInitDone;
        public GridUnitStaticType GridUnitStaticType => gridUnitStaticType;

        public override void OnInit(GameGridCell mainCellIn, HeightLevel startHeightIn = HeightLevel.One,
            bool isUseInitData = true, Direction skinDirection = Direction.None, bool hasSetPosAndRot = false)
        {
            base.OnInit(mainCellIn, startHeightIn, isUseInitData, skinDirection, hasSetPosAndRot);
            if (!hasInteractBtn || _isFirstInitDone || btnCanvas is null) return;
            _isFirstInitDone = true;
            // btnCanvas.worldCamera = CameraFollow.Ins.MainCamera;
            btnCanvas.worldCamera = CameraManager.Ins.BrainCamera;
            interactBtn.onClick.AddListener(OnInteract);
        }

        public virtual void OnInteract()
        {

        }

        protected override void OnEnterTriggerNeighbor(GridUnit triggerUnit)
        {
            if (hasInteractBtn && triggerUnit is Player) btnCanvas.gameObject.SetActive(true);
        }

        protected override void OnOutTriggerNeighbor(GridUnit triggerUnit)
        {
            if (hasInteractBtn && triggerUnit is Player) btnCanvas.gameObject.SetActive(false);
        }

        public override void OnDespawn()
        {
            if (hasInteractBtn) btnCanvas.gameObject.SetActive(false);
            base.OnDespawn();
        }
    }
}
