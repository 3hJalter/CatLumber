// Toony Colors Pro+Mobile 2
// (c) 2014-2023 Jean Moreno

using System;
using ToonyColorsPro.Runtime;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Use this script to generate the Reflection Render Texture when using the "Planar Reflection" mode from the Shader Generator

// Usage:
// - generate a water shader with "Planar Reflection"
// - assign this shader to a planar mesh's material
// - add this script on the same GameObject

// Based on: http://wiki.unity3d.com/index.php/MirrorReflection4

namespace ToonyColorsPro
{
    namespace Runtime
    {
        [ExecuteInEditMode]
        public class TCP2_PlanarReflection : MonoBehaviour
        {
            private static bool s_InsideRendering;

            private static int _ShaderID_ReflectionTex = -1;

            private static int _ShaderID_ReflectionDepthTex = -1;

            private static int _ShaderID_ReflectivePlaneY = -1;

            private static int _ShaderID_ReflectionDepthRange = -1;

            private static int _ShaderID_UseReflectionDepth = -1;
            public int textureSize = 1024;
            public RenderTextureFormat renderTextureFormat = RenderTextureFormat.Default;
            public LayerMask reflectLayers = -1;
            public bool disablePixelLights;
            public float clipPlaneOffset = 0.07f;

            [Space] public bool useCustomBackgroundColor;

            public Color backgroundColor = Color.black;

            [Space] public bool applyBlur;

            public Shader blurShader;
            [Range(1, 4)] public int blurIterations = 1;
            [Range(1, 8)] public float blurDistance = 1;
            private readonly float blurDepthRange = 2;

            // WIP, not visible in the Inspector yet
            private readonly bool useBlurDepth = false;
            private Material blurMaterial;
            private CommandBuffer commandBufferBlur;
#if UNITY_2019_3_OR_NEWER
            private bool isURP;
#endif

            private Camera reflectionCamera;
            private RenderTexture reflectionDepthRenderTexture;
            private Shader reflectionDepthShader;
            private RenderTexture reflectionRenderTexture;

            private static int ShaderID_ReflectionTex
            {
                get
                {
                    if (_ShaderID_ReflectionTex < 0) _ShaderID_ReflectionTex = Shader.PropertyToID("_ReflectionTex");
                    return _ShaderID_ReflectionTex;
                }
            }

            private static int ShaderID_ReflectionDepthTex
            {
                get
                {
                    if (_ShaderID_ReflectionDepthTex < 0)
                        _ShaderID_ReflectionDepthTex = Shader.PropertyToID("_ReflectionDepthTex");
                    return _ShaderID_ReflectionDepthTex;
                }
            }

            private static int ShaderID_ReflectivePlaneY
            {
                get
                {
                    if (_ShaderID_ReflectivePlaneY < 0)
                        _ShaderID_ReflectivePlaneY = Shader.PropertyToID("_ReflectivePlaneY");
                    return _ShaderID_ReflectivePlaneY;
                }
            }

            private static int ShaderID_ReflectionDepthRange
            {
                get
                {
                    if (_ShaderID_ReflectionDepthRange < 0)
                        _ShaderID_ReflectionDepthRange = Shader.PropertyToID("_ReflectionDepthRange");
                    return _ShaderID_ReflectionDepthRange;
                }
            }

            private static int ShaderID_UseReflectionDepth
            {
                get
                {
                    if (_ShaderID_UseReflectionDepth < 0)
                        _ShaderID_UseReflectionDepth = Shader.PropertyToID("_UseReflectionDepth");
                    return _ShaderID_UseReflectionDepth;
                }
            }

            private void OnEnable()
            {
#if UNITY_2019_3_OR_NEWER
                isURP = GraphicsSettings.currentRenderPipeline != null &&
                        GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal");
#endif

#if UNITY_2019_3_OR_NEWER
                if (isURP)
                    RenderPipelineManager.beginCameraRendering += BeginCameraRendering_URP;
                else
#endif
                    Camera.onPreRender += BeginCameraRendering_Bultin;

                UpdateRenderTexture();
                UpdateCommandBuffer();
            }

            private void OnDisable()
            {
#if UNITY_2019_3_OR_NEWER
                if (isURP)
                    RenderPipelineManager.beginCameraRendering -= BeginCameraRendering_URP;
                else
#endif
                    Camera.onPreRender -= BeginCameraRendering_Bultin;

                ClearCommandBuffer();
                ClearRenderTexture();
            }

            // --------------------------------------------------------------------------------------------------------------------------------
            // Unity Events

            private void OnValidate()
            {
                UpdateRenderTexture();
                UpdateCommandBuffer();
            }

