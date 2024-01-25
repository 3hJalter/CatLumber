using UnityEngine;

namespace HControls
{
    [RequireComponent(typeof(GameObject))]
    public class HDpadButton : HMonoBehaviour
    {
        [SerializeField] private Direction direction;
        [SerializeField] private GameObject pointerDownImg;
        public Direction Direction => direction;

        public GameObject PointerDownImg => pointerDownImg;

        private void Awake()
        {
            pointerDownImg.SetActive(false);
        }
    }
}
