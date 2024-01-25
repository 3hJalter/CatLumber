// Toony Colors Pro+Mobile 2
// (c) 2014-2023 Jean Moreno

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// General helper functions for TCP2

namespace ToonyColorsPro
{
    namespace Utilities
    {
        public static class Utils
        {
            //--------------------------------------------------------------------------------------------------------------------------------

            public enum TextureChannel
            {
                Alpha,
                Red,
                Green,
                Blue
            }

            public static string ToShader(this TextureChannel channel)
            {
                switch (channel)
                {
                    case TextureChannel.Alpha: return ".a";
                    case TextureChannel.Red: return ".r";
                    case TextureChannel.Green: return ".g";
                    case TextureChannel.Blue: return ".b";
                    default:
                        Debug.LogError("[Utils] Unrecognized texture channel: " + channel.ToShader());
                        return null;
                }
            }

            public static TextureChannel FromShader(string str)
            {
                if (string.IsNullOrEmpty(str))
                    return TextureChannel.Alpha;

                switch (str)
                {
                    case ".a": return TextureChannel.Alpha;
                    case ".r": return TextureChannel.Red;
                    case ".g": return TextureChannel.Green;
                    case ".b": return TextureChannel.Blue;
                    default:
                        Debug.LogError("[Utils] Unrecognized texture channel from shader: " + str +
                                       "\nDefaulting to Alpha");
                        return TextureChannel.Alpha;
                }
            }

            //Fix for retina displays
#if UNITY_5_4_OR_NEWER
            public static float ScreenWidthRetina => Screen.width / EditorGUIUtility.pixelsPerPoint;
#else
			static public float ScreenWidthRetina { get { return Screen.width; } }
#endif

            //--------------------------------------------------------------------------------------------------------------------------------
            // CUSTOM INSPECTOR UTILS

            internal static bool IsUsingURP()
            {
#if UNITY_2019_3_OR_NEWER
                RenderPipelineAsset renderPipeline = GraphicsSettings.currentRenderPipeline;
#else
				var renderPipeline = GraphicsSettings.renderPipelineAsset;
#endif
                return renderPipeline != null && renderPipeline.GetType().Name.Contains("Universal");
            }

            public static bool HasKeywords(List<string> list, params string[] keywords)
            {
                bool v = false;
                foreach (string kw in keywords)
                    v |= list.Contains(kw);

                return v;
            }

            public static bool ShaderKeywordToggle(string keyword, string label, string tooltip, List<string> list,
                ref bool update, string helpTopic = null)
            {
                float w = EditorGUIUtility.labelWidth;
                if (!string.IsNullOrEmpty(helpTopic))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = w - 16;
                    TCP2_GUI.HelpButton(helpTopic);
                }

                bool boolValue = list.Contains(keyword);
                EditorGUI.BeginChangeCheck();
                boolValue = EditorGUILayout.ToggleLeft(new GUIContent(label, tooltip), boolValue,
                    boolValue ? EditorStyles.boldLabel : EditorStyles.label);
                if (EditorGUI.EndChangeCheck())
                {
                    if (boolValue)
                        list.Add(keyword);
                    else
                        list.Remove(keyword);

                    update = true;
                }

                if (!string.IsNullOrEmpty(helpTopic))
                {
                    EditorGUIUtility.labelWidth = w;
                    EditorGUILayout.EndHorizontal();
                }

                return boolValue;
            }

            public static bool ShaderKeywordRadio(string header, string[] keywords, GUIContent[] labels,
                List<string> list, ref bool update)
            {
                int index = 0;
                for (int i = 1; i < keywords.Length; i++)
                    if (list.Contains(keywords[i]))
                    {
                        index = i;
                        break;
                    }

                EditorGUI.BeginChangeCheck();

                //Header and rect calculations
                bool hasHeader = !string.IsNullOrEmpty(header);
                Rect headerRect = GUILayoutUtility.GetRect(120f, 16f, GUILayout.ExpandWidth(false));
                Rect r = headerRect;
                if (hasHeader)
                {
                    Rect helpRect = headerRect;
                    helpRect.width = 16;
                    headerRect.width -= 16;
                    headerRect.x += 16;
                    string helpTopic = header.ToLowerInvariant();
                    helpTopic = char.ToUpperInvariant(helpTopic[0]) + helpTopic.Substring(1);
                    TCP2_GUI.HelpButton(helpRect, helpTopic);
                    GUI.Label(headerRect, header, index > 0 ? EditorStyles.boldLabel : EditorStyles.label);
                    r.width = ScreenWidthRetina - headerRect.width - 34f;
                    r.x += headerRect.width;
                }
                else
                {
                    r.width = ScreenWidthRetina - 34f;
                }

                for (int i = 0; i < keywords.Length; i++)
                {
                    Rect rI = r;
                    rI.width /= keywords.Length;
                    rI.x += i * rI.width;
                    if (GUI.Toggle(rI, index == i, labels[i],
                            i == 0 ? EditorStyles.miniButtonLeft :
                            i == keywords.Length - 1 ? EditorStyles.miniButtonRight :
                            EditorStyles.miniButtonMid)) index = i;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    //Remove all other keywords and add selected
                    for (int i = 0; i < keywords.Length; i++)
                        if (list.Contains(keywords[i]))
                            list.Remove(keywords[i]);

                    if (index > 0) list.Add(keywords[index]);

                    update = true;
                }

                return index > 0;
            }

