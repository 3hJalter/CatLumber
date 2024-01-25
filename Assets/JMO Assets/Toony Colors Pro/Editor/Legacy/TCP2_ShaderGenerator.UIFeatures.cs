// Toony Colors Pro+Mobile 2
// (c) 2014-2023 Jean Moreno

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ToonyColorsPro.Utilities;
using UnityEditor;
using UnityEngine;

// Utility to generate custom Toony Colors Pro 2 shaders with specific features

namespace ToonyColorsPro
{
    namespace Legacy
    {
        public partial class TCP2_ShaderGenerator : EditorWindow
        {
            //--------------------------------------------------------------------------------------------------
            // UI from Template System

            public class UIFeature
            {
                protected const float LABEL_WIDTH = 210f;
                private static Rect LastPosition;
                private static Rect LastPositionFull;
                private static bool LastVisible;

                private static readonly GUIContent tempContent = new();

                protected static Stack<bool> FoldoutStack = new();
                protected bool customGUI; //complete custom GUI that overrides the default behaviors (e.g. separator)
                protected string[] excludes; //features required to be OFF for this feature to be enabled
                protected string[] excludesAll; //features required to be OFF for this feature to be enabled
                private bool halfWidth; //draw in half space of the position (for inline)
                protected string helpTopic;
                protected bool ignoreVisibility; //ignore the current visible state and force the UI element to be drawn
                protected bool increaseIndent;
                private bool inline; //draw next to previous position

                protected string label;
                protected string[] requires; //features required for this feature to be enabled (AND)
                protected string[] requiresOr; //features required for this feature to be enabled (OR)
                protected bool showHelp = true;
                protected string tooltip;
                private bool wasEnabled; //track when the Enabled flag changes

                //Initialize a UIFeature given a list of arbitrary properties
                public UIFeature(List<KeyValuePair<string, string>> list)
                {
                    if (list != null)
                        foreach (KeyValuePair<string, string> kvp in list)
                            ProcessProperty(kvp.Key, kvp.Value);
                }

                protected static GUIContent TempContent(string label, string tooltip = null)
                {
                    tempContent.text = label;
                    tempContent.tooltip = tooltip;
                    return tempContent;
                }

                public static void ClearFoldoutStack()
                {
                    UIFeature_DropDownStart.ClearDropDownsList();
                    FoldoutStack.Clear();
                }

                //Process a property from the Template in the form key=value
                protected virtual void ProcessProperty(string key, string value)
                {
                    //Direct inline properties, no need for a value
                    if (string.IsNullOrEmpty(value))
                        switch (key)
                        {
                            case "nohelp":
                                showHelp = false;
                                break;
                            case "indent":
                                increaseIndent = true;
                                break;
                            case "inline":
                                inline = true;
                                break;
                            case "half":
                                halfWidth = true;
                                break;
                        }
                    else
                        //Common properties to all UIFeature classes
                        switch (key)
                        {
                            case "lbl":
                                label = value.Replace("  ", "\n");
                                break;
                            case "tt":
                                tooltip = value.Replace(@"\n", "\n").Replace("  ", "\n");
                                break;
                            case "help":
                                showHelp = bool.Parse(value);
                                break;
                            case "indent":
                                increaseIndent = bool.Parse(value);
                                break;
                            case "needs":
                                requires = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                break;
                            case "needsOr":
                                requiresOr = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                break;
                            case "excl":
                                excludes = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                break;
                            case "exclAll":
                                excludesAll = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                break;
                            case "hlptop":
                                helpTopic = value;
                                break;
                            case "inline":
                                inline = bool.Parse(value);
                                break;
                            case "half":
                                halfWidth = bool.Parse(value);
                                break;
                        }
                }

                private static Rect HeaderRect(ref Rect lineRect, float width)
                {
                    Rect rect = lineRect;
                    rect.width = width;

                    lineRect.x += rect.width;
                    lineRect.width -= rect.width;

                    return rect;
                }

