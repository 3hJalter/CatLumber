using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.DynamicUnit.ButtonUnitState;
using GameGridEnum;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit
{
    public class ButtonUnit : GridUnitDynamic
    {
        [SerializeField] private Animator animator;
        private string _currentAnimName = " ";

        public StateMachine<ButtonUnit> StateMachine { get; private set; }

        private bool _isAddState;

        public override void OnInit(GameGridCell mainCellIn, HeightLevel startHeightIn = HeightLevel.One, bool isUseInitData = true,
            Direction skinDirection = Direction.None, bool hasSetPosAndRos = false)
        {
            base.OnInit(mainCellIn, startHeightIn, isUseInitData, skinDirection, hasSetPosAndRos);
            if (!_isAddState)
            {
                _isAddState = true;
                StateMachine = new StateMachine<ButtonUnit>(this);
                AddState();
            }
            StateMachine.ChangeState(StateEnum.Idle);
        }

        private void AddState()
        {
            StateMachine.AddState(StateEnum.Idle, new IdleButtonState());
            StateMachine.AddState(StateEnum.Enter, new EnterButtonState());
        }
        
        protected override void OnEnterTriggerUpper(GridUnit triggerUnit)
        {
            if (CurrentStateId == StateEnum.Enter) return;
            StateMachine.ChangeState(StateEnum.Enter);
        }
        
        protected override void OnOutTriggerUpper(GridUnit triggerUnit)
        {
            if (CurrentStateId == StateEnum.Idle) return;
            StateMachine.ChangeState(StateEnum.Idle);
        }
        
        public void ChangeAnim(string animName)
        {
            if (_currentAnimName == animName) return;
            _currentAnimName = animName;
            animator.SetTrigger(_currentAnimName);
        }

        public override StateEnum CurrentStateId 
        { 
            get => StateMachine?.CurrentStateId ?? StateEnum.Idle;
            set => StateMachine.ChangeState(value);
        }
    }
}
