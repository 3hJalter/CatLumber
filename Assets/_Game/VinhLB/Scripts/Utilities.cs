using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

namespace VinhLB
{
    public static class Utilities
    {
        public static TextMeshPro CreateWorldText(string text, Transform parent = null, Vector3 localPosition = default,
            Vector2 size = default,
            float fontSize = 32f, Color color = default,
            TextAlignmentOptions alignmentOptions = TextAlignmentOptions.TopLeft, int sortingOrder = 1000)
        {
            GameObject gameObject = new("WorldText");
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            TextMeshPro textMesh = gameObject.AddComponent<TextMeshPro>();
            textMesh.rectTransform.sizeDelta = size;
            textMesh.alignment = alignmentOptions;
            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.color = color;
            textMesh.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;

            return textMesh;
        }

        public static bool IsPointerOverUIGameObject()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> resultList = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, resultList);
            
            return resultList.Count > 0;
        }

        public static bool TryGetMouseWorldPosition(out Vector3 mousePosition,
            Camera camera = null, float distance = Mathf.Infinity, LayerMask layerMask = default)
        {
            mousePosition = Vector3.zero;
            camera = camera != null ? camera : Camera.main;
            layerMask = layerMask != default ? layerMask : Physics.DefaultRaycastLayers;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, distance, layerMask))
            {
                mousePosition = hit.point;

                return true;
            }

            return false;
        }

        public static bool TryGetMouseWorldPosition(out Vector3 mousePosition, LayerMask layerMask)
        {
            return TryGetMouseWorldPosition(out mousePosition, null, Mathf.Infinity, layerMask);
        }

        public static bool TryGetCenterScreenPosition(out Vector3 centerScreenPosition,
            Camera camera = null, float distance = Mathf.Infinity, LayerMask layerMask = default)
        {
            centerScreenPosition = Vector3.zero;
            camera = camera != null ? camera : Camera.main;
            layerMask = layerMask != default ? layerMask : Physics.DefaultRaycastLayers;
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, distance, layerMask))
            {
                centerScreenPosition = hit.point;

                return true;
            }

            return false;
        }

        public static bool TryGetCenterScreenPosition(out Vector3 mousePosition, LayerMask layerMask)
        {
            return TryGetCenterScreenPosition(out mousePosition, null, Mathf.Infinity, layerMask);
        }

        public static bool TryGetRendererFeature<T>(ScriptableRendererData rendererData, out T rendererFeature)
            where T : ScriptableRendererFeature
        {
            rendererFeature = rendererData.rendererFeatures.OfType<T>().FirstOrDefault();

            return rendererFeature != null;
        }

        public static bool TryGetRendererFeature<T>(ScriptableRendererData rendererData, string featureName,
            out T rendererFeature)
            where T : ScriptableRendererFeature
        {
            var rendererFeatures = rendererData.rendererFeatures.OfType<T>();
            foreach (T feature in rendererFeatures)
            {
                if (feature.name.Equals(featureName))
                {
                    rendererFeature = feature;

                    return true;
                }
            }

            rendererFeature = null;

            return false;
        }

        public static GameObject[] FindGameObjectsInLayer(string layerName, bool includeInactive)
        {
            return FindGameObjectsInLayer(LayerMask.NameToLayer(layerName), includeInactive);
        }
        
        public static GameObject[] FindGameObjectsInLayer(int layer, bool includeInactive)
        {
            GameObject[] goArray = Object.FindObjectsOfType(typeof(GameObject), includeInactive) as GameObject[];
            if (goArray == null)
            {
                return null;
            }
            List<GameObject> goList = new List<GameObject>();
            for (int i = 0; i < goArray.Length; i++)
            {
                if (goArray[i].layer == layer)
                {
                    goList.Add(goArray[i]);
                }
            }
            if (goList.Count == 0)
            {
                return null;
            }
            
            return goList.ToArray();
        }
    }
}