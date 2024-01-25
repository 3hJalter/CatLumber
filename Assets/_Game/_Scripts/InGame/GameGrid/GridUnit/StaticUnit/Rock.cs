using _Game.Utilities;
using DG.Tweening;
using UnityEngine;

namespace _Game.GameGrid.Unit.StaticUnit
{
    public class Rock : GridUnitStatic
    {
        [SerializeField]
        private int DEGREE = 6;
        [SerializeField]
        private float DECAY_VALUE = 0.9f;
        public override void OnBePushed(Direction direction = Direction.None, GridUnit unit = null)
        {
            //NOTE: Refactor
            //NOTE: Play Shake Animation
            //DevLog.Log(DevId.Hung, "Rock Block");
            Vector3 axis = Vector3.Cross(Vector3.up, Constants.DirVector3[direction]);
            float lastAngle = 0;
            DOVirtual.Float(0, DEGREE * 4 * DECAY_VALUE * DECAY_VALUE, Constants.MOVING_TIME * 1.2f, i =>
            {
                float rotateAngle;
                //NOTE: Calculate Angle
                if (i <= DEGREE)
                {
                    rotateAngle = i;
                }
                else if (i <= 3 * DEGREE * DECAY_VALUE)
                {
                    rotateAngle = 2 * DEGREE - i;
                }
                else
                {
                    rotateAngle = i - 4 * DEGREE * DECAY_VALUE * DECAY_VALUE;
                }
                transform.RotateAround(anchor.Tf.position, axis, rotateAngle - lastAngle);
                lastAngle = rotateAngle;
            }).SetUpdate(UpdateType.Fixed)
                    .SetEase(Ease.OutQuad);
        }
    }
}