                public void DrawGUI(TCP2_Config config)
                {
                    bool enabled = Enabled(config);
                    GUI.enabled = enabled;
                    bool visible = sHideDisabled && increaseIndent ? enabled : true;
                    if (inline) visible = LastVisible;

                    visible &= FoldoutStack.Count > 0 ? FoldoutStack.Peek() : true;

                    ForceValue(config);

                    if (customGUI)
                    {
                        if (visible || ignoreVisibility)
                        {
                            DrawGUI(new Rect(0, 0, EditorGUIUtility.currentViewWidth, 0), config);
                            return;
                        }
                    }
                    else if (visible)
                    {
                        //Total line rect
                        Rect position;
                        position = inline ? LastPosition : EditorGUILayout.GetControlRect();
                        Rect fullPosition = position;

                        if (halfWidth) position.width = Mathf.Max(position.width / 2f - 8f, LABEL_WIDTH + 10f);

                        //LastPosition is already halved
                        if (inline)
                        {
                            position.x += position.width + 8f;
                            position.width = LastPositionFull.width - LastPosition.width - 8f;
                        }

                        //Last Position for inlined properties
                        LastPosition = position;
                        LastPositionFull = fullPosition;

                        if (!inline)
                        {
                            //Help
                            if (showHelp)
                            {
                                Rect helpRect = HeaderRect(ref position, 20f);
                                TCP2_GUI.HelpButton(helpRect, label,
                                    string.IsNullOrEmpty(helpTopic) ? label : helpTopic);
                            }
                            else
                            {
                                HeaderRect(ref position, 20f);
                            }

                            //Indent
                            if (increaseIndent) HeaderRect(ref position, 6f);
                        }

                        //Label
                        GUIContent guiContent = TempContent((increaseIndent ? "▪ " : "") + label, tooltip);
                        Rect labelPosition = HeaderRect(ref position,
                            inline ? EditorStyles.label.CalcSize(guiContent).x + 8f : LABEL_WIDTH - position.x);
                        TCP2_GUI.SubHeader(labelPosition, guiContent, Highlighted(config) && Enabled(config));

                        //Actual property
                        DrawGUI(position, config);
                    }

                    LastVisible = visible;

                    GUI.enabled = sGUIEnabled;
                }

                //Internal DrawGUI: actually draws the feature
                protected virtual void DrawGUI(Rect position, TCP2_Config config)
                {
                    GUI.Label(position, "Unknown feature type for: " + label);
                }

                //Defines if the feature is selected/toggle/etc. or not
                public virtual bool Highlighted(TCP2_Config config)
                {
                    return false;
                }

                //Called when processing this UIFeature, in case any forced value needs to be set even if the UI component isn't visible
                protected virtual void ForceValue(TCP2_Config config)
                {

                }

                //Called when Enabled(config) has changed state
                //Originally used to force Multiple UI to enable the default feature, if any
                protected virtual void OnEnabledChangedState(TCP2_Config config, bool newState)
                {

                }

                public bool Enabled(TCP2_Config config)
                {
                    bool enabled = true;
                    if (requiresOr != null)
                    {
                        enabled = false;
                        enabled |= config.HasFeaturesAny(requiresOr);
                    }

                    if (excludesAll != null)
                        enabled &= !config.HasFeaturesAll(excludesAll);
                    if (requires != null)
                        enabled &= config.HasFeaturesAll(requires);
                    if (excludes != null)
                        enabled &= !config.HasFeaturesAny(excludes);

                    if (wasEnabled != enabled) OnEnabledChangedState(config, enabled);
                    wasEnabled = enabled;

                    return enabled;
                }