            // --------------------------------------------------------------------------------------------------------------------------------
            // Render Texture

            private void UpdateRenderTexture()
            {
                if (reflectionRenderTexture != null) ClearRenderTexture();

                reflectionRenderTexture = new RenderTexture(textureSize, textureSize, 16, renderTextureFormat,
                    RenderTextureReadWrite.sRGB);
                reflectionRenderTexture.name = "Planar Reflection for " + GetInstanceID();
                reflectionRenderTexture.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            }

            private void ClearRenderTexture()
            {
                if (reflectionRenderTexture != null)
                {
                    reflectionRenderTexture.Release();
                    DestroyImmediate(reflectionRenderTexture);
                }
            }

            // --------------------------------------------------------------------------------------------------------------------------------
            // Command Buffer (blur)

            private void UpdateCommandBuffer()
            {
                if (commandBufferBlur != null) ClearCommandBuffer();

                if (!applyBlur) return;

                if (blurMaterial == null)
                {
                    if (blurShader == null)
                    {
                        Debug.LogError("[TCP2 Planar Reflection] Can't find Gaussian Blur Filter shader!", gameObject);
                        return;
                    }

                    blurMaterial = new Material(blurShader);
                    blurMaterial.name = "Planar Reflection Blur";
                }

                blurMaterial.SetFloat("_SamplingDistance", blurDistance);

                if (reflectionRenderTexture == null) return;

                if (reflectionCamera == null) return;

                commandBufferBlur = new CommandBuffer();
                {
                    // Create temp render texture
                    int tempRT = Shader.PropertyToID("_PlanarReflectionTempRT");
                    commandBufferBlur.GetTemporaryRT(tempRT, textureSize, textureSize, 16, FilterMode.Bilinear,
                        reflectionRenderTexture.format, RenderTextureReadWrite.sRGB);

                    // Down sample
                    commandBufferBlur.CopyTexture(reflectionRenderTexture, tempRT); // copy reflection to temp
                    commandBufferBlur.Blit(tempRT, reflectionRenderTexture, blurMaterial, 0); // down sample

                    // Blur passes
                    for (int i = 0; i < blurIterations; i++)
                    {
                        commandBufferBlur.Blit(reflectionRenderTexture, tempRT, blurMaterial, 1); // blur 1st pass
                        commandBufferBlur.Blit(tempRT, reflectionRenderTexture, blurMaterial, 2); // blur 2nd pass
                    }

                    // Release temp render texture
                    commandBufferBlur.ReleaseTemporaryRT(tempRT);
                }

                // Add command buffer
                reflectionCamera.AddCommandBuffer(CameraEvent.AfterEverything, commandBufferBlur);
            }

            private void ClearCommandBuffer()
            {
                if (reflectionCamera != null && reflectionCamera.commandBufferCount > 0)
                    reflectionCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, commandBufferBlur);
                if (commandBufferBlur != null)
                {
                    commandBufferBlur.Clear();
                    commandBufferBlur.Release();
                    commandBufferBlur = null;
                }
            }

            // --------------------------------------------------------------------------------------------------------------------------------
            // Reflection Camera Rendering

            public void BeginCameraRendering_Bultin(Camera camera)
            {
                if ((camera.cameraType & (CameraType.Game | CameraType.SceneView)) == 0) return;

                if (reflectionCamera == null)
                {
                    GameObject go = new("Planar Reflection Camera", typeof(Camera));
                    reflectionCamera = go.GetComponent<Camera>();
                    reflectionCamera.enabled = false;
                    go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
                    //go.hideFlags = HideFlags.DontSave;

                    UpdateRenderTexture();
                    UpdateCommandBuffer();
                    reflectionCamera.targetTexture = reflectionRenderTexture;
                }

                RenderPlanarReflection(camera);
            }

#if UNITY_2019_3_OR_NEWER
            public void BeginCameraRendering_URP(ScriptableRenderContext context, Camera camera)
            {
                if ((camera.cameraType & (CameraType.Game | CameraType.SceneView)) == 0) return;

                if (reflectionCamera == null)
                {
                    GameObject go = new("Planar Reflection Camera", typeof(Camera));
                    reflectionCamera = go.GetComponent<Camera>();
                    reflectionCamera.enabled = false;
                    go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                    //go.hideFlags = HideFlags.DontSave;

                    UpdateRenderTexture();
                    UpdateCommandBuffer();
                    reflectionCamera.targetTexture = reflectionRenderTexture;
                }

#if TCP2_UNIVERSAL_RP
                RenderPlanarReflection(context, camera);
#else
				RenderPlanarReflection(camera);
#endif
            }
#endif

