// Toony Colors Pro+Mobile 2
// (c) 2014-2023 Jean Moreno

//Enable this to display the default Inspector (in case the custom Inspector is broken)
//#define SHOW_DEFAULT_INSPECTOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ToonyColorsPro.CustomShaderImporter;
using ToonyColorsPro.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Custom material inspector for generated shader

namespace ToonyColorsPro
{
    namespace ShaderGenerator
    {
        public class MaterialInspector_Hybrid : ShaderGUI
        {
            public enum MobileMode
            {
                Disabled = 0,
                Enabled = 1
            }

            public enum RenderingMode
            {
                Opaque,
                Fade,
                Transparent
            }

            private const string PROP_MOBILE_MODE = "_UseMobileMode";
            private const string KEYWORD_MOBILE_MODE = "TCP2_MOBILE";
            private const string PROP_RENDERING_MODE = "_RenderingMode";
            private const string PROP_ZWRITE = "_ZWrite";
            private const string PROP_BLEND_SRC = "_SrcBlend";
            private const string PROP_BLEND_DST = "_DstBlend";
            private const string PROP_CULLING = "_Cull";
            private const string PROP_OUTLINE = "_UseOutline";
            private const string PROP_OUTLINE_LAST = "_IndirectIntensityOutline";
            private const string PASS_OUTLINE_URP = "Outline";
            private const string PASS_OUTLINE_BUILTIN = "Always";

            private const string OUTLINE_URP_DOCUMENTATION =
                "https://jeanmoreno.com/unity/toonycolorspro/doc/shader_generator_2#templates/urp/outlineandsilhouettepassesinurp";

            //--------------------------------------------------------------------------------------------------

            private const string kGuiCommandPrefix = "//#";
            private const string kGC_IfURP = "IF_URP";
            private const string kGC_IfNotURP = "IF_NOT_URP";
            private const string kGC_IfKeyword = "IF_KEYWORD";
            private const string kGC_IfProperty = "IF_PROPERTY";
            private const string kGC_EndIf = "END_IF";
            private const string kGC_IfDisableKeyword = "IF_KEYWORD_DISABLE";
            private const string kGC_IfDisableProperty = "IF_PROPERTY_DISABLE";
            private const string kGC_EndIfDisable = "END_IF_DISABLE";
            private const string kGC_Else = "ELSE";
            private const string kGC_HelpBox = "HELP_BOX";
            private const string kGC_Label = "LABEL";
            public static bool _isURP;
            public static bool _isMobile;

            //--------------------------------------------------------------------------------------------------

            // Set by custom conditions (IF_KEYWORD, IF_PROPERTY) to tell if the next properties should be visible
            private static readonly Stack<bool> ShowStack = new();

            // Set by custom conditions (IF_PROPERTY_DISABLE) to tell if the next properties should be disabled
            private static readonly Stack<bool> DisableStack = new();
            private MaterialEditor _materialEditor;
            private MaterialProperty[] _properties;

            private readonly Dictionary<int, List<GUICommand>> guiCommands = new();

            private bool initialized;
            private ulong lastTimestamp;

            //--------------------------------------------------------------------------------------------------

            private float[] materialsOutlineMapping;

            private readonly string mobileModeHelp =
                "'Mobile Mode' makes the shader faster by disabling some of the features, and doing more calculations in the vertex shader at the expense of precision.";

            private AssetImporter shaderImporter;
            private readonly Dictionary<int, string[]> splitLabels = new();

            //Properties
            private Material targetMaterial => _materialEditor == null ? null : _materialEditor.target as Material;

            public static bool ShowNextProperty { get; private set; }
            public static bool DisableNextProperty { get; private set; }

            public static void PushShowProperty(bool value)
            {
                ShowStack.Push(ShowNextProperty);
                ShowNextProperty &= value;
            }

            public static void PopShowProperty()
            {
                ShowNextProperty = ShowStack.Pop();
            }

            public static void PushDisableProperty(bool value)
            {
                DisableStack.Push(DisableNextProperty);
                DisableNextProperty |= value;
            }

            public static void PopDisableProperty()
            {
                DisableNextProperty = DisableStack.Pop();
            }

