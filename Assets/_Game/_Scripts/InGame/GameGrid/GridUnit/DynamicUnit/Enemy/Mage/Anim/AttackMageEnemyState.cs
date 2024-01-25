using _Game.DesignPattern.StateMachine;
using _Game.Utilities.Timer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Enemy.EnemyStates
{
    public class AttackMageEnemyState : IState<MageEnemy>
    {
        public StateEnum Id => StateEnum.Attack;
        private const float ATTACK_TIME = 0.4f;
        private const float ATTACK_ANIM_TIME = 1.15f;
        List<Action> actions = new List<Action>();
        List<float> times = new List<float>() { ATTACK_TIME, ATTACK_ANIM_TIME };

        public void OnEnter(MageEnemy t)
        {
            t.ChangeAnim(Constants.ATTACK, true);
            actions.Clear();
            actions.Add(Attack);
            actions.Add(ChangeToIdle);
            TimerManager.Inst.WaitForTime(times, actions);

            void Attack()
            {
                LevelManager.Ins.player.IsDead = true;
            }
            void ChangeToIdle()
            {
                t.StateMachine.ChangeState(StateEnum.Idle);
            }
        }

        public void OnExecute(MageEnemy t)
        {
            
        }

        public void OnExit(MageEnemy t)
        {
            
        }
    }
}