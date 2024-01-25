using System.Collections.Generic;
using GameGridEnum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VinhLB
{
    [CreateAssetMenu(fileName = "VFXData", menuName = "ScriptableObjects/VFXData", order = 3)]
    public class VFXData : SerializedScriptableObject
    {
        [Title("Particle Data")]
        [SerializeField]
        private readonly Dictionary<VFXType, ParticleSystem> _particleDict;

        public ParticleSystem GetParticleSystem(VFXType type)
        {
            return _particleDict.GetValueOrDefault(type);
        }
    }
}