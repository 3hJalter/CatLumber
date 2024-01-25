using System;
using System.Collections.Generic;
using _Game._Scripts.Managers;
using _Game.Managers;
using _Game.UIs.Screen;
using _Game.Utilities;
using HControls;
using MEC;
using UnityEngine;
using UnityEngine.UI;

namespace _Game._Scripts.Tutorial.ScreenTutorial
{
    public class TutorialScreen12 : TutorialScreen
    {
        [SerializeField] private Image panel;
        [SerializeField] private GameObject deco;
        [SerializeField] private HDpad dpad;
        [SerializeField] private HDpadButton dpadButtonLeft;
        [SerializeField] private HDpadButton dpadButtonRight;
        [SerializeField] private HDpadButton dpadButtonUp;
        [SerializeField] private HDpadButton dpadButtonDown;
       
        public override void Open()
        {
            base.Open();
            Timing.RunCoroutine(WaitOneFrame(() =>
            {
                panel.enabled = true;
                deco.SetActive(true);
                dpadButtonLeft.PointerDownImg.SetActive(true);
                dpadButtonRight.PointerDownImg.SetActive(true);
                dpadButtonUp.PointerDownImg.SetActive(true);
                dpadButtonDown.PointerDownImg.SetActive(true);
            }));
        }

        public override void CloseDirectly()
        {
            // MoveInputManager.Ins.WaitToForceResetMove(Time.fixedDeltaTime * 2);
            base.CloseDirectly();
        }

        private IEnumerator<float> WaitOneFrame(Action action)
        {
            yield return Timing.WaitForOneFrame;
            action?.Invoke();
        }
        
        public void OpenInGameScreen()
        {
            if (!UIManager.Ins.IsOpened<InGameScreen>()) UIManager.Ins.OpenUI<InGameScreen>();
            panel.enabled = false;
            deco.SetActive(false);
        }

        public void OnClickUpButton()
        {
            dpadButtonRight.PointerDownImg.SetActive(false);
            dpadButtonLeft.PointerDownImg.SetActive(false);
            dpadButtonDown.PointerDownImg.SetActive(false);
        }
    }
}