                //Parses a #FEATURES text block
                public static UIFeature[] GetUIFeatures(StringReader reader)
                {
                    List<UIFeature> uiFeaturesList = new List<UIFeature>();
                    string subline;
                    int overflow = 0;
                    while ((subline = reader.ReadLine()) != "#END")
                    {
                        //Just in case template file is badly written
                        overflow++;
                        if (overflow > 99999)
                            break;

                        //Empty line
                        if (string.IsNullOrEmpty(subline))
                            continue;

                        string[] data = subline.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                        //Skip empty or comment # lines
                        if (data == null || data.Length == 0 || (data.Length > 0 && data[0].StartsWith("#")))
                            continue;

                        List<KeyValuePair<string, string>> kvpList = new List<KeyValuePair<string, string>>();
                        for (int i = 1; i < data.Length; i++)
                        {
                            string[] sdata = data[i].Split('=');
                            if (sdata.Length == 2)
                                kvpList.Add(new KeyValuePair<string, string>(sdata[0], sdata[1]));
                            else if (sdata.Length == 1)
                                kvpList.Add(new KeyValuePair<string, string>(sdata[0], null));
                            else
                                Debug.LogError("Couldn't parse UI property from Template:\n" + data[i]);
                        }

                        UIFeature feature = null;
                        switch (data[0])
                        {
                            case "---":
                                feature = new UIFeature_Separator();
                                break;
                            case "space":
                                feature = new UIFeature_Space(kvpList);
                                break;
                            case "flag":
                                feature = new UIFeature_Flag(kvpList);
                                break;
                            case "float":
                                feature = new UIFeature_Float(kvpList);
                                break;
                            case "subh":
                                feature = new UIFeature_SubHeader(kvpList);
                                break;
                            case "header":
                                feature = new UIFeature_Header(kvpList);
                                break;
                            case "warning":
                                feature = new UIFeature_Warning(kvpList);
                                break;
                            case "sngl":
                                feature = new UIFeature_Single(kvpList);
                                break;
                            case "mult":
                                feature = new UIFeature_Multiple(kvpList);
                                break;
                            case "keyword":
                                feature = new UIFeature_Keyword(kvpList);
                                break;
                            case "mask":
                                feature = new UIFeature_Mask(kvpList);
                                break;
                            case "shader_target":
                                feature = new UIFeature_ShaderTarget();
                                break;
                            case "dd_start":
                                feature = new UIFeature_DropDownStart(kvpList);
                                break;
                            case "dd_end":
                                feature = new UIFeature_DropDownEnd();
                                break;
                            //case "texture_list": feature = new UIFeature_TextureList(); break;
                            //case "tex": feature = new UIFeature_Texture(kvpList); break;

                            default:
                                feature = new UIFeature(kvpList);
                                break;
                        }

                        uiFeaturesList.Add(feature);
                    }

                    return uiFeaturesList.ToArray();
                }
            }

            //----------------------------------------------------------------------------------------------------------------------------------------------------------------
            // SINGLE FEATURE TOGGLE

            public class UIFeature_Single : UIFeature
            {
                private string keyword;
                private string[] toggles; //features forced to be toggled when this feature is enabled

                public UIFeature_Single(List<KeyValuePair<string, string>> list) : base(list)
                {
                }

                protected override void ProcessProperty(string key, string value)
                {
                    if (key == "kw")
                        keyword = value;
                    else if (key == "toggles")
                        toggles = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    else
                        base.ProcessProperty(key, value);
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    bool feature = Highlighted(config);
                    EditorGUI.BeginChangeCheck();
                    feature = EditorGUI.Toggle(position, feature);
                    if (EditorGUI.EndChangeCheck())
                    {
                        config.ToggleFeature(keyword, feature);

                        if (toggles != null)
                            foreach (string t in toggles)
                                config.ToggleFeature(t, feature);
                    }
                }

                public override bool Highlighted(TCP2_Config config)
                {
                    return config.HasFeature(keyword);
                }
            }

            //----------------------------------------------------------------------------------------------------------------------------------------------------------------
            // FEATURES COMBOBOX

            public class UIFeature_Multiple : UIFeature
            {
                private string[] features;
                private string[] labels;
                private string[] toggles; //features forced to be toggled when this feature is enabled

                public UIFeature_Multiple(List<KeyValuePair<string, string>> list) : base(list)
                {
                }

                protected override void ProcessProperty(string key, string value)
                {
                    if (key == "kw")
                    {
                        string[] data = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        labels = new string[data.Length];
                        features = new string[data.Length];

                        for (int i = 0; i < data.Length; i++)
                        {
                            string[] lbl_feat = data[i].Split('|');
                            if (lbl_feat.Length != 2)
                            {
                                Debug.LogWarning("[UIFeature_Multiple] Invalid data:" + data[i]);
                                continue;
                            }

                            labels[i] = lbl_feat[0];
                            features[i] = lbl_feat[1];
                        }
                    }
                    else if (key == "toggles")
                    {
                        toggles = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        base.ProcessProperty(key, value);
                    }
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    int feature = GetSelectedFeature(config);
                    if (feature < 0) feature = 0;

                    EditorGUI.BeginChangeCheck();
                    feature = EditorGUI.Popup(position, feature, labels);
                    if (EditorGUI.EndChangeCheck()) ToggleSelectedFeature(config, feature);
                }

