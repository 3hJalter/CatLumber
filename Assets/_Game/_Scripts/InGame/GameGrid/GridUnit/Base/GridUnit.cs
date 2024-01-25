using _Game.DesignPattern;
using _Game.DesignPattern.ConditionRule;
using _Game.DesignPattern.StateMachine;
using _Game.Utilities.Grid;
using DG.Tweening;
using GameGridEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using _Game._Scripts.Managers;
using _Game.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using VinhLB;
using _Game.Managers;

namespace _Game.GameGrid.Unit
{
    public abstract class GridUnit : GameUnit, IOriginator
    {
        [Title("Skin")]
        // Model of Unit
        [SerializeField] public Transform skin; // Model location
        // MeshRenderer of Unit
        [SerializeField] private MeshRenderer meshRenderer;

        [Title("Size")]
        // Size of this unit, the X and Z equal to the size of the main cell, the Y equal to height level
        [SerializeField] protected Vector3Int size;

        // Use when the init size Y need to be x.5 when Up (ex: 1.5, 2.5, 3.5)
        [SerializeField] protected bool isMinusHalfSizeY;

        [Title("Height & Rotation Type")]
        // Height level of this unit
        [SerializeField] protected HeightLevel startHeight = HeightLevel.One; // Serialize for test

        [SerializeField] protected HeightLevel endHeight; // Serialize for test
        
        [Tooltip("Set this to true if you want to override the start height of the unit when it enter a cell by a constant start height value at first init")]
        public bool overrideStartHeight; // Basically for button Unit, but static Unit is okay too
        
        // How many height level can floating up when unit is on floating Surface (bool canFloating at GameGridCell)
        [SerializeField] private int floatingHeightOffset;

        public int FloatingHeightOffset => floatingHeightOffset;
        
        // The current state of this unit (Up or Down)
        [FormerlySerializedAs("unitState")]
        [SerializeField]
        protected UnitTypeY unitTypeY = UnitTypeY.Up; // Serialize for test
        
        // Offset when unit is down
        [SerializeField] public float yOffsetOnDown = 0.5f;

        // The type of this unit (Horizontal, Vertical, Both or None)
        [FormerlySerializedAs("unitType")]
        [SerializeField]
        protected UnitTypeXZ unitTypeXZ = UnitTypeXZ.Both; // Serialize for test       

        [SerializeField] protected Direction skinRotationDirection = Direction.None;

        
        [Title("In Game Data")]
        // The island that this unit is on
        public int islandID = -1;
        
        // Position data when this unit enter a cell
        public readonly EnterCellPosData EnterPosData = new();

        public readonly List<GameGridCell> cellInUnits = new();
        
        // All neighbor units of this unit
        public readonly HashSet<GridUnit> neighborUnits = new();
        public readonly HashSet<GridUnit> upperUnits = new();
        public readonly HashSet<GridUnit> belowUnits = new();

        // Save init data on first Initialize
        private UnitInitData _unitInitData;

        // The last direction that this unit is pushed
        protected Direction lastPushedDirection = Direction.None;

        public Direction LastPushedDirection => lastPushedDirection;

        // The main cell that this unit is on
        protected GameGridCell mainCell;
        #region Saving Spawn State
        // Is GridUnit is spawn or not - save state of unit.
        protected bool isSpawn;
        protected IMemento overrideSpawnSave;
        protected IMemento overrideDespawnSave;

        public bool IsSpawn => isSpawn;
        #endregion

        public Vector3Int Size
        {
            get => size;
            set => size = value;
        }

        public UnitTypeXZ UnitTypeXZ
        {
            get => unitTypeXZ;
            set => unitTypeXZ = value;
        }

        public UnitTypeY UnitTypeY
        {
            get => unitTypeY;
            set => unitTypeY = value;
        }

        public virtual GameGridCell MainCell
        {
            get => mainCell;
            protected set
            {
                if (mainCell != null && mainCell.Data.IsBlockDanger)
                {
                    GameManager.Ins.PostEvent(EventID.ObjectInOutDangerCell, mainCell);
                }
                mainCell = value;
                if (mainCell != null && mainCell.Data.IsDanger)
                    GameManager.Ins.PostEvent(EventID.ObjectInOutDangerCell, mainCell);
            }
        }

