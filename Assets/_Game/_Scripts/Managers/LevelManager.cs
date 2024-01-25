using System.Collections.Generic;
using System.Linq;
using _Game._Scripts.InGame;
using _Game._Scripts.Managers;
using _Game._Scripts.Tutorial;
using _Game.Camera;
using _Game.Data;
using _Game.DesignPattern;
using _Game.GameGrid.Unit;
using _Game.GameGrid.Unit.DynamicUnit.Player;
using _Game.Managers;
using _Game.UIs.Screen;
using _Game.Utilities;
using _Game.Utilities.Grid;
using UnityEngine;
using UnityEngine.Serialization;
using VinhLB;
using static _Game.Utilities.Grid.Grid<_Game.GameGrid.GameGridCell, _Game.GameGrid.GameGridCellData>;

namespace _Game.GameGrid
{
    public class LevelManager : Singleton<LevelManager>
    {
        // private readonly Dictionary<int, Level> _preLoadLevels = new();

        // public Dictionary<int, Level> PreLoadLevels => _preLoadLevels;

        [SerializeField]
        private int _levelIndex;

        private Level _currentLevel;
        //Test
        [SerializeField]
        private Material _fontMaterial;
        [SerializeField]
        private FishSpawner _fishSpawner;
        [SerializeField]
        private CloudSpawner _cloudSpawner;

        public int LevelIndex => _levelIndex;
        public Level CurrentLevel => _currentLevel;
        public bool IsConstructingLevel;


        private CareTaker savingState;
        public Player player;

        private void Start()
        {
            if (DebugManager.Ins && DebugManager.Ins.Level >= 0)
            {
                DataManager.Ins.GameData.user.normalLevelIndex = DebugManager.Ins.Level;
            }
            _levelIndex = DataManager.Ins.GameData.user.normalLevelIndex;
            GridUtilities.OverlayMaterial = _fontMaterial;
            OnGenerateLevel(_levelIndex == 0);
            SetCameraToPosition(CurrentLevel.GetCenterPos());
        }

        public void OnGenerateLevel(bool needInit)
        {
            OnCheckTutorial();
            IsConstructingLevel = true;
            _currentLevel = new Level(LevelType.Normal, _levelIndex);
            if (needInit && !_currentLevel.IsInit)
            {
                InitLevel();
            }

            _fishSpawner.SpawnFish();
            // _cloudSpawner.SpawnClouds();
            FXManager.Ins.UpdateFogColliderPositionAndScale(_currentLevel.GetBottomLeftPos(),
                _currentLevel.GetTopRightPos());
        }

        public void InitLevel()
        {
            GameManager.Ins.PostEvent(EventID.StartGame);
            if (_currentLevel.IsInit) return;
            _currentLevel.OnInitLevelSurfaceAndUnit();
            _currentLevel.OnInitPlayerToLevel();
            savingState = new CareTaker(this);
            _currentLevel.GridMap.CompleteObjectInit();
            IsConstructingLevel = false;
            savingState = new CareTaker(this);
            SetCameraToPlayerIsland();
            //NOTE: Test
            DebugManager.Ins?.DebugGridData(_currentLevel.GridMap);
            // TEMPORARY: CUTSCENE, player will be show when cutscene end
            if (_levelIndex == 0) HidePlayer(true);
        }

        public void ResetLevelIsland()
        {
            _currentLevel.ResetIslandPlayerOn();

            // _fishSpawner.SpawnFish(false);
            // _cloudSpawner.SpawnClouds();
            // FXManager.Ins.UpdateFogColliderPositionAndSize(_currentLevel.GetBottomLeftPos(),
            //     _currentLevel.GetTopRightPos());
        }

        private void OnCheckTutorial()
        {
            if (TutorialManager.Ins.TutorialList.TryGetValue(_levelIndex, out ITutorialCondition tutorialData))
            {
                tutorialData.ResetTutorial();
            }
        }

        public void HidePlayer(bool isHide)
        {
            player.gameObject.SetActive(!isHide);
        }

        public void SetCameraToPlayerIsland()
        {
            SetCameraToIsland(player.islandID);
        }

        private void SetCameraToIsland(int index)
        {
            CameraManager.Ins.ChangeCameraTargetPosition(CurrentLevel.GetIsland(index).centerIslandPos);
        }

        private void SetCameraToPosition(Vector3 position)
        {
            CameraManager.Ins.ChangeCameraTargetPosition(position);
        }

        public void OnWin()
        {
            // Show win screen
            // +1 LevelIndex and save
            _levelIndex++;
            // Temporary handle when out of level
            if (_levelIndex >= DataManager.Ins.CountNormalLevel) _levelIndex = 0;
            DataManager.Ins.GameData.user.normalLevelIndex = _levelIndex;
            DataManager.Ins.Save();
            GameManager.Ins.PostEvent(EventID.WinGame);
            // Future: Add reward collected in-game
        }

        public void OnGoLevel(int index)
        {
            if (_levelIndex == index) return;
            _levelIndex = index;
            IsConstructingLevel = true;
            _currentLevel.OnDeSpawnLevel();
            OnGenerateLevel(true);
        }

