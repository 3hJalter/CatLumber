using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VinhLB
{
    public class GrassTrampleObject : HMonoBehaviour
    {
        [SerializeField] private UniversalRendererData _rendererData;

        [SerializeField] private float _trampleRadius = 1f;

        private GrassTrampleFeature _feature;

        public float TrampleRadius => _trampleRadius;

        private void Awake()
        {
            Utilities.TryGetRendererFeature(_rendererData, out _feature);
        }

        private void OnEnable()
        {
            if (_feature != null) _feature.AddTrackedTrampleObject(this);
        }

        private void OnDisable()
        {
            if (_feature != null) _feature.RemoveTrackedTrampleObject(this);
        }
    }
}
