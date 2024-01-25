using TMPro;
using UnityEngine;

namespace _Game.Utilities.Grid
{
    public static class GridUtilities
    {
        public static Material OverlayMaterial;
        public static TextMeshPro CreateWorldText(string text, Transform parent = null, Vector3 localPosition = default,
            int fontSize = 40,
            Color color = default, TextAnchor textAnchor = TextAnchor.UpperLeft,
            TextAlignment textAlignment = TextAlignment.Center, int sortingOrder = 5000)
        {
            return CreateWorldText(parent, text, localPosition, fontSize, color, textAnchor, textAlignment,
                sortingOrder);
        }

        public static TextMeshPro CreateWorldText(Transform parent, string text, Vector3 localPosition,
            int fontSize, Color color, TextAnchor textAnchor, TextAlignment textAlignment = default,
            int sortingOrder = default)
        {
            GameObject gameObject = new("World_Text", typeof(TextMeshPro));
            Transform transform = gameObject.transform;
            transform.SetParent(parent, false);
            transform.localPosition = localPosition;
            TextMeshPro textMesh = gameObject.GetComponent<TextMeshPro>();
            textMesh.alignment = (TextAlignmentOptions)textAlignment;
            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.color = color;
            textMesh.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
            textMesh.fontSharedMaterial = OverlayMaterial;
            return textMesh;
        }

        public static Vector3 GetMouseWorldPosition()
        {
            Vector3 vec = GetMouseWorldPositionWithZ(Input.mousePosition, UnityEngine.Camera.main);
            vec.z = 0;
            return vec;
        }

        public static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, UnityEngine.Camera worldCamera)
        {
            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            return worldPosition;
        }

        public static void CreateEmptyMeshArray(int quadCount, out Vector3[] vertices, out Vector2[] uvs,
            out int[] triangles)
        {
            int triangleCount = quadCount * 2;
            vertices = new Vector3[quadCount * 4];
            uvs = new Vector2[quadCount * 4];
            triangles = new int[triangleCount * 3];
        }

        public static void AddToMeshArray(Vector3[] vertices, Vector2[] uvs, int[] triangles, int index, Vector3 pos,
            float rot, Vector3 baseSize, Vector2 uv00, Vector2 uv11)
        {
            int vIndex = index * 4;
            int vIndex0 = vIndex;
            int vIndex1 = vIndex + 1;
            int vIndex2 = vIndex + 2;
            int vIndex3 = vIndex + 3;

            bool skewed = Mathf.Abs(baseSize.x - baseSize.y) > 0.001f;
            baseSize *= 0.5f;
            if (skewed)
            {
                vertices[vIndex0] =
                    pos + Quaternion.Euler(Vector3.forward * rot) * new Vector3(-baseSize.x, -baseSize.y);
                vertices[vIndex1] =
                    pos + Quaternion.Euler(Vector3.forward * rot) * new Vector3(-baseSize.x, baseSize.y);
                vertices[vIndex2] = pos + Quaternion.Euler(Vector3.forward * rot) * baseSize;
                vertices[vIndex3] =
                    pos + Quaternion.Euler(Vector3.forward * rot) * new Vector3(baseSize.x, -baseSize.y);
            }
            else
            {
                vertices[vIndex0] = pos + Quaternion.Euler(Vector3.forward * (rot - 180)) * baseSize;
                vertices[vIndex1] = pos + Quaternion.Euler(Vector3.forward * (rot - 270)) * baseSize;
                vertices[vIndex2] = pos + Quaternion.Euler(Vector3.forward * rot) * baseSize;
                vertices[vIndex3] = pos + Quaternion.Euler(Vector3.forward * (rot - 90)) * baseSize;
            }

            uvs[vIndex0] = new Vector2(uv00.x, uv11.y);
            uvs[vIndex1] = new Vector2(uv00.x, uv00.y);
            uvs[vIndex2] = new Vector2(uv11.x, uv00.y);
            uvs[vIndex3] = new Vector2(uv11.x, uv11.y);

            int tIndex = index * 6;
            triangles[tIndex] = vIndex0;
            triangles[tIndex + 1] = vIndex1;
            triangles[tIndex + 2] = vIndex2;

            triangles[tIndex + 3] = vIndex0;
            triangles[tIndex + 4] = vIndex2;
            triangles[tIndex + 5] = vIndex3;
        }
    }
}