        public void OnNextLevel()
        {
            // Load next level
            IsConstructingLevel = true;
            _currentLevel.OnDeSpawnLevel();
            // TEMPORARY: Destroy object on cutscene at level 1, need other way to handle this
            if (_levelIndex - 1 == 0)
            {
                TutorialManager.Ins.OnDestroyCutsceneObject();
            }
            OnGenerateLevel(true);
            // OnChangeTutorialIndex();
        }

        public void OnRestart()
        {
            player.OnDespawn();
            CurrentLevel.ResetAllIsland();
            CurrentLevel.ResetNonIslandUnit();
            CurrentLevel.GridMap.Reset();
            // player = SimplePool.Spawn<Player>(DataManager.Ins.GetGridUnit(PoolType.Player));
            // player.OnInit(CurrentLevel.firstPlayerInitCell);
            SetCameraToPlayerIsland();
            // FxManager.Ins.ResetTrackedTrampleObjectList();
            _fishSpawner.SpawnFish();
            savingState.Reset();
        }

        public void ResetGameState()
        {
            savingState.Reset();
        }

        public void OnUndo()
        {
            savingState.Undo();
            SetCameraToPlayerIsland();
        }

        public void SaveGameState(bool isMerge)
        {
            savingState.Save(isMerge);
        }

        public void DiscardSaveState()
        {
            savingState.PopSave();
        }

        private void SetCameraToPlayer()
        {
            // CameraFollow.Ins.SetTarget(Player.Tf);`
            CameraManager.Ins.ChangeCameraTarget(ECameraType.InGameCamera, player.Tf);
        }

        public class CareTaker
        {
            LevelManager main;
            Stack<IMemento> dataHistorys = new Stack<IMemento>();
            Stack<List<IMemento>> objectHistorys = new Stack<List<IMemento>>();

            public CareTaker(LevelManager main)
            {
                this.main = main;

                #region Init Spawn State
                SavingObjects();
                HashSet<GridUnit> gridUnits = main._currentLevel.Islands[main.player.islandID].GridUnits;
                foreach (GridUnit gridUnit in gridUnits)
                {
                    gridUnit.MainCell.ValueChange();
                }
                SavingState();
                #endregion
            }

            public void Undo()
            {
                if (main.CurrentLevel.GridMap.IsChange)
                {
                    SavingState(true);
                    if (objectHistorys.Count < dataHistorys.Count)
                        objectHistorys.Push(objectHistorys.Peek());
                }
                if (dataHistorys.Count > 0)
                {
                    dataHistorys.Pop().Restore();
                    foreach (IMemento objectRevert in objectHistorys.Pop())
                    {
                        objectRevert.Restore();
                    }
                    if (objectHistorys.Count == 0)
                    {
                        SavingObjects();
                    }
                    DevLog.Log(DevId.Hung, "UNDO_STATE - SUCCESS!!");
                }
                else
                {
                    DevLog.Log(DevId.Hung, "UNDO_STATE - FAILURE!!");
                }
            }

            public void Save(bool isMerge = false)
            {
                SavingState(isMerge);
                SavingObjects(isMerge);
            }

            public void PopSave()
            {
                dataHistorys.Pop();
                objectHistorys.Pop();
            }

            private void SavingState(bool isMerge = false)
            {
                if (!isMerge || dataHistorys.Count == 0)
                {
                    if (main.CurrentLevel.GridMap.IsChange)
                    {
                        IMemento states = main._currentLevel.GridMap.Save();
                        dataHistorys.Push(states);
                    }
                }
                else
                {
                    GridMemento stateData = (GridMemento)dataHistorys.Peek();
                    stateData.Merge((GridMemento)main._currentLevel.GridMap.Save());
                }
            }

            private void SavingObjects(bool isMerge = false)
            {
                List<IMemento> states;
                if (!isMerge || objectHistorys.Count == 0)
                {
                    states = new List<IMemento>() { main.player.Save() };
                    HashSet<GridUnit> gridUnits = main._currentLevel.Islands[main.player.islandID].GridUnits;
                    foreach (GridUnit gridUnit in gridUnits)
                    {
                        if (gridUnit.IsSpawn)
                            states.Add(gridUnit.Save());
                    }
                    objectHistorys.Push(states);
                }
                else
                {
                    states = objectHistorys.Peek();
                    HashSet<GridUnit> gridUnits = main._currentLevel.Islands[main.player.islandID].GridUnits;
                    foreach (GridUnit gridUnit in gridUnits)
                    {
                        if (gridUnit.IsSpawn && !states.Any(x => x.Id == gridUnit.GetHashCode()))
                            states.Add(gridUnit.Save());
                    }
                }
            }

            public void Reset()
            {
                dataHistorys.Clear();
                objectHistorys.Clear();

                #region Init Spawn State
                SavingObjects();
                HashSet<GridUnit> gridUnits = main._currentLevel.Islands[main.player.islandID].GridUnits;
                foreach (GridUnit gridUnit in gridUnits)
                {
                    gridUnit.MainCell.ValueChange();
                }
                SavingState();
                #endregion
            }
        }
    }
}