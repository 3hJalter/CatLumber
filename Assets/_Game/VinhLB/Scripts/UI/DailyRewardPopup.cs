using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace VinhLB
{
    public class DailyRewardPopup : UICanvas
    {
        [SerializeField]
        private List<DailyRewardItem> _dailyRewardItemList;

        private void Awake()
        {
            SetupDailyRewards();

            DailyRewardManager.Ins.OnDailyRewardParamsChanged += DailyRewardManager_OnOnDailyRewardParamsChanged;
        }

        private void SetupDailyRewards()
        {
            for (int i = 0; i < DailyRewardManager.Ins.DailyRewardSettingsSO.CycleDays; i++)
            {
                if (_dailyRewardItemList[i].Reward != DailyRewardManager.Ins.Rewards[i])
                {
                    _dailyRewardItemList[i].Initialize(i, DailyRewardManager.Ins.Rewards[i]);
                }

                _dailyRewardItemList[i].UpdateVisual();
            }
        }

        private void DailyRewardManager_OnOnDailyRewardParamsChanged()
        {
            SetupDailyRewards();
        }
    }
}