            private void Initialize(MaterialEditor editor, MaterialProperty[] properties, bool force)
            {
                if ((!initialized || force) && editor != null)
                {
                    initialized = true;

                    // Check for outline in shader name

                    IterateMaterials(mat => UpdateOutlineProp(mat, mat.shader.name.Contains("Outline")));

                    // Split labels

                    splitLabels.Clear();
                    for (int i = 0; i < properties.Length; i++)
                        if (properties[i].displayName.Contains("#"))
                            splitLabels.Add(i, properties[i].displayName.Split('#'));

                    // Gui commands

                    guiCommands.Clear();

                    //Find the shader and parse the source to find special comments that will organize the GUI
                    //It's hackish, but at least it allows any character to be used (unlike material property drawers/decorators) and can be used along with property drawers

                    List<Material> materials = new();
                    foreach (Object o in editor.targets)
                    {
                        Material m = o as Material;
                        if (m != null)
                            materials.Add(m);
                    }

                    if (materials.Count > 0 && materials[0].shader != null)
                    {
                        string path = AssetDatabase.GetAssetPath(materials[0].shader);
                        //get asset importer
                        shaderImporter = AssetImporter.GetAtPath(path);
                        if (shaderImporter != null) lastTimestamp = shaderImporter.assetTimeStamp;

                        // Get source code lines to parse comment-based inspector
                        string[] lines;
                        if (shaderImporter != null && shaderImporter is TCP2_ShaderImporter)
                        {
                            // .tcp2shader file, parse the generated source for comment-based inspector
                            lines = ((TCP2_ShaderImporter)shaderImporter).shaderSourceCode.Split(new[] { "\r", "\n" },
                                StringSplitOptions.None);
                        }
                        else
                        {
                            //remove 'Assets' and replace with OS path
                            path = Application.dataPath + path.Substring(6);
                            //convert to cross-platform path
                            path = path.Replace('/', Path.DirectorySeparatorChar);
                            //open file for reading
                            lines = File.ReadAllLines(path);
                        }

                        bool insideProperties = false;
                        //regex pattern to find properties, as they need to be counted so that
                        //special commands can be inserted at the right position when enumerating them
                        Regex regex = new("[a-zA-Z0-9_]+\\s*\\(\"[a-zA-Z0-9#\\-() ]+\"[^\\)]*\\)");
                        int propertyCount = 0;
                        bool insideCommentBlock = false;

                        foreach (string l in lines)
                        {
                            string line = l.TrimStart();

                            if (insideProperties)
                            {
                                bool isComment = line.StartsWith("//");

                                if (line.Contains("/*"))
                                    insideCommentBlock = true;
                                if (line.Contains("*/"))
                                    insideCommentBlock = false;

                                //finished properties block?
                                if (line.StartsWith("}"))
                                    break;

                                //comment
                                if (line.StartsWith(kGuiCommandPrefix))
                                {
                                    string fullCommand = line.Substring(kGuiCommandPrefix.Length).TrimStart();
                                    int spaceIndex = fullCommand.IndexOf(' ');
                                    string command = spaceIndex >= 0
                                        ? fullCommand.Substring(0, spaceIndex)
                                        : fullCommand;

                                    //space
                                    if (string.IsNullOrEmpty(command))
                                    {
                                        AddGUICommand(propertyCount, new GC_Space());
                                    }
                                    //separator
                                    else if (command.StartsWith("---"))
                                    {
                                        AddGUICommand(propertyCount, new GC_Separator());
                                    }
                                    //separator
                                    else if (command.StartsWith("==="))
                                    {
                                        AddGUICommand(propertyCount, new GC_SeparatorDouble());
                                    }
                                    //if URP
                                    else if (command == kGC_IfURP)
                                    {
                                        AddGUICommand(propertyCount, new GC_IfURP());
                                    }
                                    //if not URP
                                    else if (command == kGC_IfNotURP)
                                    {
                                        AddGUICommand(propertyCount, new GC_IfURP(false));
                                    }
                                    //if keyword
                                    else if (command == kGC_IfKeyword)
                                    {
                                        string expr = fullCommand.Substring(fullCommand.LastIndexOf(kGC_IfKeyword) +
                                                                            kGC_IfKeyword.Length + 1);
                                        AddGUICommand(propertyCount,
                                            new GC_IfKeyword { expression = expr, materials = materials.ToArray() });
                                    }
                                    //if disable keyword
                                    else if (command == kGC_IfDisableKeyword)
                                    {
                                        string expr = fullCommand.Substring(
                                            fullCommand.LastIndexOf(kGC_IfDisableKeyword) +
                                            kGC_IfDisableKeyword.Length + 1);
                                        AddGUICommand(propertyCount,
                                            new GC_IfDisableKeyword
                                                { expression = expr, materials = materials.ToArray() });
                                    }
                                    //if disable property
                                    else if (command == kGC_IfDisableProperty)
                                    {
                                        string expr = fullCommand.Substring(
                                            fullCommand.LastIndexOf(kGC_IfDisableProperty) +
                                            kGC_IfDisableProperty.Length + 1);
                                        AddGUICommand(propertyCount,
                                            new GC_IfDisableProperty
                                                { expression = expr, materials = materials.ToArray() });
                                    }
                                    //if property
                                    else if (command == kGC_IfProperty)
                                    {
                                        string expr = fullCommand.Substring(fullCommand.LastIndexOf(kGC_IfProperty) +
                                                                            kGC_IfProperty.Length + 1);
                                        AddGUICommand(propertyCount,
                                            new GC_IfProperty { expression = expr, materials = materials.ToArray() });
                                    }
                                    //end if disable
                                    else if (command == kGC_EndIfDisable)
                                    {
                                        AddGUICommand(propertyCount, new GC_EndIfDisable());
                                    }
                                    //end if
                                    else if (command == kGC_EndIf)
                                    {
                                        AddGUICommand(propertyCount, new GC_EndIf());
                                    }
                                    //else
                                    else if (command == kGC_Else)
                                    {
                                        AddGUICommand(propertyCount, new GC_Else());
                                    }
                                    //help box
                                    else if (command == kGC_HelpBox)
                                    {
                                        MessageType messageType = MessageType.Error;
                                        string message = "Invalid format for HELP_BOX:\n" + fullCommand;
                                        string[] cmd = fullCommand
                                            .Substring(fullCommand.LastIndexOf(kGC_HelpBox) + kGC_HelpBox.Length + 1)
                                            .Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
                                        if (cmd.Length == 1)
                                        {
                                            message = cmd[0];
                                            messageType = MessageType.None;
                                        }
                                        else if (cmd.Length == 2)
                                        {
                                            try
                                            {
                                                MessageType msgType =
                                                    (MessageType)Enum.Parse(typeof(MessageType), cmd[0], true);
                                                message = cmd[1].Replace("  ", "\n");
                                                messageType = msgType;
                                            }
                                            catch
                                            {
                                            }
                                        }

                                        AddGUICommand(propertyCount, new GC_HelpBox
                                        {
                                            message = message,
                                            messageType = messageType
                                        });
                                    }
                                    //label
                                    else if (command == kGC_Label)
                                    {
                                        string label = fullCommand.Substring(fullCommand.LastIndexOf(kGC_Label) +
                                                                             kGC_Label.Length + 1);
                                        AddGUICommand(propertyCount, new GC_Label { label = label });
                                    }
                                    //header: plain text after command
                                    else
                                    {
                                        AddGUICommand(propertyCount, new GC_Header { label = fullCommand });
                                    }
                                }
                                else
                                    //property
                                {
                                    if (regex.IsMatch(line) && !insideCommentBlock && !isComment) propertyCount++;
                                }
                            }

                            //start properties block?
                            if (line.StartsWith("Properties")) insideProperties = true;
                        }
                    }
                }
            }

