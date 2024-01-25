using _Game._Scripts.Managers;
using _Game.Camera;
using _Game.GameGrid;
using _Game.Managers;
using _Game.Utilities;
using HControls;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Game.UIs.Screen
{
    public class WorldLevelScreen : UICanvas
    {
        [SerializeField] private Transform btnMiddleLevel;
        [SerializeField] private Transform btnFurthestLevel;
        [SerializeField] private Transform btnNearestLevel;
        
        public Transform BtnMiddleLevel => btnMiddleLevel;

        public Transform BtnFurthestLevel => btnFurthestLevel;

        public Transform BtnNearestLevel => btnNearestLevel;
        
        public void OnClickNearestLevel()
        {
            // int levelIndex = CameraManager.Ins.WorldMapCameraTarget.NearestLevelIndex;
            // DevLog.Log(DevId.Hoang, "OnClickNearestLevel: " + levelIndex);
            Close();
            // LevelManager.Ins.OnGoLevel(levelIndex);
        }
        
        public void OnClickFurthestLevel()
        {
            // int levelIndex = CameraManager.Ins.WorldMapCameraTarget.FurthestLevelIndex;
            // DevLog.Log(DevId.Hoang, "OnClickFurthestLevel: " + levelIndex);
            Close();
            // LevelManager.Ins.OnGoLevel(levelIndex);
        }
        
        public void OnClickGoMiddleLevel()
        {
            // int levelIndex = CameraManager.Ins.WorldMapCameraTarget.MiddleLevelIndex;
            // DevLog.Log(DevId.Hoang, "OnClickGoMiddleLevel: " + levelIndex);
            Close();
            // LevelManager.Ins.OnGoLevel(levelIndex);
        }
        
        // TEST:
        public void OnClickGoLevel()
        {
            // int levelIndex = CameraManager.Ins.WorldMapCameraTarget.MiddleLevelIndex;
            // DevLog.Log(DevId.Hoang, "OnClickGoLevel: " + levelIndex);
            Close();
            // LevelManager.Ins.OnGoLevel(levelIndex);
        }
        
        public override void Setup()
        {
            base.Setup();
            GameManager.Ins.ChangeState(GameState.WorldMap);
            // CurveWorld
            // FxManager.Ins.ChangePlanetCurvatureSize();
            // CameraManager.Ins.WorldMapCameraTarget.WorldLevelScreen = this;
            // CameraManager.Ins.ChangeWorldTargetPosition();
            // CameraManager.Ins.EnableWorldCamera(true);
            CameraManager.Ins.ChangeCamera(ECameraType.WorldMapCamera);
        }

        public override void Close()
        {
            // FxManager.Ins.ChangePlanetCurvatureSize(0f);
            // CameraManager.Ins.EnableWorldCamera(false);
            CameraManager.Ins.ChangeCamera(ECameraType.InGameCamera);
            LevelManager.Ins.SetCameraToPlayerIsland();
            UIManager.Ins.OpenUI<InGameScreen>();
            base.Close();
        }
    }
}
