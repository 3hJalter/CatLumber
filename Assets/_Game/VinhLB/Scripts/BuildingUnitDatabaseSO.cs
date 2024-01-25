using System;
using System.Collections.Generic;
using _Game.DesignPattern;
using GameGridEnum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VinhLB
{
    [CreateAssetMenu(fileName = "PlacedObjectDatabase", menuName = "ScriptableObjects/PlacedObjectDatabase")]
    public class BuildingUnitDatabaseSO : ScriptableObject
    {
        public List<BuildingUnitData> BuildingUnitDataList;
    }

    [Serializable]
    public class BuildingUnitData
    {
        public int Id;
        public string Name;
        public Sprite Sprite;
        public int Width;
        public int Height;
        public BuildingUnit Prefab;
        public Transform Visual;
        public PoolType PoolType;
        public GridSurfaceType BelowSurfaceType;
        public bool CheckAdjacentCells;
        [ShowIf(nameof(CheckAdjacentCells))]
        public GridSurfaceType AdjacentSurfaceType;
        [ShowIf(nameof(CheckAdjacentCells))]
        public int MinAdjacentCells;

        public static Direction GetNextDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    return Direction.Forward;
                case Direction.Forward:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Back;
                case Direction.Back:
                default:
                    return Direction.Left;
            }
        }

        public static Direction GetDirection(float rotationAngle)
        {
            rotationAngle = Mathf.Clamp(rotationAngle, 0, 360);
            return rotationAngle switch
            {
                >= 315 or < 45 => Direction.Forward,
                >= 45 and < 135 => Direction.Right,
                >= 135 and < 225 => Direction.Back,
                _ => Direction.Left
            };
        }
        
        public static float GetRotationAngle(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    return 270f;
                case Direction.Right:
                    return 90f;
                case Direction.Forward:
                case Direction.None:
                    return 0;
                case Direction.Back:
                default:
                    return 180f;
            }
        }

        public static Vector2Int GetRotationOffset(int width, int height, Direction direction)
        {
            width = Mathf.Clamp(width - 1, 0, width - 1);
            height = Mathf.Clamp(height - 1, 0, height - 1);

            switch (direction)
            {
                case Direction.Left:
                    return new Vector2Int(0, width);
                case Direction.Right:
                    return new Vector2Int(height, 0);
                case Direction.Forward:
                    return new Vector2Int(width, height);
                case Direction.Back:
                default:
                    return Vector2Int.zero;
            }
        }

        public static List<Vector2Int> GetGridPositionList(int width, int height, Vector2Int offset,
            Direction direction)
        {
            List<Vector2Int> gridPositionList = new();
            switch (direction)
            {
                default:
                case Direction.Forward:
                case Direction.Back:
                    for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                        gridPositionList.Add(offset + new Vector2Int(i, j));
                    break;
                case Direction.Left:
                case Direction.Right:
                    for (int i = 0; i < height; i++)
                    for (int j = 0; j < width; j++)
                        gridPositionList.Add(offset + new Vector2Int(i, j));
                    break;
            }

            return gridPositionList;
        }
    }
}
