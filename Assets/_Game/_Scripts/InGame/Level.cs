using System;
using System.Collections.Generic;
using _Game.Data;
using _Game.DesignPattern;
using _Game.GameGrid;
using _Game.GameGrid.GridSurface;
using _Game.GameGrid.Unit;
using _Game.GameGrid.Unit.DynamicUnit.Player;
using _Game.Managers;
using _Game.Utilities.Grid;
using DG.Tweening;
using GameGridEnum;
using MapEnum;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Game._Scripts.InGame
{
    public class Level
    {
        #region constructor

        public Level(LevelType type, int index, Transform parent = null)
        {
            IsInit = false;
            // Set GridMap
            Index = index;
            // Get data
            // _textGridData = GameGridDataHandler.CreateGridData(index);
            _rawLevelData = JsonGridDataHandler.CreateLevelData(type, index);
            GridSizeX = _rawLevelData.s.x;
            gridSizeY = _rawLevelData.s.y;
            // Create Grid Map
            CreateGridMap();
            // Spawn Grid Surface
            SpawnGridSurfaceToGrid();
            // Spawn Units (Not Init)
            OnSpawnUnits();
            OnSpawnShadowUnit();
            OnSetHintLine();
            // if isInit -> AddIsland & InitUnit
            if (parent is null) return;
            // Set parent
            SetParent(parent);
        }

        #endregion

        public void ResetNonIslandUnit()
        {
            for (int i = 0; i < nonIslandUnitLis.Count; i++)
            {
                nonIslandUnitLis[i].unit.OnDespawn();
                nonIslandUnitLis[i].unit.ResetData();
                // Spawn
                GridUnit unit = (GridUnit) SimplePool.SpawnDirectFromPool(nonIslandUnitLis[i].unit, Vector3.zero, Quaternion.identity);
                unit.OnInit(nonIslandUnitLis[i].mainCellIn, nonIslandUnitLis[i].startHeightIn, true,
                    nonIslandUnitLis[i].directionIn);
            }
        }

        public struct LevelUnitData
        {
            public GameGridCell mainCellIn;
            public HeightLevel startHeightIn;
            public Direction directionIn;
            public PoolType unitType;
            public GridUnit unit;
        }

        #region data

        // Init Data
        // private readonly TextGridData _textGridData;
        private readonly RawLevelData _rawLevelData;

        // Map

        private readonly int gridSizeY;

        // Surface & Unit

        // Island (Each island has some surfaces and units)
        public Dictionary<int, Island> Islands { get; } = new();

        // Some other data
        private GameGridCell firstPlayerInitCell;
        private Direction firstPlayerDirection;

        // Get Data
        private int GridSizeX { get; }

        public int Index { get; }

        public bool IsInit { get; private set; }

        private GridSurface[,] GridSurfaceMap { get; set; }

        public List<Vector3> HintLinePosList { get; } = new();

        public List<LevelUnitData> UnitDataList { get; } = new();
        private readonly List<LevelUnitData> nonIslandUnitLis = new();

        public List<GridUnit> ShadowUnitList { get; } = new();

        public Grid<GameGridCell, GameGridCellData> GridMap { get; private set; }

        #endregion

        #region public function

        public void OnInitLevelSurfaceAndUnit()
        {
            AddIslandIdToSurface();
            for (int i = 0; i < UnitDataList.Count; i++) OnInitUnit(UnitDataList[i]);
            IsInit = true;
        }

        public Island GetIsland(int islandID)
        {
            return Islands[islandID];
        }

        public Vector3 GetCenterPos()
        {
            float centerX = (GridMap.GetGridCell(0, 0).WorldX + GridMap.GetGridCell(GridSizeX - 1, 0).WorldX) / 2;
            float centerZ = (GridMap.GetGridCell(0, 0).WorldY + GridMap.GetGridCell(0, gridSizeY - 1).WorldY) / 2;
            return new Vector3(centerX, 0, centerZ);
        }

        public Vector3 GetBottomLeftPos()
        {
            GameGridCell cell = GridMap.GetGridCell(0, 0);

            return new Vector3(cell.WorldX, 0f, cell.WorldY);
        }

        public Vector3 GetTopRightPos()
        {
            GameGridCell cell = GridMap.GetGridCell(GridSizeX - 1, gridSizeY - 1);

            return new Vector3(cell.WorldX, 0f, cell.WorldY);
        }

        public float GetMaxZPos()
        {
            return GridMap.GetGridCell(0, gridSizeY - 1).WorldY;
        }

        public float GetMinZPos()
        {
            return GridMap.GetGridCell(0, 0).WorldY;
        }

        public void AddNewUnitToIsland(GridUnit unit)
        {
            if (!Islands.ContainsKey(unit.islandID)) return;
            Islands[unit.islandID].AddNewUnitToIsland(unit);
        }

        public GameGridCell GetCellWorldPos(Vector3 position)
        {
            return GridMap.GetGridCell(position);
        }

        public GameGridCell GetCell(Vector2Int position)
        {
            return GridMap.GetGridCell(position.x, position.y);
        }

        public void SetFirstPlayerStepOnIsland(GameGridCell cell)
        {
            Islands[LevelManager.Ins.player.islandID].SetFirstPlayerStepCell(cell);
            CameraManager.Ins.ChangeCameraTargetPosition(Islands[LevelManager.Ins.player.islandID].centerIslandPos);
        }

        public void ResetIslandPlayerOn()
        {
            if (!Islands.ContainsKey(LevelManager.Ins.player.islandID)) return;
            GridMap.Reset();
            LevelManager.Ins.IsConstructingLevel = true;
            Islands[LevelManager.Ins.player.islandID].ResetIsland();
            LevelManager.Ins.player.OnDespawn();
            LevelManager.Ins.player = SimplePool.Spawn<Player>(DataManager.Ins.GetGridUnit(PoolType.Player));
            LevelManager.Ins.player.OnInit(Islands[LevelManager.Ins.player.islandID].FirstPlayerStepCell);
            GridMap.CompleteObjectInit();
            LevelManager.Ins.IsConstructingLevel = false;
            LevelManager.Ins.ResetGameState();
        }

        public void ResetAllIsland()
        {
            for (int i = 0; i < Islands.Count; i++) Islands[i].ResetIsland();
        }

        public void OnDeSpawnLevel()
        {
            // Despawn all groundUnit
            for (int index0 = 0; index0 < GridSurfaceMap.GetLength(0); index0++)
            for (int index1 = 0; index1 < GridSurfaceMap.GetLength(1); index1++)
            {
                GridSurface gridSurface = GridSurfaceMap[index0, index1];
                if (gridSurface is not null) gridSurface.OnDespawn();
            }

            for (int i = 0; i < ShadowUnitList.Count; i++) Object.Destroy(ShadowUnitList[i].gameObject);

            if (IsInit)
            {
                // Despawn all unit in each island
                for (int i = 0; i < Islands.Count; i++)
                {
                    Island island = Islands[i];
                    island.ClearIsland();
                }

                for (int i = 0; i < nonIslandUnitLis.Count; i++) nonIslandUnitLis[i].unit.OnDespawn();
                nonIslandUnitLis.Clear();
                // Set player to null
                LevelManager.Ins.player.OnDespawn();
                LevelManager.Ins.player = null;
            }
            else
            {
                for (int i = 0; i < UnitDataList.Count; i++)
                {
                    LevelUnitData data = UnitDataList[i];
                    data.unit.OnDespawn();
                }
            }

            firstPlayerInitCell = null;
            // Clear all _islandDic data
            Islands.Clear();
            // Clear all _gridSurfaceMap data
            GridSurfaceMap = null;
            // Clear all _gridMap data
            GridMap = null;
            // Clear all _unitDataList data
            UnitDataList.Clear();
            IsInit = false;
        }

        private Tween _tweenShadowUnitList;

        public void ChangeShadowUnitAlpha(bool isHide)
        {
            if (ShadowUnitList.Count == 0) return;
            // Kill the previous tween if they are running
            _tweenShadowUnitList?.Kill();
            float currentAlphaTransparency = ShadowUnitList[0].GetAlphaTransparency();

            if (isHide)
            {
                _tweenShadowUnitList = DOVirtual.Float(currentAlphaTransparency, 0, currentAlphaTransparency,
                        value => ShadowUnitList[0].SetAlphaTransparency(value))
                    .OnComplete(() =>
                    {
                        // Set active to false for all
                        for (int i = 0; i < ShadowUnitList.Count; i++) ShadowUnitList[i].gameObject.SetActive(false);
                    });
            }
            else
            {
                // Set active to true for all
                for (int i = 0; i < ShadowUnitList.Count; i++) ShadowUnitList[i].gameObject.SetActive(true);
                _tweenShadowUnitList = DOVirtual.Float(currentAlphaTransparency, 0.5f, 0.5f - currentAlphaTransparency,
                    value => ShadowUnitList[0].SetAlphaTransparency(value));
            }

        }

        public void ChangeShadowUnitAlpha(bool isHide, int index)
        {
            if (ShadowUnitList.Count <= index) return;
            // Kill the previous tween if they are running
            _tweenShadowUnitList?.Kill();
            float currentAlphaTransparency = ShadowUnitList[index].GetAlphaTransparency();
            if (isHide)
            {
                Debug.Log("Hide");
                _tweenShadowUnitList = DOVirtual.Float(currentAlphaTransparency, 0, currentAlphaTransparency,
                        value => ShadowUnitList[index].SetAlphaTransparency(value))
                    .OnComplete(() => ShadowUnitList[index].gameObject.SetActive(false));
            }
            else
            {
                ShadowUnitList[index].gameObject.SetActive(true);
                Debug.Log("Show shadow unit is active: " + ShadowUnitList[index].gameObject.activeSelf);
                _tweenShadowUnitList = DOVirtual.Float(currentAlphaTransparency, 0.5f, 0.5f - currentAlphaTransparency,
                    value => { ShadowUnitList[index].SetAlphaTransparency(value); }).OnComplete(() =>
                    Debug.Log("Show shadow unit is active 2: " + ShadowUnitList[index].gameObject.activeSelf));
                Debug.Log("Show shadow unit is active 3: " + ShadowUnitList[index].gameObject.activeSelf);
            }
        }

        #endregion

        #region private function

        private void SetParent(Transform parent)
        {
            // set all gridSurface tp parent
            for (int index0 = 0; index0 < GridSurfaceMap.GetLength(0); index0++)
            for (int index1 = 0; index1 < GridSurfaceMap.GetLength(1); index1++)
            {
                GridSurface gridSurface = GridSurfaceMap[index0, index1];
                if (gridSurface is null) continue;
                gridSurface.Tf.SetParent(parent);
            }

            // set all gridUnit to parent
            for (int i = 0; i < UnitDataList.Count; i++)
            {
                LevelUnitData data = UnitDataList[i];
                data.unit.Tf.SetParent(parent);
            }
        }

        private void CreateGridMap()
        {
            GridMap = new Grid<GameGridCell, GameGridCellData>(GridSizeX, gridSizeY, Constants.CELL_SIZE,
                default, () => new GameGridCell(), GridPlane.XZ);
        }

        private void SpawnGridSurfaceToGrid()
        {
            GridSurfaceMap = new GridSurface[GridSizeX, gridSizeY];
            // Loop through all sfD (surface data) in _rawLevelData
            for (int i = 0; i < _rawLevelData.sfD.Length; i++)
            {
                RawLevelData.GridSurfaceData surfaceData = _rawLevelData.sfD[i];
                // Get surface from data manager
                GridSurface gridSurface = DataManager.Ins.GetGridSurface((PoolType)surfaceData.t);
                // If surface is null -> continue
                if (gridSurface is null) continue;
                // Spawn surface
                GameGridCell gridCell = GridMap.GetGridCell(surfaceData.p.x, surfaceData.p.y);
                GridSurface surfaceClone = SimplePool.Spawn<GridSurface>(gridSurface,
                    new Vector3(gridCell.WorldX, 0, gridCell.WorldY), Quaternion.identity);
                // Set surface to grid cell
                gridCell.SetSurface(surfaceClone);
                // Set surface to GridSurfaceMap
                GridSurfaceMap[surfaceData.p.x, surfaceData.p.y] = surfaceClone;
                // Init surface
                surfaceClone.OnInit((Direction)surfaceData.d, (MaterialEnum)surfaceData.m);
            }
        }

        private void AddIslandIdToSurface()
        {
            int currentIslandID = 0;
            for (int y = 0; y < GridSurfaceMap.GetLength(1); y++)
            for (int x = 0; x < GridSurfaceMap.GetLength(0); x++)
                if (IsGridSurfaceHadIsland(x, y, out GridSurface gridSurface))
                {
                    FloodFillIslandID(gridSurface, x, y, currentIslandID);
                    Islands[currentIslandID].SetIslandPos();
                    currentIslandID++;
                }

            return;

            void FloodFillIslandID(GridSurface gridSurface, int x, int y, int islandID)
            {
                gridSurface.IslandID = islandID;
                Islands.TryAdd(islandID, new Island(islandID));
                Islands[islandID].AddGridCell(GridMap.GetGridCell(x, y));
                if (IsGridSurfaceHadIsland(x - 1, y, out GridSurface leftGridSurface))
                    FloodFillIslandID(leftGridSurface, x - 1, y, islandID);
                if (IsGridSurfaceHadIsland(x + 1, y, out GridSurface rightGridSurface))
                    FloodFillIslandID(rightGridSurface, x + 1, y, islandID);
                if (IsGridSurfaceHadIsland(x, y - 1, out GridSurface downGridSurface))
                    FloodFillIslandID(downGridSurface, x, y - 1, islandID);
                if (IsGridSurfaceHadIsland(x, y + 1, out GridSurface upGridSurface))
                    FloodFillIslandID(upGridSurface, x, y + 1, islandID);
            }

            bool IsGridSurfaceHadIsland(int x, int y, out GridSurface gridSurface)
            {
                gridSurface = null;
                int rows = GridSurfaceMap.GetLength(0);
                int cols = GridSurfaceMap.GetLength(1);
                if (x < 0 || x >= rows || y < 0 || y >= cols) return false;
                gridSurface = GridSurfaceMap[x, y];
                if (gridSurface is null) return false;
                if (gridSurface.SurfaceType == GridSurfaceType.Water) return false;
                return gridSurface.IslandID < 0;
            }
        }

        private void OnSpawnUnits()
        {
            // Loop through all uD (unit data) in _rawLevelData
            for (int i = 0; i < _rawLevelData.uD.Length; i++)
            {
                RawLevelData.GridUnitData unitData = _rawLevelData.uD[i];
                // Get unit from data manager
                GridUnit gridUnit = DataManager.Ins.GetGridUnit((PoolType)unitData.t);
                // If unit is null -> continue
                if (gridUnit is null) continue;
                // Check if unit is player
                if ((PoolType)unitData.t is PoolType.Player)
                {
                    if (LevelManager.Ins.player != null) LevelManager.Ins.player.OnDespawn();
                    firstPlayerInitCell = GridMap.GetGridCell(unitData.c.x, unitData.c.y);
                    firstPlayerDirection = (Direction)unitData.d;
                    LevelManager.Ins.player = (Player)SpawnUnit(unitData.c.x, unitData.c.y, (PoolType)unitData.t,
                        (Direction)unitData.d);
                }
                else
                {
                    SpawnUnit(unitData.c.x, unitData.c.y, (PoolType)unitData.t, (Direction)unitData.d);
                }
            }

            return;

            GridUnit SpawnUnit(int x, int y, PoolType type, Direction direction)
            {
                GameGridCell cell = GridMap.GetGridCell(x, y);
                GridUnit unit = SimplePool.Spawn<GridUnit>(DataManager.Ins.GetGridUnit(type));
                LevelUnitData levelUnitData = new()
                {
                    mainCellIn = cell,
                    startHeightIn = unit.overrideStartHeight ? unit.StartHeight : HeightLevel.One,
                    directionIn = direction,
                    unitType = type,
                    unit = unit
                };
                UnitDataList.Add(levelUnitData);
                unit.OnSetPositionAndRotation(PredictUnitPos(levelUnitData.startHeightIn), direction);
                return unit;

                Vector3 PredictUnitPos(HeightLevel heightLevel)
                {
                    float offsetY = (float)heightLevel / 2 * Constants.CELL_SIZE;
                    if (unit.UnitTypeY == UnitTypeY.Down) offsetY -= unit.yOffsetOnDown;
                    return cell.WorldPos + Vector3.up * offsetY;
                }
            }
        }

        private void OnSpawnShadowUnit()
        {
            // Loop through all suD (shadow unit data) in _rawLevelData
            for (int i = 0; i < _rawLevelData.suD.Length; i++)
            {
                RawLevelData.ShadowUnitData shadowUnitData = _rawLevelData.suD[i];
                // Get unit from data manager
                GridUnit gridUnit = DataManager.Ins.GetGridUnit((PoolType)shadowUnitData.t);
                // If unit is null -> continue
                if (gridUnit is null) continue;
                // Spawn unit
                GridUnit unit = Object.Instantiate(gridUnit);
                unit.Tf.position = shadowUnitData.p;
                unit.Tf.eulerAngles = shadowUnitData.rA;
                unit.ChangeMaterial(DataManager.Ins.GetTransparentMaterial());
                unit.SetAlphaTransparency(0);
                unit.ChangeReceiveShadow(false);
                ShadowUnitList.Add(unit);
                unit.gameObject.SetActive(false);
            }
        }

        private void OnSetHintLine()
        {
            // Loop through all htD (hint trail data) in _rawLevelData
            for (int i = 0; i < _rawLevelData.htD.Length; i++)
            {
                RawLevelData.HintTrailData hintTrailData = _rawLevelData.htD[i];
                HintLinePosList.Add(new Vector3(hintTrailData.p.x, Constants.DEFAULT_HINT_TRAIL_HEIGHT,
                    hintTrailData.p.y));
            }
        }

        public void OnInitPlayerToLevel()
        {
            LevelManager.Ins.player.OnInit(firstPlayerInitCell, HeightLevel.One, true, firstPlayerDirection);
            Islands[firstPlayerInitCell.Data.gridSurface.IslandID].SetFirstPlayerStepCell(firstPlayerInitCell);
        }

        private void OnInitUnit(LevelUnitData data)
        {
            data.unit.ResetData();
            data.unit.OnInit(data.mainCellIn, data.startHeightIn, true, data.directionIn, true);
            if (data.mainCellIn.Data.gridSurface == null)
            {
                nonIslandUnitLis.Add(data);
                return;
            }

            Islands[data.mainCellIn.Data.gridSurface.IslandID]
                .AddInitUnitToIsland(data.unit, data.unit.UnitUnitData, data.mainCellIn);
        }

        #endregion
    }

    [Serializable]
    public struct RawLevelData
    {
        public Vector2Int s; // SIZE
        public GridSurfaceData[] sfD; // SURFACE DATA
        public GridUnitData[] uD; // UNIT DATA
        public ShadowUnitData[] suD; // SHADOW UNIT DATA
        public HintTrailData[] htD; // HINT TRAIL DATA

        [Serializable]
        public struct GridSurfaceData
        {
            public Vector2Int p; // POSITION
            public int t; // TYPE
            public int d; // DIRECTION
            public int m; // MATERIAL
        }

        [Serializable]
        public struct GridUnitData
        {
            public Vector2Int c; // CELL
            public int t; // TYPE 
            public int d; // DIRECTION
        }

        [Serializable]
        public struct ShadowUnitData
        {
            public Vector3 p; // POSITION
            public Vector3 rA; // ROTATION ANGLE
            public int t; // TYPE
        }

        [Serializable]
        public struct HintTrailData
        {
            public Vector2 p; // POSITION XZ
        }
    }
}
