using _Game.DesignPattern.StateMachine;
using DG.Tweening;
using GameGridEnum;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Box.BoxState
{
    public class EmergeBoxState : IState<Box>
    {
        private const float FLOATING_OFFSET = 0.25f;
        public StateEnum Id => StateEnum.Emerge;
        Tween moveTween;
        public void OnEnter(Box t)
        {
            //DEV: Refactor 
            Vector3 position = t.Tf.position;
            position = new Vector3(position.x, Constants.POS_Y_BOTTOM, position.z);
            t.Tf.position = position;
            float y = (float)(Constants.DirFirstHeightOfSurface[GridSurfaceType.Water] + t.FloatingHeightOffset) / 2 * Constants.CELL_SIZE 
                - t.yOffsetOnDown + FLOATING_OFFSET;
            moveTween = t.Tf.DOMoveY(y, Constants.MOVING_TIME * 1.5f).OnComplete(() =>
            {
                t.StateMachine.ChangeState(StateEnum.Idle);
            });
        }

        public void OnExecute(Box t)
        {
            
        }

        public void OnExit(Box t)
        {
            moveTween.Kill();
        }
    }
}