                private int GetSelectedFeature(TCP2_Config config)
                {
                    for (int i = 0; i < features.Length; i++)
                        if (config.HasFeature(features[i]))
                            return i;

                    return -1;
                }

                public override bool Highlighted(TCP2_Config config)
                {
                    int feature = GetSelectedFeature(config);
                    return feature > 0;
                }

                protected override void OnEnabledChangedState(TCP2_Config config, bool newState)
                {
                    int feature = -1;
                    if (newState)
                    {
                        feature = GetSelectedFeature(config);
                        if (feature < 0) feature = 0;
                    }

                    ToggleSelectedFeature(config, feature);
                }

                private void ToggleSelectedFeature(TCP2_Config config, int selectedFeature)
                {
                    for (int i = 0; i < features.Length; i++)
                    {
                        bool enable = i == selectedFeature;
                        config.ToggleFeature(features[i], enable);
                    }

                    if (toggles != null)
                        foreach (string t in toggles)
                            config.ToggleFeature(t, selectedFeature > 0);
                }
            }

            //----------------------------------------------------------------------------------------------------------------------------------------------------------------
            // KEYWORD COMBOBOX

            public class UIFeature_Keyword : UIFeature
            {
                private int defaultValue;
                private bool forceValue;
                private string keyword;
                private string[] labels;
                private string[] values;

                public UIFeature_Keyword(List<KeyValuePair<string, string>> list) : base(list)
                {
                }

                protected override void ProcessProperty(string key, string value)
                {
                    if (key == "kw")
                    {
                        keyword = value;
                    }
                    else if (key == "default")
                    {
                        defaultValue = int.Parse(value, CultureInfo.InvariantCulture);
                    }
                    else if (key == "forceKeyword")
                    {
                        forceValue = bool.Parse(value);
                    }
                    else if (key == "values")
                    {
                        string[] data = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        labels = new string[data.Length];
                        values = new string[data.Length];

                        for (int i = 0; i < data.Length; i++)
                        {
                            string[] lbl_feat = data[i].Split('|');
                            if (lbl_feat.Length != 2)
                            {
                                Debug.LogWarning("[UIFeature_Keyword] Invalid data:" + data[i]);
                                continue;
                            }

                            labels[i] = lbl_feat[0];
                            values[i] = lbl_feat[1];
                        }
                    }
                    else
                    {
                        base.ProcessProperty(key, value);
                    }
                }

                protected override void ForceValue(TCP2_Config config)
                {
                    int selectedValue = GetSelectedValue(config);
                    if (selectedValue < 0)
                        selectedValue = defaultValue;

                    if (forceValue && Enabled(config) && !config.HasKeyword(keyword))
                        config.SetKeyword(keyword, values[selectedValue]);
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    int selectedValue = GetSelectedValue(config);
                    if (selectedValue < 0)
                    {
                        selectedValue = defaultValue;
                        if (forceValue && Enabled(config)) config.SetKeyword(keyword, values[defaultValue]);
                    }

                    EditorGUI.BeginChangeCheck();
                    selectedValue = EditorGUI.Popup(position, selectedValue, labels);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (string.IsNullOrEmpty(values[selectedValue]))
                            config.RemoveKeyword(keyword);
                        else
                            config.SetKeyword(keyword, values[selectedValue]);
                    }
                }

                private int GetSelectedValue(TCP2_Config config)
                {
                    string currentValue = config.GetKeyword(keyword);
                    for (int i = 0; i < values.Length; i++)
                        if (currentValue == values[i])
                            return i;

                    return -1;
                }

                public override bool Highlighted(TCP2_Config config)
                {
                    int feature = GetSelectedValue(config);
                    return feature != defaultValue;
                }
            }

