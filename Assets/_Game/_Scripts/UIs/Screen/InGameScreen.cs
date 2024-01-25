using _Game._Scripts.Managers;
using _Game.Camera;
using _Game.GameGrid;
using _Game.Managers;
using _Game.UIs.Popup;
using _Game.Utilities.Timer;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VinhLB;

namespace _Game.UIs.Screen
{
    public class InGameScreen : UICanvas
    {
        public event Action OnUndo;
        public event Action OnResetIsland;

        private float UNDO_CD_TIME = 0.3f;
        [SerializeField] private Image blockPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button resetIslandButton;
        [SerializeField] private TMP_Text timeText;

        private int time;
        public int Time
        {
            get => time;
            set
            {
                time = value;
                int second = time % 60;
                int minute = time / 60;
                timeText.text = $"{minute.ToString("00")}:{second.ToString("00")}";
            }
        }

        STimer undoTimer;
        STimer resetIslandTimer;
        private void Awake()
        {
            undoTimer = TimerManager.Inst.PopSTimer();
            resetIslandTimer = TimerManager.Inst.PopSTimer();
        }

        public override void Setup()
        {
            base.Setup();
            GameManager.Ins.ChangeState(GameState.InGame);
            MoveInputManager.Ins.ShowContainer(true);
            MoveInputManager.Ins.OnChangeMoveChoice(MoveInputManager.Ins
                .CurrentChoice); // TODO: Change to use PlayerRef 
            // if (CameraFollow.Ins.IsCurrentCameraIs(ECameraType.InGameCamera)) return;
            // CameraFollow.Ins.ChangeCamera(ECameraType.InGameCamera);
            if (CameraManager.Ins.IsCurrentCameraIs(ECameraType.InGameCamera)) return;
            CameraManager.Ins.ChangeCamera(ECameraType.InGameCamera);
            blockPanel.enabled = true;
        }

        public override void Open()
        {
            base.Open();
            DOVirtual.Float(0, 1, 1f, value => canvasGroup.alpha = value)
                .OnComplete(() => { blockPanel.enabled = false; });
        }

        public override void Close()
        {
            MoveInputManager.Ins.ShowContainer(false);
            base.Close();
        }

        public void OnClickSettingButton()
        {
            UIManager.Ins.OpenUI<SettingsPopup>();
        }

        public void OnClickOpenMapButton()
        {
            Close();
            UIManager.Ins.OpenUI<WorldMapScreen>();
        }

        public void OnClickResetIslandButton()
        {
            OnResetIsland?.Invoke();
            resetIslandButton.interactable = false;
            resetIslandTimer.Start(UNDO_CD_TIME, () => resetIslandButton.interactable = true);
        }

        public void OnClickToggleBuildingMode()
        {
            Close();
            UIManager.Ins.OpenUI<BuildingScreen>();
            GridBuildingManager.Ins.ToggleBuildMode();
        }

        public void OnClickUndo()
        {
            OnUndo?.Invoke();
            undoButton.interactable = false;
            undoTimer.Start(UNDO_CD_TIME, () => undoButton.interactable = true);
        }

        public void SetActiveUndo(bool active)
        {
            undoTimer.Stop();
            undoButton.interactable = active;
        }

        public void SetActiveResetIsland(bool active)
        {
            resetIslandTimer.Stop();
            resetIslandButton.interactable = active;
        }

        private void OnDestroy()
        {
            TimerManager.Inst.PushSTimer(undoTimer);
            TimerManager.Inst.PushSTimer(resetIslandTimer);
        }

        public void OnShowShadowObj()
        {
            LevelManager.Ins.CurrentLevel.ChangeShadowUnitAlpha(false);
            FXManager.Ins.TrailHint.OnPlay(
                LevelManager.Ins.CurrentLevel.HintLinePosList);
        }

        public void OnHideShadowObj()
        {
            LevelManager.Ins.CurrentLevel.ChangeShadowUnitAlpha(true);
            FXManager.Ins.TrailHint.OnCancel();
        }
    }
}
