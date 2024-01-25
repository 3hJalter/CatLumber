using System;
using System.Collections.Generic;
using _Game.Camera;
using _Game.Managers;
using _Game.Utilities;
using _Game.Utilities.Timer;

namespace HControls
{
    public class HHoldingButton : HMonoBehaviour
    {
        private bool isHolding;

        private readonly List<float> timerList = new();
        private readonly List<Action> actions = new();
        private STimer timer;
        private void Awake()
        {
            timerList.Add(Constants.HOLD_TOUCH_TIME);
            actions.Add(() =>
            {
                if (isHolding)
                {
                    CameraManager.Ins.ChangeCamera(ECameraType.ZoomOutCamera, Constants.ZOOM_OUT_TIME);
                }
            });
        }

        public void OnButtonPointerDown()
        {
            isHolding = true;
            timer = TimerManager.Inst.WaitForTime(timerList, actions);
        }

        public void OnButtonPointerUp()
        {
            isHolding = false;
            timer.Stop();
            if (CameraManager.Ins.IsCurrentCameraIs(ECameraType.ZoomOutCamera))
                CameraManager.Ins.ChangeCamera(ECameraType.InGameCamera, Constants.ZOOM_OUT_TIME);
        }
    }
}