            // Given position/normal of the plane, calculates plane in camera space.
            private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
            {
                Vector3 offsetPos = pos + normal * clipPlaneOffset;
                Matrix4x4 m = cam.worldToCameraMatrix;
                Vector3 cpos = m.MultiplyPoint(offsetPos);
                Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
                return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
            }

            // Calculates reflection matrix around the given plane
            private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
            {
                reflectionMat.m00 = 1F - 2F * plane[0] * plane[0];
                reflectionMat.m01 = -2F * plane[0] * plane[1];
                reflectionMat.m02 = -2F * plane[0] * plane[2];
                reflectionMat.m03 = -2F * plane[3] * plane[0];

                reflectionMat.m10 = -2F * plane[1] * plane[0];
                reflectionMat.m11 = 1F - 2F * plane[1] * plane[1];
                reflectionMat.m12 = -2F * plane[1] * plane[2];
                reflectionMat.m13 = -2F * plane[3] * plane[1];

                reflectionMat.m20 = -2F * plane[2] * plane[0];
                reflectionMat.m21 = -2F * plane[2] * plane[1];
                reflectionMat.m22 = 1F - 2F * plane[2] * plane[2];
                reflectionMat.m23 = -2F * plane[3] * plane[2];

                reflectionMat.m30 = 0F;
                reflectionMat.m31 = 0F;
                reflectionMat.m32 = 0F;
                reflectionMat.m33 = 1F;
            }

#if TCP2_UNIVERSAL_RP
            public void RenderPlanarReflection(Camera worldCamera)
            {
                RenderPlanarReflection(new ScriptableRenderContext(), worldCamera);
            }

            public void RenderPlanarReflection(ScriptableRenderContext context, Camera worldCamera)
#else
			public void RenderPlanarReflection(Camera worldCamera)
#endif
            {
                if (worldCamera == null) return;

                Renderer rend = GetComponent<Renderer>();
                if (!enabled || !rend || !rend.sharedMaterial || !rend.enabled) return;

                // Safeguard from recursive reflections.      
                if (s_InsideRendering) return;
                s_InsideRendering = true;

                //CreateMirrorObjects(reflectionCamera);

                // find out the reflection plane: position and normal in world space
                Vector3 pos = transform.position;
                Vector3 normal = transform.up;

                // Optionally disable pixel lights for reflection
                int oldPixelLightCount = QualitySettings.pixelLightCount;
                if (disablePixelLights) QualitySettings.pixelLightCount = 0;

                reflectionCamera.CopyFrom(worldCamera);
                if (useCustomBackgroundColor)
                {
                    reflectionCamera.clearFlags = CameraClearFlags.Color;
                    reflectionCamera.backgroundColor = backgroundColor;
                }

                // Reflect camera around reflection plane
                float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;
                Vector4 reflectionPlane = new(normal.x, normal.y, normal.z, d);

                Matrix4x4 reflectionMatrix = Matrix4x4.zero;
                CalculateReflectionMatrix(ref reflectionMatrix, reflectionPlane);
                Vector3 oldpos = worldCamera.transform.position;
                Vector3 newpos = reflectionMatrix.MultiplyPoint(oldpos);
                reflectionCamera.worldToCameraMatrix = worldCamera.worldToCameraMatrix * reflectionMatrix;

                // Setup oblique projection matrix so that near plane is our reflection plane. This way we clip everything below/above it for free.
                Vector4 clipPlane = CameraSpacePlane(reflectionCamera, pos, normal, 1.0f);
                Matrix4x4 projection = worldCamera.CalculateObliqueMatrix(clipPlane);
                reflectionCamera.projectionMatrix = projection;

                reflectionCamera.targetTexture = reflectionRenderTexture;
                reflectionCamera.cullingMask =
                    ~(1 << gameObject.layer) & reflectLayers.value; // never render this object's layer
                GL.invertCulling = true;

                reflectionCamera.transform.position = newpos;
                Vector3 euler = worldCamera.transform.eulerAngles;
                reflectionCamera.transform.eulerAngles = new Vector3(0, euler.y, euler.z);

#if UNITY_2019_3_OR_NEWER && TCP2_UNIVERSAL_RP
                if (isURP)
                {
                    UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
                    if (applyBlur && commandBufferBlur != null) context.ExecuteCommandBuffer(commandBufferBlur);
                }
                else
#endif
                {
                    if (applyBlur && useBlurDepth)
                    {
                        if (reflectionDepthShader == null)
                            reflectionDepthShader = Shader.Find("Hidden/TCP2 Planar Reflection Depth");

                        if (reflectionDepthRenderTexture == null)
                        {
                            reflectionDepthRenderTexture = new RenderTexture(textureSize, textureSize, 16,
                                RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
                            blurMaterial.SetTexture(ShaderID_ReflectionDepthTex, reflectionDepthRenderTexture);
                        }

                        RenderTexture prevTarget = reflectionCamera.targetTexture;
                        reflectionCamera.targetTexture = reflectionDepthRenderTexture;
                        reflectionCamera.RenderWithShader(reflectionDepthShader, null);
                        reflectionCamera.targetTexture = prevTarget;

                        blurMaterial.SetFloat(ShaderID_ReflectivePlaneY, transform.position.y + clipPlaneOffset);
                        blurMaterial.SetFloat(ShaderID_ReflectionDepthRange, blurDepthRange);
                        blurMaterial.SetFloat(ShaderID_UseReflectionDepth, 1);
                    }
                    else if (applyBlur && !useBlurDepth)
                    {
                        blurMaterial.SetFloat(ShaderID_UseReflectionDepth, 0);
                    }
                    else
                    {
                        if (reflectionDepthRenderTexture != null)
                        {
                            reflectionDepthRenderTexture.Release();
                            reflectionDepthRenderTexture = null;
                        }
                    }

                    reflectionCamera.Render();
                }

                reflectionCamera.transform.position = oldpos;
                GL.invertCulling = false;

                Material[] materials = rend.sharedMaterials;
                foreach (Material mat in materials)
                {
                    if (mat.HasProperty(ShaderID_ReflectionTex))
                        mat.SetTexture(ShaderID_ReflectionTex, reflectionRenderTexture);
                    if (mat.HasProperty(ShaderID_ReflectionDepthTex))
                        mat.SetTexture(ShaderID_ReflectionDepthTex, reflectionDepthRenderTexture);
                }

                // Restore pixel light count
                if (disablePixelLights) QualitySettings.pixelLightCount = oldPixelLightCount;

                s_InsideRendering = false;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TCP2_PlanarReflection))]
    internal class TCP2_PlanarReflectionEditor : Editor
    {
        private static readonly GUIContent[] textureSizeLabels =
        {
            new("64"),
            new("128"),
            new("256"),
            new("512"),
            new("1024"),
            new("2048"),
            new("4096"),
            new("8192")
        };

