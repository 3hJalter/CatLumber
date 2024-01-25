using System;
using _Game.DesignPattern;
using _Game.Managers;
using _Game.Utilities;
using UnityEngine;

namespace VinhLB
{
    public class DailyRewardManager : Singleton<DailyRewardManager>
    {
        public event Action OnDailyRewardParamsChanged;

        [SerializeField]
        private DailyRewardSettingsSO _dailyRewardSettingsSO;

        #region Properties
        public DailyRewardSettingsSO DailyRewardSettingsSO => _dailyRewardSettingsSO;
        public bool IsTodayRewardObtained =>
            DateTime.Now.Date <= DataManager.Ins.GameData.user.lastDailyRewardClaimTime;
        public int TotalDays => _dailyRewardSettingsSO.MissRewardIfNotLogin
            ? (DateTime.Now.Date - DataManager.Ins.GameData.user.startDailyRewardClaimTime.Date).Days
            : (DataManager.Ins.GameData.user.dailyRewardClaimedCount - (IsTodayRewardObtained ? 1 : 0));
        public int CycleDay => TotalDays % _dailyRewardSettingsSO.CycleDays;
        public bool IsInFirstCycle => TotalDays / _dailyRewardSettingsSO.CycleDays == 0;
        public Reward[] Rewards => IsInFirstCycle && _dailyRewardSettingsSO.DifferentFirstCycle
            ? _dailyRewardSettingsSO.FirstCycleRewards
            : _dailyRewardSettingsSO.Rewards;
        public Reward CurrentReward => Rewards[CycleDay];
        #endregion

        public void ObtainTodayReward()
        {
            if (IsTodayRewardObtained)
            {
                DevLog.Log(DevId.Vinh, $"Day {CycleDay + 1} reward is obtained");
                return;
            }

            DevLog.Log(DevId.Vinh, $"Obtain day {CycleDay + 1} reward");
            // CurrentReward.Obtain();
            UIManager.Ins.GetUI<RewardPopup>().Open(new Reward[] { CurrentReward });

            DataManager.Ins.GameData.user.dailyRewardClaimedCount += 1;
            DataManager.Ins.GameData.user.lastDailyRewardClaimTime = DateTime.Now.Date;
            DataManager.Ins.Save();

            Ins.OnDailyRewardParamsChanged?.Invoke();
        }

        private void SetPlayerPrefsDateTime(string key, DateTime dateTime)
        {
            string dateTimeString = dateTime.ToShortDateString();
            PlayerPrefs.SetString(key, dateTimeString);
        }

        private DateTime GetPlayerPrefsDateTime(string key, DateTime defaultDateTime = default)
        {
            string dateTimeString = PlayerPrefs.GetString(key);
            if (!DateTime.TryParse(dateTimeString, out DateTime dateTime))
            {
                dateTime = defaultDateTime;
            }

            return dateTime;
        }

        #region Debug
#if UNITY_EDITOR
        // [UnityEditor.MenuItem("Debug/Daily Reward/Print Parameters")]
        public static void PrintParameters()
        {
            DevLog.Log(DevId.Vinh,
                $"StartDailyRewardDateTime: {DataManager.Ins.GameData.user.startDailyRewardClaimTime}");
            DevLog.Log(DevId.Vinh,
                $"DailyRewardCount: {DataManager.Ins.GameData.user.dailyRewardClaimedCount}");
            DevLog.Log(DevId.Vinh,
                $"LastDailyRewardDateTime: {DataManager.Ins.GameData.user.lastDailyRewardClaimTime}");

            if (Application.isPlaying)
            {
                DevLog.Log(DevId.Vinh, $"IsInFirstCycle: {Ins.IsInFirstCycle}");
                DevLog.Log(DevId.Vinh, $"TotalDays: {Ins.TotalDays}");
                DevLog.Log(DevId.Vinh, $"CycleDay: {Ins.CycleDay}");
                DevLog.Log(DevId.Vinh, $"IsTodayRewardObtained: {Ins.IsTodayRewardObtained}");
            }
        }

        // [UnityEditor.MenuItem("Debug/Daily Reward/Set Can Collect Today")]
        public static void SetCanCollectToday()
        {
            DataManager.Ins.GameData.user.lastDailyRewardClaimTime = DateTime.Now.AddHours(-24);
            DataManager.Ins.Save();

            if (Application.isPlaying)
            {
                Ins.OnDailyRewardParamsChanged?.Invoke();
            }
        }

        // [UnityEditor.MenuItem("Debug/Daily Reward/Increase 1 Daily Day")]
        public static void Increase1DailyDay()
        {
            if (Ins._dailyRewardSettingsSO.MissRewardIfNotLogin)
            {
                DataManager.Ins.GameData.user.startDailyRewardClaimTime =
                    DataManager.Ins.GameData.user.startDailyRewardClaimTime.AddDays(-1);
                DataManager.Ins.Save();

                DevLog.Log(DevId.Vinh,
                    $"StartDailyRewardDateTime: {DataManager.Ins.GameData.user.startDailyRewardClaimTime}");
            }
            else
            {
                DataManager.Ins.GameData.user.dailyRewardClaimedCount += 1;
                DevLog.Log(DevId.Vinh, $"DailyRewardCount: {DataManager.Ins.GameData.user.dailyRewardClaimedCount}");
            }

            if (Application.isPlaying)
            {
                Ins.OnDailyRewardParamsChanged?.Invoke();
            }
        }

        // [UnityEditor.MenuItem("Debug/Daily Reward/Decrease 1 Daily Day")]
        public static void Decrease1DailyDay()
        {
            if (Ins._dailyRewardSettingsSO.MissRewardIfNotLogin)
            {
                if (DataManager.Ins.GameData.user.startDailyRewardClaimTime.AddDays(1) <= DateTime.Now.Date)
                {
                    // Make sure start date is not after today
                    DataManager.Ins.GameData.user.startDailyRewardClaimTime =
                        DataManager.Ins.GameData.user.startDailyRewardClaimTime.AddDays(1);
                    DataManager.Ins.Save();
                }

                DevLog.Log(DevId.Vinh,
                    $"StartDailyRewardDateTime: {DataManager.Ins.GameData.user.startDailyRewardClaimTime}");
            }
            else
            {
                DataManager.Ins.GameData.user.dailyRewardClaimedCount =
                    Mathf.Max(0, DataManager.Ins.GameData.user.dailyRewardClaimedCount - 1);
                DevLog.Log(DevId.Vinh, $"DailyRewardCount: {DataManager.Ins.GameData.user.dailyRewardClaimedCount}");
            }

            if (Application.isPlaying)
            {
                Ins.OnDailyRewardParamsChanged?.Invoke();
            }
        }

        // [UnityEditor.MenuItem("Debug/Daily Reward/Reset All")]
        public static void ResetAll()
        {
            if (!Application.isPlaying)
            {
                DataManager.Ins.GameData.user.startDailyRewardClaimTime = DateTime.Now.Date;
                DataManager.Ins.GameData.user.dailyRewardClaimedCount = 0;
                DataManager.Ins.GameData.user.lastDailyRewardClaimTime = DateTime.Now.AddHours(-24);
            }
        }
#endif
        #endregion
    }
}