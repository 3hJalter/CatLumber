// Toony Colors Pro+Mobile 2
// (c) 2014-2023 Jean Moreno

//Enable this to display the default Inspector (in case the custom Inspector is broken)
//#define SHOW_DEFAULT_INSPECTOR

using System;
using System.Collections.Generic;
using ToonyColorsPro.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using RenderingMode = ToonyColorsPro.ShaderGenerator.MaterialInspector_Hybrid.RenderingMode;

// Custom material inspector for generated shader

namespace ToonyColorsPro
{
    namespace ShaderGenerator
    {
#if UNITY_2019_4_OR_NEWER
        public class MaterialInspector_SG2 : ShaderGUI, ITerrainLayerCustomUI
#else
		public class MaterialInspector_SG2 : ShaderGUI
#endif
        {
            //Properties
            private Material targetMaterial => _materialEditor == null ? null : _materialEditor.target as Material;
            private MaterialEditor _materialEditor;
            private MaterialProperty[] _properties;
            private readonly Stack<bool> toggledGroups = new();
            private bool hasAutoTransparency;

            //--------------------------------------------------------------------------------------------------

            public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
            {
                _materialEditor = materialEditor;
                _properties = properties;

                hasAutoTransparency = Array.Exists(_properties, prop => prop.name == PROP_RENDERING_MODE);

#if SHOW_DEFAULT_INSPECTOR
		base.OnGUI();
		return;
#else

                //Header
                EditorGUILayout.BeginHorizontal();
                string label = Screen.width > 450f ? "TOONY COLORS PRO 2 - INSPECTOR (Generated Shader)" :
                    Screen.width > 300f ? "TOONY COLORS PRO 2 - INSPECTOR" : "TOONY COLORS PRO 2";
                TCP2_GUI.HeaderBig(label);
                if (TCP2_GUI.Button(TCP2_GUI.CogIcon2, "SG2", "Open in Shader Generator"))
                    if (targetMaterial.shader != null)
                        ShaderGenerator2.OpenWithShader(targetMaterial.shader);
                EditorGUILayout.EndHorizontal();
                TCP2_GUI.Separator();

                //Iterate Shader properties
                materialEditor.serializedObject.Update();
                SerializedProperty mShader = materialEditor.serializedObject.FindProperty("m_Shader");
                toggledGroups.Clear();

                // Auto-transparency
                if (hasAutoTransparency)
                {
                    int indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel++;
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(15);
                            GUILayout.Label(TCP2_GUI.TempContent("Transparency"), EditorStyles.boldLabel);
                        }
                        EditorGUILayout.EndHorizontal();
                        HandleRenderingMode();
                    }
                    EditorGUI.indentLevel = indent;
                }

                if (materialEditor.isVisible && !mShader.hasMultipleDifferentValues &&
                    mShader.objectReferenceValue != null)
                {
                    //Retina display fix
                    EditorGUIUtility.labelWidth = Utils.ScreenWidthRetina - 120f;
                    EditorGUIUtility.fieldWidth = 64f;

                    EditorGUI.BeginChangeCheck();

                    EditorGUI.indentLevel++;
                    foreach (MaterialProperty p in properties)
                    {
                        bool visible = toggledGroups.Count == 0 || toggledGroups.Peek();

                        //Hacky way to separate material inspector properties into foldout groups
                        if (p.name.StartsWith("__BeginGroup"))
                        {
                            //Foldout
                            if (visible)
                            {
                                GUILayout.Space(2f);
                                Rect propertyRect = EditorGUILayout.GetControlRect(true,
                                    EditorGUIUtility.singleLineHeight, EditorStyles.layerMaskField);
                                propertyRect.x += 12;
                                propertyRect.width -= 12;
                                p.floatValue = EditorGUI.Foldout(propertyRect, p.floatValue > 0, p.displayName, true)
                                    ? 1
                                    : 0;
                            }

                            EditorGUI.indentLevel++;
                            toggledGroups.Push(p.floatValue > 0 && visible);
                        }
                        else if (p.name.StartsWith("__EndGroup"))
                        {
                            EditorGUI.indentLevel--;
                            toggledGroups.Pop();
                            GUILayout.Space(2f);
                        }
                        else
                        {
                            //Draw regular property
                            if (visible && (p.flags & (MaterialProperty.PropFlags.PerRendererData |
                                                       MaterialProperty.PropFlags.HideInInspector)) ==
                                MaterialProperty.PropFlags.None) _materialEditor.ShaderProperty(p, p.displayName);
                        }
                    }

                    EditorGUI.indentLevel--;

                    if (EditorGUI.EndChangeCheck()) materialEditor.PropertiesChanged();
                }

#endif // !SHOW_DEFAULT_INSPECTOR

#if UNITY_5_5_OR_NEWER
                TCP2_GUI.Separator();
                materialEditor.RenderQueueField();
#endif
#if UNITY_5_6_OR_NEWER
                materialEditor.EnableInstancingField();
#endif
            }

