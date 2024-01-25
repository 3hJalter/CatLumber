using _Game.Camera;
using _Game.Managers;
using DG.Tweening;
using UnityEngine;

namespace _Game.UIs.Screen
{
    public class WorldMapScreen : UICanvas
    {
        [SerializeField] private CanvasGroup canvasGroup;

        public override void Setup()
        {
            base.Setup();
            canvasGroup.alpha = 0f;
        }

        public override void Open()
        {
            base.Open();
            // CameraFollow.Ins.ChangeCamera(ECameraType.WorldMapCamera);
            // FxManager.Ins.PlayTweenFog(false, 1.5f, 1.5f, ShowUI);
            CameraManager.Ins.ChangeCamera(ECameraType.WorldMapCamera);
            ShowUI();
        }

        private void ShowUI()
        {
            DOVirtual.Float(0, 1, 0.5f, value => canvasGroup.alpha = value)
                .SetEase(Ease.Linear);
        }

        public void OnClickCloseButton()
        {
            Close();
            UIManager.Ins.OpenUI<InGameScreen>();
            // FxManager.Ins.StopTweenFog();
        }
    }
}
