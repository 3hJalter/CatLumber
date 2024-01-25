// Toony Colors Pro+Mobile 2
// (c) 2014-2023 Jean Moreno

#define WRITE_UNCOMPRESSED_SERIALIZED_DATA

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using ToonyColorsPro.ShaderGenerator.CodeInjection;
using ToonyColorsPro.Utilities;
using UnityEditor;
using UnityEngine;

// Represents a Toony Colors Pro 2 configuration to generate the corresponding shader
// (new version for Shader Generator 2)

namespace ToonyColorsPro
{
    namespace ShaderGenerator
    {
        internal interface IMaterialPropertyName
        {
            string GetPropertyName();
        }

        internal static class UniqueMaterialPropertyName
        {
            internal static event CheckUniqueVariableName checkUniqueVariableName;

            internal static string GetUniquePropertyName(string baseName, IMaterialPropertyName materialPropertyName)
            {
                if (checkUniqueVariableName == null) return baseName;

                //name doesn't exist: all good
                if (checkUniqueVariableName(baseName, materialPropertyName))
                    return baseName;

                //extract the last digits of the name, if any
                for (int i = baseName.Length - 1; i >= 0; i--)
                {
                    if (baseName[i] >= '0' && baseName[i] <= '9')
                        continue;
                    baseName = baseName.Substring(0, i + 1);
                    break;
                }

                //check if name is unique: requires a class that registers to the event and supply its own checks
                string newName = baseName;
                int count = 1;
                while (!checkUniqueVariableName(newName, materialPropertyName))
                {
                    newName = string.Format("{0}{1}", baseName, count);
                    count++;
                }

                return newName;
            }

            internal delegate bool CheckUniqueVariableName(string variableName,
                IMaterialPropertyName materialPropertyName);
        }

        [Serialization.SerializeAs("config")]
        internal class Config
        {
            internal const string kSerializationPrefix = "/* TCP_DATA ";
            internal const string kSerializationPrefixUncompressed = "/* TCP_DATA u ";
            internal const string kSerializationSuffix = " */";

            internal const string kHashPrefix = "/* TCP_HASH ";
            internal const string kHashSuffix = " */";

            // Cache the expanded state of the visible shader properties, to restore them after shader generation/update
            private static HashSet<string> expandedCache;
            private static Dictionary<string, bool> headersExpandedCache;

            private KeyValuePair<string, string>[] _materialLayersNames;

            //Serialize all cached Shader Properties so that their custom implementation is saved, even if they are not used in the shader
            [Serialization.SerializeAs("shaderProperties")]
            private readonly List<ShaderProperty> cachedShaderProperties = new();


            // Code Injection properties
            [Serialization.SerializeAs("codeInjection")]
            internal CodeInjectionManager codeInjection = new();

            [Serialization.SerializeAs("customTextures")]
            private List<ShaderProperty.CustomMaterialProperty> customMaterialPropertiesList = new();

            public ShaderProperty customMaterialPropertyShaderProperty =
                new("_CustomMaterialPropertyDummy", ShaderProperty.VariableType.color_rgba);

            private readonly ReorderableLayoutList customTexturesLayoutList = new();
            internal List<string> ExtraTempFeatures = new();

            [Serialization.SerializeAs("features")]
            internal List<string> Features = new();

            internal string Filename = "My TCP2 Shader";
            [Serialization.SerializeAs("flags")] internal List<string> Flags = new();

            [Serialization.SerializeAs("flags_extra")]
            internal Dictionary<string, List<string>> FlagsExtra = new();

            private readonly Dictionary<string, bool>
                headersExpanded =
                    new(); // the struct array above is always recreated, so we can't track expanded state there

            internal bool isModifiedExternally;

            [Serialization.SerializeAs("keywords")]
            internal Dictionary<string, string> Keywords = new();

            // Material Layers
            [Serialization.SerializeAs("matLayers")]
            internal List<MaterialLayer> materialLayers = new();

            private readonly ReorderableLayoutList matLayersLayoutList = new();
            private int selected;
            internal string ShaderName = "Toony Colors Pro 2/User/My TCP2 Shader";
            private List<List<ShaderProperty>> shaderPropertiesPerPass;
            private readonly List<ShaderPropertyGroup> shaderPropertiesUIGroups = new();

            // Material Layers UI
            private float tabOffsets;
            private float tabOffsetsTarget;
            [Serialization.SerializeAs("tmplt")] internal string templateFile = "TCP2_ShaderTemplate_Default";
            private readonly List<ShaderProperty> visibleShaderProperties = new();

            internal bool isTerrainShader => Features.Contains("TERRAIN_SHADER");

            internal ShaderProperty.CustomMaterialProperty[] CustomMaterialProperties =>
                customMaterialPropertiesList.ToArray();

            internal ShaderProperty[] VisibleShaderProperties => visibleShaderProperties.ToArray();
            internal ShaderProperty[] AllShaderProperties => cachedShaderProperties.ToArray();

            internal KeyValuePair<string, string>[] materialLayersNames
            {
                get
                {
                    if (_materialLayersNames == null || _materialLayersNames.Length != materialLayers.Count)
                    {
                        List<KeyValuePair<string, string>> list = materialLayers.ConvertAll(element =>
                            new KeyValuePair<string, string>(element.name, element.uid));
                        list.Insert(0, new KeyValuePair<string, string>("Base", null));
                        _materialLayersNames = list.ToArray();
                    }

                    return _materialLayersNames;
                }
            }

            /// Iterate through all Shader Properties associated with this config, including Material Layers and Code Injection
            private IEnumerable<ShaderProperty> IterateAllShaderProperties()
            {
                HashSet<ShaderProperty> processed = new();
                foreach (ShaderProperty shaderProperty in cachedShaderProperties)
                {
                    if (processed.Contains(shaderProperty)) continue;

                    processed.Add(shaderProperty);
                    yield return shaderProperty;
                }

                foreach (ShaderProperty shaderProperty in visibleShaderProperties)
                {
                    if (processed.Contains(shaderProperty)) continue;

                    processed.Add(shaderProperty);
                    yield return shaderProperty;
                }

                foreach (MaterialLayer materialLayer in materialLayers)
                {
                    if (materialLayer.sourceShaderProperty != null)
                    {
                        if (processed.Contains(materialLayer.sourceShaderProperty)) continue;
                        processed.Add(materialLayer.sourceShaderProperty);
                        yield return materialLayer.sourceShaderProperty;
                    }

                    if (materialLayer.noiseProperty != null)
                    {
                        if (processed.Contains(materialLayer.noiseProperty)) continue;
                        processed.Add(materialLayer.noiseProperty);
                        yield return materialLayer.noiseProperty;
                    }

                    if (materialLayer.contrastProperty != null)
                    {
                        if (processed.Contains(materialLayer.contrastProperty)) continue;
                        processed.Add(materialLayer.contrastProperty);
                        yield return materialLayer.contrastProperty;
                    }
                }

                foreach (CodeInjectionManager.InjectedFile injectedFile in codeInjection.injectedFiles)
                foreach (CodeInjectionManager.InjectedPoint point in injectedFile.injectedPoints)
                foreach (ShaderProperty shaderProperty in point.shaderProperties)
                    if (shaderProperty != null)
                    {
                        if (processed.Contains(shaderProperty)) continue;
                        processed.Add(shaderProperty);
                        yield return shaderProperty;
                    }
            }