            public static int ShaderKeywordRadioGeneric(string header, int index, GUIContent[] labels)
            {
                //Header and rect calculations
                bool hasHeader = !string.IsNullOrEmpty(header);
                Rect controlRect = EditorGUILayout.GetControlRect();
                Rect headerRect = EditorGUI.IndentedRect(controlRect);
                Rect r = headerRect;
                if (hasHeader)
                {
                    headerRect.width = EditorGUIUtility.labelWidth - EditorGUI.indentLevel * 15f;
                    GUI.Label(headerRect, header, EditorStyles.label);
                    r.width -= headerRect.width;
                    r.x += headerRect.width;
                }
                else
                {
                    r.width = ScreenWidthRetina - 20f;
                }

                for (int i = 0; i < labels.Length; i++)
                {
                    Rect rI = r;
                    rI.width /= labels.Length;
                    rI.x += i * rI.width;
                    if (GUI.Toggle(rI, index == i, labels[i],
                            i == 0 ? EditorStyles.miniButtonLeft :
                            i == labels.Length - 1 ? EditorStyles.miniButtonRight :
                            EditorStyles.miniButtonMid)) index = i;
                }

                return index;
            }

            // Enable/Disable a feature on the shader and mark it for update
            public static void ShaderVariantUpdate(string feature, List<string> featuresList,
                List<bool> featuresEnabled, bool enable, ref bool update)
            {
                int featureIndex = featuresList.IndexOf(feature);
                if (featureIndex < 0)
                {
                    EditorGUILayout.HelpBox("Couldn't find shader feature in list: " + feature, MessageType.Error);
                    return;
                }

                if (featuresEnabled[featureIndex] != enable)
                {
                    featuresEnabled[featureIndex] = enable;
                    update = true;
                }
            }

            public static bool AddIfMissing(List<string> list, string item)
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                    return true;
                }

