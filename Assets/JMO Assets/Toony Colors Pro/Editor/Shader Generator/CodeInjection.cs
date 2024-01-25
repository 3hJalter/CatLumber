using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ToonyColorsPro.Utilities;
using UnityEditor;
using UnityEngine;

namespace ToonyColorsPro
{
    namespace ShaderGenerator
    {
        namespace CodeInjection
        {
            [Serialization.SerializeAs("codeInjection")]
            internal class CodeInjectionManager
            {
                //================================================================================================================================

                internal static CodeInjectionManager instance;

                [Serialization.SerializeAs("injectedFiles")]
                internal List<InjectedFile> injectedFiles = new();

                private readonly ReorderableLayoutList injectedFilesList = new();
                [Serialization.SerializeAs("mark")] private bool markInjectionPoints;

                public CodeInjectionManager()
                {
                    instance = this;
                }

                internal void ShowGUI(Template template)
                {
                    markInjectionPoints = EditorGUILayout.Toggle(
                        TCP2_GUI.TempContent("Mark injection points",
                            "Add a comment for each injection point in the output file, to easily identify their locations, e.g.\n\"// Injection Point: Properties/Start\""),
                        markInjectionPoints);

                    // Info
                    if (injectedFiles.Count == 0) EditorGUILayout.HelpBox("No injected file added.", MessageType.Info);

                    // Draw list
                    int injectedFileToRemove = -1;
                    Action<int, float> drawInjectedFile = (index, margin) =>
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(margin);
                                GUILayout.Label("Injected File", EditorStyles.boldLabel);
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button(TCP2_GUI.TempContent("-", "Remove this injected file")))
                                    injectedFileToRemove = index;
                            }
                            GUILayout.EndHorizontal();
                            injectedFiles[index].ShowGUI(template, margin);
                        }
                        EditorGUILayout.EndVertical();
                    };
                    injectedFilesList.DoLayoutList(drawInjectedFile, injectedFiles, 10);

                    if (injectedFileToRemove >= 0)
                    {
                        injectedFiles[injectedFileToRemove].WillBeRemoved();
                        injectedFiles.RemoveAt(injectedFileToRemove);
                    }

