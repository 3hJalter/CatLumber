using System;
using System.Collections.Generic;
using _Game.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace GG.Infrastructure.Utils.Swipe
{
    [Serializable]
    public class SwipeListenerEvent : UnityEvent<string>
    {
    }

    public class SwipeListener : MonoBehaviour
    {
        public UnityEvent onSwipeCancelled;

        public SwipeListenerEvent onSwipe;
        public UnityEvent onUnHold;
        

        [SerializeField] private float sensitivity = 100;

        [SerializeField] private bool continuousDetection;

        [SerializeField] private SwipeDetectionMode swipeDetectionMode = SwipeDetectionMode.EightSides;

        private VectorToDirection _directions;

        private float _minMoveDistance = 0.1f;

        private Vector3 _offset;

        private Vector3 _swipeStartPoint;

        private bool _waitForSwipe = true;
        
        private float _timer = Constants.HOLD_TOUCH_TIME;

        private bool _isHolding;
        
        public bool ContinuousDetection
        {
            get => continuousDetection;
            set => continuousDetection = value;
        }

        public float Sensitivity
        {
            get => sensitivity;
            set
            {
                sensitivity = value;
                UpdateSensitivity();
            }
        }

        public SwipeDetectionMode SwipeDetectionMode
        {
            get => swipeDetectionMode;
            set => swipeDetectionMode = value;
        }

        private void Start()
        {
            UpdateSensitivity();

            if (SwipeDetectionMode != SwipeDetectionMode.Custom)
                SetDetectionMode(DirectionPresets.GetPresetByMode(SwipeDetectionMode));
        }

        private void Update()
        {
            if (_isHolding && Input.GetMouseButtonUp(0))
            {
                onUnHold?.Invoke();
                _isHolding = false;
            }
            
            if (Input.GetMouseButtonDown(0) && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            
            if (Input.GetMouseButtonDown(0)) InitSwipe();

            if (_waitForSwipe && Input.GetMouseButton(0)) CheckSwipe();

            if (continuousDetection == false) CheckSwipeCancellation();
        }

        public void SetDetectionMode(List<DirectionId> directions)
        {
            _directions = new VectorToDirection(directions);
        }

        private void UpdateSensitivity()
        {
            int screenShortSide = Screen.width < Screen.height ? Screen.width : Screen.height;
            _minMoveDistance = screenShortSide / sensitivity;
        }

        private void CheckSwipeCancellation()
        {
            if (Input.GetMouseButtonUp(0))
                if (_waitForSwipe)
                    if (onSwipeCancelled != null)
                        onSwipeCancelled.Invoke();
        }

        private void InitSwipe()
        {
            SampleSwipeStart();
            _waitForSwipe = true;
        }

        private void CheckSwipe()
        {
            _offset = Input.mousePosition - _swipeStartPoint;
            if (_offset.magnitude >= _minMoveDistance)
            {
                onSwipe?.Invoke(_directions.GetSwipeId(_offset));
                if (!continuousDetection) _waitForSwipe = false;
                SampleSwipeStart();
                _isHolding = false;
            }
            else if (_timer > 0) // Add Hold Gesture for this code
            {
                _timer -= Time.deltaTime;
                if (_timer > 0) return;
                _timer = Constants.HOLD_TOUCH_TIME;
                onSwipe?.Invoke("None");
                if (!continuousDetection) _waitForSwipe = false;
                SampleSwipeStart();
                _isHolding = true;
            }
        }

        private void SampleSwipeStart()
        {
            _swipeStartPoint = Input.mousePosition;
            _offset = Vector3.zero;
        }
    }
}