                return false;
            }

            public static bool RemoveIfExists(List<string> list, string item)
            {
                if (list.Contains(item))
                {
                    list.Remove(item);
                    return true;
                }

                return false;
            }


            //--------------------------------------------------------------------------------------------------------------------------------

            private static string cachedReadmePath;

            public static string FindReadmePath(bool relativeToAssets = false)
            {
                string readmePathFull = null;

                // check cached path
                if (!string.IsNullOrEmpty(cachedReadmePath))
                    if (File.Exists(cachedReadmePath))
                        readmePathFull = cachedReadmePath;

                if (readmePathFull == null)
                {
                    // try to find by GUID
                    const string readmeGuid = "d6d278f2c506dde44ab56fd1555fb4d4";
                    string guidPath = AssetDatabase.GUIDToAssetPath(readmeGuid);
                    if (!string.IsNullOrEmpty(guidPath))
                    {
                        readmePathFull = Application.dataPath + guidPath.Substring("Assets".Length);
                        readmePathFull = ToSystemSlashPath(Path.GetDirectoryName(readmePathFull));
                    }
                }

                if (readmePathFull == null)
                {
                    // GUID has been changed, try to find through the file system
                    string readmePath = GetFileSafe(Application.dataPath, "!ToonyColorsPro Readme.txt");
                    if (!string.IsNullOrEmpty(readmePath))
                        readmePath = ToSystemSlashPath(Path.GetDirectoryName(readmePath));
                }

                if (readmePathFull == null)
                {
                    Debug.LogError(
                        "Couldn't find the path to '!ToonyColorsPro Readme.txt', you might have to reimport Toony Colors Pro.\nThis file is necessary to figure out the root directory of Toony Colors Pro.");
                    return null;
                }

                // Cache for future use
                cachedReadmePath = readmePathFull;

                if (relativeToAssets)
                {
#if UNITY_EDITOR_WIN
                    return readmePathFull.Replace(ToSystemSlashPath(Application.dataPath), "Assets").Replace(@"\", "/");
#else
					return readmePathFull.Replace(ToSystemSlashPath(Application.dataPath), "Assets");
#endif
                }

                return readmePathFull;
            }

            /// <summary>
            ///     Similar to Directory.GetFiles, but won't raise exceptions for wrong read permissions
            /// </summary>
            public static string GetFileSafe(string path, string pattern)
            {
                List<string> list = new();
                GetFilesSafeInternal(list, path, pattern, true);
                if (list.Count > 0) return list[0];

                return null;
            }

            /// <summary>
            ///     Similar to Directory.GetFiles, but won't raise exceptions for wrong read permissions
            /// </summary>
            public static string[] GetFilesSafe(string path, string pattern)
            {
                List<string> list = new();
                GetFilesSafeInternal(list, path, pattern, false);
                return list.ToArray();
            }

            // Need to use this recursive version, because the regular GetFiles with subdirectories could raise exceptions depending on read permissions
            private static void GetFilesSafeInternal(List<string> list, string path, string pattern,
                bool stopAtFirstMatch)
            {
                string[] results = Directory.GetFiles(path, pattern);
                if (results != null && results.Length > 0)
                {
                    list.AddRange(results);
                    if (stopAtFirstMatch) return;
                }

                string[] dirs = Directory.GetDirectories(path);
                foreach (string dir in dirs)
                    if (Directory.Exists(dir))
                        GetFilesSafeInternal(list, dir, pattern, stopAtFirstMatch);
            }

            //--------------------------------------------------------------------------------------------------------------------------------

            public enum SmoothedNormalsChannel
            {
                VertexColors,
                Tangents,
                UV1,
                UV2,
                UV3,
                UV4
            }

            public enum SmoothedNormalsUVType
            {
                FullXYZ,
                CompressedXY,
                CompressedZW
            }

            public static Mesh CreateSmoothedMesh(Mesh originalMesh, string format,
                SmoothedNormalsChannel smoothedNormalsChannel, SmoothedNormalsUVType uvType, bool overwriteMesh)
            {
                if (originalMesh == null)
                {
                    Debug.LogWarning(
                        "[TCP2 : Smoothed Mesh] Supplied OriginalMesh is null!\nCan't create smooth normals version.");
                    return null;
                }

                //Create new mesh
                Mesh newMesh = overwriteMesh ? originalMesh : new Mesh();
                if (!overwriteMesh)
                {
                    string originalAssetPath = AssetDatabase.GetAssetPath(originalMesh);
                    ModelImporter originalImporter = null;
                    bool restoreOptimizeGameObjects = false;
                    if (!string.IsNullOrEmpty(originalAssetPath))
                    {
                        originalImporter = AssetImporter.GetAtPath(originalAssetPath) as ModelImporter;

                        if (originalImporter != null && originalImporter.optimizeGameObjects)
                        {
                            originalImporter.optimizeGameObjects = false;
                            AssetDatabase.ImportAsset(originalAssetPath, ImportAssetOptions.ForceSynchronousImport);
                            restoreOptimizeGameObjects = true;
                        }
                    }

                    newMesh.vertices = originalMesh.vertices;
                    newMesh.normals = originalMesh.normals;
                    newMesh.tangents = originalMesh.tangents;
                    newMesh.uv = originalMesh.uv;
                    newMesh.uv2 = originalMesh.uv2;
                    newMesh.uv3 = originalMesh.uv3;
                    newMesh.uv4 = originalMesh.uv4;
                    newMesh.colors32 = originalMesh.colors32;
                    newMesh.triangles = originalMesh.triangles;
                    newMesh.bindposes = originalMesh.bindposes;
                    newMesh.boneWeights = originalMesh.boneWeights;

                    if (originalMesh.blendShapeCount > 0) CopyBlendShapes(originalMesh, newMesh);

                    newMesh.subMeshCount = originalMesh.subMeshCount;
                    if (newMesh.subMeshCount > 1)
                        for (int i = 0; i < newMesh.subMeshCount; i++)
                            newMesh.SetTriangles(originalMesh.GetTriangles(i), i);

                    if (restoreOptimizeGameObjects)
                    {
                        originalImporter.optimizeGameObjects = true;
                        AssetDatabase.ImportAsset(originalAssetPath, ImportAssetOptions.ForceSynchronousImport);
                    }
                }

                //--------------------------------
                // Format

                Vector3 chSign = Vector3.one;
                if (string.IsNullOrEmpty(format)) format = "xyz";
                format = format.ToLowerInvariant();
                int[] channels = { 0, 1, 2 };
                bool skipFormat = format == "xyz";
                int charIndex = 0;
                int ch = 0;
                while (charIndex < format.Length)
                {
                    switch (format[charIndex])
                    {
                        case '-':
                            chSign[ch] = -1;
                            break;
                        case 'x':
                            channels[ch] = 0;
                            ch++;
                            break;
                        case 'y':
                            channels[ch] = 1;
                            ch++;
                            break;
                        case 'z':
                            channels[ch] = 2;
                            ch++;
                            break;
                    }

                    if (ch > 2) break;
                    charIndex++;
                }

                //--------------------------------
                //Calculate smoothed normals

                //Iterate, find same-position vertices and calculate averaged values as we go
                Dictionary<Vector3, Vector3> averageNormalsHash = new();
                for (int i = 0; i < newMesh.vertexCount; i++)
                    if (!averageNormalsHash.ContainsKey(newMesh.vertices[i]))
                        averageNormalsHash.Add(newMesh.vertices[i], newMesh.normals[i]);
                    else
                        averageNormalsHash[newMesh.vertices[i]] =
                            (averageNormalsHash[newMesh.vertices[i]] + newMesh.normals[i]).normalized;

                //Convert to Array
                Vector3[] averageNormals = new Vector3[newMesh.vertexCount];
                for (int i = 0; i < newMesh.vertexCount; i++)
                {
                    averageNormals[i] = averageNormalsHash[newMesh.vertices[i]];
                    if (!skipFormat)
                        averageNormals[i] =
                            Vector3.Scale(
                                new Vector3(averageNormals[i][channels[0]], averageNormals[i][channels[1]],
                                    averageNormals[i][channels[2]]), chSign);
                }

#if DONT_ALTER_NORMALS
				//Debug: don't alter normals to see if converting into colors/tangents/uv2 works correctly
				for(int i = 0; i < newMesh.vertexCount; i++)
				{
					averageNormals[i] = newMesh.normals[i];
				}
#endif

                //--------------------------------
                // Store in Vertex Colors

                if (smoothedNormalsChannel == SmoothedNormalsChannel.VertexColors)
                {
                    //Assign averaged normals to colors
                    Color32[] colors = new Color32[newMesh.vertexCount];
                    for (int i = 0; i < newMesh.vertexCount; i++)
                    {
                        byte r = (byte)((averageNormals[i].x * 0.5f + 0.5f) * 255);
                        byte g = (byte)((averageNormals[i].y * 0.5f + 0.5f) * 255);
                        byte b = (byte)((averageNormals[i].z * 0.5f + 0.5f) * 255);

                        colors[i] = new Color32(r, g, b, 255);
                    }

                    newMesh.colors32 = colors;
                }

                //--------------------------------
                // Store in Tangents

                if (smoothedNormalsChannel == SmoothedNormalsChannel.Tangents)
                {
                    //Assign averaged normals to tangent
                    Vector4[] tangents = new Vector4[newMesh.vertexCount];
                    for (int i = 0; i < newMesh.vertexCount; i++)
                        tangents[i] = new Vector4(averageNormals[i].x, averageNormals[i].y, averageNormals[i].z, 0f);
                    newMesh.tangents = tangents;
                }

                //--------------------------------
                // Store in UVs

                if (smoothedNormalsChannel == SmoothedNormalsChannel.UV1 ||
                    smoothedNormalsChannel == SmoothedNormalsChannel.UV2 ||
                    smoothedNormalsChannel == SmoothedNormalsChannel.UV3 ||
                    smoothedNormalsChannel == SmoothedNormalsChannel.UV4)
                {
                    int uvIndex = -1;

                    switch (smoothedNormalsChannel)
                    {
                        case SmoothedNormalsChannel.UV1:
                            uvIndex = 0;
                            break;
                        case SmoothedNormalsChannel.UV2:
                            uvIndex = 1;
                            break;
                        case SmoothedNormalsChannel.UV3:
                            uvIndex = 2;
                            break;
                        case SmoothedNormalsChannel.UV4:
                            uvIndex = 3;
                            break;
                        default:
                            Debug.LogError("Invalid smoothed normals UV channel: " + smoothedNormalsChannel);
                            break;
                    }

                    if (uvType == SmoothedNormalsUVType.FullXYZ)
                    {
                        //Assign averaged normals directly to UV fully (xyz)
                        newMesh.SetUVs(uvIndex, new List<Vector3>(averageNormals));
                    }
                    else
                    {
                        if (uvType == SmoothedNormalsUVType.CompressedXY)
                        {
                            //Assign averaged normals to UV compressed (x,y to uv.x and z to uv.y)
                            List<Vector2> uvs = new(newMesh.vertexCount);
                            for (int i = 0; i < newMesh.vertexCount; i++)
                            {
                                float x, y;
                                GetCompressedSmoothedNormals(averageNormals[i], out x, out y);
                                Vector2 v2 = new(x, y);
                                uvs.Add(v2);
                            }

                            newMesh.SetUVs(uvIndex, uvs);
                        }
                        else if (uvType == SmoothedNormalsUVType.CompressedZW)
                        {
                            //Assign averaged normals to UV compressed (x,y to uv.z and z to uv.w)
                            List<Vector4> existingUvs = new();
                            newMesh.GetUVs(uvIndex, existingUvs);
                            if (existingUvs.Count == 0) existingUvs.AddRange(new Vector4[newMesh.vertexCount]);
                            List<Vector4> uvs = new(newMesh.vertexCount);
                            for (int i = 0; i < newMesh.vertexCount; i++)
                            {
                                float x, y;
                                GetCompressedSmoothedNormals(averageNormals[i], out x, out y);
                                Vector4 v4 = existingUvs[i];
                                v4.z = x;
                                v4.w = y;
                                uvs.Add(v4);
                            }

                            newMesh.SetUVs(uvIndex, uvs);
                        }
                    }
                }

                return newMesh;
            }

            private static void GetCompressedSmoothedNormals(Vector3 smoothedNormal, out float x, out float y)
            {
                float _x = smoothedNormal.x * 0.5f + 0.5f;
                float _y = smoothedNormal.y * 0.5f + 0.5f;
                float _z = smoothedNormal.z * 0.5f + 0.5f;

                //pack x,y to uv2.x
                _x = Mathf.Round(_x * 15);
                _y = Mathf.Round(_y * 15);
                float packed = Vector2.Dot(new Vector2(_x, _y),
                    new Vector2((float)(1.0 / (255.0 / 16.0)), (float)(1.0 / 255.0)));

                x = packed;
                y = _z;
            }

            //Only available from Unity 5.3 onward
            private static void CopyBlendShapes(Mesh originalMesh, Mesh newMesh)
            {
                for (int i = 0; i < originalMesh.blendShapeCount; i++)
                {
                    string shapeName = originalMesh.GetBlendShapeName(i);
                    int frameCount = originalMesh.GetBlendShapeFrameCount(i);
                    for (int j = 0; j < frameCount; j++)
                    {
                        Vector3[] dv = new Vector3[originalMesh.vertexCount];
                        Vector3[] dn = new Vector3[originalMesh.vertexCount];
                        Vector3[] dt = new Vector3[originalMesh.vertexCount];

                        float frameWeight = originalMesh.GetBlendShapeFrameWeight(i, j);
                        originalMesh.GetBlendShapeFrameVertices(i, j, dv, dn, dt);
                        newMesh.AddBlendShapeFrame(shapeName, frameWeight, dv, dn, dt);
                    }
                }
            }

            public static string UnityRelativeToSystemPath(string path)
            {
                string sysPath = path;
#if UNITY_EDITOR_WIN
                sysPath = path.Replace("/", @"\");
#endif
                string appPath = ToSystemSlashPath(Application.dataPath);
                appPath = appPath.Substring(0, appPath.Length - 6); // Remove 'Assets'
                sysPath = appPath + sysPath;
                return sysPath;
            }

            public static string ToSystemSlashPath(string path)
            {
#if UNITY_EDITOR_WIN
                return path.Replace("/", @"\");
#else
				return path;
#endif
            }

            public static bool SystemToUnityPath(ref string sysPath)
            {
                if (sysPath.IndexOf(Application.dataPath) < 0) return false;

                sysPath = string.Format("Assets{0}", sysPath.Replace(Application.dataPath, ""));
                return true;
            }

            public static string OpenFolderPanel_ProjectPath(string label, string startDir)
            {
                string output = null;

                if (startDir.Length > 0 && startDir[0] != '/') startDir = "/" + startDir;

                string startPath = Application.dataPath.Replace(@"\", "/") + startDir;
                if (!Directory.Exists(startPath)) startPath = Application.dataPath;

                string path = EditorUtility.OpenFolderPanel(label, startPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    bool validPath = SystemToUnityPath(ref path);
                    if (validPath)
                    {
                        if (path == "Assets")
                            output = "/";
                        else
                            output = path.Substring("Assets/".Length);
                    }
                    else
                    {
                        EditorApplication.Beep();
                        EditorUtility.DisplayDialog("Invalid Path",
                            "The selected path is invalid.\n\nPlease select a folder inside the \"Assets\" folder of your project!",
                            "Ok");
                    }
                }

                return output;
            }
        }
    }
}