            private void AddGUICommand(int propertyIndex, GUICommand command)
            {
                if (!guiCommands.ContainsKey(propertyIndex))
                    guiCommands.Add(propertyIndex, new List<GUICommand>());

                guiCommands[propertyIndex].Add(command);
            }

            //--------------------------------------------------------------------------------------------------

            private void UpdateOutlineProp(Material material, bool needsOutline)
            {
                material.SetFloat(PROP_OUTLINE, needsOutline ? 1.0f : 0.0f);
            }

            public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
            {
                bool needsOutline = newShader.name.Contains("Outline") && newShader.name.Contains("Hybrid");
                UpdateOutlineProp(material, needsOutline);
                initialized = false;
                base.AssignNewShaderToMaterial(material, oldShader, newShader);
            }

            public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
            {
                _materialEditor = materialEditor;
                _properties = properties;

#if UNITY_2019_3_OR_NEWER
                RenderPipelineAsset srp = GraphicsSettings.currentRenderPipeline;
#else
				var srp = GraphicsSettings.renderPipelineAsset;
#endif
                _isURP = srp != null && srp.GetType().ToString().Contains("Universal");
                _isMobile = FindProperty(PROP_MOBILE_MODE, properties).floatValue > 0;

#if SHOW_DEFAULT_INSPECTOR
				base.OnGUI();
				return;
#endif
                //init:
                //- read metadata in properties comment to generate ui layout
                //- force update if timestamp doesn't match last (= file externally updated)
                //- do at the first Repaint to avoid layout mismatch errors
                if (Event.current.type == EventType.Repaint)
                {
                    bool force = shaderImporter != null && shaderImporter.assetTimeStamp != lastTimestamp;
                    bool wasInitialized = initialized;
                    Initialize(materialEditor, properties, force);

                    if (!wasInitialized)
                    {
                        GUIUtility.ExitGUI();
                        return;
                    }
                }

                Shader shader = (materialEditor.target as Material).shader;
                materialEditor.SetDefaultGUIWidths();

                ShowNextProperty = true;
                DisableNextProperty = false;
                ShowStack.Clear();
                DisableStack.Clear();

                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = labelWidth - 50;

                // Header
                GUILayout.Label(
                    TCP2_GUI.TempContent(EditorGUIUtility.currentViewWidth > 355f
                        ? "Toony Colors Pro 2 - Hybrid Shader"
                        : "TCP2 - Hybrid Shader"), SGUILayout.Styles.OrangeHeader);
                TCP2_GUI.Separator();

                // Mobile mode
                HandleMobileMode();
                TCP2_GUI.Separator();

                // Specific
                GUILayout.Label(TCP2_GUI.TempContent("Transparency"), SGUILayout.Styles.OrangeBoldLabel);
                HandleRenderingMode();

                // Iterate properties
                MaterialProperty outlineProp = null;
                for (int i = 0; i < properties.Length; i++)
                {
                    if (properties[i].type == MaterialProperty.PropType.Float)
                        EditorGUIUtility.labelWidth = labelWidth - 50;

                    if (guiCommands.ContainsKey(i))
                        for (int j = 0; j < guiCommands[i].Count; j++)
                            guiCommands[i][j].OnGUI();

                    //Use custom properties to enable/disable groups based on keywords
                    if (ShowNextProperty)
                    {
                        bool guiEnabled = GUI.enabled;
                        GUI.enabled = !DisableNextProperty;

                        if (properties[i].name == PROP_OUTLINE)
                        {
                            outlineProp = properties[i];
                            HandleOutlinePass(outlineProp);
                        }
                        else
                        {
                            if ((properties[i].flags & (MaterialProperty.PropFlags.HideInInspector |
                                                        MaterialProperty.PropFlags.PerRendererData)) ==
                                MaterialProperty.PropFlags.None)
                            {
                                string displayName = splitLabels.ContainsKey(i)
                                    ? splitLabels[i][_isMobile ? 1 : 0]
                                    : properties[i].displayName;
                                DisplayProperty(properties[i], displayName, materialEditor);
                            }

                            if (properties[i].name == "_UseAlphaTest" && GUI.changed)
                                IterateMaterials(mat =>
                                {
                                    string currentTag = mat.GetTag("RenderType", false, null);
                                    if (currentTag != "Transparent")
                                    {
                                        string tag = mat.IsKeywordEnabled("_ALPHATEST_ON") ? "TransparentCutout" : "";
                                        mat.SetOverrideTag("RenderType", tag);
                                        if (mat.IsKeywordEnabled("_ALPHATEST_ON"))
                                            mat.renderQueue = (int)RenderQueue.AlphaTest;
                                        else
                                            mat.renderQueue = -1;
                                    }
                                });
                        }

                        GUI.enabled = guiEnabled;
                    }

                    if (properties[i].name == PROP_OUTLINE_LAST) DisplayOutlineWarning(outlineProp);

                    EditorGUIUtility.labelWidth = labelWidth;
                }

                //make sure to show gui commands that are after properties
                int index = properties.Length;
                if (guiCommands.ContainsKey(index))
                    for (int j = 0; j < guiCommands[index].Count; j++)
                        guiCommands[index][j].OnGUI();

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                bool missingEmission = Array.Exists(_materialEditor.targets,
                    t => t != null && !((Material)t).HasProperty("_EmissionColor"));
                if (!missingEmission)
                {
                    EditorGUIUtility.labelWidth = labelWidth - 50;
                    materialEditor.LightmapEmissionFlagsProperty(0, Array.Exists(_materialEditor.targets, t =>
                    {
                        Material mat = t as Material;
                        return mat != null && mat.HasProperty("_EmissionColor") && mat.IsKeywordEnabled("_EMISSION");
                    }));
                    EditorGUIUtility.labelWidth = labelWidth;
                }

                materialEditor.RenderQueueField();
                materialEditor.EnableInstancingField();
            }

