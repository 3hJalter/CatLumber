using System;
using System.Collections.Generic;
using _Game.Camera;
using _Game.Managers;
using _Game.Utilities.Timer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HControls
{
    public class HSwitch : HMonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        [SerializeField] private float movementRange = 40f;
        [SerializeField] private bool hideOnRelease = true;
        [SerializeField] private bool snapToFinger = true;
        [SerializeField] private bool isHighlight = true;

        [Tooltip("Highlight the direction of the switch")] [SerializeField]
        private SwitchDirectionHandler switchDirectionHandler;

        [Tooltip("Constraints on the switch movement axis")] [SerializeField]
        private ControlMovementDirection switchMoveAxis = ControlMovementDirection.Both;

        [Tooltip("Image of the switch base")] public Image switchBase;

        [Tooltip("Image of the stick itself")] public Image stick;

        private RectTransform _baseTransform;

        private float _horizontalAxis;
        private Vector2 _initialBasePosition;

        private Vector2 _initialStickPosition;
        private Vector2 _intermediateStickPosition;

        private float _oneOverMovementRange;
        private RectTransform _stickTransform;
        private float _verticalAxis;
        
        // TEST: ADD holding gesture
        private bool _isHolding;
        private readonly List<float> timerList = new();
        private readonly List<Action> actions = new();
        private STimer timer;

        /// <summary>
        ///     Current event camera reference. Needed for the sake of Unity Remote input
        /// </summary>
        private Camera CurrentEventCamera { get; set; }

        private void Awake()
        {
            _stickTransform = stick.GetComponent<RectTransform>();
            _baseTransform = switchBase.GetComponent<RectTransform>();

            _initialStickPosition = _stickTransform.anchoredPosition;
            _intermediateStickPosition = _initialStickPosition;
            _initialBasePosition = _baseTransform.anchoredPosition;

            _stickTransform.anchoredPosition = _initialStickPosition;
            _baseTransform.anchoredPosition = _initialBasePosition;

            _oneOverMovementRange = 1f / movementRange;

            if (hideOnRelease) HideOnRelease(true);
            HInputManager.SetDefault();
            timerList.Add(Constants.HOLD_TOUCH_TIME);
            actions.Add(() =>
            {
                if (_isHolding)
                {
                    CameraManager.Ins.ChangeCamera(ECameraType.ZoomOutCamera, Constants.ZOOM_OUT_TIME);
                }
            });
        }

        private void OnEnable()
        {
            ResetSwitchPos();
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Unity remote multitouch related thing
            // When we feed fake PointerEventData we can't really provide a camera, 
            // it has a lot of private setters via not created objects, so even the Reflection magic won't help a lot here
            // Instead, we just provide an actual event camera as a public property so we can easily set it in the Input Helper class
            CurrentEventCamera = eventData.pressEventCamera ? eventData.pressEventCamera : CurrentEventCamera;
            // We get the local position of the switch
            RectTransformUtility.ScreenPointToWorldPointInRectangle(_stickTransform, eventData.position,
                CurrentEventCamera, out Vector3 worldSwitchPosition);
            // Then we change it's actual position so it snaps to the user's finger
            _stickTransform.position = worldSwitchPosition;
            // We then query it's anchored position. It's calculated internally and quite tricky to do from scratch here in C#
            Vector2 stickAnchoredPosition = _stickTransform.anchoredPosition;
            // Some bitwise logic for constraining the switch along one of the axis
            // If the "Both" option was selected, non of these two checks will yield "true"
            if ((switchMoveAxis & ControlMovementDirection.Horizontal) == 0)
                stickAnchoredPosition.x = _intermediateStickPosition.x;
            if ((switchMoveAxis & ControlMovementDirection.Vertical) == 0)
                stickAnchoredPosition.y = _intermediateStickPosition.y;
            _stickTransform.anchoredPosition = stickAnchoredPosition;
            // Find current difference between the previous central point of the switch and it's current position
            Vector2 difference = new Vector2(stickAnchoredPosition.x, stickAnchoredPosition.y) -
                                 _intermediateStickPosition;
            // Normalisation stuff
            float diffMagnitude = difference.magnitude;
            Vector2 normalizedDifference = difference / diffMagnitude;
            // If the switch is being dragged outside of it's range
            if (diffMagnitude > movementRange)
                _stickTransform.anchoredPosition = _intermediateStickPosition + normalizedDifference * movementRange;
            // We should now calculate axis values based on final position and not on "virtual" one
            Vector2 finalStickAnchoredPosition = _stickTransform.anchoredPosition;
            // Sanity recalculation
            Vector2 finalDifference = new Vector2(finalStickAnchoredPosition.x, finalStickAnchoredPosition.y) -
                                      _intermediateStickPosition;
            // We don't need any values that are greater than 1 or less than -1
            float horizontalValue = Mathf.Clamp(finalDifference.x * _oneOverMovementRange, -1f, 1f);
            float verticalValue = Mathf.Clamp(finalDifference.y * _oneOverMovementRange, -1f, 1f);
            // Finally, we update our virtual axis
            HInputManager.SetHorizontalInput(horizontalValue);
            HInputManager.SetVerticalInput(verticalValue);
            HInputManager.SetDirectionInput(Constants.INPUT_THRESHOLD_P2);
            Direction direction = HInputManager.GetDirectionInput();
            if (direction is not Direction.None) _isHolding = false;
            if (isHighlight) switchDirectionHandler.ShowObject(direction);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // We also want to show it if we specified that behaviour
            if (hideOnRelease) HideOnRelease(false);
            if (isHighlight) switchDirectionHandler.Reset();
            _isHolding = true;
            // When we press, we first want to snap the switch to the user's finger
            if (snapToFinger)
            {
                CurrentEventCamera = eventData.pressEventCamera ? eventData.pressEventCamera : CurrentEventCamera;

                RectTransformUtility.ScreenPointToWorldPointInRectangle(_stickTransform, eventData.position,
                    CurrentEventCamera, out Vector3 localStickPosition);
                RectTransformUtility.ScreenPointToWorldPointInRectangle(_baseTransform, eventData.position,
                    CurrentEventCamera, out Vector3 localBasePosition);

                _baseTransform.position = localBasePosition;
                _stickTransform.position = localStickPosition;
                _intermediateStickPosition = _stickTransform.anchoredPosition;
                timer = TimerManager.Inst.WaitForTime(timerList, actions);
            }
            else
            {
                OnDrag(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // When we lift our finger, we reset everything to the initial state
            _baseTransform.anchoredPosition = _initialBasePosition;
            _stickTransform.anchoredPosition = _initialStickPosition;
            _intermediateStickPosition = _initialStickPosition;
            HInputManager.SetDefault();
            // We also hide it if we specified that behaviour
            if (hideOnRelease) HideOnRelease(true);
            _isHolding = false;
            timer.Stop();
            if (CameraManager.Ins.IsCurrentCameraIs(ECameraType.ZoomOutCamera))
                CameraManager.Ins.ChangeCamera(ECameraType.InGameCamera, Constants.ZOOM_OUT_TIME);
        }

        private void ResetSwitchPos()
        {
            // When we lift our finger, we reset everything to the initial state
            _baseTransform.anchoredPosition = _initialBasePosition;
            _stickTransform.anchoredPosition = _initialStickPosition;
            _intermediateStickPosition = _initialStickPosition;
            // We also hide it if we specified that behaviour
            if (hideOnRelease) HideOnRelease(true);
        }

        private void HideOnRelease(bool isHidden)
        {
            switchBase.gameObject.SetActive(!isHidden);
            stick.gameObject.SetActive(!isHidden);
        }

        public void HideAllTime(bool isHidden)
        {
            switchBase.color = new Color(switchBase.color.r,
                switchBase.color.g, switchBase.color.b, !isHidden ? 1 : 0);
            stick.color = new Color(stick.color.r,
                stick.color.g, stick.color.b, !isHidden ? 1 : 0);
            isHighlight = !isHidden;
            switchDirectionHandler.gameObject.SetActive(isHighlight);
        }

        [Flags]
        private enum ControlMovementDirection
        {
            Horizontal = 0x1,
            Vertical = 0x2,
            Both = Horizontal | Vertical
        }
    }
}
