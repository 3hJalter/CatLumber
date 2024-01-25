// Toony Colors Pro+Mobile 2
// (c) 2014-2023 Jean Moreno

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Represents a Toony Colors Pro 2 configuration to generate the corresponding shader

namespace ToonyColorsPro
{
    namespace Legacy
    {
        public class TCP2_Config
        {
            public string configType = "Normal";

            public List<string> Features = new();
            //--------------------------------------------------------------------------------------------------

            public string Filename = "TCP2 Custom";
            public List<string> Flags = new();
            public bool isModifiedExternally;
            public Dictionary<string, string> Keywords = new();
            public string ShaderName = "Toony Colors Pro 2/User/My TCP2 Shader";
            public int shaderTarget = 30;
            public string templateFile = "TCP2_ShaderTemplate_Default";

            public static TCP2_Config CreateFromFile(TextAsset asset)
            {
                return CreateFromFile(asset.text);
            }

            public static TCP2_Config CreateFromFile(string text)
            {
                string[] lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                TCP2_Config config = new TCP2_Config();

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

            public static TCP2_Config CreateFromShader(Shader shader)
            {
                ShaderImporter shaderImporter =
                    AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(shader)) as ShaderImporter;

                TCP2_Config config = new TCP2_Config();
                config.ShaderName = shader.name;
                config.Filename = Path.GetFileName(AssetDatabase.GetAssetPath(shader)).Replace(".shader", "");
                config.isModifiedExternally = false;
                bool valid = config.ParseUserData(shaderImporter);

                if (valid)
                    return config;
                return null;
            }

            public TCP2_Config Copy()
            {
                TCP2_Config config = new TCP2_Config();

                config.Filename = Filename;
                config.ShaderName = ShaderName;

                foreach (string feature in Features)
                    config.Features.Add(feature);

                foreach (string flag in Flags)
                    config.Flags.Add(flag);

                foreach (KeyValuePair<string, string> kvp in Keywords)
                    config.Keywords.Add(kvp.Key, kvp.Value);

                config.shaderTarget = shaderTarget;
                config.configType = configType;
                config.templateFile = templateFile;

                return config;
            }

            public string GetShaderTargetCustomData()
            {
                return string.Format("SM:{0}", shaderTarget);
            }

            public string GetConfigTypeCustomData()
            {
                if (configType != "Normal") return string.Format("CT:{0}", configType);

                return null;
            }

            public string GetConfigFileCustomData()
            {
                return string.Format("CF:{0}", templateFile);
            }

            public int ToHash()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Filename);
                sb.Append(ShaderName);
                List<string> orderedFeatures = new List<string>(Features);
                orderedFeatures.Sort();
                List<string> orderedFlags = new List<string>(Flags);
                orderedFlags.Sort();
                List<string> sortedKeywordsKeys = new List<string>(Keywords.Keys);
                sortedKeywordsKeys.Sort();
                List<string> sortedKeywordsValues = new List<string>(Keywords.Values);
                sortedKeywordsValues.Sort();

                foreach (string f in orderedFeatures)
                    sb.Append(f);
                foreach (string f in orderedFlags)
                    sb.Append(f);
                foreach (string f in sortedKeywordsKeys)
                    sb.Append(f);
                foreach (string f in sortedKeywordsValues)
                    sb.Append(f);

                sb.Append(shaderTarget.ToString());

                return sb.ToString().GetHashCode();
            }

            //Convert Config to ShaderImporter UserData
            public string ToUserData(string[] customData)
            {
                string userData = "";
                if (!Features.Contains("USER"))
                    userData = "USER,";

                foreach (string feature in Features)
                    if (feature.Contains("USER"))
                        userData += string.Format("{0},", feature);
                    else
                        userData += string.Format("F{0},", feature);
                foreach (string flag in Flags)
                    userData += string.Format("f{0},", flag);
                foreach (KeyValuePair<string, string> kvp in Keywords)
                    userData += string.Format("K{0}:{1},", kvp.Key, kvp.Value);
                foreach (string custom in customData)
                    userData += string.Format("c{0},", custom);
                userData = userData.TrimEnd(',');

                return userData;
            }

            private bool ParseUserData(ShaderImporter importer)
            {
                if (string.IsNullOrEmpty(importer.userData))
                    return false;

                string[] data = importer.userData.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                List<string> customDataList = new List<string>();

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
                {
                    //Hash
                    if (customData.Length > 0 && customData[0] == 'h')
                    {
                        string dataHash = customData;
                        string fileHash = TCP2_ShaderGeneratorUtils.GetShaderContentHash(importer);

                        if (!string.IsNullOrEmpty(fileHash) && dataHash != fileHash) isModifiedExternally = true;
                    }
                    //Timestamp
                    else
                    {
                        ulong timestamp;
                        if (ulong.TryParse(customData, out timestamp))
                            if (importer.assetTimeStamp != timestamp)
                                isModifiedExternally = true;
                    }

                    //Shader Model target
                    if (customData.StartsWith("SM:")) shaderTarget = int.Parse(customData.Substring(3));

                    //Configuration Type
                    if (customData.StartsWith("CT:")) configType = customData.Substring(3);

                    //Configuration File
                    if (customData.StartsWith("CF:")) templateFile = customData.Substring(3);
                }

                return true;
            }

            public void AutoNames()
            {
                string rawName = ShaderName.Replace("Toony Colors Pro 2/", "");
                Filename = rawName;
            }

            //--------------------------------------------------------------------------------------------------
            // FEATURES

            public bool HasFeature(string feature)
            {
                return TCP2_ShaderGeneratorUtils.HasEntry(Features, feature);
            }

            public bool HasFeaturesAny(params string[] features)
            {
                return TCP2_ShaderGeneratorUtils.HasAnyEntries(Features, features);
            }

            public bool HasFeaturesAll(params string[] features)
            {
                return TCP2_ShaderGeneratorUtils.HasAllEntries(Features, features);
            }

            public void ToggleFeature(string feature, bool enable)
            {
                if (string.IsNullOrEmpty(feature))
                    return;

                TCP2_ShaderGeneratorUtils.ToggleEntry(Features, feature, enable);
            }

            //--------------------------------------------------------------------------------------------------
            // FLAGS

            public bool HasFlag(string flag)
            {
                return TCP2_ShaderGeneratorUtils.HasEntry(Flags, flag);
            }

            public bool HasFlagsAny(params string[] flags)
            {
                return TCP2_ShaderGeneratorUtils.HasAnyEntries(Flags, flags);
            }

            public bool HasFlagsAll(params string[] flags)
            {
                return TCP2_ShaderGeneratorUtils.HasAllEntries(Flags, flags);
            }

            public void ToggleFlag(string flag, bool enable)
            {
                TCP2_ShaderGeneratorUtils.ToggleEntry(Flags, flag, enable);
            }

            //--------------------------------------------------------------------------------------------------
            // KEYWORDS

            public bool HasKeyword(string key)
            {
                return GetKeyword(key) != null;
            }

            public string GetKeyword(string key)
            {
                if (key == null)
                    return null;

                if (!Keywords.ContainsKey(key))
                    return null;

                return Keywords[key];
            }

            public void SetKeyword(string key, string value)
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

            public void RemoveKeyword(string key)
            {
                if (Keywords.ContainsKey(key))
                    Keywords.Remove(key);
            }

            //--------------------------------------------------------------------------------------------------

            private enum ParseBlock
            {
                None,
                Features,
                Flags
            }
        }
    }
}
