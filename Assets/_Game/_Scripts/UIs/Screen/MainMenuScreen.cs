using _Game.Camera;
using _Game.DesignPattern;
using _Game.GameGrid;
using _Game.Managers;
using _Game.UIs.Popup;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VinhLB;

namespace _Game.UIs.Screen
{
    public class MainMenuScreen : UICanvas
    {
        [SerializeField]
        private CanvasGroup _canvasGroup;
        [SerializeField]
        private Image _blockPanel;
        [SerializeField]
        private TabGroup _bottomNavigationTabGroup;
        [SerializeField]
        private TMP_Text _goldValueText;
        [SerializeField]
        private TMP_Text _gemValueText;
        
        private bool _isFirstOpen;

        public override void Setup()
        {
            base.Setup();
            
            _blockPanel.gameObject.SetActive(true);
            _goldValueText.text = $"{DataManager.Ins.GameData.user.gold}";
            _gemValueText.text = $"{DataManager.Ins.GameData.user.gems}";
        }

        public override void Open()
        {
            base.Open();
            // CameraFollow.Ins.ChangeCamera(ECameraType.MainMenuCamera);
            // FxManager.Ins.PlayTweenFog();
            GameManager.Ins.ChangeState(GameState.MainMenu);
            CameraManager.Ins.ChangeCamera(ECameraType.MainMenuCamera);
            _bottomNavigationTabGroup.ResetSelectedTab();
            DOVirtual.Float(0, 1, 1f, value => _canvasGroup.alpha = value)
                .OnComplete(() => _blockPanel.gameObject.SetActive(false));
        }

        public void OnClickStart()
        {
            UIManager.Ins.OpenUI<InGameScreen>();
            LevelManager.Ins.InitLevel();
            Close();
        }
        
        public void OnClickSettingButton()
        {
            UIManager.Ins.OpenUI<SettingsPopup>();
        }
    }
}