            protected void DisplayProperty(MaterialProperty property, string label, MaterialEditor materialEditor)
            {
                float propertyHeight = materialEditor.GetPropertyHeight(property, label);
                Rect controlRect = EditorGUILayout.GetControlRect(true, propertyHeight, EditorStyles.layerMaskField);
                materialEditor.ShaderProperty(controlRect, property, label);
            }

            private void IterateMaterials(Action<Material> action)
            {
                foreach (Object target in _materialEditor.targets) action(target as Material);
            }

            private void IterateMaterialsByIndex(Action<Material, int> action)
            {
                int i = 0;
                foreach (Object target in _materialEditor.targets)
                {
                    action(target as Material, i);
                    i++;
                }
            }

            private void InitOutlineMapping()
            {
                if (materialsOutlineMapping == null || materialsOutlineMapping.Length != _materialEditor.targets.Length)
                {
                    materialsOutlineMapping = new float[_materialEditor.targets.Length];
                    IterateMaterialsByIndex((mat, i) => materialsOutlineMapping[i] = mat.GetFloat(PROP_OUTLINE));
                }
            }

            private void UpdateOutlineMapping(bool updateShader)
            {
                IterateMaterialsByIndex((mat, i) =>
                {
                    materialsOutlineMapping[i] = mat.GetFloat(PROP_OUTLINE);

                    if (updateShader)
                    {
                        bool needsOutline = materialsOutlineMapping[i] > 0;
                        bool hasOutline = mat.shader.name.Contains("Outline");

                        if (needsOutline && !hasOutline)
                        {
                            Shader outlineShader = Shader.Find(mat.shader.name + " Outline");
                            if (outlineShader == null) outlineShader = Shader.Find(mat.shader.name + " (Outline)");
                            mat.shader = outlineShader;
                            initialized = false;
                        }
                        else if (!needsOutline && hasOutline)
                        {
                            mat.shader = Shader.Find(mat.shader.name.Replace(" Outline", "").Replace(" (Outline)", ""));
                            initialized = false;
                        }
                    }
                });
            }