            //--------------------------------------------------------------------------------------------------
            // Auto-transparency handling

            private const string PROP_RENDERING_MODE = "_RenderingMode";
            private const string PROP_ZWRITE = "_ZWrite";
            private const string PROP_BLEND_SRC = "_SrcBlend";
            private const string PROP_BLEND_DST = "_DstBlend";
            private const string PROP_CULLING = "_Cull";

            private void HandleRenderingMode()
            {
                bool showMixed = EditorGUI.showMixedValue;
                MaterialProperty renderingModeProp = FindProperty(PROP_RENDERING_MODE, _properties);
                EditorGUI.showMixedValue = renderingModeProp.hasMixedValue;
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PrefixLabel(TCP2_GUI.TempContent("Rendering Mode"));
                        GUILayout.FlexibleSpace();
                        RenderingMode newRenderingMode = (RenderingMode)EditorGUILayout.EnumPopup(GUIContent.none,
                            (RenderingMode)renderingModeProp.floatValue, GUILayout.Width(118));
                        if ((float)newRenderingMode != renderingModeProp.floatValue)
                        {
                            Undo.RecordObjects(_materialEditor.targets, "Change Material Rendering Mode");
                            SetRenderingMode(newRenderingMode);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.showMixedValue = showMixed;
            }

            private void SetRenderingMode(RenderingMode mode)
            {
                switch (mode)
                {
                    case RenderingMode.Opaque:
                        SetRenderQueue(RenderQueue.Geometry);
                        //SetCulling(Culling.Back);
                        SetZWrite(true);
                        SetBlending(BlendFactor.One, BlendFactor.Zero);
                        IterateMaterials(mat => mat.DisableKeyword("_ALPHAPREMULTIPLY_ON"));
                        IterateMaterials(mat => mat.DisableKeyword("_ALPHABLEND_ON"));
                        break;

                    case RenderingMode.Fade:
                        SetRenderQueue(RenderQueue.Transparent);
                        //SetCulling(Culling.Off);
                        SetZWrite(false);
                        SetBlending(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
                        IterateMaterials(mat => mat.DisableKeyword("_ALPHAPREMULTIPLY_ON"));
                        IterateMaterials(mat => mat.EnableKeyword("_ALPHABLEND_ON"));
                        break;

                    case RenderingMode.Transparent:
                        SetRenderQueue(RenderQueue.Transparent);
                        //SetCulling(Culling.Off);
                        SetZWrite(false);
                        SetBlending(BlendFactor.One, BlendFactor.OneMinusSrcAlpha);
                        IterateMaterials(mat => mat.EnableKeyword("_ALPHAPREMULTIPLY_ON"));
                        IterateMaterials(mat => mat.DisableKeyword("_ALPHABLEND_ON"));
                        break;
                }

                IterateMaterials(mat => mat.SetFloat(PROP_RENDERING_MODE, (float)mode));
            }

            private void SetZWrite(bool enable)
            {
                IterateMaterials(mat => mat.SetFloat(PROP_ZWRITE, enable ? 1.0f : 0.0f));
            }

            private void SetRenderQueue(RenderQueue queue)
            {
                IterateMaterials(mat => mat.renderQueue = (int)queue);
            }

            private void SetCulling(Culling culling)
            {
                IterateMaterials(mat => mat.SetFloat(PROP_CULLING, (float)culling));
            }

            private void SetBlending(BlendFactor src, BlendFactor dst)
            {
                IterateMaterials(mat => mat.SetFloat(PROP_BLEND_SRC, (float)src));
                IterateMaterials(mat => mat.SetFloat(PROP_BLEND_DST, (float)dst));
            }

            private void IterateMaterials(Action<Material> action)
            {
                foreach (Object target in _materialEditor.targets) action(target as Material);
            }

            //--------------------------------------------------------------------------------------------------
            // Terrain layer UI, if the shader is terrain-compatible

#if UNITY_2019_4_OR_NEWER

            // System to know when to update the UI:
            private long SG2Timestamp;

            private Shader terrainShader;
            private readonly List<TerrainLayerProperty> terrainLayerProperties = new();

            private enum TerrainLayerVariable
            {
                error,
                diffuseRemapMax,
                diffuseRemapMin,
                diffuseTexture,
                maskMapRemapMax,
                maskMapRemapMin,
                maskMapTexture,
                metallic,
                normalMapTexture,
                normalScale,
                smoothness,
                specular,
                tileOffset,
                tileSize
            }

            private class TerrainLayerProperty
            {
                private readonly bool hdr;

                private readonly bool isVector4;
                private readonly string label;
                private readonly string labelAlpha;
                private readonly string labelB;
                private readonly string labelG;
                private readonly string labelR;
                private readonly Vector2 rangeLimits;
                private readonly bool showA;
                private readonly bool showB;
                private readonly bool showG;
                private readonly bool showR;
                private readonly ShaderPropertyType type;
                private readonly TerrainLayerVariable variable;

                private Color colorValue;
                private float floatValue;
                private Texture2D textureValue;
                private Vector4 vectorValue;

                public TerrainLayerProperty(string label, ShaderPropertyType type, TerrainLayerVariable variable,
                    bool hdr, Vector2 rangeLimits = new())
                {
                    this.label = label;
                    this.type = type;
                    this.variable = variable;
                    this.hdr = hdr;
                    this.rangeLimits = rangeLimits;

                    isVector4 = this.variable == TerrainLayerVariable.diffuseRemapMax
                                || this.variable == TerrainLayerVariable.diffuseRemapMin
                                || this.variable == TerrainLayerVariable.maskMapRemapMax
                                || this.variable == TerrainLayerVariable.maskMapRemapMin;

                    if (isVector4 && type == ShaderPropertyType.Range)
                    {
                        string[] labels = this.label.Split(',');
                        this.label = labels[0];
                        labelAlpha = labels[1];

                        showA = labelAlpha != "Unused";
                    }
                    else if (isVector4 && type == ShaderPropertyType.Float)
                    {
                        string[] labels = this.label.Split(',');
                        labelR = labels[0];
                        labelG = labels[1];
                        labelB = labels[2];
                        labelAlpha = labels[3];

                        showR = labelR != "Unused";
                        showG = labelG != "Unused";
                        showB = labelB != "Unused";
                        showA = labelAlpha != "Unused";
                    }
                }

                public void DrawGUI(TerrainLayer terrainLayer)
                {
                    bool disabledByDiffuseAlpha = false;
                    if (variable == TerrainLayerVariable.smoothness)
                    {
                        bool diffuseHasAlpha = terrainLayer.diffuseTexture != null &&
                                               GraphicsFormatUtility.HasAlphaChannel(terrainLayer.diffuseTexture
                                                   .graphicsFormat);
                        if (diffuseHasAlpha) disabledByDiffuseAlpha = true;
                    }

                    if (disabledByDiffuseAlpha) EditorGUI.BeginDisabledGroup(true);

                    switch (type)
                    {
                        case ShaderPropertyType.Color:
                        {
                            colorValue = EditorGUILayout.ColorField(TCP2_GUI.TempContent(label), colorValue, true, true,
                                hdr);
                            break;
                        }
                        case ShaderPropertyType.Float:
                        {
                            if (isVector4)
                            {
                                // Hack: this defines the 4 Floats mode
                                if (showR)
                                    vectorValue.x =
                                        EditorGUILayout.FloatField(TCP2_GUI.TempContent(labelR), vectorValue.x);
                                if (showG)
                                    vectorValue.y =
                                        EditorGUILayout.FloatField(TCP2_GUI.TempContent(labelG), vectorValue.y);
                                if (showB)
                                    vectorValue.z =
                                        EditorGUILayout.FloatField(TCP2_GUI.TempContent(labelB), vectorValue.z);
                                if (showA)
                                    vectorValue.w = EditorGUILayout.FloatField(TCP2_GUI.TempContent(labelAlpha),
                                        vectorValue.w);
                            }
                            else
                            {
                                floatValue = EditorGUILayout.FloatField(TCP2_GUI.TempContent(label),
                                    disabledByDiffuseAlpha ? 1f : floatValue);
                            }

                            break;
                        }
                        case ShaderPropertyType.Range:
                            if (isVector4)
                            {
                                // Hack: this defines the Color RGB + Float mode
                                colorValue = EditorGUILayout.ColorField(TCP2_GUI.TempContent(label), colorValue, true,
                                    false, true);
                                floatValue = EditorGUILayout.FloatField(TCP2_GUI.TempContent(labelAlpha), floatValue);
                            }
                            else
                            {
                                floatValue = EditorGUILayout.Slider(TCP2_GUI.TempContent(label),
                                    disabledByDiffuseAlpha ? 1f : floatValue, rangeLimits.x, rangeLimits.y);
                            }

                            break;
                        case ShaderPropertyType.Vector:
                        {
                            vectorValue = EditorGUILayout.Vector4Field(TCP2_GUI.TempContent(label), vectorValue);
                            break;
                        }
                        case ShaderPropertyType.Texture:
                            textureValue = EditorGUILayout.ObjectField(TCP2_GUI.TempContent(label), textureValue,
                                typeof(Texture2D), false) as Texture2D;
                            break;
                    }

                    if (disabledByDiffuseAlpha)
                    {
                        EditorGUI.EndDisabledGroup();
                        TCP2_GUI.HelpBoxLayout(
                            "The Albedo texture has an alpha channel, so this value is <b>forced at 1.0</b> (this is a limitation of Unity's terrain system).\nPlease either use a <b>texture without alpha</b>, or use <b>another terrain layer data slot</b> in the <b>Shader Properties</b> tab of the <b>Shader Generator 2</b>.",
                            MessageType.Warning);
                    }
                }

                public void TransferValueToTerrainLayer(TerrainLayer layer)
                {
                    switch (variable)
                    {
                        case TerrainLayerVariable.metallic:
                            layer.metallic = floatValue;
                            break;
                        case TerrainLayerVariable.smoothness:
                            layer.smoothness = floatValue;
                            break;
                        case TerrainLayerVariable.specular:
                            layer.specular = colorValue;
                            break;
                        case TerrainLayerVariable.diffuseRemapMin:
                            layer.diffuseRemapMin = TransferValueToVector();
                            layer.diffuseRemapMax = Vector4.one;
                            break;
                        /*
                        case TerrainLayerVariable.diffuseRemapMax:
                            layer.diffuseRemapMax = TransferValueToVector();
                            break;
                        */
                        case TerrainLayerVariable.maskMapRemapMin:
                            layer.maskMapRemapMin = TransferValueToVector();
                            layer.maskMapRemapMax = Vector4.one;
                            break;
                        /*
                        case TerrainLayerVariable.maskMapRemapMax:
                            layer.maskMapRemapMin = TransferValueToVector();
                            break;
                        */
                        case TerrainLayerVariable.maskMapTexture:
                            layer.maskMapTexture = textureValue;
                            break;
                        case TerrainLayerVariable.normalMapTexture:
                            layer.normalMapTexture = textureValue;
                            break;
                        case TerrainLayerVariable.normalScale:
                            layer.normalScale = floatValue;
                            break;
                    }
                }

                public void FetchValuesFromTerrainLayer(TerrainLayer layer)
                {
                    switch (variable)
                    {
                        case TerrainLayerVariable.metallic:
                            floatValue = layer.metallic;
                            break;
                        case TerrainLayerVariable.smoothness:
                            floatValue = layer.smoothness;
                            break;
                        case TerrainLayerVariable.specular:
                            colorValue = layer.specular;
                            break;
                        case TerrainLayerVariable.diffuseRemapMin:
                            FetchValueFromVector(layer.diffuseRemapMin);
                            break;
                        /*
                        case TerrainLayerVariable.diffuseRemapMax:
                            FetchValueFromVector(layer.diffuseRemapMax);
                            break;
                        */
                        case TerrainLayerVariable.maskMapRemapMin:
                            FetchValueFromVector(layer.maskMapRemapMin);
                            break;
                        /*
                        case TerrainLayerVariable.maskMapRemapMax:
                            FetchValueFromVector(layer.maskMapRemapMax);
                            break;
                        */
                        case TerrainLayerVariable.maskMapTexture:
                            textureValue = layer.maskMapTexture;
                            break;
                        case TerrainLayerVariable.normalMapTexture:
                            textureValue = layer.normalMapTexture;
                            break;
                        case TerrainLayerVariable.normalScale:
                            floatValue = layer.normalScale;
                            break;
                    }
                }

                // Handles color space conversions if needed
                private Vector4 TransferValueToVector()
                {
                    Vector4 vec = Vector4.zero;
                    if (type == ShaderPropertyType.Color)
                    {
                        Color colorCorrectSpace = PlayerSettings.colorSpace == ColorSpace.Gamma
                            ? colorValue.gamma
                            : colorValue;
                        float alphaCorrectSpace = PlayerSettings.colorSpace == ColorSpace.Linear
                            ? Mathf.GammaToLinearSpace(colorValue.a)
                            : colorValue.a;
                        vec.x = 1 - colorCorrectSpace.r;
                        vec.y = 1 - colorCorrectSpace.g;
                        vec.z = 1 - colorCorrectSpace.b;
                        vec.w = 1 - alphaCorrectSpace;
                    }
                    else if (type == ShaderPropertyType.Range)
                    {
                        // RGB + Float mode
                        Color colorCorrectSpace = PlayerSettings.colorSpace == ColorSpace.Gamma
                            ? colorValue.gamma
                            : colorValue;
                        vec.x = 1 - colorCorrectSpace.r;
                        vec.y = 1 - colorCorrectSpace.g;
                        vec.z = 1 - colorCorrectSpace.b;
                        vec.w = 1 - floatValue;
                    }
                    else
                    {
                        if (PlayerSettings.colorSpace == ColorSpace.Linear)
                        {
                            vec.x = 1 - Mathf.GammaToLinearSpace(vectorValue.x);
                            vec.y = 1 - Mathf.GammaToLinearSpace(vectorValue.y);
                            vec.z = 1 - Mathf.GammaToLinearSpace(vectorValue.z);
                            vec.w = 1 - Mathf.GammaToLinearSpace(vectorValue.w);
                        }
                        else
                        {
                            vec = Vector4.one - vectorValue;
                        }
                    }

                    return vec;
                }

                // Handles color space conversions if needed
                private void FetchValueFromVector(Vector4 vec)
                {
                    if (type == ShaderPropertyType.Color)
                    {
                        if (PlayerSettings.colorSpace == ColorSpace.Linear)
                        {
                            colorValue = new Color(1 - vec.x, 1 - vec.y, 1 - vec.z, 1 - vec.w);
                            colorValue.a = Mathf.LinearToGammaSpace(1 - vec.w);
                        }
                        else
                        {
                            colorValue.r = Mathf.GammaToLinearSpace(1 - vec.x);
                            colorValue.g = Mathf.GammaToLinearSpace(1 - vec.y);
                            colorValue.b = Mathf.GammaToLinearSpace(1 - vec.z);
                            colorValue.a = 1 - vec.w;
                        }
                    }
                    else if (type == ShaderPropertyType.Range)
                    {
                        // RGB + Float mode
                        if (PlayerSettings.colorSpace == ColorSpace.Linear)
                        {
                            colorValue = new Color(1 - vec.x, 1 - vec.y, 1 - vec.z, 1);
                        }
                        else
                        {
                            colorValue.r = Mathf.GammaToLinearSpace(1 - vec.x);
                            colorValue.g = Mathf.GammaToLinearSpace(1 - vec.y);
                            colorValue.b = Mathf.GammaToLinearSpace(1 - vec.z);
                        }

                        floatValue = 1 - vec.w;
                    }
                    else
                    {
                        if (PlayerSettings.colorSpace == ColorSpace.Linear)
                        {
                            vectorValue.x = Mathf.LinearToGammaSpace(1 - vec.x);
                            vectorValue.y = Mathf.LinearToGammaSpace(1 - vec.y);
                            vectorValue.z = Mathf.LinearToGammaSpace(1 - vec.z);
                            vectorValue.w = Mathf.LinearToGammaSpace(1 - vec.w);
                        }
                        else
                        {
                            vectorValue = Vector4.one - vec;
                        }
                    }
                }
            }

            private void FindUsedTerrainProperties(Shader shader)
            {
                terrainLayerProperties.Clear();
                int count = shader.GetPropertyCount();
                for (int i = 0; i < count; i++)
                {
                    string name = shader.GetPropertyName(i);
                    if (name.StartsWith("TerrainMeta_"))
                    {
                        TerrainLayerVariable variable;
                        if (!Enum.TryParse(name.Substring("TerrainMeta_".Length), true, out variable))
                            variable = TerrainLayerVariable.error;

                        if (variable == TerrainLayerVariable.error)
                        {
                            Debug.LogError("Couldn't parse terrain variable from: " + name);
                            continue;
                        }

                        ShaderPropertyType type = shader.GetPropertyType(i);
                        string label = shader.GetPropertyDescription(i);

                        // Float range: 
                        Vector2 rangeLimits = Vector2.zero;
                        if (type == ShaderPropertyType.Range) rangeLimits = shader.GetPropertyRangeLimits(i);

                        // Find out if HDR for color:
                        bool hdr = false;
                        if (type == ShaderPropertyType.Color)
                        {
                            ShaderPropertyFlags flags = shader.GetPropertyFlags(i);
                            hdr = (flags & ShaderPropertyFlags.HDR) != 0;
                        }

                        terrainLayerProperties.Add(new TerrainLayerProperty(label, type, variable, hdr, rangeLimits));
                    }
                }
            }

            private TerrainLayer currentTerrainLayer;

            bool ITerrainLayerCustomUI.OnTerrainLayerGUI(TerrainLayer terrainLayer, Terrain terrain)
            {
                if (terrain.materialTemplate == null) return false;

                bool updatedTimestamp = SG2Timestamp != ShaderGenerator2.LastCompilationTimestamp;

                if (terrainShader != terrain.materialTemplate.shader || updatedTimestamp)
                {
                    SG2Timestamp = ShaderGenerator2.LastCompilationTimestamp;
                    terrainShader = terrain.materialTemplate.shader;
                    FindUsedTerrainProperties(terrainShader);
                }

                if (currentTerrainLayer != terrainLayer || updatedTimestamp)
                {
                    currentTerrainLayer = terrainLayer;
                    foreach (TerrainLayerProperty terrainLayerProperty in terrainLayerProperties)
                        terrainLayerProperty.FetchValuesFromTerrainLayer(currentTerrainLayer);
                }

                // Header
                string label = Screen.width > 450f ? "TOONY COLORS PRO 2 - Terrain Layer" : "TCP2 - Terrain Layer";
                TCP2_GUI.HeaderBig(label);

                // Diffuse texture
                terrainLayer.diffuseTexture = EditorGUILayout.ObjectField(TCP2_GUI.TempContent("Albedo"),
                    terrainLayer.diffuseTexture, typeof(Texture2D), false) as Texture2D;
                TerrainLayerUtility.ValidateDiffuseTextureUI(terrainLayer.diffuseTexture);
                // TerrainLayerUtility.TilingSettingsUI(terrainLayer);

                TCP2_GUI.Header("Tiling Settings");
                terrainLayer.tileSize =
                    EditorGUILayout.Vector2Field(TCP2_GUI.TempContent("Size"), terrainLayer.tileSize);
                terrainLayer.tileOffset =
                    EditorGUILayout.Vector2Field(TCP2_GUI.TempContent("Offset"), terrainLayer.tileOffset);

                GUILayout.Space(8f);

                // Custom properties
                if (terrainLayerProperties.Count > 0)
                {
                    TCP2_GUI.Header("Custom Properties");
                    foreach (TerrainLayerProperty terrainLayerProperty in terrainLayerProperties)
                    {
                        EditorGUI.BeginChangeCheck();
                        {
                            terrainLayerProperty.DrawGUI(terrainLayer);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(terrainLayer, "Change terrain layer properties");
                            terrainLayerProperty.TransferValueToTerrainLayer(terrainLayer);
                        }
                    }
                }

                return true;
            }
#endif
        }
    }
}
