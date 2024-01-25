using _Game.GameGrid;

namespace _Game.UIs.Screen
{
    public class WinScreen : UICanvas
    {
        public void OnClickNextButton()
        {
            LevelManager.Ins.OnNextLevel();
            Close();
        }
    }
}
