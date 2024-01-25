using System.Collections.Generic;
using AudioEnum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Game.Data
{
    [CreateAssetMenu(fileName = "AudioData", menuName = "ScriptableObjects/AudioData", order = 1)]
    public class AudioData : SerializedScriptableObject
    {
        [Title("BGM")]
        [SerializeField]
        private readonly Dictionary<BgmType, AudioClip> _bgmDict;
        
        [Title("SFX")]
        [SerializeField]
        private readonly Dictionary<SfxType, AudioClip> _sfxDict;

        public Dictionary<BgmType, AudioClip> BGMDict => _bgmDict;

        public Dictionary<SfxType, AudioClip> SfxDict => _sfxDict;
    }
}

namespace AudioEnum
{
    public enum SfxType
    {
        None = -1
    }

    public enum BgmType
    {
        None = -1,
        MainMenu = 0
    }
}