            internal MaterialLayer GetMaterialLayerByUID(string uid)
            {
                return materialLayers.Find(ml => ml.uid == uid);
            }

            internal string[] GetShaderPropertiesNeededFeaturesForPass(int passIndex)
            {
                if (shaderPropertiesPerPass == null || shaderPropertiesPerPass.Count == 0)
                    return new string[0];

                if (passIndex >= shaderPropertiesPerPass.Count)
                    return new string[0];

                if (shaderPropertiesPerPass[passIndex] == null || shaderPropertiesPerPass[passIndex].Count == 0)
                    return new string[0];

                List<string> usedMaterialLayersVertex = new();
                List<string> usedMaterialLayersFragment = new();
                List<string> features = new();
                foreach (ShaderProperty sp in shaderPropertiesPerPass[passIndex])
                {
                    features.AddRange(sp.NeededFeatures());

                    // figure out used MaterialLayers and their programs
                    foreach (string uid in sp.linkedMaterialLayers)
                        if (sp.Program == ShaderProperty.ProgramType.Vertex)
                        {
                            if (!usedMaterialLayersVertex.Contains(uid)) usedMaterialLayersVertex.Add(uid);
                        }
                        else if (sp.Program == ShaderProperty.ProgramType.Fragment)
                        {
                            if (!usedMaterialLayersFragment.Contains(uid)) usedMaterialLayersFragment.Add(uid);
                        }
                }

                // needed features for Material Layer sources
                // HACK: We override the program type so that the relevant needed features get added.
                //       This is cleaner than refactoring all the methods called.

                Action<ShaderProperty, ShaderProperty.ProgramType> GetNeededFeaturesForProperty =
                    (shaderProperty, programType) =>
                    {
                        if (shaderProperty == null) return;

                        ShaderProperty.ProgramType program = shaderProperty.Program;
                        shaderProperty.Program = programType;
                        {
                            features.AddRange(shaderProperty.NeededFeatures());
                        }
                        shaderProperty.Program = program;
                    };

                foreach (string uid in usedMaterialLayersVertex)
                {
                    MaterialLayer ml = GetMaterialLayerByUID(uid);
                    GetNeededFeaturesForProperty(ml.sourceShaderProperty, ShaderProperty.ProgramType.Vertex);
                    GetNeededFeaturesForProperty(ml.contrastProperty, ShaderProperty.ProgramType.Vertex);
                    GetNeededFeaturesForProperty(ml.noiseProperty, ShaderProperty.ProgramType.Vertex);
                }

                foreach (string uid in usedMaterialLayersFragment)
                {
                    MaterialLayer ml = GetMaterialLayerByUID(uid);
                    GetNeededFeaturesForProperty(ml.sourceShaderProperty, ShaderProperty.ProgramType.Fragment);
                    GetNeededFeaturesForProperty(ml.contrastProperty, ShaderProperty.ProgramType.Fragment);
                    GetNeededFeaturesForProperty(ml.noiseProperty, ShaderProperty.ProgramType.Fragment);
                }

                return features.Distinct().ToArray();
            }

            internal string[] GetShaderPropertiesNeededFeaturesAll()
            {
                if (shaderPropertiesPerPass == null || shaderPropertiesPerPass.Count == 0) return new string[0];

                List<string> features = new();
                for (int i = 0; i < shaderPropertiesPerPass.Count; i++)
                    features.AddRange(GetShaderPropertiesNeededFeaturesForPass(i));
                return features.Distinct().ToArray();

                /*

                if (shaderPropertiesPerPass == null || shaderPropertiesPerPass.Count == 0)
                    return new string[0];

                // iterate through used Shader Properties for all passes and toggle needed features
                List<string> usedMaterialLayers = new List<string>();
                var features = new List<string>();
                foreach (var list in shaderPropertiesPerPass)
                {
                    foreach (var sp in list)
                    {
                        features.AddRange(sp.NeededFeatures());

                        foreach (string uid in sp.linkedMaterialLayers)
                        {
                            if (!usedMaterialLayers.Contains(uid))
                            {
                                usedMaterialLayers.Add(uid);
                            }
                        }
                    }
                }

                // needed features for Material Layer sources
                foreach (string uid in usedMaterialLayers)
                {
                    var ml = this.GetMaterialLayerByUID(uid);
                    features.AddRange(ml.sourceShaderProperty.NeededFeatures());
                }

                return features.Distinct().ToArray();

                */
            }

            internal string[] GetHooksNeededFeatures()
            {
                // iterate through Hook Shader Properties and toggle features if needed
                List<string> features = new();
                foreach (ShaderProperty sp in visibleShaderProperties)
                    if (sp.isHook && !string.IsNullOrEmpty(sp.toggleFeatures))
                        if (sp.manuallyModified)
                            features.AddRange(sp.toggleFeatures.Split(','));
                return features.ToArray();
            }

            internal string[] GetCodeInjectionNeededFeatures()
            {
                return codeInjection.GetNeededFeatures();
            }

            /// <summary>
            ///     Remove all features associated with specific Shader Property options,
            ///     so that they don't stay when toggling an option on, compile, then off
            /// </summary>
            internal void ClearShaderPropertiesFeatures()
            {
                foreach (string f in ShaderProperty.AllOptionFeatures()) Utils.RemoveIfExists(Features, f);
            }

            internal static Config CreateFromFile(TextAsset asset)
            {
                return CreateFromFile(asset.text);
            }

            internal static Config CreateFromFile(string text)
            {
                string[] lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                Config config = new();

                //Flags
                ParseBlock currentBlock = ParseBlock.None;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    if (line.StartsWith("//")) continue;

                    string[] data = line.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    if (line.StartsWith("#"))
                    {
                        currentBlock = ParseBlock.None;

                        switch (data[0])
                        {
                            case "#filename":
                                config.Filename = data[1];
                                break;
                            case "#shadername":
                                config.ShaderName = data[1];
                                break;
                            case "#features":
                                currentBlock = ParseBlock.Features;
                                break;
                            case "#flags":
                                currentBlock = ParseBlock.Flags;
                                break;

                            default:
                                Debug.LogWarning("[TCP2 Shader Config] Unrecognized tag: " + data[0] + "\nline " +
                                                 (i + 1));
                                break;
                        }
                    }
                    else
                    {
                        if (data.Length > 1)
                        {
                            bool enabled = false;
                            bool.TryParse(data[1], out enabled);

                            if (enabled)
                            {
                                if (currentBlock == ParseBlock.Features)
                                    config.Features.Add(data[0]);
                                else if (currentBlock == ParseBlock.Flags)
                                    config.Flags.Add(data[0]);
                                else
                                    Debug.LogWarning("[TCP2 Shader Config] Unrecognized line while parsing : " + line +
                                                     "\nline " + (i + 1));
                            }
                        }
                    }
                }

