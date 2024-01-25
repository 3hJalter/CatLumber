using System.Collections.Generic;
using System.Linq;
using _Game.DesignPattern;
using _Game.DesignPattern.StateMachine;
using _Game.Managers;
using _Game.Utilities;
using DG.Tweening;
using GameGridEnum;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Chump.ChumpState
{
    public class FormRaftChumpState : IState<Chump>
    {
        private readonly Vector3 WATER_SPLASH_OFFSET = Vector3.up * 0.18f;
        private readonly List<GameGridCell> createRaftCells = new();
        private List<GameGridCell> createChumpCells = new();
        private Chump chumpInWater;
        HashSet<GridUnit> spawnUnits = new();
        Tween moveTween;

        public StateEnum Id => StateEnum.FormRaft;

        public void OnEnter(Chump t)
        {
            t.StateMachine.OverrideState = StateEnum.FormRaft;
            
            InitData(t);
            #region ANIM
            Sequence s = DOTween.Sequence();
            moveTween = s;
            ParticlePool.Play(DataManager.Ins.VFXData.GetParticleSystem(VFXType.WaterSplash), chumpInWater.Tf.position + WATER_SPLASH_OFFSET);

            s.Append(t.Tf.DOMoveY(Constants.POS_Y_BOTTOM, Constants.MOVING_TIME * 1.2f).SetEase(Ease.Linear).OnComplete(() =>
            {
                OnExit(t);
                Spawn(t);              
            }));
            s.Join(chumpInWater.Tf.DOMoveY(Constants.POS_Y_BOTTOM, Constants.MOVING_TIME * 1.2f).SetEase(Ease.Linear));
            t.RemoveUnitFromCell();
            #endregion
        }

        public void OnExecute(Chump t)
        {
            
        }

        public void OnExit(Chump t)
        {
            chumpInWater = null;
            spawnUnits.Clear();
            t.StateMachine.OverrideState = StateEnum.None;
            moveTween.Kill();
        }

        private void InitData(Chump t)
        {
            createRaftCells.Clear();
            createChumpCells.Clear();
            // Add all cell in t.belowUnits[i].cellInUnits to cells
            HashSet<GameGridCell> cells = new(t.belowUnits.SelectMany(unit => unit.cellInUnits));
            foreach (GameGridCell cell in cells)
            {
                GameUnit unit = cell.GetGridUnitAtHeight(
                    Constants.DirFirstHeightOfSurface[GridSurfaceType.Water]
                    + t.FloatingHeightOffset); // Check the floating unit, also we know water is floatSurface, no need to check its bool
                if(unit != null)
                {
                    chumpInWater = (Chump)unit;
                    break;
                }
            }
            // createRaftCells is all cell that both t.cellInUnits and cells have
            for (int i = 0; i < t.cellInUnits.Count; i++)
            {
                if (cells.Add(t.cellInUnits[i])) continue;
                createRaftCells.Add(t.cellInUnits[i]);
                cells.Remove(t.cellInUnits[i]);
            }

            // createChumpCells is all cell in cells
            createChumpCells = cells.ToList();
        }

        private void Spawn(Chump t)
        {
            
            // Chose what the UnitTypeXZ of RaftPrefab and ChumpPrefab
            UnitTypeXZ typeXZWhenSpawn = t.UnitTypeY is UnitTypeY.Up ? t.belowUnits.First().UnitTypeXZ : t.UnitTypeXZ;
            // Raft direction for rotate skin is right if UnitTypeXZ is Horizontal, Back if UnitTypeXZ is Vertical
            Direction spawnRaftDirection =
                typeXZWhenSpawn == UnitTypeXZ.Horizontal ? Direction.Right : Direction.Back;
            if (t.cellInUnits.Count == createRaftCells.Count)
            {
                Raft.Raft raft = SimplePool.Spawn<Raft.Raft>(t.RaftPrefab);
                raft.OnInit(t.MainCell, HeightLevel.ZeroPointFive, false, spawnRaftDirection);              
                raft.islandID = t.islandID;
                LevelManager.Ins.CurrentLevel.AddNewUnitToIsland(raft);
                spawnUnits.Add(raft);
            }
            else
            {
                for (int i = 0; i < createRaftCells.Count; i++)
                {
                    Raft.Raft raft = SimplePool.Spawn<Raft.Raft>(DataManager.Ins.GetGridUnit(PoolType.Raft));
                    raft.OnInit(createRaftCells[i], Constants.DirFirstHeightOfSurface[GridSurfaceType.Water] + raft.FloatingHeightOffset, 
                        false, spawnRaftDirection);
                    raft.islandID = t.islandID;
                    LevelManager.Ins.CurrentLevel.AddNewUnitToIsland(raft);
                    spawnUnits.Add(raft);
                }
            }

            for (int i = 0; i < createChumpCells.Count; i++)
            {
                Chump chumpUnit =
                    SimplePool.Spawn<Chump>(DataManager.Ins.GetGridUnit(PoolType.ChumpShort));
                chumpUnit.UnitTypeY = UnitTypeY.Down;
                chumpUnit.OnInit(createChumpCells[i], Constants.DirFirstHeightOfSurface[GridSurfaceType.Water] + chumpUnit.FloatingHeightOffset, false); 
                chumpUnit.StateMachine.ChangeState(StateEnum.Emerge);
                chumpUnit.islandID = t.islandID;
                LevelManager.Ins.CurrentLevel.AddNewUnitToIsland(chumpUnit);
                chumpUnit.UnitTypeXZ = typeXZWhenSpawn;
                chumpUnit.skin.localRotation =
                    Quaternion.Euler(chumpUnit.UnitTypeXZ is UnitTypeXZ.Horizontal
                        ? Constants.HorizontalSkinRotation
                        : Constants.VerticalSkinRotation);
                spawnUnits.Add(chumpUnit);
            }
            foreach (GridUnit unit in t.belowUnits.ToList().Where(unit => !spawnUnits.Contains(unit))) unit.OnDespawn();
            t.OnDespawn();
            LevelManager.Ins.SaveGameState(true);
        }
    }
}