            //----------------------------------------------------------------------------------------------------------------------------------------------------------------
            // MASK

            public class UIFeature_Mask : UIFeature
            {
                private string channelKeyword;

                private readonly string[] labels =
                    { "Off", "Main Texture", "Mask 1", "Mask 2", "Mask 3", "Vertex Colors" };

                private readonly string[] masks = { "", "mainTex", "mask1", "mask2", "mask3", "vcolors" };
                private readonly string[] uvs = { "Main Tex UV", "Independent UV0", "Independent UV1" };

                public UIFeature_Mask(List<KeyValuePair<string, string>> list) : base(list)
                {
                }

                public string Keyword { get; private set; }

                public string MaskKeyword { get; private set; }

                public string DisplayName { get; private set; }

                protected override void ProcessProperty(string key, string value)
                {
                    if (key == "kw")
                        Keyword = value;
                    else if (key == "ch")
                        channelKeyword = value;
                    else if (key == "msk")
                        MaskKeyword = value;
                    else if (key == "dispName")
                        DisplayName = value;
                    else
                        base.ProcessProperty(key, value);
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    //GUIMask(config, this.label, this.tooltip, this.maskKeyword, this.channelKeyword, this.keyword, this.Enabled(config), this.increaseIndent, helpTopic: this.helpTopic, helpIndent: this.helpIndent);

                    int curMask = Array.IndexOf(masks, config.GetKeyword(MaskKeyword));
                    if (curMask < 0) curMask = 0;
                    Utils.TextureChannel curChannel = Utils.FromShader(config.GetKeyword(channelKeyword));
                    string uvKey = curMask > 1 && curMask < 5 ? "UV_" + masks[curMask] : null;
                    int curUv = Array.IndexOf(uvs, config.GetKeyword(uvKey));
                    if (curUv < 0) curUv = 0;

                    EditorGUI.BeginChangeCheck();

                    //Calculate rects
                    Rect helpButton = position;
                    helpButton.width = 16f;
                    helpButton.x += 2f;
                    position.width -= helpButton.width;
                    helpButton.x += position.width;

                    //Mask type (MainTex, 1, 2, 3)
                    Rect sideRect = position;
                    sideRect.width = position.width * 0.75f / 2f;
                    curMask = EditorGUI.Popup(sideRect, curMask, labels);

                    //Mask Channel (RGBA)
                    Rect middleRect = position;
                    middleRect.width = position.width * 0.25f;
                    middleRect.x += sideRect.width;
                    GUI.enabled &= curMask > 0;
                    curChannel = (Utils.TextureChannel)EditorGUI.EnumPopup(middleRect, curChannel);

                    //Mask UVs
                    sideRect.x += sideRect.width + middleRect.width;
                    GUI.enabled &= curMask > 1 && curMask < 5;
                    curUv = EditorGUI.Popup(sideRect, curUv, uvs);

                    //Mask Help
                    TCP2_GUI.HelpButton(helpButton, "Masks");

                    if (EditorGUI.EndChangeCheck())
                    {
                        config.SetKeyword(MaskKeyword, masks[curMask]);
                        if (curMask > 0) config.SetKeyword(channelKeyword, curChannel.ToShader());
                        if (curMask > 1 && !string.IsNullOrEmpty(uvKey)) config.SetKeyword(uvKey, uvs[curUv]);
                        config.ToggleFeature("VCOLORS_MASK", curMask == 5);
                        config.ToggleFeature(Keyword, curMask > 0);
                    }
                }

                public override bool Highlighted(TCP2_Config config)
                {
                    int curMask = GetCurrentMask(config);
                    return curMask > 0;
                }

                private int GetCurrentMask(TCP2_Config config)
                {
                    int curMask = Array.IndexOf(masks, config.GetKeyword(MaskKeyword));
                    return curMask;
                }
            }

            //----------------------------------------------------------------------------------------------------------------------------------------------------------------
            // SHADER TARGET

