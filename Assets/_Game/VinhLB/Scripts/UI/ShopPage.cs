using UnityEngine;
using UnityEngine.UI;

namespace VinhLB
{
    public class ShopPage : TabPage
    {
        [SerializeField]
        private ScrollRect _scrollRect;

        public override void Open()
        {
            base.Open();

            _scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}