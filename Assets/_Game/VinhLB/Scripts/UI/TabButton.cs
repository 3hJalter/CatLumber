using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VinhLB
{
    public class TabButton : HMonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField]
        private TabGroup _tabGroup;
        [SerializeField]
        private bool _interactable = true;
        [SerializeField]
        private Image _background;
        [SerializeField]
        private LayoutElement _layoutElement;
        [SerializeField]
        private GameObject _activeGO;
        [SerializeField]
        private Image _icon;
        [SerializeField]
        private TMP_Text _nameText;

        private bool _active;
        private Transform _iconTransform;

        public void SetActiveState(bool active, bool animated)
        {
            // if (active == _active)
            // {
            //     return;
            // }

            _active = active;

            if (_iconTransform is null)
            {
                _iconTransform = _icon.transform;
            }
            _iconTransform.DOKill();

            if (_active)
            {
                _layoutElement.flexibleWidth = 1f;

                _activeGO.SetActive(true);

                _nameText.gameObject.SetActive(true);

                if (animated)
                {
                    float duration = 0.2f;
                    Sequence sequence = DOTween.Sequence();
                    sequence.Append(_iconTransform.DOLocalMove(Vector3.up * 50f, duration).SetEase(Ease.OutBack))
                        .Join(_iconTransform.DOScale(1.25f, duration).SetEase(Ease.OutBack));
                }
                else
                {
                    _iconTransform.localPosition = Vector3.up * 50f;
                    _iconTransform.localScale = Vector3.one * 1.25f;
                }
            }
            else
            {
                _layoutElement.flexibleWidth = 0f;

                _activeGO.SetActive(false);

                _nameText.gameObject.SetActive(false);

                _iconTransform.localPosition = Vector3.zero;
                _iconTransform.localScale = Vector3.one;
            }
        }

        public void SetBackgroundAlpha(float value)
        {
            Color color = _background.color;
            color.a = Mathf.Clamp01(value);
            _background.color = color;
        }

        public void SetBackgroundSprite(Sprite sprite)
        {
            _background.sprite = sprite;
        }

        public void SetPreferredWidth(float value)
        {
            _layoutElement.preferredWidth = value;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_interactable)
            {
                _tabGroup.OnTabSelected(this, true);
            }
            else
            {
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_interactable)
            {
                _tabGroup.OnTabEnter(this);
            }
            else
            {
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_interactable)
            {
                _tabGroup.OnTabExit(this);
            }
            else
            {
            }
        }
    }
}