            private void HandleOutlinePass(MaterialProperty outlineProp)
            {
                bool showMixed = EditorGUI.showMixedValue;

                // Keep track of the outline prop values and detect any change
                // This is to handle the "Reset" context menu option, which won't trigger any callback
                InitOutlineMapping();
                bool outlineValuesChanged = false;
                IterateMaterialsByIndex((mat, i) =>
                    outlineValuesChanged |= materialsOutlineMapping[i] != mat.GetFloat(PROP_OUTLINE));
                if (outlineValuesChanged) UpdateOutlineMapping(true);

                EditorGUI.showMixedValue = outlineProp.hasMixedValue;
                {
                    EditorGUI.BeginChangeCheck();
                    _materialEditor.ShaderProperty(outlineProp, TCP2_GUI.TempContent(outlineProp.displayName));
                    if (EditorGUI.EndChangeCheck())
                    {
                        bool enableOutline = outlineProp.floatValue > 0;
                        Undo.RecordObjects(_materialEditor.targets,
                            (enableOutline ? "Enable" : "Disable") + " Outline on Material(s)");
                        UpdateOutlineMapping(true);
                    }
                }
                EditorGUI.showMixedValue = showMixed;
            }

            private void DisplayOutlineWarning(MaterialProperty outlineProp)
            {
                if (outlineProp == null) return;

                if (outlineProp.floatValue > 0 || outlineProp.hasMixedValue)
                {
                    GUILayout.Space(4f);

#if UNITY_2019_3_OR_NEWER
                    RenderPipelineAsset srp = GraphicsSettings.currentRenderPipeline;
#else
					var srp = GraphicsSettings.renderPipelineAsset;
#endif
                    if (srp != null && srp.GetType().ToString().Contains("Universal"))
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            GUILayout.Label(
                                TCP2_GUI.TempContent(
                                    "Universal Render Pipeline requires some additional setup for the Outline to work, using the 'Renderer Features' system."),
                                EditorStyles.wordWrappedLabel);
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button(TCP2_GUI.TempContent("See documentation"),
                                        GUILayout.ExpandWidth(false))) Application.OpenURL(OUTLINE_URP_DOCUMENTATION);
                            }
                            GUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            private void HandleMobileMode()
            {
                bool showMixed = EditorGUI.showMixedValue;
                MaterialProperty mobileModeProp = FindProperty(PROP_MOBILE_MODE, _properties);
                EditorGUI.showMixedValue = mobileModeProp.hasMixedValue;
                {
                    MobileMode newMobileMode = (MobileMode)EditorGUILayout.EnumPopup(
                        TCP2_GUI.TempContent("Mobile Mode", mobileModeHelp), (MobileMode)mobileModeProp.floatValue);
                    if ((float)newMobileMode != mobileModeProp.floatValue)
                    {
                        Undo.RecordObjects(_materialEditor.targets, "Change Material Mobile Mode");
                        IterateMaterials(mat =>
                        {
                            mat.SetFloat(PROP_MOBILE_MODE, (float)newMobileMode);
                            if (newMobileMode == MobileMode.Enabled)
                                mat.EnableKeyword(KEYWORD_MOBILE_MODE);
                            else
                                mat.DisableKeyword(KEYWORD_MOBILE_MODE);
                        });
                    }
                }
                EditorGUI.showMixedValue = showMixed;

                if (mobileModeProp.floatValue > 0) EditorGUILayout.HelpBox(mobileModeHelp, MessageType.Info);
            }

