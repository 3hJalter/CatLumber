using System.Collections.Generic;
using _Game.DesignPattern;
using HControls;
using MEC;
using UnityEngine;

namespace _Game._Scripts.Managers
{
    public class MoveInputManager : Singleton<MoveInputManager>
    {
        public enum MoveChoice
        {
            DPad,
            Switch,
            Swipe,
            SwipeContinuous,
        }

        [SerializeField] private GameObject container;
        [SerializeField] private HSwitch hSwitch;
        [SerializeField] private HDpad dpad;
        [SerializeField] private HSwipe hSwipe;
        
        public HDpad Dpad => dpad;

        private HSwitch HSwitch => hSwitch;
        private GameObject DpadObj => dpad.gameObject;

        public MoveChoice CurrentChoice { get; private set; }
        
        public void OnForceResetMove()
        {
            if (CurrentChoice == MoveChoice.DPad)
            {
                int index = (int) HInputManager.GetDirectionInput();
                if (index != -1) dpad.OnButtonPointerUp(index);
            } else HInputManager.SetDirectionInput(Direction.None);
        }
        
        public void WaitToForceResetMove(float time)
        {
            Timing.RunCoroutine(WaitToResetMove(time));
        }
        
        private IEnumerator<float> WaitToResetMove(float time)
        {
            yield return time;
            OnForceResetMove();
        }
        
        private void HideButton()
        {
            HInputManager.SetDefault();
            DpadObj.SetActive(false);
            HSwitch.gameObject.SetActive(false);
            hSwipe.gameObject.SetActive(false);
        }

        public void OnChangeMoveChoice(MoveChoice moveChoice)
        {
            HideButton();
            switch (moveChoice)
            {
                case MoveChoice.DPad:
                    DpadObj.SetActive(true);
                    break;
                case MoveChoice.Switch:
                {
                    HSwitch.gameObject.SetActive(true);
                    HSwitch.HideAllTime(false);
                    break;
                }
                case MoveChoice.Swipe:
                {
                    hSwipe.gameObject.SetActive(true);
                    break;
                }
                case MoveChoice.SwipeContinuous:
                {
                    HSwitch.gameObject.SetActive(true);
                    HSwitch.HideAllTime(true);
                    break;
                }
            }

            CurrentChoice = moveChoice;
        }
        
        public void ShowContainer(bool isShow)
        {
            container.SetActive(isShow);
            if (!isShow) HideButton();
        }
    }
}
