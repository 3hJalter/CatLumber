using _Game.Managers;
using _Game.UIs.Screen;
using UnityEngine;
using UnityEngine.UI;

namespace _Game._Scripts.Tutorial.ScreenTutorial
{
    public class TutorialScreen11 : TutorialScreen
    {
        [SerializeField] private Image panel;
        [SerializeField] private GameObject deco;
        public void OpenInGameScreen()
        {
            
            if (!UIManager.Ins.IsOpened<InGameScreen>()) UIManager.Ins.OpenUI<InGameScreen>();
            panel.enabled = false;
            deco.SetActive(false);
        }
    }
}