            private void HandleRenderingMode()
            {
                bool showMixed = EditorGUI.showMixedValue;
                MaterialProperty renderingModeProp = FindProperty(PROP_RENDERING_MODE, _properties);
                EditorGUI.showMixedValue = renderingModeProp.hasMixedValue;
                {
                    RenderingMode newRenderingMode = (RenderingMode)EditorGUILayout.EnumPopup(
                        TCP2_GUI.TempContent("Rendering Mode"), (RenderingMode)renderingModeProp.floatValue);
                    if ((float)newRenderingMode != renderingModeProp.floatValue)
                    {
                        Undo.RecordObjects(_materialEditor.targets, "Change Material Rendering Mode");
                        SetRenderingMode(newRenderingMode);
                    }
                }
                EditorGUI.showMixedValue = showMixed;
            }

            private void SetRenderingMode(RenderingMode mode)
            {
                switch (mode)
                {
                    case RenderingMode.Opaque:
                        SetRenderQueue(-1);
                        //SetCulling(Culling.Back);
                        SetZWrite(true);
                        SetBlending(BlendFactor.One, BlendFactor.Zero);
                        IterateMaterials(mat =>
                        {
                            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            mat.SetOverrideTag("RenderType",
                                mat.IsKeywordEnabled("_ALPHATEST_ON") ? "TransparentCutout" : "");
                        });
                        break;

                    case RenderingMode.Fade:
                        SetRenderQueue((int)RenderQueue.Transparent);
                        //SetCulling(Culling.Off);
                        SetZWrite(false);
                        SetBlending(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
                        IterateMaterials(mat =>
                        {
                            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            mat.SetOverrideTag("RenderType", "Transparent");
                        });
                        break;

                    case RenderingMode.Transparent:
                        SetRenderQueue((int)RenderQueue.Transparent);
                        //SetCulling(Culling.Off);
                        SetZWrite(false);
                        SetBlending(BlendFactor.One, BlendFactor.OneMinusSrcAlpha);
                        IterateMaterials(mat =>
                        {
                            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                            mat.SetOverrideTag("RenderType", "Transparent");
                        });
                        break;
                }

                IterateMaterials(mat => { mat.SetFloat(PROP_RENDERING_MODE, (float)mode); });
            }

            private void SetZWrite(bool enable)
            {
                IterateMaterials(mat => mat.SetFloat(PROP_ZWRITE, enable ? 1.0f : 0.0f));
            }

            private void SetRenderQueue(int queue)
            {
                IterateMaterials(mat => mat.renderQueue = queue);
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
        }

        //================================================================================================================================================================================================
        // GUI Commands System
        //
        // Workaround to Material Property Drawers limitations:
        // - uses shader comments to organize the GUI, and show/hide properties based on conditions
        // - can use any character (unlike property drawers)
        // - parsed once at material editor initialization

        internal class GUICommand
        {
            public virtual bool Visible()
            {
                return true;
            }

            public virtual void OnGUI()
            {
            }
        }

        internal class GC_Separator : GUICommand
        {
            public override void OnGUI()
            {
                if (MaterialInspector_Hybrid.ShowNextProperty) TCP2_GUI.SeparatorSimple();
            }
        }

        internal class GC_SeparatorDouble : GUICommand
        {
            public override void OnGUI()
            {
                if (MaterialInspector_Hybrid.ShowNextProperty) TCP2_GUI.Separator();
            }
        }

        internal class GC_Space : GUICommand
        {
            public override void OnGUI()
            {
                if (MaterialInspector_Hybrid.ShowNextProperty) GUILayout.Space(8);
            }
        }

        internal class GC_HelpBox : GUICommand
        {
            public string message { get; set; }
            public MessageType messageType { get; set; }

            public override void OnGUI()
            {
                if (MaterialInspector_Hybrid.ShowNextProperty)
                    TCP2_GUI.HelpBoxLayout(message, messageType);
            }
        }

        internal class GC_Header : GUICommand
        {
            private GUIContent guiContent;
            public string label { get; set; }

            public override void OnGUI()
            {
                if (guiContent == null)
                    guiContent = new GUIContent(label);

                if (MaterialInspector_Hybrid.ShowNextProperty)
                    GUILayout.Label(guiContent, SGUILayout.Styles.OrangeBoldLabel);
            }
        }

        internal class GC_Label : GUICommand
        {
            private GUIContent guiContent;
            public string label { get; set; }

            public override void OnGUI()
            {
                if (guiContent == null)
                    guiContent = new GUIContent(label);

                if (MaterialInspector_Hybrid.ShowNextProperty)
                    GUILayout.Label(guiContent);
            }
        }

        internal class GC_IfKeyword : GUICommand
        {
            public string expression { get; set; }
            public Material[] materials { get; set; }

            public override void OnGUI()
            {
                bool show = ExpressionParser.EvaluateExpression(expression, s =>
                {
                    foreach (Material m in materials)
                        if (m.IsKeywordEnabled(s))
                            return true;
                    return false;
                });
                MaterialInspector_Hybrid.PushShowProperty(show);
            }
        }

        internal class GC_IfURP : GUICommand
        {
            private readonly bool needUrp;

            public GC_IfURP(bool urp = true)
            {
                needUrp = urp;
            }

            public override void OnGUI()
            {
                bool show = MaterialInspector_Hybrid._isURP;
                if (!needUrp) show = !show;
                MaterialInspector_Hybrid.PushShowProperty(show);
            }
        }

        internal class GC_Else : GUICommand
        {
            public override void OnGUI()
            {
                bool invertCondition = !MaterialInspector_Hybrid.ShowNextProperty;
                MaterialInspector_Hybrid.PopShowProperty();
                MaterialInspector_Hybrid.PushShowProperty(invertCondition);
            }
        }

        internal class GC_EndIf : GUICommand
        {
            public override void OnGUI()
            {
                MaterialInspector_Hybrid.PopShowProperty();
            }
        }

        internal class GC_EndIfDisable : GUICommand
        {
            public override void OnGUI()
            {
                MaterialInspector_Hybrid.PopDisableProperty();
            }
        }

        internal class GC_IfProperty : GUICommand
        {
            private string _expression;

            public string expression
            {
                get => _expression;
                set => _expression = value.Replace("!=", "<>");
            }

            public Material[] materials { get; set; }

            public override void OnGUI()
            {
                bool show = ExpressionParser.EvaluateExpression(expression, EvaluatePropertyExpression);
                MaterialInspector_Hybrid.PushShowProperty(show);
            }

            protected bool EvaluatePropertyExpression(string expr)
            {
                //expression is expected to be in the form of: property operator value
                StringReader reader = new(expr);
                string property = "";
                string op = "";
                float value = 0f;

                int overflow = 0;
                while (true)
                {
                    char c = (char)reader.Read();

                    //operator
                    if (c == '=' || c == '>' || c == '<' || c == '!')
                    {
                        op += c;
                        //second operator character, if any
                        char c2 = (char)reader.Peek();
                        if (c2 == '=' || c2 == '>')
                        {
                            reader.Read();
                            op += c2;
                        }

                        //end of string is the value
                        string end = reader.ReadToEnd();
                        if (!float.TryParse(end, out value))
                        {
                            Debug.LogError("Couldn't parse float from property expression:\n" + end);
                            return false;
                        }

                        break;
                    }

                    //property name
                    property += c;

                    overflow++;
                    if (overflow >= 9999)
                    {
                        Debug.LogError("Expression parsing overflow!\n");
                        return false;
                    }
                }

                //evaluate property
                bool conditionMet = false;
                foreach (Material m in materials)
                {
                    float propValue = 0f;
                    if (property.Contains(".x") || property.Contains(".y") || property.Contains(".z") ||
                        property.Contains(".w"))
                    {
                        string[] split = property.Split('.');
                        string component = split[1];
                        switch (component)
                        {
                            case "x":
                                propValue = m.GetVector(split[0]).x;
                                break;
                            case "y":
                                propValue = m.GetVector(split[0]).y;
                                break;
                            case "z":
                                propValue = m.GetVector(split[0]).z;
                                break;
                            case "w":
                                propValue = m.GetVector(split[0]).w;
                                break;
                            default:
                                Debug.LogError("Invalid component for vector property: '" + property + "'");
                                break;
                        }
                    }
                    else
                    {
                        propValue = m.HasProperty(property) ? m.GetFloat(property) : 0;
                    }

                    switch (op)
                    {
                        case ">=":
                            conditionMet = propValue >= value;
                            break;
                        case "<=":
                            conditionMet = propValue <= value;
                            break;
                        case ">":
                            conditionMet = propValue > value;
                            break;
                        case "<":
                            conditionMet = propValue < value;
                            break;
                        case "<>":
                            conditionMet = propValue != value;
                            break; //not equal, "!=" is replaced by "<>" to prevent bug with leading ! ("not" operator)
                        case "==":
                            conditionMet = propValue == value;
                            break;
                        default:
                            Debug.LogError("Invalid property expression:\n" + expr);
                            break;
                    }

                    if (conditionMet)
                        return true;
                }

                return false;
            }
        }

        internal class GC_IfDisableProperty : GC_IfProperty
        {
            public override void OnGUI()
            {
                bool enable = ExpressionParser.EvaluateExpression(expression, EvaluatePropertyExpression);
                MaterialInspector_Hybrid.PushDisableProperty(!enable);
            }
        }

        internal class GC_IfDisableKeyword : GC_IfKeyword
        {
            public override void OnGUI()
            {
                bool enable = ExpressionParser.EvaluateExpression(expression, s =>
                {
                    foreach (Material m in materials)
                        if (m.IsKeywordEnabled(s))
                            return true;
                    return false;
                });
                MaterialInspector_Hybrid.PushDisableProperty(!enable);
            }
        }
    }
}
