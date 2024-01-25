// Toony Colors Pro+Mobile 2
// (c) 2014-2023 Jean Moreno

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

internal class TCP2_MaterialInspector_PBS : ShaderGUI
{
    private enum WorkflowMode
    {
        Specular,
        Metallic,
        Dielectric
    }

    public enum BlendMode
    {
        Opaque,
        Cutout,
        Fade, // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
    }

    public enum SmoothnessMapChannel
    {
        SpecularMetallicAlpha,
        AlbedoAlpha
    }

    private static class Styles
    {
        //public static GUIStyle optionsButton = "PaneOptions";
        public static readonly GUIContent uvSetLabel = new("UV Set");
        //public static GUIContent[] uvSetOptions = { new GUIContent("UV channel 0"), new GUIContent("UV channel 1") };

        public static string emptyTootip = "";
        public static readonly GUIContent albedoText = new("Albedo", "Albedo (RGB) and Transparency (A)");
        public static readonly GUIContent alphaCutoffText = new("Alpha Cutoff", "Threshold for alpha cutoff");
        public static readonly GUIContent specularMapText = new("Specular", "Specular (RGB) and Smoothness (A)");
        public static readonly GUIContent metallicMapText = new("Metallic", "Metallic (R) and Smoothness (A)");
        public static readonly GUIContent smoothnessText = new("Smoothness", "Smoothness Value");
        public static readonly GUIContent smoothnessScaleText = new("Smoothness", "Smoothness scale factor");
        public static readonly GUIContent smoothnessMapChannelText = new("Source", "Smoothness texture and channel");
        public static readonly GUIContent highlightsText = new("Specular Highlights", "Specular Highlights");
        public static readonly GUIContent reflectionsText = new("Reflections", "Glossy Reflections");
        public static readonly GUIContent normalMapText = new("Normal Map", "Normal Map");
        public static readonly GUIContent heightMapText = new("Height Map", "Height Map (G)");
        public static readonly GUIContent occlusionText = new("Occlusion", "Occlusion (G)");
        public static readonly GUIContent emissionText = new("Emission", "Emission (RGB)");
        public static readonly GUIContent detailMaskText = new("Detail Mask", "Mask for Secondary Maps (A)");
        public static readonly GUIContent detailAlbedoText = new("Detail Albedo x2", "Albedo (RGB) multiplied by 2");
        public static readonly GUIContent detailNormalMapText = new("Normal Map", "Normal Map");

        //public static string whiteSpaceString = " ";
        public static readonly string primaryMapsText = "Main Maps";
        public static readonly string secondaryMapsText = "Secondary Maps";
        public static readonly string forwardText = "Forward Rendering Options";
        public static readonly string renderingMode = "Rendering Mode";

        public static readonly GUIContent emissiveWarning =
            new(
                "Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive.");

        //public static GUIContent emissiveColorWarning = new GUIContent ("Ensure emissive color is non-black for emission to have effect.");
        public static readonly string[] blendNames = Enum.GetNames(typeof(BlendMode));

        //public static string tcp2_HeaderText = "Toony Colors Pro 2 - Stylization";
        public static readonly string tcp2_highlightColorText = "Highlight Color";
        public static readonly string tcp2_shadowColorText = "Shadow Color";
        public static readonly GUIContent tcp2_rampText = new("Ramp Texture", "Ramp 1D Texture (R)");

        public static readonly GUIContent tcp2_rampThresholdText =
            new("Threshold", "Threshold for the separation between shadows and highlights");

        public static readonly GUIContent tcp2_rampSmoothText = new("Main Light Smoothing",
            "Main Light smoothing of the separation between shadows and highlights");

        public static readonly GUIContent tcp2_rampSmoothAddText = new("Other Lights Smoothing",
            "Additional Lights smoothing of the separation between shadows and highlights");

        public static readonly GUIContent
            tcp2_specSmoothText = new("Specular Smoothing", "Stylized Specular smoothing");

        public static readonly GUIContent tcp2_SpecBlendText =
            new("Specular Blend", "Stylized Specular contribution over regular Specular");

        public static readonly GUIContent tcp2_rimStrengthText =
            new("Fresnel Strength", "Stylized Fresnel overall strength");

