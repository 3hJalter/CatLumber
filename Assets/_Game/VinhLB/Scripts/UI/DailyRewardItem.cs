using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VinhLB
{
    public class DailyRewardItem : HMonoBehaviour
    {
        [SerializeField]
        private Button _button;
        [SerializeField]
        private TMP_Text _dayText;
        [SerializeField]
        private Image _iconImage;
        [SerializeField]
        private TMP_Text _amountText;
        [SerializeField]
        private RectTransform _activeRectTf;
        [SerializeField]
        private RectTransform _inactiveRectTf;
        [SerializeField]
        private RectTransform _checkedRectTf;

        private int _day = -1;
        private Reward _reward = null;

        public Reward Reward => _reward;

        public void Initialize(int day, Reward reward)
        {
            _day = day;
            _reward = reward;
            
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() =>
            {
                DailyRewardManager.Ins.ObtainTodayReward();
            });
        }

        public void UpdateVisual()
        {
            if (_day < 0 || _reward is null)
            {
                return;
            }
            
            _dayText.text = $"Day {_day + 1}";
            
            if (_reward.IconSprite is not null)
            {
                _iconImage.sprite = _reward.IconSprite;
            }
            
            _amountText.text = $"{_reward.Amount}";

            if (_day <= DailyRewardManager.Ins.CycleDay)
            {
                if (_day == DailyRewardManager.Ins.CycleDay && !DailyRewardManager.Ins.IsTodayRewardObtained)
                {
                    _button.interactable = true;
                    _activeRectTf.gameObject.SetActive(true);
                    _inactiveRectTf.gameObject.SetActive(false);
                    _checkedRectTf.gameObject.SetActive(false);
                }
                else
                {
                    _button.interactable = false;
                    _activeRectTf.gameObject.SetActive(false);
                    _inactiveRectTf.gameObject.SetActive(false);
                    _checkedRectTf.gameObject.SetActive(true);
                }
            }
            else
            {
                _button.interactable = false;
                _activeRectTf.gameObject.SetActive(false);
                _inactiveRectTf.gameObject.SetActive(true);
                _checkedRectTf.gameObject.SetActive(false);
            }
        }
    }
}