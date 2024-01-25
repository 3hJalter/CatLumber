using _Game._Scripts.InGame;
using _Game.DesignPattern;
using AmazingAssets.CurvedWorld;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;
using VinhLB;

namespace _Game.Managers
{
    public class FXManager : Singleton<FXManager>
    {
        // [SerializeField]
        // private CurvedWorldController curvedWorldController;
        [SerializeField]
        private HintLineTrail trailHint;

        public HintLineTrail TrailHint => trailHint;

        [SerializeField]
        private GameObject _groundGO;
        [SerializeField]
        private GameObject _waterGO;
        [SerializeField]
        private GameObject _gridGO;
        [SerializeField]
        private VisualEffect _fogVisualEffect;
        [SerializeField]
        private UniversalRendererData _rendererData;
        [SerializeField]
        private UniversalRenderPipelineAsset _renderPipelineAsset;
        [SerializeField]
        private UniversalAdditionalCameraData _cameraData;

        [SerializeField]
        private bool _activeGround = true;
        [SerializeField]
        private bool _activeWater = true;
        [SerializeField]
        private bool _activeGrid = false;
        [SerializeField]
        private bool _activeFog = true;

        private GrassTrampleFeature _feature;

        private void Awake()
        {
            _groundGO.SetActive(_activeGround);
            _waterGO.SetActive(_activeWater);
            _gridGO.SetActive(_activeGrid);
            _fogVisualEffect.gameObject.SetActive(_activeFog);

            VinhLB.Utilities.TryGetRendererFeature(_rendererData, out _feature);
        }

        public void ChangeShadowDistance(float distance)
        {
            _renderPipelineAsset.shadowDistance = distance;
            // if distance >= 150, normal bias is 0 
            // else increase it until normal = 1 when distance <= 50
            float normalBias = distance >= 150 ? 0f : 1f - (distance - 50f) / 100f;
            _renderPipelineAsset.shadowNormalBias = normalBias;
        }

        private Tween _changePlantCurveTween;

        // public void ChangePlanetCurvatureSize(float curve = 7f, float time = 1f)
        // {
        //     _changePlantCurveTween?.Kill();
        //     float currentCurve = curvedWorldController.bendCurvatureSize;
        //     _changePlantCurveTween = DOVirtual.Float(currentCurve, curve, time, value => curvedWorldController.bendCurvatureSize = value);
        // }

        public void ResetTrackedTrampleObjectList()
        {
            _feature.ResetTrackedTrampleList();
        }

        public void TogglePostProcessing()
        {
            _cameraData.renderPostProcessing = !_cameraData.renderPostProcessing;
        }

        public void ToggleWater()
        {
            _waterGO.SetActive(!_waterGO.activeInHierarchy);
        }

        public void ToggleGrasses()
        {
            GameObject[] grassGOs = VinhLB.Utilities.FindGameObjectsInLayer("Grass", true);
            for (int i = 0; i < grassGOs.Length; i++)
            {
                grassGOs[i].SetActive(!grassGOs[i].activeInHierarchy);
            }
        }

        public void SwitchGridActive(bool manual = false, bool active = true)
        {
            if (!manual)
            {
                _gridGO.SetActive(!_gridGO.activeSelf);
            }
            else
            {
                _gridGO.SetActive(active);
            }
        }

        public void UpdateFogColliderPositionAndScale(Vector3 bottomLeftPosition, Vector3 topRightPosition)
        {
            Vector3 colliderPosition =
                (bottomLeftPosition + topRightPosition) / 2f - _fogVisualEffect.transform.position;
            colliderPosition.z -= 5.5f;
            colliderPosition.y = 0f;
            _fogVisualEffect.SetVector3("Collider Position", colliderPosition);

            Vector3 colliderScale = topRightPosition - bottomLeftPosition;
            colliderScale.x = Mathf.Abs(colliderScale.x) * 0.45f;
            colliderScale.z = Mathf.Abs(colliderScale.z) * 0.55f;
            colliderScale.y = 8f;
            _fogVisualEffect.SetVector3("Collider Scale", colliderScale);
        }
    }
}