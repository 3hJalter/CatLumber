using System.Linq;
using _Game.Data;
using _Game.Managers;
using Unity.VisualScripting;
using UnityEngine;

namespace _Game.GameGrid
{
    public static class GameGridDataHandler
    {
        public static TextGridData CreateGridData(LevelType type, int mapIndex)
        {
            TextAsset textAsset = DataManager.Ins.GetNormalLevelData(type, mapIndex);
            string[] splitData = textAsset.text.Split('@');
            return new TextGridData(
                splitData[(int)GridDataType.SurfaceData],
                splitData[(int)GridDataType.SurfaceRotationDirectionData],
                splitData[(int)GridDataType.SurfaceMaterialData],
                splitData[(int)GridDataType.UnitData],
                splitData[(int)GridDataType.RotationDirectionUnitData],
                splitData[(int)GridDataType.ShadowUnitData],
                splitData[(int)GridDataType.TrailHintData]);
        }

        private enum GridDataType
        {
            SurfaceData = 0,
            SurfaceRotationDirectionData = 1,
            SurfaceMaterialData = 2,
            UnitData = 3,
            RotationDirectionUnitData = 4,
            ShadowUnitData = 5,
            TrailHintData = 6,
        }
    }

    public class TextGridData
    {
        public TextGridData(string surfaceData, string surfaceRotationDirectionData,
            string surfaceMaterialData, string unitData, string unitRotationDirectionData, string shadowUnitData = null,
            string trailHintData = null)
        {
            SurfaceData = surfaceData;
            SurfaceRotationDirectionData = surfaceRotationDirectionData;
            SurfaceMaterialData = surfaceMaterialData;
            UnitData = unitData;
            UnitRotationDirectionData = unitRotationDirectionData;
            ShadowUnitData = shadowUnitData ?? string.Empty;
            TrailHintData = trailHintData ?? string.Empty;
        }
        
        public string SurfaceData { get; }
        public string SurfaceRotationDirectionData { get; }
        public string SurfaceMaterialData { get; }
        public string UnitData { get; }
        public string UnitRotationDirectionData { get; }
        public string ShadowUnitData { get; }
        public string TrailHintData { get; }

        public Vector2Int GetSize(bool skipFirstLine = true)
        {
            string[] surfaceData = SurfaceData.Split('\n');
            if (skipFirstLine) surfaceData = surfaceData.Skip(1).ToArray();
            int x = surfaceData.Length;
            int y = surfaceData[0].Split(' ').Length;
            return new Vector2Int(x, y);
        }
    }
}