        public static readonly GUIContent tcp2_rimMinText = new("Fresnel Min", "Stylized Fresnel min ramp threshold");
        public static readonly GUIContent tcp2_rimMaxText = new("Fresnel Max", "Stylized Fresnel max ramp threshold");
        public static readonly GUIContent tcp2_outlineColorText = new("Outline Color", "Color of the outline");
        public static readonly GUIContent tcp2_outlineWidthText = new("Outline Width", "Width of the outline");

        public static readonly GUIContent tcp2_normalsSourceText = new("Outline Normals Source",
            "Vertex data source to use as smoothed normals, see the Smoothed Normals Utility in the documentation");

        public static readonly GUIContent tcp2_uvDataTypeText = new("UV Data Type",
            "Defines how the smoothed normals are encoded in the selected UV channel");

        public static readonly string tcp2_TexLodText = "Outline Texture LOD";
        public static readonly string tcp2_ZSmoothText = "ZSmooth Value";
        public static readonly string tcp2_Offset1Text = "Offset Factor";
        public static readonly string tcp2_Offset2Text = "Offset Units";
        public static readonly string tcp2_srcBlendOutlineText = "Source Factor";
        public static readonly string tcp2_dstBlendOutlineText = "Destination Factor";
    }

    private MaterialProperty blendMode;
    private MaterialProperty albedoMap;
    private MaterialProperty albedoColor;
    private MaterialProperty alphaCutoff;
    private MaterialProperty specularMap;
    private MaterialProperty specularColor;
    private MaterialProperty metallicMap;
    private MaterialProperty metallic;
    private MaterialProperty smoothness;
    private MaterialProperty smoothnessScale;
    private MaterialProperty smoothnessMapChannel;
    private MaterialProperty highlights;
    private MaterialProperty reflections;
    private MaterialProperty bumpScale;
    private MaterialProperty bumpMap;
    private MaterialProperty occlusionStrength;
    private MaterialProperty occlusionMap;
    private MaterialProperty heigtMapScale;
    private MaterialProperty heightMap;
    private MaterialProperty emissionColorForRendering;
    private MaterialProperty emissionMap;
    private MaterialProperty detailMask;
    private MaterialProperty detailAlbedoMap;
    private MaterialProperty detailNormalMapScale;
    private MaterialProperty detailNormalMap;
    private MaterialProperty uvSetSecondary;

    //TCP2
    private MaterialProperty tcp2_highlightColor;
    private MaterialProperty tcp2_shadowColor;
    private MaterialProperty tcp2_TCP2_DISABLE_WRAPPED_LIGHT;
    private MaterialProperty tcp2_TCP2_RAMPTEXT;
    private MaterialProperty tcp2_ramp;
    private MaterialProperty tcp2_rampThreshold;
    private MaterialProperty tcp2_rampSmooth;
    private MaterialProperty tcp2_rampSmoothAdd;
    private MaterialProperty tcp2_SPEC_TOON;
    private MaterialProperty tcp2_specSmooth;
    private MaterialProperty tcp2_SpecBlend;
    private MaterialProperty tcp2_STYLIZED_FRESNEL;
    private MaterialProperty tcp2_rimStrength;
    private MaterialProperty tcp2_rimMin;
    private MaterialProperty tcp2_rimMax;
    private MaterialProperty tcp2_outlineColor;
    private MaterialProperty tcp2_outlineWidth;
    private MaterialProperty tcp2_TCP2_OUTLINE_TEXTURED;
    private MaterialProperty tcp2_TexLod;
    private MaterialProperty tcp2_TCP2_OUTLINE_CONST_SIZE;
    private MaterialProperty tcp2_TCP2_ZSMOOTH_ON;
    private MaterialProperty tcp2_ZSmooth;
    private MaterialProperty tcp2_Offset1;
    private MaterialProperty tcp2_Offset2;
    private MaterialProperty tcp2_srcBlendOutline;
    private MaterialProperty tcp2_dstBlendOutline;
    private MaterialProperty tcp2_normalsSource;
    private MaterialProperty tcp2_uvDataType;
    private static bool expandStandardProperties = true;
    private static bool expandTCP2Properties = true;