                return config;
            }

            internal static Config CreateFromShader(Shader shader)
            {
                ShaderImporter shaderImporter =
                    AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(shader)) as ShaderImporter;

                Config config = new()
                {
                    ShaderName = shader.name,
                    Filename = Path.GetFileName(AssetDatabase.GetAssetPath(shader)).Replace(".shader", "")
                };

                bool valid = config.ParseUserData(shaderImporter);
                valid |= config.ParseSerializedDataAndHash(shaderImporter, null,
                    false); //first run (see method comment)

                if (valid)
                    return config;
                return null;
            }

            internal Config Copy()
            {
                Config config = new()
                {
                    Filename = Filename,
                    ShaderName = ShaderName
                };

                foreach (string feature in Features)
                    config.Features.Add(feature);

                foreach (string flag in Flags)
                    config.Flags.Add(flag);

                foreach (KeyValuePair<string, List<string>> kvp in FlagsExtra)
                    config.FlagsExtra.Add(kvp.Key, new List<string>(kvp.Value));

                foreach (KeyValuePair<string, string> kvp in Keywords)
                    config.Keywords.Add(kvp.Key, kvp.Value);

                config.templateFile = templateFile;

                config.codeInjection = codeInjection;

                return config;
            }

            //Copy implementations from this config to another
            public void CopyImplementationsTo(Config otherConfig)
            {
                for (int i = 0; i < cachedShaderProperties.Count; i++)
                for (int j = 0; j < otherConfig.cachedShaderProperties.Count; j++)
                    if (cachedShaderProperties[i].Name == otherConfig.cachedShaderProperties[j].Name)
                    {
                        otherConfig.cachedShaderProperties[j].implementations =
                            cachedShaderProperties[i].implementations;
                        otherConfig.cachedShaderProperties[j].CheckHash();
                        otherConfig.cachedShaderProperties[j].CheckErrors();
                        break;
                    }

                for (int i = 0; i < otherConfig.cachedShaderProperties.Count; i++)
                    otherConfig.cachedShaderProperties[i].ResolveShaderPropertyReferences();
            }

            public void CopyCustomTexturesTo(Config otherConfig)
            {
                otherConfig.customMaterialPropertiesList = customMaterialPropertiesList;
                for (int i = 0; i < otherConfig.cachedShaderProperties.Count; i++)
                    otherConfig.cachedShaderProperties[i].ResolveShaderPropertyReferences();
            }

            internal bool HasErrors()
            {
                foreach (ShaderProperty shaderProperty in visibleShaderProperties)
                    if (shaderProperty.error)
                        return true;

                foreach (ShaderProperty.CustomMaterialProperty customTexture in CustomMaterialProperties)
                    if (customTexture.HasErrors)
                        return true;

                return false;
            }

            internal string GetConfigFileCustomData()
            {
                return string.Format("CF:{0}", templateFile);
            }

            internal int ToHash()
            {
                StringBuilder sb = new();
                /*
                sb.Append(Filename);
                sb.Append(ShaderName);
                */
                List<string> orderedFeatures = new(Features);
                orderedFeatures.Sort();
                List<string> orderedFlags = new(Flags);
                orderedFlags.Sort();
                List<string> orderedFlagsExtra = new();
                foreach (KeyValuePair<string, List<string>> kvp in FlagsExtra)
                foreach (string flag in kvp.Value)
                    orderedFlagsExtra.Add(flag);
                orderedFlagsExtra.Sort();
                List<string> sortedKeywordsKeys = new(Keywords.Keys);
                sortedKeywordsKeys.Sort();
                List<string> sortedKeywordsValues = new(Keywords.Values);
                sortedKeywordsValues.Sort();

                foreach (string f in orderedFeatures)
                    sb.Append(f);
                foreach (string f in orderedFlags)
                    sb.Append(f);
                foreach (string f in sortedKeywordsKeys)
                    sb.Append(f);
                foreach (string f in sortedKeywordsValues)
                    sb.Append(f);

                foreach (ShaderProperty sp in visibleShaderProperties)
                    sb.Append(sp);
                foreach (ShaderProperty.CustomMaterialProperty ct in customMaterialPropertiesList)
                    sb.Append(ct);

                return sb.ToString().GetHashCode();
            }

            private bool ParseUserData(ShaderImporter importer)
            {
                if (string.IsNullOrEmpty(importer.userData))
                    return false;

                string[] data = importer.userData.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                List<string> customDataList = new();

                foreach (string d in data)
                {
                    if (string.IsNullOrEmpty(d)) continue;

                    switch (d[0])
                    {
                        //Features
                        case 'F':
                            if (d == "F") break; //Prevent getting "empty" feature
                            Features.Add(d.Substring(1));
                            break;

                        //Flags
                        case 'f':
                            Flags.Add(d.Substring(1));
                            break;

                        //Keywords
                        case 'K':
                            string[] kw = d.Substring(1).Split(':');
                            if (kw.Length != 2)
                            {
                                Debug.LogError(
                                    "[TCP2 Shader Generator] Error while parsing userData: invalid Keywords format.");
                                return false;
                            }

                            Keywords.Add(kw[0], kw[1]);
                            break;

                        //Custom Data
                        case 'c':
                            customDataList.Add(d.Substring(1));
                            break;
                        //old format
                        default:
                            Features.Add(d);
                            break;
                    }
                }

                foreach (string customData in customDataList)
                    //Configuration File
                    if (customData.StartsWith("CF:"))
                        templateFile = customData.Substring(3);

                return true;
            }

            private static string CompressString(string uncompressed)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(uncompressed);
                using (MemoryStream compressedStream = new())
                {
                    using (GZipStream gZipStream = new(compressedStream, CompressionMode.Compress))
                    {
                        gZipStream.Write(bytes, 0, bytes.Length);
                    }

                    bytes = compressedStream.ToArray();
                }

