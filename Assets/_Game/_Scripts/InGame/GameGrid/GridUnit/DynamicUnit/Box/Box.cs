using System.Linq;
using _Game._Scripts.InGame.GameCondition.Data;
using _Game.DesignPattern.ConditionRule;
using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.DynamicUnit.Box.BoxState;
using _Game.Utilities;
using GameGridEnum;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Box
{
    public class Box : GridUnitDynamic
    {
        private StateMachine<Box> _stateMachine;

        public StateMachine<Box> StateMachine => _stateMachine;

        private bool _isAddState;

        public override void OnInit(GameGridCell mainCellIn, HeightLevel startHeightIn = HeightLevel.One, bool isUseInitData = true,
            Direction skinDirection = Direction.None, bool hasSetPosAndRos = false)
        {
            base.OnInit(mainCellIn, startHeightIn, isUseInitData, skinDirection, hasSetPosAndRos);
            if (!_isAddState)
            {
                _isAddState = true;
                _stateMachine = new StateMachine<Box>(this);
                AddState();
            }
            _stateMachine.ChangeState(StateEnum.Idle);
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            _stateMachine.ChangeState(StateEnum.Idle);
        }

        public override void OnPush(Direction direction, ConditionData conditionData = null)
        {
            if (conditionData is not MovingData movingData) return;
            for (int i = 0; i < movingData.blockDynamicUnits.Count; i++) movingData.blockDynamicUnits[i].OnBePushed(direction, this);
        }

        public override void OnBePushed(Direction direction = Direction.None, GridUnit pushUnit = null)
        {
            BeInteractedData.SetData(direction, pushUnit);
            if (!ConditionMergeOnBeInteracted.IsApplicable(BeInteractedData)) return;
            
            base.OnBePushed(direction, pushUnit);
            _stateMachine.ChangeState(StateEnum.Move);
        }
        
        public bool IsInWater()
        {
            return startHeight <= Constants.DirFirstHeightOfSurface[GridSurfaceType.Water] + FloatingHeightOffset &&
                   cellInUnits.All(t => t.SurfaceType is GridSurfaceType.Water);
        }

        public override bool IsCurrentStateIs(StateEnum stateEnum)
        {
            return _stateMachine.CurrentState.Id == stateEnum;
        }
        
        protected virtual void AddState()
        {
            _stateMachine.AddState(StateEnum.Idle, new IdleBoxState());
            _stateMachine.AddState(StateEnum.Move, new MoveBoxState());
            _stateMachine.AddState(StateEnum.Fall, new FallBoxState());
            _stateMachine.AddState(StateEnum.Emerge, new EmergeBoxState());
        }

        public override StateEnum CurrentStateId 
        { 
            get => _stateMachine?.CurrentStateId ?? StateEnum.Idle;
            set => _stateMachine.ChangeState(value);
        }

        protected override void OnOutTriggerBelow(GridUnit triggerUnit)
        {
            base.OnOutTriggerBelow(triggerUnit);
            if (!LevelManager.Ins.IsConstructingLevel) 
                _stateMachine.ChangeState(StateEnum.Fall);
        }

        #region Ruling

        [SerializeField] private ConditionMerge conditionMergeOnBePushed;
        [SerializeField] private ConditionMerge conditionMergeOnBeInteracted;
        public ConditionMerge ConditionMergeOnBePushed => conditionMergeOnBePushed;
        private ConditionMerge ConditionMergeOnBeInteracted => conditionMergeOnBeInteracted;

        private MovingData _movingData;
        private BeInteractedData _beInteractedData;
        
        public MovingData MovingData => _movingData ??= new MovingData(this);
        public BeInteractedData BeInteractedData => _beInteractedData ??= new BeInteractedData(this);
        
        #endregion
    }
}