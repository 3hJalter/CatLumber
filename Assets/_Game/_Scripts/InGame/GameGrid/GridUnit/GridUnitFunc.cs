using System.Collections.Generic;
using _Game.Utilities.Grid;
using GameGridEnum;
using UnityEngine;

namespace _Game.GameGrid.Unit
{
    public static class GridUnitFunc
    {
        #region Debug

        // Debug cell information
        public static void DebugCellInformation(GameGridCell cell)
        {
            // debug cell information
            // Cell position
            Debug.Log("Cell position: " + cell.X + ", " + cell.Y);
            // World position
            Debug.Log("World position: " + cell.WorldPos);
            // Name of unit in each height
            for (HeightLevel i = Constants.DirFirstHeightOfSurface[cell.SurfaceType]; i < cell.GetMaxHeight(); i++)
                Debug.Log("Unit name at height " + i + ": " + cell.GetGridUnitAtHeight(i)?.name);
        }

        #endregion


        public static Direction InvertDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                case Direction.Forward:
                    return Direction.Back;
                case Direction.Back:
                    return Direction.Forward;
                case Direction.None:
                default:
                    return Direction.None;
            }
        }

        public static Vector3Int RotateSize(Direction direction, Vector3Int sizeIn)
        {
            switch (direction)
            {
                case Direction.Left:
                case Direction.Right:
                    return new Vector3Int(sizeIn.y, sizeIn.x, sizeIn.z);
                case Direction.Forward:
                case Direction.Back:
                    return new Vector3Int(sizeIn.x, sizeIn.z, sizeIn.y);
                case Direction.None:
                default:
                    return Vector3Int.zero;
            }
        }

        public static GameGridCell GetNeighbourCell(Grid<GameGridCell, GameGridCellData> grid,
            GameGridCell cell, Direction direction, int distance = 1)
        {
            Vector2Int cellPos = cell.GetCellPosition();
            Vector2Int dir = Constants.DirVector[direction];
            Vector2Int neighbourPos = cellPos + dir * distance;
            Debug.Log(neighbourPos);

            return grid.GetGridCell(neighbourPos.x, neighbourPos.y);
        }

        #region Handle Get Neighbor Unit

        public static GameGridCell GetNeighborCell(this GameGridCell cell, Direction direction, int distance = 1,
            Grid<GameGridCell, GameGridCellData> map = null)
        {
            map ??= LevelManager.Ins.CurrentLevel.GridMap;
            Vector2Int cellPos = cell.GetCellPosition();
            Vector2Int dir = Constants.DirVector[direction];
            Vector2Int neighbourPos = cellPos + dir * distance;
            return map.GetGridCell(neighbourPos.x, neighbourPos.y);
        }

        // Get next cells from cellInUnits by direction
        public static List<GameGridCell> GetUnitNeighborCells(this GridUnit unit, Direction direction,
            Grid<GameGridCell, GameGridCellData> map = null)
        {
            map ??= LevelManager.Ins.CurrentLevel.GridMap;
            List<GameGridCell> nextCells = new();
            for (int i = 0; i < unit.cellInUnits.Count; i++)
            {
                GameGridCell nextCell = GetNeighborCell(unit.cellInUnits[i], direction, 1, map);
                if (nextCell == null || nextCells.Contains(nextCell) || unit.cellInUnits.Contains(nextCell)) continue;
                nextCells.Add(nextCell);
            }

            return nextCells;
        }

        // Get all next units from cellInUnits by direction
        public static List<GridUnit> GetAllNeighborUnits(this GridUnit unit, Direction direction,
            Grid<GameGridCell, GameGridCellData> map = null)
        {
            map ??= LevelManager.Ins.CurrentLevel.GridMap;
            List<GridUnit> nextUnits = new();
            List<GameGridCell> nextCells = GetUnitNeighborCells(unit, direction, map);
            for (int i = 0; i < nextCells.Count; i++)
            {
                // Get Unit from start height to end height using GameGridCell.GetGridUnits
                List<GridUnit> units = nextCells[i].GetGridUnits(unit.StartHeight, unit.EndHeight);
                if (units is null) continue;
                for (int j = 0; j < units.Count; j++)
                {
                    if (nextUnits.Contains(units[j])) continue;
                    nextUnits.Add(units[j]);
                }
            }

            return nextUnits;
        }

        // Get below units from MainCell
        public static GridUnit GetBelowUnitAtMainCell(this GridUnit unit)
        {
            return unit.MainCell.GetGridUnitAtHeight(unit.BelowStartHeight);
        }

        // Get below units from cellInUnits (only the first below height)
        public static List<GridUnit> GetBelowUnits(this GridUnit unit)
        {
            List<GridUnit> belowUnits = new();
            for (int i = 0; i < unit.cellInUnits.Count; i++)
            {
                GridUnit aboveUnit = unit.cellInUnits[i].GetGridUnitAtHeight(unit.BelowStartHeight);
                if (aboveUnit is null || belowUnits.Contains(aboveUnit)) continue;
                belowUnits.Add(aboveUnit);
            }

            return belowUnits;
        }

        // Get all below units from cellInUnits (all below height)
        public static List<GridUnit> GetAllBelowUnits(this GridUnit unit)
        {
            List<GridUnit> belowUnits = new();
            for (int i = 0; i < unit.cellInUnits.Count; i++)
            for (HeightLevel j = unit.StartHeight - 1; j >= Constants.MIN_HEIGHT; j--)
            {
                GridUnit belowUnit = unit.cellInUnits[i].GetGridUnitAtHeight(j);
                if (belowUnit is null || belowUnits.Contains(belowUnit)) continue;
                belowUnits.Add(belowUnit);
            }

            return belowUnits;
        }

        // Get above units from MainCell
        public static GridUnit GetAboveUnitAtMainCell(this GridUnit unit)
        {
            return unit.MainCell.GetGridUnitAtHeight(unit.UpperEndHeight);
        }

        // Get above units from cellInUnits (only the first above height)
        public static List<GridUnit> GetAboveUnits(this GridUnit unit)
        {
            List<GridUnit> aboveUnits = new();
            for (int i = 0; i < unit.cellInUnits.Count; i++)
            {
                GridUnit aboveUnit = unit.cellInUnits[i].GetGridUnitAtHeight(unit.UpperEndHeight);
                if (aboveUnit is null || aboveUnits.Contains(aboveUnit)) continue;
                aboveUnits.Add(aboveUnit);
            }

            return aboveUnits;
        }

        // Get all above units from cellInUnits (all above height)
        public static List<GridUnit> GetAllAboveUnits(this GridUnit unit)
        {
            List<GridUnit> aboveUnits = new();
            for (int i = 0; i < unit.cellInUnits.Count; i++)
            for (HeightLevel j = unit.EndHeight + 1; j <= Constants.MAX_HEIGHT; j++)
            {
                GridUnit aboveUnit = unit.cellInUnits[i].GetGridUnitAtHeight(j);
                if (aboveUnit is null || aboveUnits.Contains(aboveUnit)) continue;
                aboveUnits.Add(aboveUnit);
            }

            return aboveUnits;
        }

        #endregion
    }
}