        public HeightLevel StartHeight
        {
            get => startHeight;
            set => startHeight = value;
        }

        public Direction SkinRotationDirection => skinRotationDirection;

        public HeightLevel EndHeight
        {
            get => endHeight;
            set => endHeight = value;
        }

        public HeightLevel BelowStartHeight => startHeight - Constants.BELOW_HEIGHT;
        public HeightLevel UpperEndHeight => endHeight + Constants.UPPER_HEIGHT;

        public UnitInitData UnitUnitData => _unitInitData; //DEV: Can be optimize
        public virtual void OnInit(GameGridCell mainCellIn, HeightLevel startHeightIn = HeightLevel.One,
            bool isUseInitData = true, Direction skinDirection = Direction.None, bool hasSetPosAndRos = false)
        {
            //Saving state before spawn, when map has already init
            if (!LevelManager.Ins.IsConstructingLevel)
                overrideSpawnSave = Save();
            else
                overrideSpawnSave = null;
            SaveInitData(size, unitTypeY, skin);
            if (isUseInitData) GetInitData();
            islandID = mainCellIn.IslandID;
            SetHeight(startHeightIn);
            SetEnterCellData(Direction.None, mainCellIn, unitTypeY);
            OnEnterCells(mainCellIn, InitCell(mainCellIn, skinDirection));
            // Set position
            if (!hasSetPosAndRos) OnSetPositionAndRotation(EnterPosData.finalPos, skinDirection);
            
            isSpawn = true;
        }

        public void OnSetPositionAndRotation(Vector3 position, Direction skinDirection)
        {
            Tf.position = position;

            skinRotationDirection = skinDirection;
            skin.rotation = Quaternion.Euler(0f, BuildingUnitData.GetRotationAngle(skinRotationDirection), 0f);

            Vector2Int rotationOffset = BuildingUnitData.GetRotationOffset(size.x, size.z, skinRotationDirection);
            Vector3 posOffset = new Vector3(rotationOffset.x, 0, rotationOffset.y) * Constants.CELL_SIZE;
            skin.position += posOffset;
        }

        public virtual bool IsCurrentStateIs(StateEnum stateEnum)
        {
            return false;
        }

        public HeightLevel CalculateEndHeight(HeightLevel startHeightIn, Vector3Int sizeIn)
        {
            HeightLevel endHeightOut = startHeightIn + (sizeIn.y - 1) * 2;
            if (!isMinusHalfSizeY && unitTypeY == UnitTypeY.Up) endHeightOut += 1;
            return endHeightOut;
        }

        public virtual void OnDespawn()
        {
            Tf.DOKill(true);
            //Saving state before despawn
            if (!LevelManager.Ins.IsConstructingLevel)
                overrideDespawnSave = Save();
            else
                overrideDespawnSave = null;
            OnOutCells();
            this.Despawn();
            lastPushedDirection = Direction.None;
            isSpawn = false;
        }

        public virtual void OnPush(Direction direction, ConditionData conditionData = null)
        {

        }

        public virtual void OnBePushed(Direction direction = Direction.None, GridUnit pushUnit = null)
        {
            lastPushedDirection = direction;
        }

        public void OnOutCells()
        {
            RemoveUnitFromCell();
            OnOutTrigger();
            ClearNeighbor();
            cellInUnits.Clear();
            MainCell = null;
        }

        public Vector3 GetUnitWorldPos(GameGridCell cell = null)
        {
            cell ??= mainCell;
            float offsetY = (float)startHeight / 2 * Constants.CELL_SIZE;
            if (unitTypeY == UnitTypeY.Down) offsetY -= yOffsetOnDown;
            return cell.WorldPos + Vector3.up * offsetY;
        }