    private readonly string[] outlineNormalsKeywords =
        { "TCP2_NONE", "TCP2_COLORS_AS_NORMALS", "TCP2_TANGENT_AS_NORMALS", "TCP2_UV2_AS_NORMALS" };

    private MaterialEditor m_MaterialEditor;
    private WorkflowMode m_WorkflowMode = WorkflowMode.Specular;
#if !UNITY_2018_1_OR_NEWER
	readonly ColorPickerHDRConfig m_ColorPickerHDRConfig = new ColorPickerHDRConfig(0f, 99f, 1/99f, 3f);
#endif

    private bool m_FirstTimeApply = true;

    public void FindProperties(MaterialProperty[] props)
    {
        blendMode = FindProperty("_Mode", props);
        albedoMap = FindProperty("_MainTex", props);
        albedoColor = FindProperty("_Color", props);
        alphaCutoff = FindProperty("_Cutoff", props);
        specularMap = FindProperty("_SpecGlossMap", props, false);
        specularColor = FindProperty("_SpecColor", props, false);
        metallicMap = FindProperty("_MetallicGlossMap", props, false);
        metallic = FindProperty("_Metallic", props, false);
        if (specularMap != null && specularColor != null)
            m_WorkflowMode = WorkflowMode.Specular;
        else if (metallicMap != null && metallic != null)
            m_WorkflowMode = WorkflowMode.Metallic;
        else
            m_WorkflowMode = WorkflowMode.Dielectric;
        smoothness = FindProperty("_Glossiness", props);
        smoothnessScale = FindProperty("_GlossMapScale", props, false);
        smoothnessMapChannel = FindProperty("_SmoothnessTextureChannel", props, false);
        highlights = FindProperty("_SpecularHighlights", props, false);
        reflections = FindProperty("_GlossyReflections", props, false);
        bumpScale = FindProperty("_BumpScale", props);
        bumpMap = FindProperty("_BumpMap", props);
        heigtMapScale = FindProperty("_Parallax", props);
        heightMap = FindProperty("_ParallaxMap", props);
        occlusionStrength = FindProperty("_OcclusionStrength", props);
        occlusionMap = FindProperty("_OcclusionMap", props);
        emissionColorForRendering = FindProperty("_EmissionColor", props);
        emissionMap = FindProperty("_EmissionMap", props);
        detailMask = FindProperty("_DetailMask", props);
        detailAlbedoMap = FindProperty("_DetailAlbedoMap", props);
        detailNormalMapScale = FindProperty("_DetailNormalMapScale", props);
        detailNormalMap = FindProperty("_DetailNormalMap", props);
        uvSetSecondary = FindProperty("_UVSec", props);

        //TCP2
        tcp2_highlightColor = FindProperty("_HColor", props);
        tcp2_shadowColor = FindProperty("_SColor", props);

        tcp2_rampThreshold = FindProperty("_RampThreshold", props);
        tcp2_rampSmooth = FindProperty("_RampSmooth", props);
        tcp2_rampSmoothAdd = FindProperty("_RampSmoothAdd", props);
        tcp2_TCP2_DISABLE_WRAPPED_LIGHT = FindProperty("_TCP2_DISABLE_WRAPPED_LIGHT", props);
        tcp2_TCP2_RAMPTEXT = FindProperty("_TCP2_RAMPTEXT", props);
        tcp2_ramp = FindProperty("_Ramp", props);

        tcp2_SPEC_TOON = FindProperty("_TCP2_SPEC_TOON", props);
        tcp2_specSmooth = FindProperty("_SpecSmooth", props);
        tcp2_SpecBlend = FindProperty("_SpecBlend", props);

        tcp2_STYLIZED_FRESNEL = FindProperty("_TCP2_STYLIZED_FRESNEL", props);
        tcp2_rimStrength = FindProperty("_RimStrength", props);
        tcp2_rimMin = FindProperty("_RimMin", props);
        tcp2_rimMax = FindProperty("_RimMax", props);

        tcp2_outlineColor = FindProperty("_OutlineColor", props, false);
        tcp2_outlineWidth = FindProperty("_Outline", props, false);
        tcp2_TCP2_OUTLINE_TEXTURED = FindProperty("_TCP2_OUTLINE_TEXTURED", props, false);
        tcp2_TexLod = FindProperty("_TexLod", props, false);
        tcp2_TCP2_OUTLINE_CONST_SIZE = FindProperty("_TCP2_OUTLINE_CONST_SIZE", props, false);
        tcp2_TCP2_ZSMOOTH_ON = FindProperty("_TCP2_ZSMOOTH_ON", props, false);
        tcp2_ZSmooth = FindProperty("_ZSmooth", props, false);
        tcp2_Offset1 = FindProperty("_Offset1", props, false);
        tcp2_Offset2 = FindProperty("_Offset2", props, false);
        tcp2_srcBlendOutline = FindProperty("_SrcBlendOutline", props, false);
        tcp2_dstBlendOutline = FindProperty("_DstBlendOutline", props, false);
        tcp2_normalsSource = FindProperty("_NormalsSource", props, false);
        tcp2_uvDataType = FindProperty("_NormalsUVType", props, false);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
        m_MaterialEditor = materialEditor;
        Material material = materialEditor.target as Material;

        // Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
        // material to a standard shader.
        // Do this before any GUI code has been issued to prevent layout issues in subsequent GUILayout statements (case 780071)
        if (m_FirstTimeApply)
        {
            MaterialChanged(material, m_WorkflowMode);
            m_FirstTimeApply = false;
        }

        ShaderPropertiesGUI(material);

#if UNITY_5_5_OR_NEWER
        materialEditor.RenderQueueField();
#endif
#if UNITY_5_6_OR_NEWER
        materialEditor.EnableInstancingField();
#endif
    }

