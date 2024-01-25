using System;
using _Game.DesignPattern;
using _Game.Managers;
using _Game.Utilities;
using GameGridEnum;
using UnityEngine;

namespace VinhLB
{
    public class Water : HMonoBehaviour
    {
        [SerializeField]
        private float _surfaceHeight = 0.6f;
        
        private void OnTriggerEnter(Collider collider)
        {
            DevLog.Log(DevId.Vinh, $"Something fall into water: {collider.gameObject.name}");
            Vector3 contactPoint = collider.transform.position;
            contactPoint.y = _surfaceHeight;
            ParticlePool.Play(DataManager.Ins.VFXData.GetParticleSystem(VFXType.WaterSplash), contactPoint);
        }
    }
}