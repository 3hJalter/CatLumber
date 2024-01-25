using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HControls
{
    public class InputDetection
    {
        public InputAction InputAction {  get; private set; } = InputAction.None;
        private Direction oldDirection = Direction.None;

        public void GetInput(Direction direction)
        {
            switch(direction)
            {
                case Direction.None:
                    if(oldDirection == Direction.None)
                    {
                        InputAction = InputAction.None;
                    }
                    else
                    {
                        InputAction = InputAction.ButtonUp;
                    }
                    break;
                default:
                    if(direction == oldDirection)
                    {
                        InputAction = InputAction.ButtonHold;
                    }
                    else
                    {
                        InputAction = InputAction.ButtonDown;
                    }
                    break;
            }
            oldDirection = direction;
        }
    }
}