using _Game._Scripts.Managers;
using _Game.Managers;

namespace _Game.UIs.Popup
{
    public class MoveOptionPopup : UICanvas
    {
        public void OnUseDpad()
        {
            MoveInputManager.Ins.OnChangeMoveChoice(MoveInputManager.MoveChoice.DPad);
        }

        public void OnUseSwitch()
        {
            MoveInputManager.Ins.OnChangeMoveChoice(MoveInputManager.MoveChoice.Switch);
        }

        public void OnUseSwipe()
        {
            MoveInputManager.Ins.OnChangeMoveChoice(MoveInputManager.MoveChoice.Swipe);
        }

        public void OnUseSwipeContinuous()
        {
            MoveInputManager.Ins.OnChangeMoveChoice(MoveInputManager.MoveChoice.SwipeContinuous);
        }

        public void OnSwitchGridActive()
        {
            FXManager.Ins.SwitchGridActive();
        }
    }
}