        public void SetEnterCellData(Direction direction, GameGridCell enterMainCell, UnitTypeY nextUnitTypeY,
            bool hasInitialOffset = true, List<GameGridCell> enterNextCells = null)
        {
            Vector3 initialPos = GetUnitWorldPos(enterMainCell);
            HeightLevel enterStartHeight = enterNextCells is null
                ? GetEnterStartHeight(enterMainCell)
                : GetEnterStartHeight(enterNextCells);
            Vector3 finalPos = PredictUnitPos();
            EnterPosData.SetEnterPosData(direction, enterStartHeight, initialPos, finalPos, yOffsetOnDown,
                hasInitialOffset);
            return;

            Vector3 PredictUnitPos()
            {
                float offsetY = (float)enterStartHeight / 2 * Constants.CELL_SIZE;
                if (nextUnitTypeY == UnitTypeY.Down) offsetY -= yOffsetOnDown;
                return enterMainCell.WorldPos + Vector3.up * offsetY;
            }
        }

        public void OnEnterCells(GameGridCell enterMainCell, List<GameGridCell> enterNextCells = null)
        {
            SetHeight(EnterPosData.startHeight);
            InitCellsToUnit(enterMainCell, enterNextCells);
            SetNeighbor(LevelManager.Ins.CurrentLevel.GridMap);
            TutorialManager.Ins.OnUnitGoToCell(enterMainCell, this);
            return;

            void InitCellsToUnit(GameGridCell enterMainCellIn, List<GameGridCell> enterCells = null)
            {
                // Add all nextCells to cellInUnits
                if (enterCells is not null)
                    for (int i = 0; i < enterCells.Count; i++)
                        AddCell(enterCells[i]);
                else AddCell(enterMainCellIn);
                MainCell = enterMainCellIn;
            }
        }

        #region Hint, MeshRenderer and Shadow

        public void ChangeMaterial(Material material)
        {
            meshRenderer.material = material;
        }
        
        public float GetAlphaTransparency()
        {
            return meshRenderer.sharedMaterial.GetColor(Constants.BASE_COLOR).a;
        }
        
        public void SetAlphaTransparency(float alpha)
        {
            Color color = meshRenderer.sharedMaterial.GetColor(Constants.BASE_COLOR);
            meshRenderer.sharedMaterial.SetColor(Constants.BASE_COLOR, new Color(color.r, color.g, color.b, alpha));
        }
        
