using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VinhLB
{
    public class RewardItem : HMonoBehaviour
    {
        [SerializeField]
        private TMP_Text _nameText;
        [SerializeField]
        private Image _iconImage;
        [SerializeField]
        private TMP_Text _amountText;
        
        private Reward _reward = null;

        public Reward Reward => _reward;

        public void Initialize(Reward reward)
        {
            _reward = reward;

            if (_reward == null)
            {
                return;
            }

            _nameText.text = _reward.Name;
            _iconImage.sprite = _reward.IconSprite;
            _amountText.text = _reward.Amount.ToString();
        }
    }
}