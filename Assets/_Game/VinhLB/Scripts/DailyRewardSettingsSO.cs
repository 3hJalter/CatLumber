using Sirenix.OdinInspector;
using UnityEngine;

namespace VinhLB
{
    [CreateAssetMenu(fileName = "DailyLoginSettings", menuName = "ScriptableObjects/DailyLoginSettings")]
    public class DailyRewardSettingsSO : SerializedScriptableObject
    {
        public int CycleDays = 7;
        public bool OneCycleOnly = false;
        public bool DifferentFirstCycle = true;
        public bool MissRewardIfNotLogin = false;
        [ShowIf(nameof(DifferentFirstCycle), false)]
        public Reward[] FirstCycleRewards;
        public Reward[] Rewards;
    }

    [System.Serializable]
    public class Reward
    {
        public RewardType RewardType;
        public string Name;
        public Sprite IconSprite;
        public int Amount;

        public void Obtain()
        {
            switch (RewardType)
            {
                case RewardType.Gold:
                    break;
                case RewardType.Gem:
                    break;
            }
        }
    }

    public enum RewardType
    {
        None = -1,
        Gold = 0,
        Gem = 1
    }
}