    private bool outlineShouldChange;
    private bool showOutline;
    private bool showOutlineBlended;

    public void ShaderPropertiesGUI(Material material)
    {
        // Use default labelWidth
        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 0f;

        // Detect any changes to the material
        EditorGUI.BeginChangeCheck();
        {
            BlendModePopup();

            GUILayout.Space(8f);
            expandStandardProperties = GUILayout.Toggle(expandStandardProperties, "STANDARD PROPERTIES",
                EditorStyles.toolbarButton);
            if (expandStandardProperties)
            {
                //Background
                Rect vertRect = EditorGUILayout.BeginVertical();
                vertRect.xMax += 2;
                vertRect.xMin--;
                GUI.Box(vertRect, "", "RL Background");
                GUILayout.Space(4f);

                // Primary properties
                GUILayout.Label(Styles.primaryMapsText, EditorStyles.boldLabel);
                DoAlbedoArea(material);
                DoSpecularMetallicArea();
                m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap,
                    bumpMap.textureValue != null ? bumpScale : null);
                m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap,
                    heightMap.textureValue != null ? heigtMapScale : null);
                m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionText, occlusionMap,
                    occlusionMap.textureValue != null ? occlusionStrength : null);
                DoEmissionArea(material);
                m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
                if (EditorGUI.EndChangeCheck())
                    emissionMap.textureScaleAndOffset = albedoMap.textureScaleAndOffset;
                // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake

                EditorGUILayout.Space();