        public void ChangeReceiveShadow(bool isReceive)
        {
            meshRenderer.shadowCastingMode = isReceive ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
        
        #endregion
        
        private void ClearNeighbor()
        {
            foreach (GridUnit unit in neighborUnits) unit.neighborUnits.Remove(this);
            neighborUnits.Clear();
            foreach (GridUnit unit in upperUnits) unit.belowUnits.Remove(this);
            upperUnits.Clear();
            foreach (GridUnit unit in belowUnits) unit.upperUnits.Remove(this);
            belowUnits.Clear();
        }

        private void OnOutTrigger()
        {
            foreach (GridUnit unit in neighborUnits) unit.OnOutTriggerNeighbor(this);
            foreach (GridUnit unit in belowUnits) unit.OnOutTriggerUpper(this);
            foreach (GridUnit unit in upperUnits) unit.OnOutTriggerBelow(this);
        }

        protected virtual void OnOutTriggerNeighbor(GridUnit triggerUnit)
        {
            neighborUnits.Remove(triggerUnit);
        }

        protected virtual void OnOutTriggerBelow(GridUnit triggerUnit)
        {
            belowUnits.Remove(triggerUnit);
            // TODO: Falling Logic 
        }

        protected virtual void OnOutTriggerUpper(GridUnit triggerUnit)
        {
            upperUnits.Remove(triggerUnit);
        }

        private void SetNeighbor(Grid<GameGridCell, GameGridCellData> map)
        {
            // In direction
            neighborUnits.UnionWith(this.GetAllNeighborUnits(Direction.Left, map));
            neighborUnits.UnionWith(this.GetAllNeighborUnits(Direction.Right, map));
            neighborUnits.UnionWith(this.GetAllNeighborUnits(Direction.Forward, map));
            neighborUnits.UnionWith(this.GetAllNeighborUnits(Direction.Back, map));
            foreach (GridUnit unit in neighborUnits) unit.neighborUnits.Add(this);
            // Upper and Below
            upperUnits.UnionWith(this.GetAboveUnits());
            foreach (GridUnit unit in upperUnits) unit.belowUnits.Add(this);
            belowUnits.UnionWith(this.GetBelowUnits());
            foreach (GridUnit unit in belowUnits) unit.upperUnits.Add(this);
        }

        public void OnEnterTrigger(GridUnit gridUnit)
        {
            foreach (GridUnit unit in neighborUnits.ToList()) unit.OnEnterTriggerNeighbor(gridUnit);
            foreach (GridUnit unit in belowUnits.ToList()) unit.OnEnterTriggerUpper(gridUnit);
            foreach (GridUnit unit in upperUnits.ToList()) unit.OnEnterTriggerBelow(gridUnit);
        }

        protected virtual void OnEnterTriggerNeighbor(GridUnit triggerUnit)
        {

        }

        protected virtual void OnEnterTriggerBelow(GridUnit triggerUnit)
        {

        }

        protected virtual void OnEnterTriggerUpper(GridUnit triggerUnit)
        {

        }
        public void UpHeight(GridUnit pushUnit, Direction direction = Direction.None)
        {
            // if (pushUnit.MainCell.GetGridUnitAtHeight(endHeight + 1) is not null) return;
            // Jump to treeRootUnit, Add one height 
            pushUnit.StartHeight += 1;
            pushUnit.EndHeight += 1;
        }
        private void SetHeight(HeightLevel startHeightIn)
        {
            startHeight = startHeightIn;
            endHeight = CalculateEndHeight(startHeightIn, size);
        }

        private void SaveInitData(Vector3Int sizeI, UnitTypeY unitTypeYi, Transform skinI)
        {    
            if(_unitInitData == null)
                _unitInitData = new UnitInitData(this);
        }

        private void GetInitData()
        {
            size = _unitInitData.Size;
            unitTypeY = _unitInitData.UnitTypeY;
            skin.localPosition = _unitInitData.LocalSkinPos;
            skin.localRotation = _unitInitData.LocalSkinRot;
        }

        private List<GameGridCell> InitCell(GameGridCell mainCellIn, Direction skinDirection)
        {
            List<GameGridCell> initCells = new();
            MainCell = mainCellIn;
            initCells.Add(mainCell);
            switch (skinDirection)
            {
                default:
                case Direction.Back:
                case Direction.Forward:
                    InitXCell(mainCell, size.x);
                    for (int i = initCells.Count - 1; i >= 0; i--) InitZCell(initCells[i], size.z);
                    break;
                case Direction.Left:
                case Direction.Right:
                    InitZCell(mainCell, size.x);
                    for (int i = initCells.Count - 1; i >= 0; i--) InitXCell(initCells[i], size.z);
                    break;
            }

            // Add this unit to cells
            // for (int i = 0; i < initCells.Count; i++) initCells[i].AddGridUnit(startHeight, endHeight, this);
            return initCells;

            void InitXCell(GameGridCell cell, int sizeX)
            {
                for (int i = 1; i < sizeX; i++)
                {
                    GameGridCell neighbourX = cell.GetNeighborCell(Direction.Right, i);
                    if (neighbourX == null || initCells.Contains(neighbourX)) continue;
                    initCells.Add(neighbourX);
                }
            }

            void InitZCell(GameGridCell cell, int sizeZ)
            {
                for (int i = 1; i < sizeZ; i++)
                {
                    GameGridCell neighbourZ = cell.GetNeighborCell(Direction.Forward, i);
                    if (neighbourZ == null || initCells.Contains(neighbourZ)) continue;
                    initCells.Add(neighbourZ);
                }
            }
        }
        
        private HeightLevel GetEnterStartHeight(GameGridCell enterCell)
        {
            if (overrideStartHeight) return startHeight;
            HeightLevel enterStartHeight = Constants.MIN_HEIGHT;
            HeightLevel initHeight = Constants.DirFirstHeightOfSurface[enterCell.SurfaceType];
            if (enterCell.Data.canFloating) 
                initHeight = Constants.DirFirstHeightOfSurface[enterCell.SurfaceType] + floatingHeightOffset; // Zero point five
            if (initHeight > enterStartHeight) enterStartHeight = initHeight;
            for (HeightLevel heightLevel = initHeight;
                 heightLevel <= BelowStartHeight;
                 heightLevel++)
            {
                if (enterCell.GetGridUnitAtHeight(heightLevel) is null) continue;
                if (heightLevel + 1 > enterStartHeight) enterStartHeight = heightLevel + 1;
            }

            return enterStartHeight;
        }

        private HeightLevel GetEnterStartHeight(List<GameGridCell> enterCells)
        {
            if (overrideStartHeight) return startHeight;
            HeightLevel enterStartHeight = Constants.MIN_HEIGHT;
            foreach (GameGridCell cell in enterCells)
            {
                HeightLevel initHeight = Constants.DirFirstHeightOfSurface[cell.SurfaceType];
                if (cell.Data.canFloating) 
                    initHeight = Constants.DirFirstHeightOfSurface[cell.SurfaceType] + floatingHeightOffset; // Zero point five
                if (initHeight > enterStartHeight) enterStartHeight = initHeight;
                for (HeightLevel heightLevel = initHeight;
                     heightLevel <= BelowStartHeight;
                     heightLevel++)
                {
                    if (cell.GetGridUnitAtHeight(heightLevel) is null) continue;
                    if (heightLevel + 1 > enterStartHeight) enterStartHeight = heightLevel + 1;
                }
            }

            return enterStartHeight;
        }


        public void RemoveUnitFromCell()
        {
            for (int i = cellInUnits.Count - 1; i >= 0; i--)
                cellInUnits[i].RemoveGridUnit(this);
        }
        public void ResetData()
        {
            overrideSpawnSave = null;
            overrideDespawnSave = null;
            lastPushedDirection = Direction.None;
        }
        private void AddCell(GameGridCell cell)
        {
            cellInUnits.Add(cell);
            cell.AddGridUnit(this);
        }

        #region SAVING DATA
        public virtual IMemento Save()
        {
            IMemento save;
            if(!isSpawn && overrideSpawnSave != null)
            {
                save = overrideSpawnSave;
                overrideSpawnSave = null;
            }
            else if(isSpawn && overrideDespawnSave != null)
            {
                save= overrideDespawnSave;
                overrideDespawnSave = null;
            }
            else
            {
                save = new UnitMemento<GridUnit>(this, isSpawn, Tf.position, skin.rotation, startHeight, endHeight
                , unitTypeY, unitTypeXZ, belowUnits, neighborUnits, upperUnits, mainCell, cellInUnits, islandID, lastPushedDirection);
            }
            return save;
        }
        public class UnitMemento<T> : IMemento where T : GridUnit
        {
            protected T main;
            #region MAIN DATA
            bool isSpawn;
            protected Vector3 position;
            protected Quaternion rotation;
            protected HeightLevel startHeight;
            protected HeightLevel endHeight;
            protected UnitTypeY unitTypeY;
            protected UnitTypeXZ unitTypeXZ;
            protected GridUnit[] belowsUnits;
            protected GridUnit[] neighborUnits;
            protected GridUnit[] upperUnits;
            protected GameGridCell mainCell;
            protected GameGridCell[] cellInUnits;
            int islandID;
            protected Direction lastPushDirection;

            public int Id => main.GetHashCode();
            #endregion
            public UnitMemento(T main, params object[] data)
            {
                this.main = main;
                isSpawn = (bool)data[0];
                position = (Vector3)data[1];
                rotation = (Quaternion)data[2];
                startHeight = (HeightLevel)data[3];
                endHeight = (HeightLevel)data[4];
                unitTypeY = (UnitTypeY)data[5];
                unitTypeXZ = (UnitTypeXZ)data[6];
                if (data[7] != null) belowsUnits = ((HashSet<GridUnit>)data[7]).ToArray();
                else belowsUnits = null;

                if (data[8] != null) neighborUnits = ((HashSet<GridUnit>)data[8]).ToArray();
                else neighborUnits = null;

                if (data[9] != null) upperUnits = ((HashSet<GridUnit>)data[9]).ToArray();
                else upperUnits = null;

                mainCell = (GameGridCell)data[10];
                if (data[11] != null) cellInUnits = ((List<GameGridCell>)data[11]).ToArray();
                else cellInUnits = null;
                islandID = (int)data[12];
                lastPushDirection = (Direction)data[13];
            }
            public virtual void Restore()
            {
                #region SPAWN
                if (!isSpawn && main.isSpawn)
                {
                    main.Despawn();
                    main.isSpawn = false;
                    main.belowUnits.Clear();
                    main.upperUnits.Clear();
                    main.neighborUnits.Clear();
                    main.cellInUnits.Clear();
                    main.mainCell = null;
                    return;
                }
                
                if(isSpawn && !main.isSpawn)
                {
                    SimplePool.SpawnDirectFromPool(main, position, Quaternion.identity);
                    main.isSpawn = true;
                }
                #endregion
                #region MAIN DATA
                main.Tf.position = position;
                main.skin.rotation = rotation;
                main.startHeight = startHeight;
                main.endHeight = endHeight;
                main.unitTypeY = unitTypeY;
                main.unitTypeXZ = unitTypeXZ;


                main.belowUnits.Clear();
                foreach (GridUnit unit in belowsUnits)
                {
                    main.belowUnits.Add(unit);
                }



                main.neighborUnits.Clear();
                foreach (GridUnit unit in neighborUnits)
                {
                    main.neighborUnits.Add(unit);
                }



                main.upperUnits.Clear();
                foreach (GridUnit unit in upperUnits)
                {
                    main.upperUnits.Add(unit);
                }


                main.MainCell = mainCell;
                main.cellInUnits.Clear();
                foreach (GameGridCell cell in cellInUnits)
                {
                    main.cellInUnits.Add(cell);
                }
                main.lastPushedDirection = lastPushDirection;
                main.islandID = islandID;
                #endregion
            }
        }
        #endregion

        protected bool IsOnWater()
        {
            return startHeight == Constants.DirFirstHeightOfSurface[GridSurfaceType.Ground] &&
                   cellInUnits.All(t => t.SurfaceType is GridSurfaceType.Water);
        }
        
        public bool IsInWater()
        {
            return startHeight <= Constants.DirFirstHeightOfSurface[GridSurfaceType.Water] + FloatingHeightOffset &&
                   cellInUnits.All(t => t.SurfaceType is GridSurfaceType.Water);
        }
    }