            public class UIFeature_ShaderTarget : UIFeature
            {
                public UIFeature_ShaderTarget() : base(null)
                {
                    customGUI = true;
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    EditorGUILayout.BeginHorizontal();
                    TCP2_GUI.HelpButton("Shader Target");
                    TCP2_GUI.SubHeader("Shader Target", "Defines the shader target level to compile for",
                        config.shaderTarget != 30, LABEL_WIDTH - 24f);
                    int newTarget = EditorGUILayout.IntPopup(config.shaderTarget,
#if UNITY_5_4_OR_NEWER
                        new[] { "2.0", "2.5", "3.0", "3.5", "4.0", "5.0" },
                        new[] { 20, 25, 30, 35, 40, 50 });
#else
				new string[] { "2.0", "3.0", "4.0", "5.0" },
				new int[] { 20, 30, 40, 50 });
#endif
                    if (newTarget != config.shaderTarget) config.shaderTarget = newTarget;
                    EditorGUILayout.EndHorizontal();
                }
            }

            //----------------------------------------------------------------------------------------------------------------------------------------------------------------
            // SURFACE SHADER FLAG

            public class UIFeature_Flag : UIFeature
            {
                private string keyword;
                private string[] toggles; //features forced to be toggled when this flag is enabled

                public UIFeature_Flag(List<KeyValuePair<string, string>> list) : base(list)
                {
                    showHelp = false;
                }

                protected override void ProcessProperty(string key, string value)
                {
                    if (key == "kw")
                        keyword = value;
                    else if (key == "toggles")
                        toggles = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    else
                        base.ProcessProperty(key, value);
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    bool flag = Highlighted(config);
                    EditorGUI.BeginChangeCheck();
                    flag = EditorGUI.Toggle(position, flag);

                    if (EditorGUI.EndChangeCheck())
                    {
                        config.ToggleFlag(keyword, flag);

                        if (toggles != null)
                            foreach (string t in toggles)
                                config.ToggleFeature(t, flag);
                    }
                }

                public override bool Highlighted(TCP2_Config config)
                {
                    return config.HasFlag(keyword);
                }
            }

            //----------------------------------------------------------------------------------------------------------------------------------------------------------------
            // FIXED FLOAT

            public class UIFeature_Float : UIFeature
            {
                private float defaultValue;
                private string keyword;
                private float max = float.MaxValue;
                private float min = float.MinValue;

                public UIFeature_Float(List<KeyValuePair<string, string>> list) : base(list)
                {
                }

                protected override void ProcessProperty(string key, string value)
                {
                    if (key == "kw")
                        keyword = value;
                    else if (key == "default")
                        defaultValue = float.Parse(value, CultureInfo.InvariantCulture);
                    else if (key == "min")
                        min = float.Parse(value, CultureInfo.InvariantCulture);
                    else if (key == "max")
                        max = float.Parse(value, CultureInfo.InvariantCulture);
                    else
                        base.ProcessProperty(key, value);
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    string currentValueStr = config.GetKeyword(keyword);
                    float currentValue = defaultValue;
                    if (!float.TryParse(currentValueStr, NumberStyles.Float, CultureInfo.InvariantCulture,
                            out currentValue))
                    {
                        currentValue = defaultValue;

                        //Only enforce keyword if feature is enabled
                        if (Enabled(config))
                            config.SetKeyword(keyword,
                                currentValue.ToString("0.0###############", CultureInfo.InvariantCulture));
                    }

                    EditorGUI.BeginChangeCheck();
                    float newValue = currentValue;
                    newValue = Mathf.Clamp(EditorGUI.FloatField(position, currentValue), min, max);
                    if (EditorGUI.EndChangeCheck())
                        if (newValue != currentValue)
                            config.SetKeyword(keyword,
                                newValue.ToString("0.0###############", CultureInfo.InvariantCulture));
                }
            }

            //----------------------------------------------------------------------------------------------------------------------------------------------------------------
            // DECORATORS

            public class UIFeature_Separator : UIFeature
            {
                public UIFeature_Separator() : base(null)
                {
                    customGUI = true;
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    Space();
                }
            }

            public class UIFeature_Space : UIFeature
            {
                private float space = 8f;

                public UIFeature_Space(List<KeyValuePair<string, string>> list) : base(list)
                {
                    customGUI = true;
                }