                // Secondary properties
                GUILayout.Label(Styles.secondaryMapsText, EditorStyles.boldLabel);
                m_MaterialEditor.TexturePropertySingleLine(Styles.detailAlbedoText, detailAlbedoMap);
                m_MaterialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, detailNormalMap,
                    detailNormalMapScale);
                m_MaterialEditor.TextureScaleOffsetProperty(detailAlbedoMap);
                m_MaterialEditor.ShaderProperty(uvSetSecondary, Styles.uvSetLabel.text);

                // Third properties
                GUILayout.Label(Styles.forwardText, EditorStyles.boldLabel);
                if (highlights != null)
                    m_MaterialEditor.ShaderProperty(highlights, Styles.highlightsText);
                if (reflections != null)
                    m_MaterialEditor.ShaderProperty(reflections, Styles.reflectionsText);

                GUILayout.Space(8f);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            //----------------------------------------------------------------
            //    TOONY COLORS PRO 2

            int outlineType = (int)((Material)m_MaterialEditor.target).GetFloat("_EnableOutline");
            bool useOutline = outlineType >= 1;
            bool useOutlineBlended = outlineType == 2;

            bool hasOutlineShader = tcp2_outlineWidth != null;
            bool hasOutlineBlendedShader = tcp2_srcBlendOutline != null;

            expandTCP2Properties =
                GUILayout.Toggle(expandTCP2Properties, "TOONY COLORS PRO 2", EditorStyles.toolbarButton);
            if (expandTCP2Properties)
            {
                //Background
                Rect vertRect = EditorGUILayout.BeginVertical();
                vertRect.xMax += 2;
                vertRect.xMin--;
                GUI.Box(vertRect, "", "RL Background");
                GUILayout.Space(4f);

                GUILayout.Label("Base Properties", EditorStyles.boldLabel);
                m_MaterialEditor.ColorProperty(tcp2_highlightColor, Styles.tcp2_highlightColorText);
                m_MaterialEditor.ColorProperty(tcp2_shadowColor, Styles.tcp2_shadowColorText);

                // Wrapped Lighting
                m_MaterialEditor.ShaderProperty(tcp2_TCP2_DISABLE_WRAPPED_LIGHT, "Disable Wrapped Lighting");

                // Ramp Texture / Threshold
                m_MaterialEditor.ShaderProperty(tcp2_TCP2_RAMPTEXT, "Use Ramp Texture");
                if (tcp2_TCP2_RAMPTEXT.floatValue > 0)
                {
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(tcp2_ramp, Styles.tcp2_rampText);
                    //m_MaterialEditor.TexturePropertySingleLine(Styles.tcp2_rampText, tcp2_ramp);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    m_MaterialEditor.ShaderProperty(tcp2_rampThreshold, Styles.tcp2_rampThresholdText.text, 1);
                    m_MaterialEditor.ShaderProperty(tcp2_rampSmooth, Styles.tcp2_rampSmoothText.text, 1);
                    m_MaterialEditor.ShaderProperty(tcp2_rampSmoothAdd, Styles.tcp2_rampSmoothAddText.text, 1);
                }

                EditorGUILayout.Space();
                GUILayout.Label("Stylization Options", EditorStyles.boldLabel);

                // Stylized Specular
                m_MaterialEditor.ShaderProperty(tcp2_SPEC_TOON, "Stylized Specular");
                if (tcp2_SPEC_TOON.floatValue > 0)
                {
                    m_MaterialEditor.ShaderProperty(tcp2_specSmooth, Styles.tcp2_specSmoothText.text, 1);
                    m_MaterialEditor.ShaderProperty(tcp2_SpecBlend, Styles.tcp2_SpecBlendText.text, 1);

                    EditorGUILayout.Space();
                }

                //Stylized Fresnel
                m_MaterialEditor.ShaderProperty(tcp2_STYLIZED_FRESNEL, "Stylized Fresnel");
                if (tcp2_STYLIZED_FRESNEL.floatValue > 0)
                {
                    m_MaterialEditor.ShaderProperty(tcp2_rimStrength, Styles.tcp2_rimStrengthText.text, 1);
                    m_MaterialEditor.ShaderProperty(tcp2_rimMin, Styles.tcp2_rimMinText.text, 1);
                    m_MaterialEditor.ShaderProperty(tcp2_rimMax, Styles.tcp2_rimMaxText.text, 1);

                    EditorGUILayout.Space();
                }

                //Outline
                bool useOutlineNew =
                    EditorGUILayout.Toggle(new GUIContent("Outline", "Enable mesh-based outline"), useOutline);
                if (useOutlineNew != useOutline)
                {
                    outlineShouldChange = true;
                    showOutline = useOutlineNew;
                    showOutlineBlended = hasOutlineBlendedShader;
                }

                if (useOutline && hasOutlineShader)
                {
                    //Outline base props
                    m_MaterialEditor.ShaderProperty(tcp2_outlineColor, Styles.tcp2_outlineColorText.text, 1);
                    m_MaterialEditor.ShaderProperty(tcp2_outlineWidth, Styles.tcp2_outlineWidthText.text, 1);

                    m_MaterialEditor.ShaderProperty(tcp2_TCP2_OUTLINE_TEXTURED, "Textured Outline", 1);
                    if (tcp2_TCP2_OUTLINE_TEXTURED.floatValue > 0)
                        m_MaterialEditor.ShaderProperty(tcp2_TexLod, Styles.tcp2_TexLodText, 1);

                    m_MaterialEditor.ShaderProperty(tcp2_TCP2_OUTLINE_CONST_SIZE, "Constant Screen Size", 1);
                    m_MaterialEditor.ShaderProperty(tcp2_TCP2_ZSMOOTH_ON, "Z Smooth", 1);
                    if (tcp2_TCP2_ZSMOOTH_ON.floatValue > 0)
                    {
                        m_MaterialEditor.ShaderProperty(tcp2_ZSmooth, Styles.tcp2_ZSmoothText, 2);
                        m_MaterialEditor.ShaderProperty(tcp2_Offset1, Styles.tcp2_Offset1Text, 2);
                        m_MaterialEditor.ShaderProperty(tcp2_Offset2, Styles.tcp2_Offset2Text, 2);
                    }

                    //Blended Outline
                    EditorGUI.indentLevel++;
                    bool useOutlineBlendedNew = EditorGUILayout.Toggle(
                        new GUIContent("Blended Outline", "Enable blended outline rather than opaque"),
                        useOutlineBlended);
                    if (useOutlineBlendedNew != useOutlineBlended)
                    {
                        outlineShouldChange = true;
                        showOutline = useOutline;
                        showOutlineBlended = useOutlineBlendedNew;
                    }

                    if (useOutlineBlended && hasOutlineBlendedShader)
                    {
                        EditorGUI.indentLevel++;
                        UnityEngine.Rendering.BlendMode blendSrc =
                            (UnityEngine.Rendering.BlendMode)tcp2_srcBlendOutline.floatValue;
                        UnityEngine.Rendering.BlendMode blendDst =
                            (UnityEngine.Rendering.BlendMode)tcp2_dstBlendOutline.floatValue;
                        EditorGUI.BeginChangeCheck();
                        blendSrc = (UnityEngine.Rendering.BlendMode)EditorGUILayout.EnumPopup(
                            Styles.tcp2_srcBlendOutlineText, blendSrc);
                        blendDst = (UnityEngine.Rendering.BlendMode)EditorGUILayout.EnumPopup(
                            Styles.tcp2_dstBlendOutlineText, blendDst);
                        if (EditorGUI.EndChangeCheck())
                        {
                            tcp2_srcBlendOutline.floatValue = (float)blendSrc;
                            tcp2_dstBlendOutline.floatValue = (float)blendDst;
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;

                    // Outline Normals
                    m_MaterialEditor.ShaderProperty(tcp2_normalsSource, Styles.tcp2_normalsSourceText.text, 1);
                    m_MaterialEditor.ShaderProperty(tcp2_uvDataType, Styles.tcp2_uvDataTypeText.text, 1);
                }

                GUILayout.Space(8f);
                GUILayout.EndVertical();

                // TCP2 End
                //----------------------------------------------------------------
            }

            GUILayout.Space(10f);

            if ((hasOutlineShader && !useOutline) || (!hasOutlineShader && useOutline))
            {
                outlineShouldChange = true;
                showOutline = hasOutlineShader;
                showOutlineBlended = hasOutlineBlendedShader;
            }

            //TCP2: set correct shader based on outline properties
            if (outlineShouldChange && Event.current.type == EventType.Repaint)
            {
                outlineShouldChange = false;
                SetTCP2Shader(showOutline, showOutlineBlended);
            }
        }
        if (EditorGUI.EndChangeCheck())
            foreach (Object obj in blendMode.targets)
                MaterialChanged((Material)obj, m_WorkflowMode);

        EditorGUIUtility.labelWidth = labelWidth;
    }

    private void DetermineWorkflow(MaterialProperty[] props)
    {
        if (FindProperty("_SpecGlossMap", props, false) != null && FindProperty("_SpecColor", props, false) != null)
            m_WorkflowMode = WorkflowMode.Specular;
        else if (FindProperty("_MetallicGlossMap", props, false) != null &&
                 FindProperty("_Metallic", props, false) != null)
            m_WorkflowMode = WorkflowMode.Metallic;
        else
            m_WorkflowMode = WorkflowMode.Dielectric;
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        // _Emission property is lost after assigning Standard shader to the material
        // thus transfer it before assigning the new shader
        if (material.HasProperty("_Emission")) material.SetColor("_EmissionColor", material.GetColor("_Emission"));

        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
        {
            SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));
            return;
        }

        BlendMode blendMode = BlendMode.Opaque;
        if (oldShader.name.Contains("/Transparent/Cutout/"))
            blendMode = BlendMode.Cutout;
        else if (oldShader.name.Contains("/Transparent/"))
            // NOTE: legacy shaders did not provide physically based transparency
            // therefore Fade mode
            blendMode = BlendMode.Fade;
        material.SetFloat("_Mode", (float)blendMode);

        DetermineWorkflow(MaterialEditor.GetMaterialProperties(new[] { material }));
        MaterialChanged(material, m_WorkflowMode);
    }

    private void BlendModePopup()
    {
        EditorGUI.showMixedValue = blendMode.hasMixedValue;
        BlendMode mode = (BlendMode)blendMode.floatValue;

        EditorGUI.BeginChangeCheck();
        mode = (BlendMode)EditorGUILayout.Popup(Styles.renderingMode, (int)mode, Styles.blendNames);
        if (EditorGUI.EndChangeCheck())
        {
            m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
            blendMode.floatValue = (float)mode;
        }

        EditorGUI.showMixedValue = false;
    }

    private void DoAlbedoArea(Material material)
    {
        m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);
        if ((BlendMode)material.GetFloat("_Mode") == BlendMode.Cutout)
            m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text,
                MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);
    }

    private void DoEmissionArea(Material material)
    {
        bool showHelpBox = !HasValidEmissiveKeyword(material);

        bool hadEmissionTexture = emissionMap.textureValue != null;

        // Texture and HDR color controls
#if !UNITY_2018_1_OR_NEWER
		m_MaterialEditor.TexturePropertyWithHDRColor(Styles.emissionText, emissionMap, emissionColorForRendering, m_ColorPickerHDRConfig, false);
#else
        m_MaterialEditor.TexturePropertyWithHDRColor(Styles.emissionText, emissionMap, emissionColorForRendering,
            false);
#endif

        // If texture was assigned and color was black set color to white
        float brightness = emissionColorForRendering.colorValue.maxColorComponent;
        if (emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f)
            emissionColorForRendering.colorValue = Color.white;

        // Emission for GI?
        m_MaterialEditor.LightmapEmissionProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel + 0);

        if (showHelpBox) EditorGUILayout.HelpBox(Styles.emissiveWarning.text, MessageType.Warning);
    }

    private void DoSpecularMetallicArea()
    {
        bool hasGlossMap = false;
        if (m_WorkflowMode == WorkflowMode.Specular)
        {
            hasGlossMap = specularMap.textureValue != null;
            m_MaterialEditor.TexturePropertySingleLine(Styles.specularMapText, specularMap,
                hasGlossMap ? null : specularColor);
        }
        else if (m_WorkflowMode == WorkflowMode.Metallic)
        {
            hasGlossMap = metallicMap.textureValue != null;
            m_MaterialEditor.TexturePropertySingleLine(Styles.metallicMapText, metallicMap,
                hasGlossMap ? null : metallic);
        }

        bool showSmoothnessScale = hasGlossMap;
        if (smoothnessMapChannel != null)
        {
            int smoothnessChannel = (int)smoothnessMapChannel.floatValue;
            if (smoothnessChannel == (int)SmoothnessMapChannel.AlbedoAlpha)
                showSmoothnessScale = true;
        }

        int indentation = 2; // align with labels of texture properties
        m_MaterialEditor.ShaderProperty(showSmoothnessScale ? smoothnessScale : smoothness,
            showSmoothnessScale ? Styles.smoothnessScaleText : Styles.smoothnessText, indentation);

        //++indentation;
        if (smoothnessMapChannel != null)
            m_MaterialEditor.ShaderProperty(smoothnessMapChannel, Styles.smoothnessMapChannelText, indentation);
    }

    public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
    {
        switch (blendMode)
        {
            case BlendMode.Opaque:
                material.SetOverrideTag("RenderType", "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                break;
            case BlendMode.Cutout:
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.AlphaTest;
                break;
            case BlendMode.Fade:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
                break;
            case BlendMode.Transparent:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
                break;
        }
    }

    private static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
    {
        int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
        if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
            return SmoothnessMapChannel.AlbedoAlpha;
        return SmoothnessMapChannel.SpecularMetallicAlpha;
    }

    private static bool ShouldEmissionBeEnabled(Material mat, Color color)
    {
        bool realtimeEmission = (mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeEmissive) > 0;
        return color.maxColorComponent > 0.1f / 255.0f || realtimeEmission;
    }

    private static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
    {
        // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
        // (MaterialProperty value might come from renderer material property block)
        SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") || material.GetTexture("_DetailNormalMap"));
        if (workflowMode == WorkflowMode.Specular)
            SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
        else if (workflowMode == WorkflowMode.Metallic)
            SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
        SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
        SetKeyword(material, "_DETAIL_MULX2",
            material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

        bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled(material, material.GetColor("_EmissionColor"));
        SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

        if (material.HasProperty("_SmoothnessTextureChannel"))
            SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
                GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);

        // Setup lightmap emissive flags
        MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
        if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive |
                      MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
        {
            flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            if (!shouldEmissionBeEnabled)
                flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            material.globalIlluminationFlags = flags;
        }
    }

    private bool HasValidEmissiveKeyword(Material material)
    {
        // Material animation might be out of sync with the material keyword.
        // So if the emission support is disabled on the material, but the property blocks have a value that requires it, then we need to show a warning.
        // (note: (Renderer MaterialPropertyBlock applies its values to emissionColorForRendering))
        bool hasEmissionKeyword = material.IsKeywordEnabled("_EMISSION");
        if (!hasEmissionKeyword && ShouldEmissionBeEnabled(material, emissionColorForRendering.colorValue))
            return false;
        return true;
    }

    private static void MaterialChanged(Material material, WorkflowMode workflowMode)
    {
        SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));

        SetMaterialKeywords(material, workflowMode);
    }

    private static void SetKeyword(Material m, string keyword, bool state)
    {
        if (state)
            m.EnableKeyword(keyword);
        else
            m.DisableKeyword(keyword);
    }

    //TCP2 Tools

    private int GetOutlineNormalsIndex()
    {
        if (m_MaterialEditor.target == null || !(m_MaterialEditor.target is Material))
            return 0;

        for (int i = 0; i < outlineNormalsKeywords.Length; i++)
            if ((m_MaterialEditor.target as Material).IsKeywordEnabled(outlineNormalsKeywords[i]))
                return i;
        return 0;
    }

    private void SetTCP2Shader(bool useOutline, bool blendedOutline)
    {
        bool specular = m_WorkflowMode == WorkflowMode.Specular;
        string shaderPath = null;

        if (!useOutline)
        {
            if (specular)
                shaderPath = "Toony Colors Pro 2/Standard PBS (Specular)";
            else
                shaderPath = "Toony Colors Pro 2/Standard PBS";
        }
        else if (blendedOutline)
        {
            if (specular)
                shaderPath = "Hidden/Toony Colors Pro 2/Standard PBS Outline Blended (Specular)";
            else
                shaderPath = "Hidden/Toony Colors Pro 2/Standard PBS Outline Blended";
        }
        else
        {
            if (specular)
                shaderPath = "Hidden/Toony Colors Pro 2/Standard PBS Outline (Specular)";
            else
                shaderPath = "Hidden/Toony Colors Pro 2/Standard PBS Outline";
        }

        Shader shader = Shader.Find(shaderPath);
        if (shader != null)
        {
            if ((m_MaterialEditor.target as Material).shader != shader) m_MaterialEditor.SetShader(shader, false);

            foreach (Object obj in m_MaterialEditor.targets)
                if (obj is Material)
                {
                    if (useOutline && blendedOutline)
                        (obj as Material).SetFloat("_EnableOutline", 2);
                    else if (useOutline)
                        (obj as Material).SetFloat("_EnableOutline", 1);
                    else
                        (obj as Material).SetFloat("_EnableOutline", 0);
                }

            m_MaterialEditor.Repaint();
            SceneView.RepaintAll();
        }
        else
        {
            EditorApplication.Beep();
            Debug.LogError("Toony Colors Pro 2: Couldn't find the following shader:\n\"" + shaderPath + "\"");
        }
    }
}
