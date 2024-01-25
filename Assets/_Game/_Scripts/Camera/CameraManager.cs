using System;
using System.Collections;
using System.Collections.Generic;
using _Game._Scripts.InGame;
using _Game.Camera;
using _Game.DesignPattern;
using _Game.GameGrid;
using Cinemachine;
using DG.Tweening;
using MEC;
using UnityEngine;

namespace _Game.Managers
{
    public class CameraManager : Singleton<CameraManager>
    {
        private const int CAMERA_PRIORITY_ACTIVE = 99;
        private const int CAMERA_PRIORITY_INACTIVE = 1;
        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private CinemachineVirtualCameraBase currentVirtualCamera;

        [SerializeField] private UnityEngine.Camera brainCamera;
        [SerializeField] private Transform cameraTarget;

        public Transform CameraTarget => cameraTarget;
        // [SerializeField] private WorldMapTarget worldMapCameraTarget; // may be redundant later

        // public WorldMapTarget WorldMapCameraTarget => worldMapCameraTarget;

        [SerializeField] private float cameraMoveTime = 1f;
        [SerializeField] private Vector2 worldCameraXYPos;
        
        // ReSharper disable once Unity.RedundantSerializeFieldAttribute
        // ReSharper disable once CollectionNeverUpdated.Local
        [SerializeField]
        private readonly Dictionary<ECameraType, CinemachineVirtualCameraBase> virtualCameraDic = new();

        public UnityEngine.Camera BrainCamera => brainCamera;

        public CinemachineVirtualCameraBase CurrentVirtualCamera => currentVirtualCamera;

        public bool IsCurrentCameraIs(ECameraType eCameraType)
        {
            return currentVirtualCamera == virtualCameraDic[eCameraType];
        }

        public void ChangeCamera(ECameraType eCameraType, float blendTime = 2f)
        {
            if (currentVirtualCamera != null)
            {
                currentVirtualCamera.Priority = CAMERA_PRIORITY_INACTIVE;
                currentVirtualCamera.enabled = false;
            }
            currentVirtualCamera = virtualCameraDic[eCameraType];
            currentVirtualCamera.Priority = CAMERA_PRIORITY_ACTIVE;
            currentVirtualCamera.enabled = true;
            brain.m_DefaultBlend.m_Time = blendTime;
        }

        public void ChangeCameraTargetPosition(Vector3 position, float moveTime = -1f)
        {
            // if the same position, do nothing
            if ((cameraTarget.position - position).sqrMagnitude < 0.01f) return;
            
            if (moveTime < 0f) moveTime = cameraMoveTime;
            cameraTarget.DOKill();
            cameraTarget.DOMove(position, moveTime).SetEase(Ease.Linear);
        }

        // public void ChangeWorldTargetPosition()
        // {
        //     worldMapCameraTarget.Tf.position = new Vector3(
        //         worldCameraXYPos.x,
        //         worldCameraXYPos.y,
        //         LevelManager.Ins.CurrentLevel.GetCenterPos().z);
        // }
        
        // public void EnableWorldCamera(bool enable)
        // {
        //     worldMapCameraTarget.gameObject.SetActive(enable);
        // }
        
        public void ChangeCameraTarget(ECameraType eCameraType, Transform target)
        {
            virtualCameraDic[eCameraType].Follow = target;
            virtualCameraDic[eCameraType].LookAt = target;
        }
    }
}
