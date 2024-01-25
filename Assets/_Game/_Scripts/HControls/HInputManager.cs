using _Game._Scripts.Managers;
using _Game.Utilities;
using UnityEngine;

namespace HControls
{
    public class HInputManager
    {
        private static HInputManager _instance;
        private Direction _direction = Direction.None;

        private float _horizontalInput;
        private float _verticalInput;
        private static HInputManager Instance => _instance ??= new HInputManager();

        public static void SetDefault()
        {
            Instance._horizontalInput = 0f;
            Instance._verticalInput = 0f;
            Instance._direction = Direction.None;
        }

        public static void SetHorizontalInput(float value)
        {
            Instance._horizontalInput = value;
        }

        public static void SetVerticalInput(float value)
        {
            Instance._verticalInput = value;
        }

        public static void SetDirectionInput(Direction direction)
        {
            Instance._direction = direction;
        }

        public static void SetDirectionInput(float thresholdP2)
        {
            Instance._direction = GetDirectionInput(thresholdP2);
        }

        public static Direction GetDirectionInput()
        {
            return Instance._direction;
        }

        private static Direction GetDirectionInput(float thresholdP2)
        {
            Vector2 moveInput = new(Instance._horizontalInput, Instance._verticalInput);
            if (moveInput.sqrMagnitude < thresholdP2) return Direction.None;
            float angle = Mathf.Atan2(moveInput.y, -moveInput.x);
            moveInput = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y)
                ? moveInput.x > 0 ? Direction.Left : Direction.Right
                : moveInput.y > 0
                    ? Direction.Forward
                    : Direction.Back;
        }
    }
}
