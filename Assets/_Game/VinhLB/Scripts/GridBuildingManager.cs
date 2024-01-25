using _Game.Camera;
using _Game.DesignPattern;
using _Game.GameGrid;
using _Game.Utilities.Grid;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Game.GameGrid.Unit;
using _Game.Managers;
using UnityEngine;

namespace VinhLB
{
    public class GridBuildingManager : Singleton<GridBuildingManager>
    {
        public event System.Action OnBuildingModeChanged;
        public event System.Action OnSelectedChanged;

        private const string HOME_LEVEL_NAME = "L0";

        [Header("References")]
        [SerializeField]
        private BuildingUnitDatabaseSO _buildingUnitDatabaseSo;

        [Header("Settings")]
        [SerializeField]
        private bool _testing;
        [SerializeField]
        private bool _useKeyboard;
        [SerializeField]
        private LayerMask _placeableLayerMask;

        private Grid<GameGridCell, GameGridCellData> _homeGrid;

        private bool _isOnBuildingMode;
        private int _currentBuildingUnitDataId;
        private Direction _currentDirection;
        private Vector3 _lastMousePosition;
        private BuildingUnitData _currentBuildingUnitData;
        private bool _isAnyChanged;
        
        public bool IsOnBuildingMode => _isOnBuildingMode;
        public BuildingUnitData CurrentBuildingUnitData => _currentBuildingUnitData;

        private void Start()
        {
            if (_testing)
            {
                OnInit();

                ToggleBuildMode();
            }
        }

        private void Update()
        {
            HandleControls();
        }

        public void OnInit()
        {
            _isOnBuildingMode = false;
            _currentBuildingUnitDataId = -1;
            _currentDirection = Direction.Back;

            if (_testing)
            {
                LevelManager.Ins.OnGenerateLevel(true);
                _homeGrid = LevelManager.Ins.CurrentLevel.GridMap;

                var debugGrid = new Grid<GameGridCell, GameGridCellData>.DebugGrid();
                debugGrid.DrawGrid(_homeGrid, true);

                CameraFollow.Ins.ChangeCamera(ECameraType.InGameCamera);
            }
            else
            {
                _homeGrid = LevelManager.Ins.CurrentLevel.GridMap;
            }
        }

        public List<BuildingUnitData> GetPlacedObjectDataList()
        {
            return _buildingUnitDatabaseSo.BuildingUnitDataList;
        }

        public Vector3 GetMouseWorldSnappedPosition()
        {
            return _homeGrid.GetWorldPosition(_lastMousePosition);
        }

        public Vector3 GetRotationOffset()
        {
            Vector2Int rotationOffset = BuildingUnitData.GetRotationOffset(_currentBuildingUnitData.Width,
                _currentBuildingUnitData.Height, _currentDirection);

            return new Vector3(rotationOffset.x, 0f, rotationOffset.y) * _homeGrid.CellSize;
        }

        public Quaternion GetPlacedObjectRotation()
        {
            return Quaternion.Euler(0, BuildingUnitData.GetRotationAngle(_currentDirection), 0);
        }

        public void ToggleBuildMode()
        {
            _isOnBuildingMode = !_isOnBuildingMode;

            FXManager.Ins.SwitchGridActive(true, _isOnBuildingMode);

            if (_isOnBuildingMode)
            {
                if (LevelManager.Ins.CurrentLevel != null && _homeGrid != LevelManager.Ins.CurrentLevel.GridMap)
                {
                    _homeGrid = LevelManager.Ins.CurrentLevel.GridMap;
                }

                _isAnyChanged = false;
                Utilities.TryGetCenterScreenPosition(out _lastMousePosition, _placeableLayerMask);
            }
            else
            {
                if (_isAnyChanged)
                {
                    // GridMapFileUtilities.Save(HOME_LEVEL_NAME);
                }
            }

            ChangeCurrentObjectDataId(-1);

            OnBuildingModeChanged?.Invoke();
        }

        public void ChangeCurrentObjectDataId(int id)
        {
            _currentBuildingUnitDataId = id;
            _currentBuildingUnitData = _buildingUnitDatabaseSo.BuildingUnitDataList
                .FirstOrDefault(data => data.Id == _currentBuildingUnitDataId);

            _currentDirection = Direction.Back;

            OnSelectedChanged?.Invoke();
        }

        public void ChangePlaceDirection()
        {
            _currentDirection = BuildingUnitData.GetNextDirection(_currentDirection);
        }

        public void PlaceBuilding()
        {
            if (CanBuild(out GameGridCell gameGridCell))
            {
                BuildingUnit unit = SimplePool.Spawn<BuildingUnit>(
                    DataManager.Ins.GetGridUnit(_currentBuildingUnitData.PoolType));
                unit.OnInit(gameGridCell, GameGridEnum.HeightLevel.One, true, _currentDirection);

                _isAnyChanged = true;
            }
        }

        public void DeleteBuilding()
        {
            GameGridCell gameGridCell = _homeGrid.GetGridCell(_lastMousePosition);
            if (gameGridCell != null && gameGridCell.HasBuilding())
            {
                gameGridCell.DestroyGridUnits();

                GridUnit unit = gameGridCell.GetGridUnitAtHeight(GameGridEnum.HeightLevel.One);
                List<GameGridCell> neighborCellList = unit.cellInUnits;
                for (int i = 0; i < neighborCellList.Count; i++)
                {
                    neighborCellList[i].ClearGridUnit();
                }

                _isAnyChanged = true;
            }
        }

        public bool CanBuild(out GameGridCell gameGridCell)
        {
            gameGridCell = _homeGrid.GetGridCell(_lastMousePosition);
            if (gameGridCell == null)
            {
                return false;
            }
            
            if (_currentBuildingUnitData == null)
            {
                return false;
            }
                
            List<GameGridCell> neighborCellList = gameGridCell.GetCellsInsideUnit(_homeGrid,
                _currentBuildingUnitData.Width, _currentBuildingUnitData.Height, _currentDirection);
            for (int i = 0; i < neighborCellList.Count; i++)
            {
                if (neighborCellList[i] == null ||
                    !neighborCellList[i].CanBuild(_currentBuildingUnitData.BelowSurfaceType))
                {
                    return false;
                }
            }

            if (_currentBuildingUnitData.CheckAdjacentCells)
            {
                List<GameGridCell> adjacentCellList = gameGridCell.GetAdjacentCells(_homeGrid,
                    _currentBuildingUnitData.Width, _currentBuildingUnitData.Height, _currentDirection);
                if (adjacentCellList.Count(cell =>
                        cell != null &&
                        cell.Data.gridSurface is not null &&
                        cell.Data.gridSurface.SurfaceType == _currentBuildingUnitData.AdjacentSurfaceType) <
                    _currentBuildingUnitData.MinAdjacentCells)
                {
                    return false;
                }
            }

            return true;
        }

        private void HandleControls()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                ToggleBuildMode();
            }

            if (!_isOnBuildingMode)
            {
                return;
            }

            if (Input.GetMouseButton(0) && !Utilities.IsPointerOverUIGameObject())
            {
                Utilities.TryGetMouseWorldPosition(out _lastMousePosition, _placeableLayerMask);
            }

            if (!_useKeyboard)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                PlaceBuilding();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                DeleteBuilding();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ChangePlaceDirection();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                int newPlacedObjectDataId =
                    (_currentBuildingUnitDataId + 1) % _buildingUnitDatabaseSo.BuildingUnitDataList.Count;
                ChangeCurrentObjectDataId(newPlacedObjectDataId);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                GridMapFileUtilities.Save(HOME_LEVEL_NAME);
            }
        }
    }
}