        private static readonly int[] textureSizeValues =
        {
            64,
            128,
            256,
            512,
            1024,
            2048,
            4096,
            8192
        };

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();

            // Customized 'DrawDefaultInspector'
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            bool guiEnabled = GUI.enabled;
            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    GUILayout.Space(4);
                    EditorGUILayout.HelpBox(
                        "This scripts will render planar reflections, it needs to be used with a generated shader with the \"Planar Reflections\" feature enabled.",
                        MessageType.Info);
                    GUILayout.Space(4);
                    EditorGUILayout.HelpBox(
                        "This script only works with axis-aligned meshes on the XZ plane in Unity space. (e.g. it will work with the \"Plane\" built-in mesh, but not with the \"Quad\" one).",
                        MessageType.Warning);
                    GUILayout.Space(4);
                }
                else if (iterator.propertyPath == "textureSize")
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.IntPopup(iterator, textureSizeLabels, textureSizeValues);
                        if (GUILayout.Button("-", EditorStyles.miniButtonLeft, GUILayout.Width(20)))
                            iterator.intValue =
                                textureSizeValues[
                                    Mathf.Clamp(Array.IndexOf(textureSizeValues, iterator.intValue) - 1, 0,
                                        textureSizeValues.Length - 1)];
                        if (GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(20)))
                            iterator.intValue =
                                textureSizeValues[
                                    Mathf.Clamp(Array.IndexOf(textureSizeValues, iterator.intValue) + 1, 0,
                                        textureSizeValues.Length - 1)];
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else if (iterator.propertyPath == "reflectLayers")
                {
                    EditorGUILayout.PropertyField(iterator, true);
                    EditorGUILayout.HelpBox(
                        "The layer this GameObjects is assigned to will be ignored to prevent the reflective object from reflecting itself!\nAny other object in that layer won't be rendered in the reflection as well.",
                        MessageType.Info);
                    GUILayout.Space(4);
                }
                else
                {
                    EditorGUILayout.PropertyField(iterator, true);

                    if (iterator.propertyPath == "applyBlur")
                        GUI.enabled &= iterator.boolValue;
                    else if (iterator.propertyPath == "useBlurDepth") GUI.enabled &= iterator.boolValue;
                }

                enterChildren = false;
            }

            GUI.enabled = guiEnabled;
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
