using _Game.DesignPattern.StateMachine;
using _Game.Utilities;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Raft.RaftState
{
    public class IdleRaftState : IState<Raft>
    {
        private const float MOVE_Y_VALUE = 0.1f;
        private const float MOVE_Y_TIME = 2f;

        Vector3 originTransform;
        
        private Tween floatingTween;

        public StateEnum Id => StateEnum.Idle;

        public void OnEnter(Raft t)
        {
            #region ANIM
            //DEV: Refactor
            originTransform = t.Tf.transform.position;
            floatingTween = DOVirtual.Float(0, MOVE_Y_TIME, MOVE_Y_TIME, i =>
            {
                SetSinWavePosition(i);
            }).SetLoops(-1).SetEase(Ease.Linear);



            void SetSinWavePosition(float time)
            {
                float value = Mathf.Sin(2 * time * Mathf.PI / MOVE_Y_TIME) * MOVE_Y_VALUE;
                t.Tf.transform.position = originTransform + Vector3.up * value;
            }
            #endregion
        }

        public void OnExecute(Raft t)
        {

        }

        public void OnExit(Raft t)
        {
            floatingTween.Kill();
        }
    }
}
