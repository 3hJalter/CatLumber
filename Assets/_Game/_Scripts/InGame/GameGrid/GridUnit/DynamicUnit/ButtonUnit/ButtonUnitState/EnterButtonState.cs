using _Game._Scripts.Managers;
using _Game.DesignPattern.StateMachine;
using DG.Tweening;

namespace _Game.GameGrid.Unit.DynamicUnit.ButtonUnitState
{
    public class EnterButtonState : IState<ButtonUnit>
    {
        private const float BUTTON_ENTER_TIME = 0.3f;
        public StateEnum Id => StateEnum.Enter;
        private bool _isComplete;
        private Tween _tween;
        public void OnEnter(ButtonUnit t)
        {
            t.ChangeAnim(Constants.ENTER_ANIM);
            // DUST Effect
            // Wait for animation complete
            _tween = DOVirtual.DelayedCall(BUTTON_ENTER_TIME, () =>
            {
                _isComplete = true;
                EventGlobalManager.Ins.OnButtonUnitEnter?.Dispatch(true);
            });
        }

        public void OnExecute(ButtonUnit t)
        {
        }

        public void OnExit(ButtonUnit t)
        {
            t.ChangeAnim(Constants.EXIT_ANIM);
            _tween.Kill();
            // Check if complete tween
            if (!_isComplete) return;
            EventGlobalManager.Ins.OnButtonUnitEnter?.Dispatch(false);
            _isComplete = false;
            // DUST Effect
        }
    }
}
