using _Game.DesignPattern;
using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.StaticUnit;
using _Game.Managers;
using _Game.Utilities.Timer;
using GameGridEnum;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Player.PlayerState
{
    public class CutTreePlayerState : IState<Player>
    {
        private bool _isExecuted;
        float originAnimSpeed;
        const float STATE_TIME = 0.4f;
        List<Action> actions = new List<Action>();
        List<float> times = new List<float>() { Constants.CUT_TREE_TIME, STATE_TIME };

        public StateEnum Id => StateEnum.CutTree;

        public void OnEnter(Player t)
        {
            originAnimSpeed = t.AnimSpeed;
            t.SetAnimSpeed(originAnimSpeed * Constants.CUT_TREE_ANIM_TIME / Constants.CUT_TREE_TIME);
            t.ChangeAnim(Constants.CUT_TREE_ANIM);
            t.LookDirection(t.CutTreeData.inputDirection);
            CalculateActionAndTime();
            TimerManager.Inst.WaitForTime(times, actions);


            void CalculateActionAndTime()
            {
                actions.Clear();
                actions.Add(CutTreeAction);
                actions.Add(ChangeState);
            }

            void CutTreeAction()
            {
                CutTree(t);
            }

            void ChangeState()
            {
                t.StateMachine.ChangeState(StateEnum.Idle);
            }
        }

        public void OnExecute(Player t)
        {
        }

        public void OnExit(Player t)
        {
            _isExecuted = false;
            t.SetAnimSpeed(originAnimSpeed);
        }

        private void CutTree(Player t)
        {
            if (!_isExecuted)
            {
                _isExecuted = true;
                LevelManager.Ins.SaveGameState(true);
                // Make the tree OnBePushed with direction calculated by position of the Player and the Tree
                // Spawn a TreeRoot
                t.MainCell.ValueChange();
                t.CutTreeData.tree.MainCell.ValueChange();
                LevelManager.Ins.SaveGameState(false);
                TreeRoot treeRoot = SimplePool.Spawn<TreeRoot>(DataManager.Ins.GetGridUnit(PoolType.TreeRoot));
                treeRoot.OnInit(t.CutTreeData.tree.MainCell, t.CutTreeData.tree.StartHeight);
                LevelManager.Ins.CurrentLevel.AddNewUnitToIsland(treeRoot);
                // Spawn a Chump
                Chump.Chump chump = SimplePool.Spawn<Chump.Chump>(t.CutTreeData.tree.chumpPrefab);
                chump.OnInit(t.CutTreeData.tree.MainCell, t.CutTreeData.tree.StartHeight + 1);
                LevelManager.Ins.CurrentLevel.AddNewUnitToIsland(chump);
                chump.OnBePushed(t.CutTreeData.inputDirection, t);

                //DEV: Save state for new object
                // Despawn the Tree
                t.CutTreeData.tree.OnDespawn();
                LevelManager.Ins.SaveGameState(true);
                chump.MainCell.ValueChange();
                // Push the Chump with the direction
                ParticlePool.Play(DataManager.Ins.VFXData.GetParticleSystem(VFXType.LeafExplosion),
                    t.CutTreeData.tree.Tf.position + Vector3.up * 2f);
            }
        }
    }
}