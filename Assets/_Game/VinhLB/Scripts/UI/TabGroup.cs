using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VinhLB
{
    public class TabGroup : HMonoBehaviour
    {
        [SerializeField]
        private List<TabButton> _tabButtonList;
        [SerializeField]
        private List<TabPage> _tabPageList;
        [SerializeField]
        private int _activeTabIndex;
        [SerializeField]
        private float _inactiveTabButtonWidth = 300f;
        [SerializeField]
        private bool _useCustomSprite;
        [ShowIf(nameof(_useCustomSprite), false)]
        [SerializeField]
        private Sprite _tabIdleSprite;
        [ShowIf(nameof(_useCustomSprite), false)]
        [SerializeField]
        private Sprite _tabHoverSprite;
        [ShowIf(nameof(_useCustomSprite), false)]
        [SerializeField]
        private Sprite _tabActiveSprite;

        private TabButton _selectedTabButton;

        private void Start()
        {
            for (int i = 0; i < _tabButtonList.Count; i++)
            {
                _tabButtonList[i].SetPreferredWidth(_inactiveTabButtonWidth);
            }
        }

        public void ResetSelectedTab()
        {
            if (_tabButtonList.Count > 0)
            {
                if (_activeTabIndex < 0 || _activeTabIndex >= _tabButtonList.Count)
                {
                    _activeTabIndex = 0;
                }

                OnTabSelected(_tabButtonList[_activeTabIndex], false);
            }
        }

        public void OnTabEnter(TabButton button)
        {
            // ResetTabs();
            //
            // if (_selectedTabButton == null || button != _selectedTabButton)
            // {
            //     if (!_useCustomSprite)
            //     {
            //         button.SetBackgroundAlpha(0.025f);
            //     }
            //     else
            //     {
            //         button.ChangeBackgroundSprite(_tabHoverSprite);
            //     }
            // }
        }

        public void OnTabExit(TabButton button)
        {
            // ResetTabs();
        }

        public void OnTabSelected(TabButton button, bool animated)
        {
            if (button == _selectedTabButton)
            {
                return;
            }

            _selectedTabButton = button;

            ResetTabs();

            if (!_useCustomSprite)
            {
                button.SetActiveState(true, animated);
            }
            else
            {
                button.SetBackgroundSprite(_tabActiveSprite);
            }

            int index = _tabButtonList.IndexOf(button);
            for (int i = 0; i < _tabPageList.Count; i++)
            {
                if (i == index)
                {
                    _tabPageList[i].Open();
                }
                else
                {
                    _tabPageList[i].Close();
                }
            }
        }

        private void ResetTabs()
        {
            for (int i = 0; i < _tabButtonList.Count; i++)
            {
                if (_tabButtonList[i] == _selectedTabButton)
                {
                    continue;
                }

                if (!_useCustomSprite)
                {
                    _tabButtonList[i].SetActiveState(false, false);
                }
                else
                {
                    _tabButtonList[i].SetBackgroundSprite(_tabIdleSprite);
                }
            }
        }
    }
}
