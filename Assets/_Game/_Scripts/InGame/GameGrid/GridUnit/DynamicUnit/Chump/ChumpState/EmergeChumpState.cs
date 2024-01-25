using _Game.DesignPattern;
using _Game.DesignPattern.StateMachine;
using _Game.Managers;
using DG.Tweening;
using GameGridEnum;
using UnityEngine;
namespace _Game.GameGrid.Unit.DynamicUnit.Chump.ChumpState
{
    public class EmergeChumpState : IState<Chump>
    {
        private readonly Vector3 WATER_SPLASH_OFFSET = Vector3.up * 0.18f;
        public StateEnum Id => StateEnum.Emerge;
        Tween moveTween;
        public void OnEnter(Chump t)
        {
            //DEV: Refactor 
            Vector3 position = t.Tf.position;
            position = new Vector3(position.x, Constants.POS_Y_BOTTOM, position.z);
            t.Tf.position = position;
            moveTween = t.Tf.DOMoveY((float)(Constants.DirFirstHeightOfSurface[GridSurfaceType.Water] + t.FloatingHeightOffset) / 2 * Constants.CELL_SIZE - t.yOffsetOnDown, Constants.MOVING_TIME * 1.5f)
                .OnComplete(() =>
            {
                t.StateMachine.ChangeState(StateEnum.Idle);
                ParticlePool.Play(DataManager.Ins.VFXData.GetParticleSystem(VFXType.WaterSplash), t.Tf.position + WATER_SPLASH_OFFSET);
            });
        }

        public void OnExecute(Chump t)
        {
            
        }

        public void OnExit(Chump t)
        {
            moveTween.Kill();
        }

    }
}