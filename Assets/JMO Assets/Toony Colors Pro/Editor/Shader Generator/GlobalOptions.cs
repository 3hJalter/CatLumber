// Toony Colors Pro 2
// (c) 2014-2023 Jean Moreno

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Represents the global options for the Shader Generator, using the EditorPrefs API

namespace ToonyColorsPro
{
    namespace ShaderGenerator
    {
        // Global Options shared across all Unity projects
        public static class GlobalOptions
        {
            private static Data _data;

            public static Data data
            {
                get
                {
                    if (_data == null) LoadUserPrefs();
                    return _data;
                }
            }

            public static void LoadUserPrefs()
            {
                string dataStr = EditorPrefs.GetString("TCP2_GlobalOptions", null);
                _data = new Data();
                if (!string.IsNullOrEmpty(dataStr)) EditorJsonUtility.FromJsonOverwrite(dataStr, _data);
            }

            public static void SaveUserPrefs()
            {
                EditorPrefs.SetString("TCP2_GlobalOptions", EditorJsonUtility.ToJson(data));
            }

            [Serializable]
            public class Data
            {
                public bool ShowOptions = true;
                public bool ShowDisabledFeatures = true;
                public bool SelectGeneratedShader = true;
                public bool ShowContextualHelp = true;
                public bool DockableWindow;
            }
        }

        // Project Options only saved for this Unity project
        public static class ProjectOptions
        {
            private static Data _data;

            public static Data data
            {
                get
                {
                    if (_data == null) LoadProjectOptions();
                    return _data;
                }
            }

            private static string GetPath()
            {
                return Application.dataPath.Replace(@"\", "/") + "/../ProjectSettings/ToonyColorsPro.json";
            }

            public static void LoadProjectOptions()
            {
                _data = new Data();
                string path = GetPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    EditorJsonUtility.FromJsonOverwrite(json, _data);
                }
            }

            public static void SaveProjectOptions()
            {
                string path = GetPath();
                string json = EditorJsonUtility.ToJson(_data, true);
                File.WriteAllText(path, json);
            }

            [Serializable]
            public class Data
            {
                public bool AutoNames = true;
                public bool SubFolders = true;
                public bool OverwriteConfig;
                public bool LoadAllShaders;
                public string CustomOutputPath = ShaderGenerator2.OUTPUT_PATH;
                public string LastImplementationExportImportPath = Application.dataPath;
                public List<string> OpenedFoldouts = new();
                public bool UseCustomFont;
                public Font CustomFont;
                public bool CustomFontInitialized;
                public bool Upgrade_Hybrid1toHybrid2_Done;
            }
        }
    }
}
