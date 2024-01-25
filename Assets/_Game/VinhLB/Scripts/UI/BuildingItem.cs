using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VinhLB
{
    public class BuildingItem : HMonoBehaviour
    {
        [SerializeField] private Button _button;

        [SerializeField] private Image _mainImage;

        [SerializeField] private TMP_Text _nameText;

        private BuildingUnitData _buildingUnitData;

        public void OnInit(BuildingUnitData data)
        {
            _buildingUnitData = data;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() =>
            {
                GridBuildingManager.Ins.ChangeCurrentObjectDataId(_buildingUnitData.Id);
            });

            _mainImage.sprite = data.Sprite;
            _nameText.text = data.Name;
        }
    }
}
