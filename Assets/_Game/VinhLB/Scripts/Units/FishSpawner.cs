using System.Collections;
using System.Collections.Generic;
using _Game.DesignPattern;
using _Game.GameGrid;
using _Game.Managers;
using _Game.Utilities;
using UnityEngine;

namespace VinhLB
{
    public class FishSpawner : HMonoBehaviour
    {
        private Fish _shark;

        public void SpawnFish(bool resetPath = true)
        {
            SpawnFishInternal(resetPath);
        }

        private void SpawnFishInternal(bool resetPath)
        {
            // DevLog.Log(DevId.Vinh, $"Center: {LevelManager.Ins.CurrentLevel.GetCenterPos()}");
            // DevLog.Log(DevId.Vinh, $"BottomLeft: {LevelManager.Ins.CurrentLevel.GetBottomLeftPos()}");
            // DevLog.Log(DevId.Vinh, $"TopRight: {LevelManager.Ins.CurrentLevel.GetTopRightPos()}");

            if (resetPath)
            {
                Vector3 bottomLeftPoint = LevelManager.Ins.CurrentLevel.GetBottomLeftPos();
                Vector3 topRightPoint = LevelManager.Ins.CurrentLevel.GetTopRightPos();
                Vector3 bottomRightPoint = bottomLeftPoint;
                bottomRightPoint.x = topRightPoint.x;
                Vector3 topLeftPoint = topRightPoint;
                topLeftPoint.x = bottomLeftPoint.x;
                Vector3 bottomCenterPoint = bottomLeftPoint;
                bottomCenterPoint.x += (bottomRightPoint.x - bottomLeftPoint.x) / 2;
                Vector3 topCenterPoint = bottomCenterPoint;
                topCenterPoint.z = topLeftPoint.z;
                Vector3 middleLeftPoint = bottomLeftPoint;
                middleLeftPoint.z += (topLeftPoint.z - bottomLeftPoint.z) / 2;
                Vector3 middleRightPoint = middleLeftPoint;
                middleRightPoint.x = bottomRightPoint.x;

                List<Vector3> waypointList = new List<Vector3>
                {
                    bottomLeftPoint,
                    middleLeftPoint,
                    topLeftPoint,
                    topCenterPoint,
                    topRightPoint,
                    middleRightPoint,
                    bottomRightPoint,
                    bottomCenterPoint
                };

                if (Random.Range(0, 4) > 1)
                {
                    waypointList.Reverse();
                }
                waypointList.Shift(Random.Range(0, waypointList.Count - 1));

                if (_shark == null)
                {
                    // _shark = Instantiate(_sharkPrefab, Tf);
                    _shark = SimplePool.Spawn<Fish>(DataManager.Ins.GetRandomEnvironmentObject(PoolType.Shark));
                }
                _shark.Initialize(waypointList);
            }
            else
            {
                if (_shark != null)
                {
                    _shark.ResetMovement();
                }
            }
        }
    }
}