    public enum UnitTypeY
    {
        Up = 0,
        Down = 1
    }

    public enum UnitTypeXZ
    {
        None = -1,
        Horizontal = 0,
        Vertical = 1,
        Both = 2
    }

    public class UnitInitData
    {
        public UnitInitData(GridUnit main)
        {
            Size = main.Size;
            UnitTypeY = main.UnitTypeY;
            LocalSkinPos = main.skin.transform.localPosition;
            LocalSkinRot = main.skin.transform.localRotation;
            Type = main.PoolType;
            StartHeight = main.StartHeight;
            SkinDirection = main.SkinRotationDirection;
        }
        public PoolType Type { get; }
        public Vector3Int Size { get; }
        public UnitTypeY UnitTypeY { get; }
        public Vector3 LocalSkinPos { get; }
        public Quaternion LocalSkinRot { get; }
        public HeightLevel StartHeight { get; }
        public Direction SkinDirection { get; }
    }

    public class EnterCellPosData
    {
        public Vector3 finalPos; // consider falling
        public Vector3 initialPos; // not consider falling
        public bool isFalling;
        public HeightLevel startHeight;

        public void SetEnterPosData(Direction direction, HeightLevel startHeightIn, Vector3 initialPosIn,
            Vector3 finalPosIn, float yOffsetDown, bool hasInitialOffset = true)
        {
            startHeight = startHeightIn;
            initialPos = initialPosIn;
            finalPos = finalPosIn;
            isFalling = Math.Abs(finalPosIn.y - initialPosIn.y) > yOffsetDown + 0.01;
            if (!isFalling) initialPos = finalPos;
            if (!isFalling || !hasInitialOffset) return; // make falling before go to the center of next cell
            Vector3Int dirVector3 = Constants.DirVector3[direction];
            initialPos -= new Vector3(dirVector3.x, 0, dirVector3.z) * Constants.CELL_SIZE / 2;
        }
    }
}
