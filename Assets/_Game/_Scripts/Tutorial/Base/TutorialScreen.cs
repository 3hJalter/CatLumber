using _Game.Managers;
using _Game.UIs.Screen;

namespace _Game._Scripts.Tutorial
{
    public class TutorialScreen : UICanvas
    {
        public override void Setup()
        {
            base.Setup();
            // if InInGameScreen is open, hide it
            if (UIManager.Ins.IsOpened<InGameScreen>()) UIManager.Ins.CloseUI<InGameScreen>();
        }

        public override void CloseDirectly()
        {
            // Show InGameScreen
            if (!UIManager.Ins.IsOpened<InGameScreen>()) UIManager.Ins.OpenUI<InGameScreen>();
            // Close and destroy this screen
            base.CloseDirectly();
        }
    }
}
