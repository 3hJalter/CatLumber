using _Game.GameGrid;
using _Game.Managers;
using _Game.UIs.Screen;
using UnityEngine;

namespace _Game._Scripts.InGame
{
    public class WorldMapTarget : HMonoBehaviour
    {
        // Debug
        [SerializeField] private int nearestLevelIndex;
        [SerializeField] private int middleLevelIndex;
        [SerializeField] private int furthestLevelIndex;
        // Debug
        [SerializeField] private float offset = 8f;
        private Level _footerLevel;
        private Level _headerLevel;
        private Level _middleLevel;

        private WorldLevelScreen _worldLevelScreen;

        public WorldLevelScreen WorldLevelScreen
        {
            set => _worldLevelScreen = value;
        }
        
        public int MiddleLevelIndex => _middleLevel?.Index ?? -1;
        public int NearestLevelIndex => _footerLevel?.Index ?? -1;
        public int FurthestLevelIndex => _headerLevel?.Index ?? -1;

        private void Start()
        {
            OnActive();
        }

        private void Update()
        {
            // Handle Loading
            if (Tf.position.z > _middleLevel.GetMaxZPos() - offset) // offset
                PreloadNextLevel();
            else if (_footerLevel is not null && Tf.position.z < _footerLevel.GetMaxZPos() - offset)
                PreLoadPreviousLevel();
            
            #region Handle Show WorldLevelScreen level btn
            if (_worldLevelScreen is null) return;
            if (_middleLevel is not null)
            {
                // Get Center pos of middle level from WorldMapTarget
                Vector3 centerPos = _middleLevel.GetCenterPos();
                // Convert to screen pos
                Vector3 screenPos = CameraManager.Ins.BrainCamera.WorldToScreenPoint(centerPos);
                // Set btnMiddleLevel to screen pos
                _worldLevelScreen.BtnMiddleLevel.position = screenPos;
            } else _worldLevelScreen.BtnMiddleLevel.position = new Vector3(-1000f, -1000f, 0f);
            if (_headerLevel is not null)
            {
                // Get Center pos of header level from WorldMapTarget
                Vector3 centerPos = _headerLevel.GetCenterPos();
                // Convert to screen pos
                Vector3 screenPos = CameraManager.Ins.BrainCamera.WorldToScreenPoint(centerPos);
                // Set btnNextLevel to screen pos
                _worldLevelScreen.BtnFurthestLevel.position = screenPos;
            } else _worldLevelScreen.BtnFurthestLevel.position = new Vector3(-1000f, -1000f, 0f);
            if (_footerLevel is not null)
            {
                // Get Center pos of footer level from WorldMapTarget
                Vector3 centerPos = _footerLevel.GetCenterPos();
                // Convert to screen pos
                Vector3 screenPos = CameraManager.Ins.BrainCamera.WorldToScreenPoint(centerPos);
                // Set btnPreviousLevel to screen pos
                _worldLevelScreen.BtnNearestLevel.position = screenPos;
            } else _worldLevelScreen.BtnNearestLevel.position = new Vector3(-1000f, -1000f, 0f);
            #endregion 
        }

        private void OnEnable()
        {
            OnActive();
        }

        private void OnDisable()
        {
            if (CanDespawn(_headerLevel)) _headerLevel.OnDeSpawnLevel();
            if (CanDespawn(_footerLevel)) _footerLevel.OnDeSpawnLevel();
            if (CanDespawn(_middleLevel)) _middleLevel.OnDeSpawnLevel();
            ResetData();
        }

        private bool CanDespawn(Level level)
        {
            if (level is null) return false;
            int currentLevelIndex = LevelManager.Ins.CurrentLevel.Index;
            if (level.Index == currentLevelIndex) return false;
            if (level.Index == currentLevelIndex + 1) return false;
            if (level.Index == currentLevelIndex - 1) return false;
            return true;
        }

        private void PreloadNextLevel()
        {
            // int currentLevelIndex = LevelManager.Ins.CurrentLevel.Index;
            // if (_headerLevel is null) return;
            // int nextPreloadIndex = _headerLevel.Index + 1;
            // if (nextPreloadIndex >= DataManager.Ins.CountLevel) return;
            // if (CanDespawn(_footerLevel)) _footerLevel.OnDeSpawnLevel();
            // _footerLevel = _middleLevel;
            // _middleLevel = _headerLevel;
            //
            // if (LevelManager.Ins.PreLoadLevels.ContainsKey(nextPreloadIndex))
            //     _headerLevel = LevelManager.Ins.PreLoadLevels[nextPreloadIndex];
            // else if (nextPreloadIndex == currentLevelIndex)
            //     _headerLevel = LevelManager.Ins.CurrentLevel;
            // else
            //     _headerLevel = new Level(nextPreloadIndex);
            // nearestLevelIndex = _footerLevel.Index;
            // middleLevelIndex = _middleLevel.Index;
            // furthestLevelIndex = _headerLevel.Index;
        }

        private void PreLoadPreviousLevel()
        {
            // int currentLevelIndex = LevelManager.Ins.CurrentLevel.Index;
            // int previousPreloadIndex = _footerLevel.Index - 1;
            // if (previousPreloadIndex < 0) return;
            // if (CanDespawn(_headerLevel)) _headerLevel.OnDeSpawnLevel();
            // _headerLevel = _middleLevel;
            // _middleLevel = _footerLevel;
            // if (LevelManager.Ins.PreLoadLevels.ContainsKey(previousPreloadIndex))
            //     _footerLevel = LevelManager.Ins.PreLoadLevels[previousPreloadIndex];
            // else if (previousPreloadIndex == currentLevelIndex)
            //     _footerLevel = LevelManager.Ins.CurrentLevel;
            // else
            //     _footerLevel = new Level(previousPreloadIndex);
            // nearestLevelIndex = _footerLevel.Index;
            // middleLevelIndex = _middleLevel.Index;
            // furthestLevelIndex = _headerLevel.Index;
        }

        private void ResetData()
        {
            _middleLevel = null;
            _headerLevel = null;
            _footerLevel = null;
        }

        private void OnActive()
        {
            // _middleLevel = LevelManager.Ins.CurrentLevel;
            // middleLevelIndex = _middleLevel.Index;
            // // if Preload contains current level + 1, _headerLevel = Preload[current level + 1]
            // if (LevelManager.Ins.PreLoadLevels.ContainsKey(_middleLevel.Index + 1))
            // {
            //     _headerLevel = LevelManager.Ins.PreLoadLevels[_middleLevel.Index + 1];
            //     furthestLevelIndex = _headerLevel.Index;
            // }
            // else
            // {
            //     furthestLevelIndex = _middleLevel.Index;
            // }
            //
            // // if Preload contains current level - 1, _footerLevel = Preload[current level - 1]
            // if (LevelManager.Ins.PreLoadLevels.ContainsKey(_middleLevel.Index - 1))
            // {
            //     _footerLevel = LevelManager.Ins.PreLoadLevels[_middleLevel.Index - 1];
            //     nearestLevelIndex = _footerLevel.Index;
            // }
            // else
            // {
            //     nearestLevelIndex = _middleLevel.Index;
            // }
        }
    }
}
