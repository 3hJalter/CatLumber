using _Game._Scripts.InGame;
using _Game.DesignPattern;
using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.StaticUnit;
using _Game.Managers;
using _Game.Utilities.Grid;
using GameGridEnum;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Enemy.EnemyStates
{
    public class IdleArcherEnemyState : IState<ArcherEnemy>
    {
        private const int MAX_RANGE = 20;

        private bool _isChangeAnim;
        List<GameGridCell> attackRange = new List<GameGridCell>();
        List<DangerIndicator> dangerIndicators = new List<DangerIndicator>();
        Direction attackDirection = Direction.None;
        private bool isAttack = false;
        private ArcherEnemy main;
        public StateEnum Id => StateEnum.Idle;

        public IdleArcherEnemyState()
        {
            GameManager.Ins.RegisterListenerEvent(EventID.ObjectInOutDangerCell, IsResetAttackRange);
        }
        public void OnEnter(ArcherEnemy t)
        {
            isAttack = false;
            attackDirection = t.SkinRotationDirection;
            GameGridCell cell = t.MainCell;
            main = t;
            for (int i = 0; i < MAX_RANGE; i++)
            {
                cell = cell.GetNeighborCell(attackDirection);
                if (cell == null || cell.Data.gridSurfaceType == GridSurfaceType.Water) break;
                if (IsPreventAttack())
                {
                    cell.Data.IsBlockDanger = true;
                    attackRange.Add(cell);
                    break;
                }

                cell.Data.IsDanger = true;
                cell.Data.IsBlockDanger = false;
                isAttack = isAttack || IsPlayerInAttackRange();
                attackRange.Add(cell);
                dangerIndicators.Add(SimplePool.Spawn<DangerIndicator>(PoolType.DangerIndicator, cell.WorldPos + Vector3.up * 1.25f, Quaternion.identity));
            }

            bool IsPreventAttack()
            {
                for (int i = (int)t.StartHeight; i <= (int)t.EndHeight; i++)
                {
                    if (cell.Data.gridUnits[i] && cell.Data.gridUnits[i] is not Player.Player)
                    {
                        return true;
                    }
                }
                return false;
            }
            bool IsPlayerInAttackRange()
            {
                for (int i = (int)t.StartHeight; i <= (int)t.EndHeight; i++)
                {
                    if (cell.Data.gridUnits[i] && cell.Data.gridUnits[i] is Player.Player player)
                    {
                        if(!player.IsDead)
                            return true;
                        return false;
                    }
                }
                return false;
            }
        }
        public void OnExecute(ArcherEnemy t)
        {
            if (t.IsDead)
            {
                t.StateMachine.ChangeState(StateEnum.Die);
                return;
            }
            if (isAttack)
            {
                t.StateMachine.ChangeState(StateEnum.Attack);
                isAttack = false;
                return;
            }

            if (t.Direction == Direction.None)
            {
                if (!_isChangeAnim)
                {
                    _isChangeAnim = true;
                    t.ChangeAnim(Constants.IDLE_ANIM);
                }
                return;
            }           
        }
        public void OnExit(ArcherEnemy t)
        {
            foreach (GameGridCell cell in attackRange)
            {
                cell.Data.IsDanger = false;
                cell.Data.IsBlockDanger = false;
            }
            attackRange.Clear();
            foreach(DangerIndicator cell in dangerIndicators)
            {
                SimplePool.Despawn(cell);
            }
            dangerIndicators.Clear();
            _isChangeAnim = false;
        }     
        private void IsResetAttackRange(object value)
        {
            if (!GameManager.Ins.IsState(GameState.InGame)) return;
            if(attackRange.Contains((GameGridCell)value))
                main.StateMachine.ChangeState(StateEnum.Idle);
        }
        ~IdleArcherEnemyState()
        {
            if(GameManager.Ins)
                GameManager.Ins.UnregisterListenerEvent(EventID.ObjectInOutDangerCell, IsResetAttackRange);
        }
    }
}
