using System;
using System.Collections.Generic;
using _Game.DesignPattern;
using _Game.GameGrid;
using _Game.Managers;
using _Game.Utilities;
using _Game.Utilities.Timer;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VinhLB
{
    public class CloudSpawner : HMonoBehaviour
    {
        [SerializeField]
        private float _cloudHeight = 10f;
        [SerializeField]
        [MinMaxSlider(0.1f, 20f, true)]
        private Vector2 _minMaxSpawnInterval = new Vector2(1f, 2f);
        [SerializeField]
        [MinMaxSlider(0.1f, 10f, true)]
        private Vector2 _minMaxCloudSpeed = new Vector2(0.5f, 1f);

        private float _spawnInterval;
        private STimer _timer;
        private List<Action> _actionList;
        private List<float> _timeList;
        private Vector3 _bottomLeftPosition;
        private Vector3 _topRightPosition;
        private List<Cloud> _spawnedCloudList;

        private void Awake()
        {
            _timer = new STimer();
            _actionList = new List<Action>();
            _timeList = new List<float>();
            _spawnedCloudList = new List<Cloud>();
        }

        public void SpawnClouds()
        {
            if (_timer.IsStart)
            {
                _timer.Stop();
            }

            for (int i = _spawnedCloudList.Count - 1; i >= 0; i--)
            {
                _spawnedCloudList[i].Despawn();
            }

            _bottomLeftPosition = LevelManager.Ins.CurrentLevel.GetBottomLeftPos();
            _topRightPosition = LevelManager.Ins.CurrentLevel.GetTopRightPos();

            SpawnCloudInternal();

            StartSpawnCloudTimer();
        }

        private void StartSpawnCloudTimer()
        {
            _actionList.Clear();
            _timeList.Clear();

            _actionList.Add(SpawnCloudInternal);
            _spawnInterval = Random.Range(_minMaxSpawnInterval.x, _minMaxSpawnInterval.y);
            _timeList.Add(_spawnInterval);

            _timer.Start(_timeList, _actionList);
        }

        private void SpawnCloudInternal()
        {
            DevLog.Log(DevId.Vinh, $"Spawn cloud: {_spawnInterval}");
            Vector3 cloudPosition = new Vector3(
                _topRightPosition.x + Random.Range(1f, 2f),
                _cloudHeight + Random.Range(-1f, 1f),
                Random.Range(_bottomLeftPosition.z - 5f, _topRightPosition.z + 5f));
            Quaternion cloudRotation = Quaternion.Euler(
                0f,
                Random.Range(0, 2) == 0 ? 0f : 180f,
                0f);

            Cloud cloud = SimplePool.Spawn<Cloud>(DataManager.Ins.GetRandomEnvironmentObject(PoolType.Cloud),
                cloudPosition, cloudRotation);
            Vector3 cloudEndPosition = new Vector3(
                _bottomLeftPosition.x - Random.Range(1f, 2f),
                cloudPosition.y,
                cloudPosition.z);
            cloud.Initialize(Random.Range(_minMaxCloudSpeed.x, _minMaxCloudSpeed.y), Vector3.left, cloudEndPosition);

            _spawnedCloudList.Add(cloud);

            StartSpawnCloudTimer();
        }
    }
}