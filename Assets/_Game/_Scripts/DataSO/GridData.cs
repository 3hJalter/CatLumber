using System;
using System.Collections.Generic;
using _Game.DesignPattern;
using _Game.GameGrid.GridSurface;
using _Game.GameGrid.Unit;
using _Game.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VinhLB;

namespace _Game.Data
{
    [CreateAssetMenu(fileName = "GridData", menuName = "ScriptableObjects/GridData", order = 1)]
    public class GridData : SerializedScriptableObject
    {
        [Title("Level Text Data")] 
        [SerializeField] private List<TextAsset> normalLevel = new();
        [SerializeField] private List<TextAsset> dailyChallengerLevel = new();
        [SerializeField] private List<TextAsset> secretLevel = new();
        
        [Title("Dynamic Unit")]
        [SerializeField]
        private readonly Dictionary<PoolType, GridUnitDynamic> _dynamicUnitDict = new();

        [Title("Static Unit")]
        [SerializeField]
        private readonly Dictionary<PoolType, GridUnitStatic> _staticUnitDict = new();
        
        [Title("Building Unit")]
        [SerializeField]
        private readonly Dictionary<PoolType, BuildingUnit> _buildingUnitDict = new();
        
        [Title("Surface")]
        [SerializeField]
        private readonly Dictionary<PoolType, GridSurface> _surfaceDict = new();

        [Title("Environment Object")]
        [SerializeField]
        private readonly Dictionary<PoolType, EnvironmentObject[]> _environmentObjectsDict = new();
        
        public int CountNormalLevel => normalLevel.Count;

        public TextAsset GetLevelData(LevelType type, int index)
        {
            return type switch
            {
                LevelType.Normal => normalLevel[index],
                LevelType.DailyChallenger => dailyChallengerLevel[index],
                LevelType.Secret => secretLevel[index],
                _ => null
            };
        }

        public GridSurface GetGridSurface(PoolType poolType)
        {
            return _surfaceDict.GetValueOrDefault(poolType);
        }

        public GridUnit GetGridUnit(PoolType poolType)
        {
            // Get from dictionary static unit first
            if (_staticUnitDict.TryGetValue(poolType, out GridUnitStatic staticUnit)) return staticUnit;
            // else get from dictionary dynamic unit
            if (_dynamicUnitDict.TryGetValue(poolType, out GridUnitDynamic dynamicUnit)) return dynamicUnit;
            // else get from dictionary building unit or return null (default)
            return _buildingUnitDict.GetValueOrDefault(poolType);
        }

        public EnvironmentObject GetRandomEnvironmentObject(PoolType poolType)
        {
            EnvironmentObject[] environmentObjects = _environmentObjectsDict.GetValueOrDefault(poolType);
            if (environmentObjects == null || environmentObjects.Length == 0)
            {
                return null;
            }
            int randomIndex = UnityEngine.Random.Range(0, environmentObjects.Length);
            
            return _environmentObjectsDict[poolType][randomIndex];
        }

        public void AddGridTextData(LevelType type, TextAsset textAsset)
        {
            switch (type)
            {
                case LevelType.Normal:
                    normalLevel.Add(textAsset);
                    break;
                case LevelType.DailyChallenger:
                    dailyChallengerLevel.Add(textAsset);
                    break;
                case LevelType.Secret:
                    secretLevel.Add(textAsset);
                    break;
                default:
                    DevLog.Log(DevId.Hoang, "AddGridTextData: LevelType not found");
                    break;
            }
        }

        public bool HasGridTextData(LevelType type, TextAsset textAsset)
        {
            return type switch
            {
                LevelType.Normal => normalLevel.Contains(textAsset),
                LevelType.DailyChallenger => dailyChallengerLevel.Contains(textAsset),
                LevelType.Secret => secretLevel.Contains(textAsset),
                _ => false
            };
        }

        public int GetGridTextDataIndex(LevelType type, TextAsset load)
        {
            // return -1 if not found
            switch (type)
            {
                case LevelType.Normal:
                    if (!normalLevel.Contains(load)) return -1;
                    return normalLevel.IndexOf(load);
                case LevelType.DailyChallenger:
                    if (!dailyChallengerLevel.Contains(load)) return -1;
                    return dailyChallengerLevel.IndexOf(load);
                case LevelType.Secret:
                    if (!secretLevel.Contains(load)) return -1;
                    return secretLevel.IndexOf(load);
                default:
                    DevLog.Log(DevId.Hoang, "GetGridTextDataIndex: LevelType not found");
                    return -1;
            }
        }
    }

    public enum LevelType
    {
        None = -1,
        Normal = 0,
        DailyChallenger = 1,
        Secret = 2,
    }
}
