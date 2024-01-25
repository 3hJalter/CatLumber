using _Game.GameGrid;

namespace _Game._Scripts.Tutorial.ScreenTutorial
{
    public class TutorialScreen91 : TutorialScreen
    {
        public override void CloseDirectly()
        {   
            LevelManager.Ins.CurrentLevel.ChangeShadowUnitAlpha(false);
            base.CloseDirectly();
        }
    }
}