                    // Add button
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Add Injected File", GUILayout.ExpandWidth(false), GUILayout.Height(30)))
                            injectedFiles.Add(new InjectedFile());
                    }
                    GUILayout.EndHorizontal();
                }

                internal string[] GetNeededFeatures()
                {
                    List<string> list = new();
                    foreach (InjectedFile file in injectedFiles)
                    foreach (InjectedPoint point in file.injectedPoints)
                    {
                        if (!point.enabled) continue;

                        foreach (ShaderProperty sp in point.shaderProperties) list.AddRange(sp.NeededFeatures());
                    }

                    return list.ToArray();
                }

                internal string GetCodeForInjectionPoint(string injectionPoint, string indent)
                {
                    StringBuilder sb = new();

                    sb.AppendLine(string.Format("{0}//================================", indent));
                    sb.AppendLine(string.Format("{0}// Injected Code for '{1}'", indent, injectionPoint));

                    bool hasCode = false;
                    foreach (InjectedFile file in injectedFiles)
                    foreach (InjectedPoint point in file.injectedPoints)
                    {
                        if (!point.enabled) continue;

                        if (point.name == injectionPoint)
                        {
                            hasCode = true;
                            point.InjectCode(sb, indent);
                        }
                    }

                    if (!hasCode)
                        return markInjectionPoints
                            ? string.Format("{0}// Injection Point: '{1}'", indent, injectionPoint)
                            : "";

                    sb.AppendLine(string.Format("{0}//================================", indent));
                    return sb.ToString();
                }

                internal void ProcessReplaceBlocks(StringBuilder stringBuilder)
                {
                    foreach (InjectedFile file in injectedFiles)
                    foreach (InjectedPoint ip in file.injectedPoints)
                    {
                        if (!ip.enabled) continue;

                        if (ip.block.isReplaceBlock)
                        {
                            List<string> list = ip.GetCodeLinesWithReplacedVariables("");
                            list.Insert(0, "//================================");
                            list.Insert(1, "// Replaced through Code Injection:");
                            list.Add("//================================");

                            string replaceLines = string.Join("\n", list);
                            stringBuilder.Replace(ip.block.searchString, string.Join("\n", replaceLines));
                        }
                    }
                }

                internal List<ShaderProperty> GetShaderPropertiesForInjectionPoint(string injectionPoint)
                {
                    List<ShaderProperty> list = new();

                    foreach (InjectedFile file in injectedFiles)
                    foreach (InjectedPoint point in file.injectedPoints)
                    {
                        if (!point.enabled) continue;

                        if (point.name == injectionPoint)
                            foreach (ShaderProperty sp in point.shaderProperties)
                                list.Add(sp);
                    }

                    return list;
                }

                private static float[] ExtractDefaultValue(string input)
                {
                    List<float> list = new();
                    string current = "";

                    for (int i = 0; i < input.Length; i++)
                        if (char.IsDigit(input[i]) || input[i] == '.')
                        {
                            current += input[i];
                        }
                        else
                        {
                            if (current != "")
                            {
                                list.Add(float.Parse(current, CultureInfo.InvariantCulture));
                                current = "";
                            }
                        }

                    return list.ToArray();
                }

                [Serialization.SerializeAs("injectedPoint")]
                internal class InjectedPoint
                {
                    internal InjectableBlock block;

                    [Serialization.SerializeAs("blockName")]
                    internal string blockName;

                    [Serialization.SerializeAs("enabled")] internal bool enabled = true;

                    [Serialization.SerializeAs("displayName")]
                    internal string info;

                    [Serialization.SerializeAs("replace")] internal bool isReplace;
                    [Serialization.SerializeAs("name")] internal string name;
                    [Serialization.SerializeAs("program")] internal ShaderProperty.ProgramType program;

                    [Serialization.SerializeAs("shaderProperties")]
                    internal List<ShaderProperty> shaderProperties = new();

                    // Contains the serialized properties as text, temporarily:
                    // we need to parse the existing Shader Properties from the block first, and this is done when InjectedFile has been entirely Deserialized
                    // then only we can unpack the temp serialized shader properties into the existing ones
                    private Dictionary<string, string> tempSerializedShaderProperties;

                    // Needed for serialization
                    public InjectedPoint()
                    {
                    }

                    public InjectedPoint(string name, ShaderProperty.ProgramType program, InjectableBlock block)
                    {
                        this.name = name;
                        this.program = program;
                        this.block = block;
                        blockName = block.name;

                        UpdateShaderProperties();
                    }

                    // TODO On deserialization, compare matching shader properties and new ones, if any (if source file has changed)
                    [Serialization.CustomDeserializeCallback]
                    private static InjectedPoint Deserialize(string strData, object[] args)
                    {
                        InjectedPoint ip = (InjectedPoint)Activator.CreateInstance(typeof(InjectedPoint), args);
                        ip.tempSerializedShaderProperties = new Dictionary<string, string>();

                        Func<object, string, object> onDeserializeShaderPropertyList = (obj, data) =>
                        {
                            //called with data in format 'list[sp(field:value;field:value...),sp(field:value;...)]'

                            // - make a new list, and pull matching sp from it
                            // - reset the implementations of the remaining sp for the undo/redo system
                            // var shaderPropertiesTempList = new List<ShaderProperty>(ip.shaderProperties);

                            string[] split = Serialization.SplitExcludingBlocks(data.Substring(5, data.Length - 6), ',',
                                true, true, "()", "[]");
                            foreach (string spData in split)
                            {
                                //try to match existing Shader Property by its name
                                string name = null;

                                //exclude 'sp(' and ')' and extract fields
                                string[] vars = Serialization.SplitExcludingBlocks(
                                    spData.Substring(3, spData.Length - 4), ';', true, true, "()", "[]");
                                foreach (string v in vars)
                                    //find 'name' and remove 'name:' and quotes to extract value
                                    if (v.StartsWith("name:"))
                                        name = v.Substring(6, v.Length - 7);

                                if (name != null) ip.tempSerializedShaderProperties.Add(name, spData);
                            }

                            return null;
                        };

                        Dictionary<Type, Func<object, string, object>> shaderPropertyHandling = new()
                            { { typeof(List<ShaderProperty>), onDeserializeShaderPropertyList } };

                        return (InjectedPoint)Serialization.DeserializeTo(ip, strData, typeof(InjectedPoint), args,
                            shaderPropertyHandling);
                    }

                    private string GetShaderPropertyNameSuffix()
                    {
                        return "_" + block.name.GetHashCode();
                    }

                    internal void UpdateShaderProperties()
                    {
                        foreach (ShaderPropertyInfo spi in block.shaderPropertiesInfos)
                        {
                            string spName = string.Format("{0}{1}", spi.name, GetShaderPropertyNameSuffix());
                            if (!shaderProperties.Exists(sp => sp.Name == spName))
                            {
                                ShaderProperty sp = new(spName, spi.variableType);
                                sp.DisplayName = spi.name;
                                sp.Program = isReplace ? spi.programType : program;
                                sp.deferredSampling = true;

                                ShaderProperty.Imp_ConstantValue imp_constant =
                                    sp.implementations[0] as ShaderProperty.Imp_ConstantValue;
                                imp_constant.Label = spi.name;
                                if (imp_constant != null && spi.defaultValue != null)
                                    switch (spi.variableType)
                                    {
                                        case ShaderProperty.VariableType.@float:
                                        {
                                            float value;
                                            if (float.TryParse(spi.defaultValue, out value))
                                                imp_constant.FloatValue = value;
                                        }
                                            break;

                                        case ShaderProperty.VariableType.float2:
                                        {
                                            Vector2 value = Vector2.zero;
                                            float[] array = ExtractDefaultValue(spi.defaultValue);
                                            if (array.Length >= 1) value.x = array[0];
                                            if (array.Length >= 2) value.y = array[1];
                                            imp_constant.Float2Value = value;
                                        }
                                            break;

                                        case ShaderProperty.VariableType.float3:
                                        {
                                            Vector3 value = Vector3.zero;
                                            float[] array = ExtractDefaultValue(spi.defaultValue);
                                            if (array.Length >= 1) value.x = array[0];
                                            if (array.Length >= 2) value.y = array[1];
                                            if (array.Length >= 3) value.z = array[2];
                                            imp_constant.Float3Value = value;
                                        }
                                            break;

                                        case ShaderProperty.VariableType.float4:
                                        {
                                            Vector4 value = Vector4.zero;
                                            float[] array = ExtractDefaultValue(spi.defaultValue);
                                            if (array.Length >= 1) value.x = array[0];
                                            if (array.Length >= 2) value.y = array[1];
                                            if (array.Length >= 3) value.z = array[2];
                                            if (array.Length >= 4) value.w = array[3];
                                            imp_constant.Float4Value = value;
                                        }
                                            break;

                                        case ShaderProperty.VariableType.color:
                                        case ShaderProperty.VariableType.color_rgba:
                                        {
                                            Color value = new();
                                            float[] array = ExtractDefaultValue(spi.defaultValue);
                                            if (array.Length >= 1) value.r = array[0];
                                            if (array.Length >= 2) value.g = array[1];
                                            if (array.Length >= 3) value.b = array[2];
                                            if (array.Length >= 4) value.a = array[3];
                                            else value.a = 1.0f;
                                            imp_constant.ColorValue = value;
                                        }
                                            break;
                                    }

                                sp.SetDefaultImplementations(sp.implementations.ToArray());
                                sp.ForceUpdateDefaultHash();

                                // TODO RESTRICT IMPLEMENTATIONS USABLE BY THIS SHADER PROPERTY

                                shaderProperties.Add(sp);
                            }
                        }

                        // Unpack serialized data into the shader properties
                        if (tempSerializedShaderProperties != null)
                        {
                            foreach (ShaderProperty sp in shaderProperties)
                                if (tempSerializedShaderProperties.ContainsKey(sp.Name))
                                {
                                    Func<object, string, object> onDeserializeImplementation = (impObj, impData) =>
                                    {
                                        return ShaderGenerator2.CurrentConfig.DeserializeImplementationHandler(
                                            impObj, impData, sp);
                                    };
                                    Dictionary<Type, Func<object, string, object>> implementationHandling = new()
                                        { { typeof(ShaderProperty.Implementation), onDeserializeImplementation } };

                                    string serializedData = tempSerializedShaderProperties[sp.Name];
                                    Serialization.DeserializeTo(sp, serializedData, typeof(ShaderProperty), null,
                                        implementationHandling);
                                }

                            foreach (ShaderProperty sp in shaderProperties)
                            {
                                sp.CheckErrors();
                                sp.CheckHash();
                            }
                        }
                    }

                    internal void InjectCode(StringBuilder stringBuilder, string indent)
                    {
                        List<string> newLines = GetCodeLinesWithReplacedVariables(indent);
                        foreach (string line in newLines) stringBuilder.AppendLine(line);
                    }

                    internal List<string> GetCodeLinesWithReplacedVariables(string indent)
                    {
                        List<string> newLinesList = new();
                        foreach (string line in block.codeLines)
                        {
                            string newLine = null;
                            foreach (ShaderProperty sp in shaderProperties)
                            {
                                string variableName = sp.Name.Substring(0,
                                    sp.Name.Length - GetShaderPropertyNameSuffix().Length);
                                string pattern = string.Format("\\b{0}\\b", variableName);
                                if (Regex.IsMatch(line, pattern))
                                {
                                    // figure out indent from current line to properly align variable declaration
                                    string lineIndent = "";
                                    for (int i = 0; i < line.Length; i++)
                                        if (char.IsWhiteSpace(line[i]))
                                            lineIndent += line[i];
                                        else
                                            break;

                                    // append variable declaration
                                    newLinesList.Add(indent + lineIndent +
                                                     sp.PrintVariableSampleDeferred(ShaderGenerator2.CurrentInput,
                                                         ShaderGenerator2.CurrentOutput,
                                                         ShaderGenerator2.CurrentProgram, null, true));

                                    // replace variable name with declared variable name from shader property
                                    newLine = Regex.Replace(line, pattern, sp.GetVariableName());
                                }
                            }

                            newLinesList.Add(indent + (newLine ?? line));
                        }

                        return newLinesList;
                    }
                }

                internal class InjectableBlock
                {
                    internal string autoInjection;
                    internal string[] codeLines;
                    internal string info;

                    internal bool isReplaceBlock;
                    internal string name;
                    internal string searchString;
                    internal List<ShaderPropertyInfo> shaderPropertiesInfos = new();

                    internal bool IsSameAs(InjectableBlock otherBlock)
                    {
                        if (!isReplaceBlock)
                        {
                            bool same = !otherBlock.isReplaceBlock && name == otherBlock.name &&
                                        autoInjection == otherBlock.autoInjection && shaderPropertiesInfos.Count ==
                                        otherBlock.shaderPropertiesInfos.Count;
                            if (!same) return false;

                            // verify shader properties, in case they have changed
                            for (int i = 0; i < shaderPropertiesInfos.Count; i++)
                                if (!shaderPropertiesInfos[i].IsSameAs(otherBlock.shaderPropertiesInfos[i]))
                                    return false;

                            return true;
                        }

                        return otherBlock.isReplaceBlock && name == otherBlock.name &&
                               otherBlock.searchString == searchString && otherBlock.info == info;
                    }
                }

                internal class ShaderPropertyInfo
                {
                    private readonly ShaderProperty.ColorPrecision colorPrecision = ShaderProperty.ColorPrecision.LDR;
                    internal string defaultValue;

                    private readonly ShaderProperty.FloatPrecision
                        floatPrecision = ShaderProperty.FloatPrecision.@float;

                    internal string name;

                    internal ShaderProperty.ProgramType
                        programType =
                            ShaderProperty.ProgramType
                                .Undefined; // ==> should be determined by where the injection point is hooked

                    internal ShaderProperty.VariableType variableType = ShaderProperty.VariableType.@float;

                    internal bool IsSameAs(ShaderPropertyInfo other)
                    {
                        return programType == other.programType
                               && variableType == other.variableType
                               && colorPrecision == other.colorPrecision
                               && floatPrecision == other.floatPrecision
                               && name == other.name
                               && defaultValue == other.defaultValue;
                    }
                }

                // Select a file to inject blocks from, to one or more injection points
                [Serialization.SerializeAs("injectedFile")]
                internal class InjectedFile
                {
                    private int contentHash;

                    [Serialization.SerializeAs("filename")]
                    private string filename;

                    [Serialization.SerializeAs("guid")] private string guid;

                    // UI
                    private readonly Dictionary<InjectedPoint, bool> headersExpanded = new();
                    internal TextAsset includeFile;
                    internal List<InjectableBlock> injectableBlocks = new();

                    [Serialization.SerializeAs("injectedPoints")]
                    internal List<InjectedPoint> injectedPoints = new();

                    private readonly ReorderableLayoutList injectedPointsList = new();
                    private GenericMenu pendingBlockMenu;
                    private int replaceBlockCount;

                    public InjectedFile()
                    {
                        ShaderGenerator2.onProjectChange += onProjectChanged;
                    }

                    internal void WillBeRemoved()
                    {
                        ShaderGenerator2.onProjectChange -= onProjectChanged;
                    }

                    private void onProjectChanged()
                    {
                        VerifyCodeInjectionFile();
                    }

                    [Serialization.OnDeserializeCallback]
                    private void OnDeserialize()
                    {
                        // Find back includeFile from guid
                        if (!string.IsNullOrEmpty(guid))
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            if (string.IsNullOrEmpty(path))
                            {
                                Debug.LogError("[SG2 Code Injection] Can't find path for Code Injection file GUID: " +
                                               guid + " (filename: \"" + filename + "\")");
                                return;
                            }

                            TextAsset file = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                            TryParseIncludeFile(file, null);
                        }

                        // Link back blocks to injected points and update headers array
                        for (int i = injectedPoints.Count - 1; i >= 0; i--)
                        {
                            InjectedPoint ip = injectedPoints[i];

                            if (string.IsNullOrEmpty(ip.blockName))
                            {
                                Debug.LogWarning("[SG2 Code Injection] Block name was not properly serialized.");
                                injectedPoints.RemoveAt(i);
                                continue;
                            }

                            InjectableBlock matchingBlock = injectableBlocks.Find(block => block.name == ip.blockName);
                            if (matchingBlock == null)
                            {
                                Debug.LogWarning(string.Format(
                                    "[SG2 Code Injection] Block wasn't found in source file. Block name: \"{0}\", Source file: \"{1}\"",
                                    ip.blockName, filename));
                                injectedPoints.RemoveAt(i);
                                continue;
                            }

                            ip.block = matchingBlock;
                            ip.UpdateShaderProperties();
                            headersExpanded[ip] = false;
                        }

                        VerifyCodeInjectionFile();
                    }

                    private void VerifyCodeInjectionFile()
                    {
                        if (!string.IsNullOrEmpty(guid))
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            string fileContent =
                                File.ReadAllText(Application.dataPath.Substring(0,
                                    Application.dataPath.Length - "Assets".Length) + path);
                            int fileHash = fileContent.GetHashCode();

                            if (fileHash == contentHash)
                                // same hash, no need to verify
                                return;
                            contentHash = fileHash;

                            List<InjectableBlock> blockList = TryParseFileForInjectableBlocks(fileContent);

                            // Iterate existing blocks and see if they still exist in the file:
                            for (int i = injectableBlocks.Count - 1; i >= 0; i--)
                            {
                                InjectableBlock existingBlock = injectableBlocks[i];

                                if (!blockList.Exists(b => b.IsSameAs(existingBlock)))
                                {
                                    // Block doesn't exist anymore in the source file: remove it
                                    if (existingBlock.isReplaceBlock)
                                    {
                                        replaceBlockCount--;
                                        RemoveReplaceBlock(existingBlock);
                                    }

                                    injectableBlocks.RemoveAt(i);

                                    for (int j = injectedPoints.Count - 1; j >= 0; j--)
                                        if (injectedPoints[j].blockName == existingBlock.name)
                                            RemoveInjectedPoint(j);
                                }
                            }

                            foreach (InjectableBlock fileBlock in blockList)
                            {
                                // Block from file is new: add it
                                if (!injectableBlocks.Exists(b => b.IsSameAs(fileBlock)))
                                {
                                    injectableBlocks.Add(fileBlock);
                                    if (fileBlock.isReplaceBlock)
                                    {
                                        replaceBlockCount++;
                                        AddReplaceBlock(fileBlock);
                                    }
                                }

                                // Auto-injected block that isn't injected yet
                                if (fileBlock.autoInjection != null &&
                                    !injectedPoints.Exists(item => item.blockName == fileBlock.name))
                                {
                                    Template.InjectionPoint ip =
                                        ShaderGenerator2.CurrentTemplate.injectionPoints.Find(item =>
                                            item.name == fileBlock.autoInjection);
                                    if (ip != null) AddBlockAtInjectionPoint(ip, fileBlock);
                                }
                            }

                            foreach (InjectedPoint injectedPoint in injectedPoints)
                                injectedPoint.UpdateShaderProperties();
                        }
                    }

                    private bool TryParseIncludeFile(TextAsset file, Template template)
                    {
                        // template == null means we're doing that after deserialization
                        if (template != null) injectedPoints.Clear();

                        injectableBlocks.Clear();
                        headersExpanded.Clear();
                        replaceBlockCount = 0;

                        if (file == null)
                        {
                            includeFile = null;
                            guid = null;
                            filename = null;
                            return true;
                        }

                        string fileContent =
                            File.ReadAllText(
                                Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) +
                                AssetDatabase.GetAssetPath(file));
                        if (string.IsNullOrEmpty(fileContent)) return false;

                        contentHash = fileContent.GetHashCode();

                        List<InjectableBlock> fileBlocks = TryParseFileForInjectableBlocks(fileContent);
                        if (fileBlocks == null) return false;

                        foreach (InjectableBlock block in fileBlocks)
                        {
                            injectableBlocks.Add(block);
                            if (block.isReplaceBlock)
                            {
                                replaceBlockCount++;
                                AddReplaceBlock(block);
                            }
                        }

                        includeFile = file;
                        filename = includeFile.name;
                        guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(includeFile));

                        // Parse auto-inject points and add them if found
                        if (template != null)
                            foreach (InjectableBlock block in fileBlocks)
                                if (block.autoInjection != null)
                                    foreach (Template.InjectionPoint injectionPoint in template.injectionPoints)
                                        if (injectionPoint.name == block.autoInjection)
                                            AddBlockAtInjectionPoint(injectionPoint, block);

                        return true;
                    }

                    private List<InjectableBlock> TryParseFileForInjectableBlocks(string fileContent)
                    {
                        List<InjectableBlock> blockList = new();

                        using (StringReader stringReader = new(fileContent))
                        {
                            int lineNb = 0;
                            string line;
                            try
                            {
                                InjectableBlock currentBlock = null;
                                List<string> codeLines = new();

                                Action addCurrentBlock = () =>
                                {
                                    if (currentBlock != null)
                                    {
                                        int i = codeLines.Count - 1;
                                        while (i >= 0 && codeLines[i] == "")
                                        {
                                            codeLines.RemoveAt(codeLines.Count - 1);
                                            i--;
                                        }

                                        if (codeLines.Count > 0
                                            && ((currentBlock.isReplaceBlock && currentBlock.searchString != "")
                                                || !currentBlock.isReplaceBlock))
                                        {
                                            currentBlock.codeLines = codeLines.ToArray();
                                            blockList.Add(currentBlock);
                                        }

                                        codeLines.Clear();
                                    }
                                };

                                bool parsingSearchString = false;
                                while ((line = stringReader.ReadLine()) != null)
                                {
                                    string trimmedLine = line.Trim();
                                    string trimmedLineLower = trimmedLine.ToLowerInvariant();

                                    if (line.StartsWith("///")) continue;

                                    if (line.StartsWith("//# "))
                                    {
                                        // new block
                                        if (trimmedLine.StartsWith("//# BLOCK:"))
                                        {
                                            addCurrentBlock();

                                            string blockName = trimmedLine.Substring("//# BLOCK:".Length).Trim();
                                            if (string.IsNullOrEmpty(blockName))
                                                throw new Exception(
                                                    "Line '//# BLOCK:' requires a name, please see the documentation");

                                            currentBlock = new InjectableBlock
                                            {
                                                name = blockName
                                            };
                                        }
                                        // new replace block
                                        else if (trimmedLine.StartsWith("//# REPLACE:"))
                                        {
                                            addCurrentBlock();

                                            string blockName = trimmedLine.Substring("//# REPLACE:".Length).Trim();
                                            if (string.IsNullOrEmpty(blockName))
                                                throw new Exception(
                                                    "Line '//# REPLACE:' requires a name, please see the documentation");

                                            currentBlock = new InjectableBlock
                                            {
                                                name = blockName,
                                                isReplaceBlock = true,
                                                searchString = ""
                                            };
                                            parsingSearchString = true;
                                        }
                                        else if (trimmedLine.StartsWith("//# WITH:"))
                                        {
                                            if (currentBlock == null)
                                                throw new Exception("'WITH:' tag outside of block");
                                            if (!currentBlock.isReplaceBlock)
                                                throw new Exception("'WITH:' tag only works with 'REPLACE:' blocks");

                                            // replace block replacement
                                            parsingSearchString = false;
                                        }
                                        else if (trimmedLineLower.StartsWith("//# inject @"))
                                        {
                                            if (currentBlock == null)
                                                throw new Exception("'Inject @' tag outside of block");

                                            string autoInjectPoint =
                                                trimmedLine.Substring("//# inject @".Length).Trim();

                                            currentBlock.autoInjection = autoInjectPoint;
                                        }
                                        else if (trimmedLineLower.StartsWith("//# info:"))
                                        {
                                            if (currentBlock == null)
                                                throw new Exception("'INFO:' tag outside of block");
                                            if (!currentBlock.isReplaceBlock)
                                                throw new Exception("'INFO:' tag only works with 'REPLACE:' blocks");

                                            currentBlock.info = trimmedLine.Substring("//# info:".Length).Trim();
                                        }
                                        // variable to parse
                                        else if (trimmedLineLower.StartsWith("//# float") ||
                                                 trimmedLineLower.StartsWith("//# fragment") ||
                                                 trimmedLineLower.StartsWith("//# vertex"))
                                        {
                                            // Prevent Shader Properties for Replace blocks, it's more complicated than I initially thought to implement...
                                            if (currentBlock.isReplaceBlock) continue;

                                            if (currentBlock == null)
                                                throw new Exception("Property declaration outside of block");

                                            if (currentBlock.isReplaceBlock && trimmedLineLower.StartsWith("//# float"))
                                                throw new Exception(
                                                    "//# REPLACE block variables must declare their shader program first ('vertex' or 'fragment')");

                                            if (!currentBlock.isReplaceBlock &&
                                                !trimmedLineLower.StartsWith("//# float"))
                                                throw new Exception(
                                                    "Regular block variables must not declare the shader program");

                                            string[] parts = trimmedLine.Split(new[] { ' ', '\t' },
                                                StringSplitOptions.RemoveEmptyEntries);
                                            int startIndex = currentBlock.isReplaceBlock ? 1 : 0;

                                            ShaderProperty.VariableType variableType;
                                            switch (parts[startIndex + 1])
                                            {
                                                case "float":
                                                    variableType = ShaderProperty.VariableType.@float;
                                                    break;
                                                case "float2":
                                                    variableType = ShaderProperty.VariableType.float2;
                                                    break;
                                                case "float3":
                                                    variableType = ShaderProperty.VariableType.float3;
                                                    break;
                                                case "float4":
                                                    variableType = ShaderProperty.VariableType.float4;
                                                    break;
                                                case "color":
                                                    variableType = ShaderProperty.VariableType.color_rgba;
                                                    break;
                                                case "color_rgba":
                                                    variableType = ShaderProperty.VariableType.color_rgba;
                                                    break;
                                                default:
                                                    throw new Exception("Invalid parsed property type: " + parts[1]);
                                            }

                                            string name = parts[startIndex + 2];
                                            string defaultValue = parts.Length >= startIndex + 4
                                                ? parts[startIndex + 3]
                                                : null;

                                            // check if property already exists with this block
                                            foreach (ShaderPropertyInfo existingSpi in currentBlock
                                                         .shaderPropertiesInfos)
                                                if (existingSpi.name == name)
                                                    throw new Exception(
                                                        "A property already exists with the same name: " + name);

                                            ShaderPropertyInfo spi = new()
                                            {
                                                name = name,
                                                variableType = variableType,
                                                defaultValue = defaultValue
                                            };

                                            if (currentBlock.isReplaceBlock)
                                            {
                                                ShaderProperty.ProgramType programType =
                                                    ShaderProperty.ProgramType.Fragment;
                                                if (parts[0].ToLowerInvariant() == "vertex")
                                                    programType = ShaderProperty.ProgramType.Vertex;
                                                spi.programType = programType;
                                            }

                                            currentBlock.shaderPropertiesInfos.Add(spi);
                                        }
                                    }
                                    else if (currentBlock != null)
                                    {
                                        if (currentBlock.isReplaceBlock && parsingSearchString)
                                            currentBlock.searchString +=
                                                currentBlock.searchString == "" ? line : "\n" + line;
                                        else
                                            codeLines.Add(line);
                                    }

                                    lineNb++;
                                }

                                addCurrentBlock();
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(string.Format(
                                    "[SG2 Code Injection] Couldn't load code injection include file, error at line {0}:  {1}",
                                    lineNb, e));
                                return null;
                            }
                        }

                        return blockList;
                    }

                    internal void ShowGUI(Template template, float margin)
                    {
                        if (pendingBlockMenu != null)
                        {
                            pendingBlockMenu.ShowAsContext();
                            pendingBlockMenu = null;
                        }


                        // Include file

                        TextAsset newIncludeFile = includeFile;
                        Action parseNewFile = () =>
                        {
                            if (newIncludeFile != includeFile)
                                if (!TryParseIncludeFile(newIncludeFile, template))
                                {
                                    includeFile = null;
                                    Debug.LogError(ShaderGenerator2.ErrorMsg(
                                        "[SG2 Code Injection] Couldn't load code injection include file."));
                                }
                        };

                        if (includeFile == null)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(margin);
                                newIncludeFile = (TextAsset)EditorGUILayout.ObjectField(
                                    TCP2_GUI.TempContent("Source File",
                                        "Select a source file from which to insert custom code, with the .cginc or .hlslinc format"),
                                    includeFile, typeof(TextAsset), false);
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(margin);
                                EditorGUILayout.HelpBox(
                                    "Please select a valid include file with the correct formatting adapted for Code Injection.\nSee the documentation for more information!",
                                    MessageType.Info);
                            }
                            GUILayout.EndHorizontal();
                            parseNewFile();
                            return;
                        }

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(margin);
                            newIncludeFile = (TextAsset)EditorGUILayout.ObjectField(
                                TCP2_GUI.TempContent("Source File",
                                    "Select a source file from which to insert custom code, with the .cginc or .hlslinc format"),
                                includeFile, typeof(TextAsset), false);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(margin);
                            if (GUILayout.Button("Add Block at Injection Point", GUILayout.ExpandWidth(false)))
                            {
                                GenericMenu injectionPointMenu = new();
                                injectionPointMenu.AddDisabledItem(new GUIContent("Select an injection point:"));
                                injectionPointMenu.AddSeparator("");

                                foreach (Template.InjectionPoint ip in template.injectionPoints)
                                    injectionPointMenu.AddItem(new GUIContent(ip.name), false, OnAddInjectionPoint, ip);

                                if (template.injectionPoints.Count == 0)
                                    injectionPointMenu.AddDisabledItem(
                                        new GUIContent("No injection points were found in this template!"));

                                if (injectableBlocks.Count == 0)
                                    injectionPointMenu.AddDisabledItem(
                                        new GUIContent("No injectable blocks were found in the selected file!"));

                                injectionPointMenu.ShowAsContext();
                            }
                        }
                        GUILayout.EndHorizontal();

                        int injectedPointToRemove = -1;

                        // List of added blocks/injection point
                        Action<int, float> drawInjectedPoint = (index, margin2) =>
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(margin2);
                                TCP2_GUI.SeparatorSimple();
                            }
                            GUILayout.EndHorizontal();

                            InjectedPoint point = injectedPoints[index];

                            Rect removeButtonRect;
                            Rect enableButtonRect;

                            bool guiEnabled = GUI.enabled;
                            GUI.enabled &= point.enabled;

                            EditorGUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(margin + margin2);

                                GUIContent label =
                                    TCP2_GUI.TempContent(point.isReplace ? "Replace block" : "@ " + point.name);
                                Rect rect = GUILayoutUtility.GetRect(label, EditorStyles.label,
                                    GUILayout.ExpandWidth(true));
                                rect.xMin += 4; // small left padding

                                enableButtonRect = rect;
                                enableButtonRect.width = 20;
                                rect.xMin += enableButtonRect.width;

                                removeButtonRect = rect;
                                removeButtonRect.width = 22;
                                removeButtonRect.height = 22;
                                removeButtonRect.y -= 22 - rect.height;
                                removeButtonRect.x += rect.width;

                                GUI.Label(rect, label, EditorStyles.label);

                                GUILayout.Space(removeButtonRect.width);
                            }
                            EditorGUILayout.EndHorizontal();

                            margin2 += enableButtonRect.width;

                            EditorGUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(margin + margin2);

                                EditorGUI.BeginChangeCheck();
                                {
                                    // hover rect as in 2019.3 UI
                                    GUIContent label = TCP2_GUI.TempContent(point.block.name);
                                    Rect rect = GUILayoutUtility.GetRect(label, EditorStyles.foldout,
                                        GUILayout.ExpandWidth(true));
                                    Rect hoverRect = rect;
                                    rect.xMin += 4; // small left padding

                                    // removeButtonRect.yMax = rect.yMax;
                                    // rect.xMax -= removeButtonRect.width;

                                    bool hasShaderProperties = point.shaderProperties.Count > 0;
                                    if (hasShaderProperties)
                                    {
                                        TCP2_GUI.DrawHoverRect(hoverRect);
                                        bool highlight = point.shaderProperties.Exists(sp => sp.manuallyModified);
                                        headersExpanded[point] = TCP2_GUI.HeaderFoldoutHighlightErrorGrayPosition(rect,
                                            headersExpanded[point], label, false, highlight);
                                    }
                                    else
                                    {
                                        GUI.Label(rect, label, EditorStyles.boldLabel);
                                    }
                                }
                                if (EditorGUI.EndChangeCheck())
                                {
                                    // expand/fold all when alt/control is held
                                    /*
                                    if (Event.current.alt || Event.current.control)
                                    {
                                        if (headersExpanded[group.header.text])
                                        {
                                            ExpandAllGroups();
                                        }
                                        else
                                        {
                                            FoldAllGroups();
                                        }
                                    }
                                    */
                                }

                                GUILayout.Space(removeButtonRect.width);
                            }
                            EditorGUILayout.EndHorizontal();

                            if (point.isReplace && !string.IsNullOrEmpty(point.block.info))
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    GUILayout.Space(margin + margin2 + 4);
                                    GUILayout.Label(TCP2_GUI.TempContent(point.block.info),
                                        EditorStyles.wordWrappedMiniLabel);
                                    GUILayout.Space(removeButtonRect.width);
                                }
                                EditorGUILayout.EndHorizontal();
                            }

                            GUI.enabled = guiEnabled;

                            // Enable button
                            Rect lastRect = GUILayoutUtility.GetLastRect();
                            enableButtonRect.y = (lastRect.y + enableButtonRect.y) / 2.0f;
                            point.enabled = GUI.Toggle(enableButtonRect, point.enabled, GUIContent.none);

                            // Remove button
                            removeButtonRect.y = (lastRect.y + removeButtonRect.y) / 2.0f;
                            if (!point.isReplace && GUI.Button(removeButtonRect, "X")) injectedPointToRemove = index;

                            if (headersExpanded[point])
                                foreach (ShaderProperty sp in point.shaderProperties)
                                    sp.ShowGUILayout(margin + margin2 + 8);
                        };

                        GUILayout.Space(4);

                        // List of injected blocks
                        RectOffset injectedPointListPadding = new((int)margin, 0, 0, 0);
                        injectedPointsList.DoLayoutList(drawInjectedPoint, injectedPoints, injectedPointListPadding);

                        if (injectedPointToRemove >= 0) RemoveInjectedPoint(injectedPointToRemove);

                        GUILayout.Space(2);

                        parseNewFile();
                    }


                    private void RemoveInjectedPoint(int index)
                    {
                        InjectedPoint ip = injectedPoints[index];
                        headersExpanded.Remove(ip);
                        injectedPoints.RemoveAt(index);
                    }

                    private void OnAddInjectionPoint(object ip)
                    {
                        Template.InjectionPoint injectionPoint = (Template.InjectionPoint)ip;
                        GenericMenu blocksMenu = new();
                        blocksMenu.AddDisabledItem(new GUIContent("Select a code block to inject:"));
                        blocksMenu.AddSeparator("");

                        foreach (InjectableBlock block in injectableBlocks)
                        {
                            if (block.isReplaceBlock) continue;

                            if (injectedPoints.Exists(item => item.block == block))
                                blocksMenu.AddDisabledItem(new GUIContent(block.name + " (already added)"));
                            else
                                blocksMenu.AddItem(new GUIContent(block.name), false, OnAddBlock,
                                    new object[] { injectionPoint, block });
                        }

                        pendingBlockMenu = blocksMenu;
                    }

                    private void OnAddBlock(object data)
                    {
                        object[] array = (object[])data;
                        Template.InjectionPoint injectionPoint = (Template.InjectionPoint)array[0];
                        InjectableBlock block = (InjectableBlock)array[1];

                        AddBlockAtInjectionPoint(injectionPoint, block);
                    }

                    private void AddBlockAtInjectionPoint(Template.InjectionPoint injectionPoint, InjectableBlock block)
                    {
                        InjectedPoint ip = new(injectionPoint.name, injectionPoint.program, block);
                        injectedPoints.Add(ip);
                        headersExpanded.Add(ip, true);
                    }

                    private void AddReplaceBlock(InjectableBlock block)
                    {
                        if (!block.isReplaceBlock) return;

                        if (injectedPoints.Exists(i => i.blockName == block.name)) return;

                        InjectedPoint ip = new()
                        {
                            isReplace = true,
                            block = block,
                            blockName = block.name,
                            info = block.info
                        };
                        injectedPoints.Add(ip);
                        ip.UpdateShaderProperties();
                        headersExpanded.Add(ip, true);
                    }

                    private void RemoveReplaceBlock(InjectableBlock block)
                    {
                        InjectedPoint foundIp = injectedPoints.Find(ip => ip.blockName == block.name);
                        injectedPoints.Remove(foundIp);
                    }
                }
            }
        }
    }
}
