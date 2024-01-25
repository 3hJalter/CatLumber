using _Game._Scripts.Managers;
using _Game.Data;
using _Game.DesignPattern;
using _Game.GameGrid.GridSurface;
using _Game.GameGrid.Unit;
using UnityEngine;
using VinhLB;

namespace _Game.Managers
{
    public class DataManager : Singleton<DataManager>
    {

        #region Game Saving Data
        private GameData _gameData;
        
        public GameData GameData => _gameData ?? Load();

        private GameData Load()
        {
            _gameData = Database.LoadData();
            return _gameData;
        }      
        public void Save()
        {
            Database.SaveData(_gameData);
        }
        #endregion

        #region In-Game Data
        [SerializeField]
        private AudioData audioData;
        [SerializeField]
        private GridData gridData;
        [SerializeField]
        private MaterialData materialData;
        [SerializeField]
        private VFXData vfxData;
        
        public AudioData AudioData => audioData;
        public VFXData VFXData => vfxData;
        
        #endregion

        #region Income Data function
        
        public void AddGold(int gold)
        {
            _gameData.user.gold += gold;
            EventGlobalManager.Ins.OnMoneyGoldChanged?.Dispatch(_gameData.user.gold);
            Save();
        }
        
        public void AddGem(int gem)
        {
            _gameData.user.gems += gem;
            EventGlobalManager.Ins.OnMoneyGemChanged?.Dispatch(_gameData.user.gems);
            Save();
        }
        
        public void AddTicket(int ticket)
        {
            _gameData.user.ticket += ticket;
            EventGlobalManager.Ins.OnTicketChanged?.Dispatch(_gameData.user.ticket);
            Save();
        }
        
        public void SpendGold(int gold)
        {
            _gameData.user.gold -= gold;
            EventGlobalManager.Ins.OnMoneyGoldChanged?.Dispatch(_gameData.user.gold);
            Save();
        }
        
        public void SpendGem(int gem)
        {
            _gameData.user.gems -= gem;
            EventGlobalManager.Ins.OnMoneyGemChanged?.Dispatch(_gameData.user.gems);
            Save();
        }
        
        public void SpendTicket(int ticket)
        {
            _gameData.user.ticket -= ticket;
            EventGlobalManager.Ins.OnTicketChanged?.Dispatch(_gameData.user.ticket);
            Save();
        }

        #endregion
        
        #region In-Game Function

        public int CountNormalLevel => gridData.CountNormalLevel;
        public int CountSurfaceMaterial => materialData.CountSurfaceMaterial;
        
        public Material GetTransparentMaterial()
        {
            return materialData.GetTransparentMaterial();
        }
        
        public Material GetSurfaceMaterial(MaterialEnum materialEnum)
        {
            return materialData.GetSurfaceMaterial(materialEnum);
        }
        
        public Material GetGrassMaterial(MaterialEnum materialEnum)
        {
            return materialData.GetGrassMaterial(materialEnum);
        }
        
        public TextAsset GetNormalLevelData(LevelType type, int index)
        {
            return gridData.GetLevelData(type, index);
        }
        
        public void AddGridTextData(LevelType type, TextAsset textAsset)
        {
            gridData.AddGridTextData(type, textAsset);
        }
        
        public bool HasGridTextData(LevelType type, TextAsset textAsset)
        {
            return gridData.HasGridTextData(type, textAsset);
        }

        public GridSurface GetGridSurface(PoolType poolType)
        {
            return gridData.GetGridSurface(poolType);
        }

        public GridUnit GetGridUnit(PoolType poolType)
        {
            return gridData.GetGridUnit(poolType);
        }

        public int GetGridTextDataIndex(LevelType type, TextAsset load)
        {
            return gridData.GetGridTextDataIndex(type, load);
        }

        public EnvironmentObject GetRandomEnvironmentObject(PoolType poolType)
        {
            return gridData.GetRandomEnvironmentObject(poolType);
        }

        #endregion


    }
}