                return Convert.ToBase64String(bytes);
            }

            private static string UncompressString(string compressed)
            {
                byte[] bytes = Convert.FromBase64String(compressed);
                byte[] buffer = new byte[4096];
                MemoryStream uncompressedStream = new();
                using (MemoryStream compressedStream = new(bytes))
                {
                    using (GZipStream gZipStream = new(compressedStream, CompressionMode.Decompress))
                    {
                        int length = 0;
                        do
                        {
                            length = gZipStream.Read(buffer, 0, 4096);
                            if (length > 0)
                                uncompressedStream.Write(buffer, 0, length);
                        } while (length > 0);
                    }
                }

                return Encoding.UTF8.GetString(uncompressedStream.ToArray());
            }

            //New serialization format, embedded into the shader source in a comment
            internal string GetSerializedData()
            {
                string serialized = Serialization.Serialize(this);
#if WRITE_UNCOMPRESSED_SERIALIZED_DATA
                return kSerializationPrefixUncompressed + serialized + kSerializationSuffix;
#else
				return kSerializationPrefix + CompressString(serialized) + kSerializationSuffix;
#endif
            }

            //This method is executed twice because of an ordering problem:
            // - first run: it needs to fetch the template used from TCP_DATA
            // - then it loads that template and generate the serialized properties
            // - second run: now that the serialized properties exist, replace their implementations with the ones in TPC2_DATA
            internal bool ParseSerializedDataAndHash(ShaderImporter importer, Template template,
                bool dontRebuildCustomTextures)
            {
                //try to find serialized TCP2 data
                string unityPath = importer.assetPath;
                string osPath = Application.dataPath + "/" + unityPath.Substring("Assets/".Length);
                if (File.Exists(osPath))
                {
                    string[] code = File.ReadAllLines(osPath);
                    for (int i = code.Length - 1; i >= 0; i--)
                    {
                        string line = code[i].Trim();
                        const string serializedPrefix = kSerializationPrefix;
                        const string serializedPrefixU = kSerializationPrefixUncompressed;
                        const string serializedSuffix = kSerializationSuffix;

                        const string hashPrefix = kHashPrefix;
                        const string hashSuffix = kHashSuffix;

                        //hash is always inserted after serialized data, so the function shouldn't return without it being checked
                        if (line.StartsWith(hashPrefix))
                        {
                            string hash = line.Substring(hashPrefix.Length,
                                line.Length - hashPrefix.Length - hashSuffix.Length);

                            //list of all lines, remove them from the end until the serialized prefix is found
                            List<string> codeLines = new(code);
                            for (int j = codeLines.Count - 1; j >= 0; j--)
                            {
                                bool @break = codeLines[j].StartsWith(hashPrefix);
                                codeLines.RemoveAt(j);
                                if (@break)
                                    break;
                            }

                            StringBuilder sb = new();
                            foreach (string l in codeLines) sb.AppendLine(l);
                            string normalizedLineEndings = sb.ToString().Replace("\r\n", "\n");
                            string fileHash = ShaderGenerator2.GetHash(normalizedLineEndings);

                            isModifiedExternally = string.Compare(fileHash, hash, StringComparison.Ordinal) != 0;
                        }

                        if (line.StartsWith(serializedPrefix) || line.StartsWith(serializedPrefixU))
                        {
                            string extractedData = line;
                            int j = i;
                            while (!extractedData.Contains(" */") && j < code.Length)
                            {
                                j++;
                                if (j < code.Length)
                                {
                                    line = code[j].Trim();
                                    extractedData += "\n" + line;
                                }
                                else
                                {
                                    Debug.LogError(
                                        ShaderGenerator2.ErrorMsg("Incomplete serialized data in shader file."));
                                    return false;
                                }
                            }

                            string serializedData = "";
                            if (extractedData.StartsWith(serializedPrefixU))
                            {
                                serializedData = extractedData.Substring(serializedPrefixU.Length,
                                    extractedData.Length - serializedPrefixU.Length - serializedSuffix.Length);
                            }
                            else
                            {
                                serializedData = extractedData.Substring(serializedPrefix.Length,
                                    extractedData.Length - serializedPrefix.Length - serializedSuffix.Length);
                                serializedData = UncompressString(serializedData);
                            }

                            return ParseSerializedData(serializedData, template, dontRebuildCustomTextures);
                        }
                    }
                }

                return false;
            }

            public bool ParseSerializedData(string serializedData, Template template, bool dontRebuildCustomTextures,
                bool resetEmptyImplementations = false)
            {
                Func<object, string, object> onDeserializeShaderPropertyList = (obj, data) =>
                {
                    //called with data in format 'list[sp(field:value;field:value...),sp(field:value;...)]'

                    // - make a new list, and pull matching sp from it
                    // - reset the implementations of the remaining sp for the undo/redo system
                    List<ShaderProperty> shaderPropertiesTempList = new(cachedShaderProperties);

                    string[] split = Serialization.SplitExcludingBlocks(data.Substring(5, data.Length - 6), ',', true,
                        true, "()", "[]");
                    foreach (string spData in split)
                    {
                        //try to match existing Shader Property by its name
                        string name = null;

                        //exclude 'sp(' and ')' and extract fields
                        string[] vars = Serialization.SplitExcludingBlocks(spData.Substring(3, spData.Length - 4), ';',
                            true, true, "()", "[]");
                        foreach (string v in vars)
                            //find 'name' and remove 'name:' and quotes to extract value
                            if (v.StartsWith("name:"))
                                name = v.Substring(6, v.Length - 7);

                        if (name != null)
                        {
                            //find corresponding shader property, if it exists
                            ShaderProperty matchedSp = shaderPropertiesTempList.Find(sp => sp.Name == name);

                            //if no match, try to find it in the template's shader properties
                            if (matchedSp == null && template != null)
                            {
                                matchedSp = Array.Find(template.shaderProperties, sp => sp.Name == name);
                                if (matchedSp != null)
                                {
                                    cachedShaderProperties.Add(matchedSp);
                                    shaderPropertiesTempList.Add(matchedSp);
                                }
                            }

                            if (matchedSp != null)
                            {
                                shaderPropertiesTempList.Remove(matchedSp);

                                Func<object, string, object> onDeserializeImplementation = (impObj, impData) =>
                                {
                                    return DeserializeImplementationHandler(impObj, impData, matchedSp);
                                };

                                Dictionary<Type, Func<object, string, object>> implementationHandling = new()
                                    { { typeof(ShaderProperty.Implementation), onDeserializeImplementation } };

                                Serialization.DeserializeTo(matchedSp, spData, typeof(ShaderProperty), null,
                                    implementationHandling);

                                matchedSp.CheckHash();
                                matchedSp.CheckErrors();
                            }
                        }
                    }

                    if (resetEmptyImplementations)
                        foreach (ShaderProperty remainingShaderProperty in shaderPropertiesTempList)
                            remainingShaderProperty.ResetDefaultImplementation();

                    return null;
                };

                // try
                {
                    Dictionary<Type, Func<object, string, object>> shaderPropertyHandling = new()
                        { { typeof(List<ShaderProperty>), onDeserializeShaderPropertyList } };

                    if (dontRebuildCustomTextures)
                        // if not building the custom material properties list, just skip its deserialization, else use the custom handling
                        shaderPropertyHandling.Add(typeof(List<ShaderProperty.CustomMaterialProperty>),
                            (obj, data) => { return null; });
                    Serialization.DeserializeTo(this, serializedData, GetType(), null, shaderPropertyHandling);

                    return true;
                }
                // catch (Exception e)
                {
                    // Debug.LogError(ShaderGenerator2.ErrorMsg(string.Format("Deserialization error:\n'{0}'\n{1}", e.Message, e.StackTrace.Replace(Application.dataPath, ""))));
                    // return false;
                }
            }

            internal object DeserializeImplementationHandler(object impObj, string serializedData,
                ShaderProperty existingShaderProperty)
            {
                //make sure to deserialize as a new object, so that final Implementation subtype is kept instead of creating base Implementation class
                object imp = Serialization.Deserialize(serializedData, new object[] { existingShaderProperty });

                //if custom material property, find the one with the matching serialized name
                if (imp is ShaderProperty.Imp_CustomMaterialProperty)
                {
                    ShaderProperty.Imp_CustomMaterialProperty ict = imp as ShaderProperty.Imp_CustomMaterialProperty;
                    ShaderProperty.CustomMaterialProperty matchedCt =
                        customMaterialPropertiesList.Find(ct =>
                            ct.PropertyName == ict.LinkedCustomMaterialPropertyName);
                    //will be the match, or null if nothing found
                    ict.LinkedCustomMaterialProperty = matchedCt;
                    ict.UpdateChannels();
                }
                else if (imp is ShaderProperty.Imp_ShaderPropertyReference)
                {
                    //find existing shader property and link it here
                    //TODO: what if the shader property hasn't been deserialized yet?
                    ShaderProperty.Imp_ShaderPropertyReference ispr = imp as ShaderProperty.Imp_ShaderPropertyReference;
                    string channels = ispr.Channels;
                    ShaderProperty matchedLinkedSp =
                        visibleShaderProperties.Find(sp => sp.Name == ispr.LinkedShaderPropertyName);
                    ispr.LinkedShaderProperty = matchedLinkedSp;
                    //restore channels from serialized data (it is reset when assigning a new linked shader property)
                    if (!string.IsNullOrEmpty(channels))
                        ispr.Channels = channels;
                }
                else if (imp is ShaderProperty.Imp_MaterialProperty_Texture)
                {
                    // find existing shader property for uv if that option is enabled, and link it
                    ShaderProperty.Imp_MaterialProperty_Texture impt =
                        imp as ShaderProperty.Imp_MaterialProperty_Texture;
                    string channels = impt.UVChannels;
                    ShaderProperty matchedLinkedSp =
                        visibleShaderProperties.Find(sp => sp.Name == impt.LinkedShaderPropertyName);
                    impt.LinkedShaderProperty = matchedLinkedSp;
                    //restore channels from serialized data (it is reset when assigning a new linked shader property)
                    if (!string.IsNullOrEmpty(channels))
                        impt.UVChannels = channels;
                }

                return imp;
            }

            internal void AutoNames()
            {
                string rawName = ShaderName.Replace("Toony Colors Pro 2/", "");

                if (!ProjectOptions.data.SubFolders) rawName = Path.GetFileName(rawName);

                Filename = rawName;
            }

            //--------------------------------------------------------------------------------------------------
            // FEATURES

            internal bool HasFeature(string feature)
            {
                return Features.Contains(feature);
            }

            internal bool HasFeaturesAny(params string[] features)
            {
                foreach (string f in features)
                    if (Features.Contains(f))
                        return true;

                return false;
            }

            internal bool HasFeaturesAll(params string[] features)
            {
                foreach (string f in features)
                    if (f[0] == '!')
                    {
                        if (Features.Contains(f.Substring(1))) return false;
                    }
                    else
                    {
                        if (!Features.Contains(f)) return false;
                    }

                return true;
            }

            internal void ToggleFeature(string feature, bool enable)
            {
                if (string.IsNullOrEmpty(feature))
                    return;

                if (!Features.Contains(feature) && enable)
                    Features.Add(feature);

                else if (Features.Contains(feature) && !enable)
                    Features.Remove(feature);
            }

            //--------------------------------------------------------------------------------------------------
            // FLAGS

            internal bool HasFlag(string block, string flag)
            {
                if (block == "pragma_surface_shader")
                    return Flags.Contains(flag);
                return FlagsExtra.ContainsKey(block) && FlagsExtra[block].Contains(flag);
            }

            internal void ToggleFlag(string block, string flag, bool enable)
            {
                List<string> flagList = null;
                if (block == "pragma_surface_shader")
                {
                    flagList = Flags;
                }
                else
                {
                    if (!FlagsExtra.ContainsKey(block)) FlagsExtra.Add(block, new List<string>());
                    flagList = FlagsExtra[block];
                }

                if (!flagList.Contains(flag) && enable) flagList.Add(flag);
                else if (flagList.Contains(flag) && !enable) flagList.Remove(flag);
            }

            //--------------------------------------------------------------------------------------------------
            // KEYWORDS

            internal bool HasKeyword(string key)
            {
                return GetKeyword(key) != null;
            }

            internal string GetKeyword(string key)
            {
                if (key == null)
                    return null;

                if (!Keywords.ContainsKey(key))
                    return null;

                return Keywords[key];
            }

            internal void SetKeyword(string key, string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    if (Keywords.ContainsKey(key))
                        Keywords.Remove(key);
                }
                else
                {
                    if (Keywords.ContainsKey(key))
                        Keywords[key] = value;
                    else
                        Keywords.Add(key, value);
                }
            }

            internal void RemoveKeyword(string key)
            {
                if (Keywords.ContainsKey(key))
                    Keywords.Remove(key);
            }

            //--------------------------------------------------------------------------------------------------
            // SHADER PROPERTIES / CUSTOM MATERIAL PROPERTIES

            private void ExpandAllGroups()
            {
                string[] keys = headersExpanded.Keys.ToArray();
                foreach (string key in keys) headersExpanded[key] = true;
            }

            private void FoldAllGroups()
            {
                string[] keys = headersExpanded.Keys.ToArray();
                foreach (string key in keys) headersExpanded[key] = false;
            }

            public string getHeadersExpanded()
            {
                string headersFoldout = "";
                foreach (KeyValuePair<string, bool> kvp in headersExpanded)
                    if (kvp.Value)
                        headersFoldout += kvp.Key + ",";
                return headersFoldout.TrimEnd(',');
            }

            public void setHeadersExpanded(string expandedHeaders)
            {
                string[] array = expandedHeaders.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string[] keys = headersExpanded.Keys.ToArray();
                foreach (string key in keys) headersExpanded[key] = Array.Exists(array, str => str == key);
            }

            public string getShaderPropertiesExpanded()
            {
                string spExpanded = "";
                foreach (ShaderProperty sp in IterateAllShaderProperties())
                    if (sp.expanded)
                        spExpanded += sp.Name + ",";
                return spExpanded.TrimEnd(',');
            }

            public void setShaderPropertiesExpanded(string spExpanded)
            {
                string[] array = spExpanded.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (ShaderProperty sp in IterateAllShaderProperties())
                    sp.expanded = Array.Exists(array, str => str == sp.Name);
            }

            internal void ShaderPropertiesGUI()
            {
                GUILayout.Space(6);

                GUILayout.BeginHorizontal();

                // Expand / Fold All
                if (GUILayout.Button(TCP2_GUI.TempContent(" Expand All "), EditorStyles.miniButtonLeft))
                    ExpandAllGroups();

                if (GUILayout.Button(TCP2_GUI.TempContent(" Fold All "), EditorStyles.miniButtonRight)) FoldAllGroups();

                GUILayout.FlexibleSpace();

                // Reset All
                bool canReset = false;
                foreach (ShaderProperty sp in cachedShaderProperties)
                    if (sp.manuallyModified)
                    {
                        canReset = true;
                        break;
                    }

                using (new EditorGUI.DisabledScope(!canReset))
                {
                    if (GUILayout.Button(TCP2_GUI.TempContent(" Reset All "), EditorStyles.miniButton))
                        if (EditorUtility.DisplayDialog("Reset All Shader Properties",
                                "All Custom Shader Properties will be cleared!\nThis can't be undone!\nProceed?", "Yes",
                                "No"))
                            foreach (ShaderProperty sp in cachedShaderProperties)
                                sp.ResetDefaultImplementation();
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(4);
                if (ShaderGenerator2.ContextualHelpBox(
                        "This section allows you to modify some shader properties that will be used in the shader, based on the features enabled in the corresponding tab.\nClick here to open the documentation and see some examples.",
                        "shaderproperties"))
                    GUILayout.Space(4);

                if (visibleShaderProperties.Count == 0)
                    EditorGUILayout.HelpBox("There are no shader properties for this template.", MessageType.Info);
                else
                    for (int i = 0; i < shaderPropertiesUIGroups.Count; i++)
                    {
                        ShaderPropertyGroup group = shaderPropertiesUIGroups[i];

                        if (group.header != null)
                        {
                            EditorGUI.BeginChangeCheck();

                            // hover rect as in 2019.3 UI
                            Rect rect = GUILayoutUtility.GetRect(group.header, EditorStyles.foldout,
                                GUILayout.ExpandWidth(true));
                            TCP2_GUI.DrawHoverRect(rect);
                            rect.xMin += 4; // small left padding
                            headersExpanded[group.header.text] = TCP2_GUI.HeaderFoldoutHighlightErrorGrayPosition(rect,
                                headersExpanded[group.header.text], group.header, group.hasErrors,
                                group.hasModifiedShaderProperties);

                            if (EditorGUI.EndChangeCheck())
                                // expand/fold all when alt/control is held
                                if (Event.current.alt || Event.current.control)
                                {
                                    if (headersExpanded[group.header.text])
                                        ExpandAllGroups();
                                    else
                                        FoldAllGroups();
                                }
                        }

                        if (group.header == null || headersExpanded[group.header.text])
                            foreach (ShaderProperty sp in group.shaderProperties)
                                sp.ShowGUILayout(14);
                    }

                // Custom Material Properties
                if (visibleShaderProperties.Count > 0) CustomMaterialPropertiesGUI();
            }

            internal void MaterialLayersGUI(out bool shaderPropertiesChange)
            {
                bool spChange = false;

                //button callbacks
                ShaderProperty.CustomMaterialProperty.ButtonClick onAdd = index =>
                {
                    materialLayers.Add(new MaterialLayer());
                    _materialLayersNames = null;
                    spChange = true;
                };
                ShaderProperty.CustomMaterialProperty.ButtonClick onRemove = index =>
                {
                    foreach (ShaderProperty shaderProperty in cachedShaderProperties)
                        if (shaderProperty.linkedMaterialLayers.Contains(materialLayers[index].uid))
                            shaderProperty.RemoveMaterialLayer(materialLayers[index].uid);
                    materialLayers[index].sourceShaderProperty.WillBeRemoved();

                    materialLayers.RemoveAt(index);
                    _materialLayersNames = null;
                    spChange = true;
                };

                //draw element callback
                Action<int, float> DrawMaterialLayer = (index, margin) =>
                {
                    MaterialLayer matLayer = materialLayers[index];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        using (new SGUILayout.IndentedLine(margin))
                        {
                            // Header
                            const float buttonWidth = 20;
                            Rect rect = EditorGUILayout.GetControlRect(
                                GUILayout.Height(EditorGUIUtility.singleLineHeight));
                            rect.width -= buttonWidth * 2;

                            TCP2_GUI.DrawHoverRect(rect);

                            EditorGUI.BeginChangeCheck();
                            matLayer.expanded = GUI.Toggle(rect, matLayer.expanded,
                                TCP2_GUI.TempContent("Layer: " + matLayer.name), TCP2_GUI.HeaderDropDown);
                            if (EditorGUI.EndChangeCheck())
                                if (Event.current.alt || Event.current.control)
                                {
                                    bool state = matLayer.expanded;
                                    foreach (MaterialLayer ml in materialLayers) ml.expanded = state;
                                }

                            // Add/Remove buttons
                            rect.x += rect.width;
                            rect.width = buttonWidth;
                            rect.height = EditorGUIUtility.singleLineHeight;
                            if (GUI.Button(rect, "+", EditorStyles.miniButtonLeft)) onAdd(index);

                            rect.x += rect.width;
                            if (GUI.Button(rect, "-", EditorStyles.miniButtonRight)) onRemove(index);
                        }

                        // Parameters:
                        if (matLayer.expanded)
                        {

                            using (new SGUILayout.IndentedLine(margin))
                            {
                                matLayer.name =
                                    EditorGUILayout.DelayedTextField(TCP2_GUI.TempContent("Name"), matLayer.name);
                            }

                            using (new SGUILayout.IndentedLine(margin))
                            {
                                GUILayout.Label(TCP2_GUI.TempContent("ID"),
                                    GUILayout.Width(EditorGUIUtility.labelWidth - 4));
                                using (new EditorGUI.DisabledScope(true))
                                {
                                    EditorGUILayout.TextField(GUIContent.none, matLayer.uid);
                                }
                            }

                            using (new SGUILayout.IndentedLine(margin))
                            {
                                using (new EditorGUI.DisabledScope(true))
                                {
                                    GUILayout.Label(
                                        "The ID will be replaced with the actual Material Layer name for variables and labels in the final shader.",
                                        SGUILayout.Styles.GrayMiniLabelWrap);
                                }
                            }

                            using (new SGUILayout.IndentedLine(margin))
                            {
                                EditorGUI.BeginChangeCheck();
                                matLayer.UseContrastProperty = EditorGUILayout.Toggle(
                                    TCP2_GUI.TempContent("Add Contrast Property",
                                        "Automatically add a range property to adjust the layer contrast in the material inspector"),
                                    matLayer.UseContrastProperty);
                            }

                            using (new SGUILayout.IndentedLine(margin))
                            {
                                matLayer.UseNoiseProperty = EditorGUILayout.Toggle(
                                    TCP2_GUI.TempContent("Add Noise Property",
                                        "Automatically add a properties to adjust the layer based on a noise texture"),
                                    matLayer.UseNoiseProperty);
                                if (EditorGUI.EndChangeCheck()) spChange = true;

                                if (GUILayout.Button(TCP2_GUI.TempContent("Load Source Preset "),
                                        EditorStyles.miniPullDown, GUILayout.ExpandWidth(false)))
                                    matLayer.ShowPresetsMenu();
                            }

                            matLayer.sourceShaderProperty.ShowGUILayout(margin);
                            if (matLayer.UseContrastProperty) matLayer.contrastProperty.ShowGUILayout(margin);

                            if (matLayer.UseNoiseProperty) matLayer.noiseProperty.ShowGUILayout(margin);
                        }
                    }
                    EditorGUILayout.EndVertical();
                };

                if (materialLayers.Count == 0)
                {
                    if (TCP2_GUI.HelpBoxWithButton("No Material Layers defined.", "Add", 48))
                    {
                        materialLayers.Add(new MaterialLayer());
                        _materialLayersNames = null;
                        spChange = true;
                    }
                }
                else
                {
                    matLayersLayoutList.DoLayoutList(DrawMaterialLayer, materialLayers, new RectOffset(2, 0, 0, 2));

                    CustomMaterialPropertiesGUI();
                }

                shaderPropertiesChange = spChange;
            }

            private void CustomMaterialPropertiesGUI()
            {
                GUILayout.Space(4);
                TCP2_GUI.SeparatorSimple();
                GUILayout.Label("Custom Material Properties", EditorStyles.boldLabel);
                GUILayout.Space(2);
                if (ShaderGenerator2.ContextualHelpBox(
                        "You can define your own material properties here, that can then be shared between multiple Shader Properties. For example, this can allow you to pack textures however you want, having a mask for each R,G,B,A channel.",
                        "custommaterialproperties"))
                    GUILayout.Space(4);

                if (customMaterialPropertiesList == null || customMaterialPropertiesList.Count == 0)
                {
                    if (TCP2_GUI.HelpBoxWithButton("No custom material properties defined.", "Add", 48))
                        ShowCustomMaterialPropertyMenu(0);
                }
                else
                {
                    //button callbacks
                    ShaderProperty.CustomMaterialProperty.ButtonClick onAdd = index =>
                        ShowCustomMaterialPropertyMenu(index);
                    ShaderProperty.CustomMaterialProperty.ButtonClick onRemove = index =>
                    {
                        customMaterialPropertiesList[index].WillBeRemoved();
                        customMaterialPropertiesList.RemoveAt(index);
                    };

                    //draw element callback
                    Action<int, float> DrawCustomTextureItem = (index, margin) =>
                    {
                        customMaterialPropertiesList[index].ShowGUILayout(index, onAdd, onRemove);
                    };

                    customTexturesLayoutList.DoLayoutList(DrawCustomTextureItem, customMaterialPropertiesList,
                        new RectOffset(2, 0, 0, 2));
                }
            }

            private void ShowCustomMaterialPropertyMenu(int index)
            {
                GenericMenu menu = new();
                Type impType = typeof(ShaderProperty.Imp_MaterialProperty);
                IEnumerable<Type> subTypes = impType.Assembly.GetTypes().Where(type => type.IsSubclassOf(impType));
                foreach (Type type in subTypes)
                {
                    PropertyInfo menuLabel = type.GetProperty("MenuLabel", BindingFlags.Static | BindingFlags.Public);
                    string label = (string)menuLabel.GetValue(null, null);
                    label = label.Replace("Material Property/", "");
                    menu.AddItem(new GUIContent(label), false, OnAddCustomMaterialProperty,
                        new object[] { index, type });
                }

                menu.ShowAsContext();
            }

            private void OnAddCustomMaterialProperty(object data)
            {
                object[] array = (object[])data;
                int index = (int)array[0];
                Type type = (Type)array[1];

                if (customMaterialPropertiesList.Count == 0)
                    customMaterialPropertiesList.Add(CreateUniqueCustomTexture(type));
                else
                    customMaterialPropertiesList.Insert(index + 1, CreateUniqueCustomTexture(type));

                ShaderGenerator2.PushUndoState();
            }

            //Get a Shader Property from the list by its name
            internal ShaderProperty GetShaderPropertyByName(string name)
            {
                foreach (ShaderProperty sp in visibleShaderProperties)
                    if (sp.Name == name)
                        return sp;

                return null;
            }

            //Check if the supplied property name is unique
            internal bool IsUniquePropertyName(string name, IMaterialPropertyName propertyName)
            {
                //check existing Shader Properties of Material Property type
                foreach (ShaderProperty sp in visibleShaderProperties)
                foreach (ShaderProperty.Implementation imp in sp.implementations)
                {
                    ShaderProperty.Imp_MaterialProperty mp = imp as ShaderProperty.Imp_MaterialProperty;
                    if (mp != null && mp is IMaterialPropertyName && mp != propertyName &&
                        !mp.ignoreUniquePropertyName && mp.PropertyName == name) return false;
                }

                //check Custom Material Properties
                foreach (ShaderProperty.CustomMaterialProperty ct in customMaterialPropertiesList)
                    if (ct != propertyName && ct.PropertyName == name)
                        return false;

                return true;
            }

            private ShaderProperty.CustomMaterialProperty CreateUniqueCustomTexture(Type impType)
            {
                return new ShaderProperty.CustomMaterialProperty(customMaterialPropertyShaderProperty, impType);
            }

            internal void ClearShaderProperties()
            {
                cachedShaderProperties.Clear();
                visibleShaderProperties.Clear();
            }

            //Update available Shader Properties based on conditions
            internal void UpdateShaderProperties(Template template)
            {
                //Add Unity versions to features
#if UNITY_5_4_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_5_4");
#endif
#if UNITY_5_5_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_5_5");
#endif
#if UNITY_5_6_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_5_6");
#endif
#if UNITY_2017_1_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_2017_1");
#endif
#if UNITY_2018_1_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_2018_1");
#endif
#if UNITY_2018_2_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_2018_2");
#endif
#if UNITY_2018_3_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_2018_3");
#endif
#if UNITY_2019_1_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_2019_1");
#endif
#if UNITY_2019_2_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_2019_2");
#endif
#if UNITY_2019_3_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_2019_3");
#endif
#if UNITY_2019_4_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_2019_4");
#endif
#if UNITY_2020_1_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_2020_1");
#endif
#if UNITY_2021_1_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_2021_1");
#endif
#if UNITY_2021_2_OR_NEWER
                Utils.AddIfMissing(Features, "UNITY_2021_2");
#endif
#if UNITY_2022_2_OR_NEWER
				Utils.AddIfMissing(this.Features, "UNITY_2022_2");
#endif
                Template.ParsedLine[] parsedLines = template.GetParsedLinesFromConditions(this, null, null);

                //Clear arrays: will be refilled with the template's shader properties
                visibleShaderProperties.Clear();
                Dictionary<int, GUIContent> shaderPropertiesHeaders;
                visibleShaderProperties.AddRange(
                    template.GetConditionalShaderProperties(parsedLines, out shaderPropertiesHeaders));
                foreach (ShaderProperty sp in visibleShaderProperties)
                {
                    //add to the cached properties, to be found back if needed (in case of features change)
                    if (!cachedShaderProperties.Contains(sp)) cachedShaderProperties.Add(sp);

                    // resolve linked shader property references now that all visible shader properties are known
                    sp.ResolveShaderPropertyReferences();

                    sp.onImplementationsChanged -=
                        onShaderPropertyImplementationsChanged; // lazy way to make sure we don't subscribe more than once
                    sp.onImplementationsChanged += onShaderPropertyImplementationsChanged;
                }

                // Material Layers
                foreach (MaterialLayer ml in materialLayers)
                {
                    visibleShaderProperties.Add(ml.sourceShaderProperty);
                    if (ml.contrastProperty != null) visibleShaderProperties.Add(ml.contrastProperty);
                    if (ml.noiseProperty != null) visibleShaderProperties.Add(ml.noiseProperty);
                }

                //Find used shader properties per pass, to extract used features for each
                template.UpdateInjectionPoints(parsedLines);
                shaderPropertiesPerPass = template.FindUsedShaderPropertiesPerPass(parsedLines);

                // Build list of shader properties and headers for the UI
                shaderPropertiesUIGroups.Clear();
                ShaderPropertyGroup currentGroup = new()
                {
                    shaderProperties = new List<ShaderProperty>(),
                    hasModifiedShaderProperties = false,
                    hasErrors = false,
                    header = null
                };


                Action addCurrentGroup = () =>
                {
                    if (currentGroup.shaderProperties.Count > 0)
                    {
                        shaderPropertiesUIGroups.Add(currentGroup);

                        if (!headersExpanded.ContainsKey(currentGroup.header.text))
                            headersExpanded.Add(currentGroup.header.text, false);
                    }
                };

                for (int i = 0; i < visibleShaderProperties.Count; i++)
                {
                    if (shaderPropertiesHeaders.ContainsKey(i))
                    {
                        addCurrentGroup();

                        currentGroup = new ShaderPropertyGroup
                        {
                            shaderProperties = new List<ShaderProperty>(),
                            hasModifiedShaderProperties = false,
                            hasErrors = false,
                            header = shaderPropertiesHeaders[i]
                        };
                    }

                    ShaderProperty shaderProperty = visibleShaderProperties[i];
                    if (shaderProperty.isMaterialLayerProperty)
                        // Don't show Material Layer source in regular Shader Properties
                        continue;

                    currentGroup.shaderProperties.Add(shaderProperty);
                    currentGroup.hasModifiedShaderProperties |= shaderProperty.manuallyModified;
                    currentGroup.hasErrors |= shaderProperty.error;
                }

                addCurrentGroup();
            }

            public void UpdateCustomMaterialProperties()
            {
                foreach (ShaderProperty.CustomMaterialProperty cmp in customMaterialPropertiesList)
                    cmp.implementation.CheckErrors();
            }

            private void onShaderPropertyImplementationsChanged()
            {
                ShaderGenerator2.NeedsShaderPropertiesUpdate = true;
                ShaderGenerator2.PushUndoState();
            }

            //Process #KEYWORDS line from Template
            //Use temp features & flags to avoid permanent toggles (e.g. NOTILE_SAMPLING)
            //As long as the original features are there, they should be triggered each time anyway
            /// <returns>'true' if a new feature/flag has been added/removed, so that we can reprocess the whole keywords block</returns>
            internal bool ProcessKeywords(string line, List<string> tempFeatures, List<string> tempFlags,
                Dictionary<string, List<string>> tempExtraFlags)
            {
                if (string.IsNullOrEmpty(line)) return false;

                //Inside valid block
                string[] parts = line.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);

                // Fixed expressions first:
                switch (parts[0])
                {
                    case "set": //legacy
                    case "set_keyword":
                    {
                        string keywordValue = parts.Length > 2 ? parts[2] : "";
                        if (Keywords.ContainsKey(parts[1]))
                            Keywords[parts[1]] = keywordValue;
                        else
                            Keywords.Add(parts[1], keywordValue);
                        break;
                    }

                    case "enable_kw": //legacy
                    case "feature_on":
                    {
                        if (Utils.AddIfMissing(tempFeatures, parts[1])) return true;

                        break;
                    }
                    case "disable_kw": //legacy
                    case "feature_off":
                    {
                        if (Utils.RemoveIfExists(tempFeatures, parts[1])) return true;

                        break;
                    }

                    case "enable_flag": //legacy
                    case "flag_on":
                        if (tempFlags != null)
                            if (Utils.AddIfMissing(tempFlags, parts[1]))
                                return true;
                        break;
                    case "disable_flag": //legacy
                    case "flag_off":
                        if (tempFlags != null)
                            if (Utils.RemoveIfExists(tempFlags, parts[1]))
                                return true;
                        break;

                    default:
                    {
                        // Dynamic afterwards:
                        if (parts[0].StartsWith("flag_on:"))
                        {
                            if (tempExtraFlags == null) return false;

                            string block = parts[0].Substring("flag_on:".Length);
                            if (!tempExtraFlags.ContainsKey(block)) tempExtraFlags.Add(block, new List<string>());

                            if (Utils.AddIfMissing(tempExtraFlags[block], parts[1])) return true;
                        }
                        else if (parts[0].StartsWith("flag_off:"))
                        {
                            if (tempExtraFlags == null) return false;

                            string block = parts[0].Substring("flag_on:".Length);
                            if (!tempExtraFlags.ContainsKey(block)) return false;

                            if (Utils.RemoveIfExists(tempExtraFlags[block], parts[1]))
                            {
                                if (tempExtraFlags[block].Count == 0) tempExtraFlags.Remove(block);

                                return true;
                            }
                        }
                    }
                        break;
                }

                return false;
            }

            private void UI_CacheExpandedState()
            {
                headersExpandedCache = new Dictionary<string, bool>();
                foreach (KeyValuePair<string, bool> kvp in headersExpanded)
                    headersExpandedCache.Add(kvp.Key, kvp.Value);

                expandedCache = new HashSet<string>();
                foreach (ShaderProperty shaderProperty in visibleShaderProperties)
                    if (shaderProperty.expanded)
                        expandedCache.Add(shaderProperty.Name);
            }

            private void UI_RestoreExpandedState()
            {
                if (expandedCache == null && headersExpandedCache == null) return;

                foreach (KeyValuePair<string, bool> kvp in headersExpandedCache)
                    if (headersExpanded.ContainsKey(kvp.Key))
                        headersExpanded[kvp.Key] = kvp.Value;
                    else
                        headersExpanded.Add(kvp.Key, kvp.Value);

                foreach (ShaderProperty shaderProperty in visibleShaderProperties)
                    if (expandedCache.Contains(shaderProperty.Name))
                        shaderProperty.expanded = true;

                expandedCache = null;
                headersExpandedCache = null;
            }

            // Useful callbacks
            public void OnBeforeGenerateShader()
            {
                UI_CacheExpandedState();
            }

            public void OnAfterGenerateShader()
            {
                UI_RestoreExpandedState();
            }

            // UI list of Shader Properties
            private struct ShaderPropertyGroup
            {
                public GUIContent header;
                public bool hasModifiedShaderProperties;
                public bool hasErrors;
                public List<ShaderProperty> shaderProperties;
            }

            //--------------------------------------------------------------------------------------------------

            private enum ParseBlock
            {
                None,
                Features,
                Flags
            }
#pragma warning disable 414
            [Serialization.SerializeAs("ver")] private string tcp2version => ShaderGenerator2.TCP2_VERSION;
            [Serialization.SerializeAs("unity")] private string unityVersion => Application.unityVersion;
#pragma warning restore 414
        }
    }
}