                protected override void ProcessProperty(string key, string value)
                {
                    if (key == "space")
                        space = float.Parse(value, CultureInfo.InvariantCulture);
                    else
                        base.ProcessProperty(key, value);
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    if (Enabled(config))
                        GUILayout.Space(space);
                }
            }

            public class UIFeature_SubHeader : UIFeature
            {
                public UIFeature_SubHeader(List<KeyValuePair<string, string>> list) : base(list)
                {
                    customGUI = true;
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    TCP2_GUI.SubHeaderGray(label);
                }
            }

            public class UIFeature_Header : UIFeature
            {
                public UIFeature_Header(List<KeyValuePair<string, string>> list) : base(list)
                {
                    customGUI = true;
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    TCP2_GUI.Header(label);
                }
            }

            public class UIFeature_Warning : UIFeature
            {
                private MessageType msgType = MessageType.Warning;

                public UIFeature_Warning(List<KeyValuePair<string, string>> list) : base(list)
                {
                    customGUI = true;
                }

                protected override void ProcessProperty(string key, string value)
                {
                    if (key == "msgType")
                        msgType = (MessageType)Enum.Parse(typeof(MessageType), value, true);
                    else
                        base.ProcessProperty(key, value);
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    if (Enabled(config))
                        //EditorGUILayout.HelpBox(this.label, msgType);
                        TCP2_GUI.HelpBoxLayout(label, msgType);
                }
            }

            public class UIFeature_DropDownStart : UIFeature
            {
                private static readonly List<UIFeature_DropDownStart> AllDropDowns = new();

                private bool foldout;
                private readonly GUIContent guiContent = GUIContent.none;

                public UIFeature_DropDownStart(List<KeyValuePair<string, string>> list) : base(list)
                {
                    customGUI = true;
                    ignoreVisibility = true;

                    if (list != null)
                        foreach (KeyValuePair<string, string> kvp in list)
                            if (kvp.Key == "lbl")
                                guiContent = new GUIContent(kvp.Value);

                    foldout = sOpenedFoldouts.Contains(guiContent.text);

                    AllDropDowns.Add(this);
                }

                public static void ClearDropDownsList()
                {
                    AllDropDowns.Clear();
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    //Check if any feature within that Foldout are enabled, and show different color if so
                    bool hasToggledFeatures = false;
                    int i = Array.IndexOf(ShaderGeneratorTemplate.CurrentTemplate.uiFeatures, this);
                    if (i >= 0)
                        for (i++; i < ShaderGeneratorTemplate.CurrentTemplate.uiFeatures.Length; i++)
                        {
                            UIFeature uiFeature = ShaderGeneratorTemplate.CurrentTemplate.uiFeatures[i];
                            if (uiFeature is UIFeature_DropDownEnd)
                                break;

                            hasToggledFeatures |= uiFeature.Highlighted(config) && uiFeature.Enabled(config);
                        }

                    Color color = GUI.color;
                    GUI.color *= 0.95f;
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUI.color = color;
                    EditorGUI.BeginChangeCheck();
                    foldout = TCP2_GUI.HeaderFoldoutHighlight(foldout, guiContent, hasToggledFeatures);
                    FoldoutStack.Push(foldout);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdatePersistentState();

                        if (Event.current.alt || Event.current.control)
                        {
                            bool state = foldout;
                            foreach (UIFeature_DropDownStart dd in AllDropDowns)
                            {
                                dd.foldout = state;
                                dd.UpdatePersistentState();
                            }
                        }
                    }
                }

                private void UpdatePersistentState()
                {
                    if (foldout && !sOpenedFoldouts.Contains(guiContent.text))
                        sOpenedFoldouts.Add(guiContent.text);
                    else if (!foldout && sOpenedFoldouts.Contains(guiContent.text))
                        sOpenedFoldouts.Remove(guiContent.text);
                }
            }

            public class UIFeature_DropDownEnd : UIFeature
            {
                public UIFeature_DropDownEnd() : base(null)
                {
                    customGUI = true;
                    ignoreVisibility = true;
                }

                protected override void DrawGUI(Rect position, TCP2_Config config)
                {
                    FoldoutStack.Pop();

                    EditorGUILayout.EndVertical();
                }
            }
        }
    }
}
