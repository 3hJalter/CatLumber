using System.Collections.Generic;
using _Game.Managers;
using _Game.UIs.Screen;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace VinhLB
{
    public class BuildingScreen : UICanvas
    {
        [SerializeField]
        private CanvasGroup _canvasGroup;
        [SerializeField]
        private Image _blockPanel;
        [SerializeField]
        private Button _rotateButton;
        [SerializeField]
        private Button _clearButton;
        [SerializeField]
        private Button _placeButton;
        [SerializeField]
        private Button _deleteButton;
        [SerializeField]
        private Button _exitButton;
        [SerializeField]
        private BuildingItem _buildingItemPrefab;

        [SerializeField]
        private Transform _contentTransform;

        private void Start()
        {
            _rotateButton.onClick.AddListener(() =>
            {
                GridBuildingManager.Ins.ChangePlaceDirection();
            });
            _clearButton.onClick.AddListener(() =>
            {
                GridBuildingManager.Ins.ChangeCurrentObjectDataId(-1);
            });
            _placeButton.onClick.AddListener(() =>
            {
                GridBuildingManager.Ins.PlaceBuilding();
            });
            _deleteButton.onClick.AddListener(() =>
            {
                GridBuildingManager.Ins.DeleteBuilding();
            });
            _exitButton.onClick.AddListener(() =>
            {
                Close();
                UIManager.Ins.OpenUI<InGameScreen>();

                GridBuildingManager.Ins.ToggleBuildMode();
            });

            List<BuildingUnitData> placedObjectDataList = GridBuildingManager.Ins.GetPlacedObjectDataList();
            for (int i = 0; i < placedObjectDataList.Count; i++)
            {
                BuildingItem item = Instantiate(_buildingItemPrefab, _contentTransform);
                item.OnInit(placedObjectDataList[i]);
            }
        }

        public override void Open()
        {
            base.Open();

            DOVirtual.Float(0, 1, 1f, value => _canvasGroup.alpha = value)
                .OnComplete(() => _blockPanel.gameObject.SetActive(false));
        }
    }
}