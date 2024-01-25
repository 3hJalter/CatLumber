// Toony Colors Pro 2
// (c) 2014-2023 Jean Moreno

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ToonyColorsPro.Utilities;
using UnityEditor;
using UnityEngine;

// Represents a Shader Generator 2 module: external file that has specific code for a feature, that can be reused among templates

namespace ToonyColorsPro
{
    namespace ShaderGenerator
    {
        public class Module
        {
            private Dictionary<string, List<string>> ArbitraryBlocks = new();
            public bool ExplicitFunctionsDeclaration;
            public string[] Features = new string[0];
            private readonly Dictionary<string, string[]> Fragments = new();
            private readonly Dictionary<string, Argument[]> FragmentsArgs = new();
            public string[] Functions = new string[0];
            public string[] InputStruct = new string[0];
            public string[] Keywords = new string[0];

            public string name;
            public string[] PropertiesBlock = new string[0];
            public string[] PropertiesNew = new string[0];
            public string[] ShaderFeaturesBlock = new string[0];
            public string[] Variables = new string[0];
            public string[] VariablesOutsideCBuffer = new string[0];
            private readonly Dictionary<string, string[]> Vertices = new();

            private readonly Dictionary<string, Argument[]> VerticesArgs = new();

            public List<string> GetArbitraryBlock(string block)
            {
                if (!ArbitraryBlocks.ContainsKey(block))
                {
                    Debug.LogError(string.Format("Couldn't find block with name '{0}' in module '{1}'", block, name));
                    return null;
                }

                return ArbitraryBlocks[block];
            }

            public static Module CreateFromName(string moduleName)
            {
                string moduleFile = string.Format("Module_{0}.txt", moduleName);
                string rootPath = Utils.FindReadmePath(true);
                string modulePath = string.Format("{0}/Shader Templates 2/Modules/{1}", rootPath, moduleFile);

                TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(modulePath);

                //Can't find through default path, try to search for the file using AssetDatabase
                if (textAsset == null)
                {
                    string[] matches = AssetDatabase.FindAssets(string.Format("Module_{0} t:textasset", moduleName));
                    if (matches.Length > 0)
                        // Get the first result
                        textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(matches[0]));
                    else
                        Debug.LogError(ShaderGenerator2.ErrorMsg(string.Format(
                            "Can't find module using Unity's search system. Make sure that the file 'Module_{0}' is in the project!",
                            moduleName)));
                }

                if (textAsset == null)
                {
                    Debug.LogError(ShaderGenerator2.ErrorMsg(string.Format("Can't load module: '{0}'", moduleName)));
                    return null;
                }

                string[] lines = textAsset.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                List<string> features = new();
                List<string> propertiesNew = new();
                List<string> keywords = new();
                List<string> shaderFeaturesBlock = new();
                List<string> propertiesBlock = new();
                List<string> variables = new();
                List<string> variablesOutsideCbuffer = new();
                List<string> functions = new();
                List<string> inputStruct = new();
                bool explicitFunctions = false;

                Dictionary<string, List<Argument>> verticesArgs = new();
                Dictionary<string, List<Argument>> fragmentsArgs = new();
                Dictionary<string, List<string>> vertices = new();
                Dictionary<string, List<string>> fragments = new();

                Dictionary<string, List<string>> arbitraryBlocks = new();

                List<string> currentList = null;

                foreach (string line in lines)
                    if (line.StartsWith("#") && !line.Contains("_IMPL"))
                    {
                        string lineTrim = line.Trim();

                        //fragment can have arguments, so check the start of the line instead of exact word
                        if (lineTrim.StartsWith("#VERTEX"))
                        {
                            string key = "";
                            if (lineTrim.Contains(":"))
                            {
                                int start = "#VERTEX:".Length;
                                int end = lineTrim.IndexOf('(');
                                key = lineTrim.Substring(start, end - start);
                            }

                            currentList = new List<string>();
                            vertices.Add(key, currentList);

                            if (lineTrim.Contains("(") && lineTrim.Contains(")"))
                            {
                                //parse arguments
                                List<Argument> vertexArgs = ParseArguments(lineTrim);
                                verticesArgs.Add(key, vertexArgs);
                            }
                        }
                        //#LIGHTING is an alias for fragment here, just to differentiate in the template code
                        else if (lineTrim.StartsWith("#FRAGMENT") || lineTrim.StartsWith("#LIGHTING"))
                        {
                            string key = "";
                            if (lineTrim.Contains(":"))
                            {
                                int start = "#FRAGMENT:".Length; // same character count for #LIGHTING
                                int end = lineTrim.IndexOf('(');
                                if (end >= 0)
                                    key = lineTrim.Substring(start, end - start);
                                else
                                    key = lineTrim.Substring(start);
                            }

                            currentList = new List<string>();
                            fragments.Add(key, currentList);

                            if (lineTrim.Contains("(") && lineTrim.Contains(")"))
                            {
                                //parse arguments
                                List<Argument> fragmentArgs = ParseArguments(lineTrim);
                                fragmentsArgs.Add(key, fragmentArgs);
                            }
                        }
                        else if (lineTrim.StartsWith("#FUNCTIONS:EXPLICIT"))
                        {
                            // Explicit functions that have to be declared in the template with [[Module:FUNCTIONS:module_name]]
                            currentList = functions;
                            explicitFunctions = true;
                        }
                        else
                        {
                            switch (lineTrim)
                            {
                                case "#FEATURES":
                                    currentList = features;
                                    break;
                                case "#PROPERTIES_NEW":
                                    currentList = propertiesNew;
                                    break;
                                case "#KEYWORDS":
                                    currentList = keywords;
                                    break;
                                case "#PROPERTIES_BLOCK":
                                    currentList = propertiesBlock;
                                    break;
                                case "#SHADER_FEATURES_BLOCK":
                                    currentList = shaderFeaturesBlock;
                                    break;
                                case "#FUNCTIONS":
                                    currentList = functions;
                                    break;
                                case "#VARIABLES":
                                    currentList = variables;
                                    break;
                                case "#VARIABLES_OUTSIDE_CBUFFER":
                                    currentList = variablesOutsideCbuffer;
                                    break;
                                case "#INPUT":
                                    currentList = inputStruct;
                                    break;
                                case "#END":
                                    currentList = null;
                                    break;
                                default:
                                {
                                    // An "arbitrary block" is parsed if not using a predefine keyword like above, and we are not iterating over an existing block
                                    if (currentList == null)
                                    {
                                        string block = lineTrim.Substring(1);
                                        if (block.Length > 0 && !char.IsWhiteSpace(block[0]))
                                        {
                                            currentList = new List<string>();
                                            arbitraryBlocks.Add(block, currentList);
                                        }
                                    }

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (currentList != null) currentList.Add(line);
                    }

                Module module = new();
                module.name = moduleName;
                module.Features = features.ToArray();
                module.PropertiesNew = propertiesNew.ToArray();
                module.Keywords = keywords.ToArray();
                module.ShaderFeaturesBlock = shaderFeaturesBlock.ToArray();
                module.PropertiesBlock = propertiesBlock.ToArray();
                module.Functions = functions.ToArray();
                module.Variables = variables.ToArray();
                module.VariablesOutsideCBuffer = variablesOutsideCbuffer.ToArray();
                module.InputStruct = inputStruct.ToArray();
                module.ExplicitFunctionsDeclaration = explicitFunctions;
                module.ArbitraryBlocks = arbitraryBlocks;

                // #VERTEX
                if (vertices.Count == 0)
                {
                    vertices.Add("", new List<string>());
                    verticesArgs.Add("", new List<Argument>());
                }

                foreach (KeyValuePair<string, List<string>> vertexPair in vertices)
                {
                    string key = vertexPair.Key;
                    module.Vertices.Add(key, vertexPair.Value.ToArray());
                    if (verticesArgs.ContainsKey(key)) module.VerticesArgs.Add(key, verticesArgs[key].ToArray());
                }

                // #FRAGMENT
                if (fragments.Count == 0)
                {
                    fragments.Add("", new List<string>());
                    fragmentsArgs.Add("", new List<Argument>());
                }

                foreach (KeyValuePair<string, List<string>> fragmentPair in fragments)
                {
                    string key = fragmentPair.Key;
                    module.Fragments.Add(key, fragmentPair.Value.ToArray());
                    if (fragmentsArgs.ContainsKey(key)) module.FragmentsArgs.Add(key, fragmentsArgs[key].ToArray());
                }

                module.ProcessIndentation();

                return module;
            }

            private static List<Argument> ParseArguments(string line)
            {
                List<Argument> list = new();

                //parse arguments
                int start = line.IndexOf("(") + 1;
                int end = line.IndexOf(")");
                string content = line.Substring(start, end - start);
                string[] args = content.Split(',');
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].Trim();
                    int spaceIndex = arg.IndexOf(arg.Substring(arg.IndexOf(' ')));
                    string type = arg.Substring(0, spaceIndex);
                    string name = arg.Substring(spaceIndex + 1);
                    Argument argument = new()
                    {
                        variable = type,
                        name = name
                    };
                    list.Add(argument);
                }

                return list;
            }

            //Find minimum indentation and remove for every line for each block
            private void ProcessIndentation()
            {
                RemoveMinimumIndentation(Features);
                RemoveMinimumIndentation(PropertiesNew);
                RemoveMinimumIndentation(Keywords);
                RemoveMinimumIndentation(ShaderFeaturesBlock);
                RemoveMinimumIndentation(PropertiesBlock);
                RemoveMinimumIndentation(Functions);
                RemoveMinimumIndentation(Variables);
                RemoveMinimumIndentation(VariablesOutsideCBuffer);
                RemoveMinimumIndentation(InputStruct);
                RemoveMinimumIndentation(Vertices);
                RemoveMinimumIndentation(Fragments);
            }

            private void RemoveMinimumIndentation(Dictionary<string, string[]> dict)
            {
                foreach (string key in dict.Keys) RemoveMinimumIndentation(dict[key]);
            }

            private void RemoveMinimumIndentation(string[] block)
            {
                if (block == null)
                    return;

                //Find minimum number of leading tabs across all lines
                int minIndent = 999;
                for (int i = 0; i < block.Length; i++)
                {
                    string trimmedBlock = block[i].Trim();
                    if (trimmedBlock.StartsWith("///") || block[i].StartsWith("#") ||
                        string.IsNullOrEmpty(trimmedBlock)) continue;

                    // special cases to ignore, as they won't be part of the shader code
                    if (trimmedBlock[0] == '#' && trimmedBlock.Contains("not_empty")) continue;

                    int j = 0;
                    while (j < block[i].Length && block[i][j] == '\t') j++;
                    minIndent = Mathf.Min(minIndent, j);
                }

                //Remove that minimum value for all lines (excluding /// and ENABLE_IMPL and DISABLE_IMPL)
                for (int i = 0; i < block.Length; i++)
                {
                    string trim = block[i].Trim();
                    if (trim.StartsWith("///") || (trim.StartsWith("#") && trim.Contains("_IMPL")))
                        continue;

                    if (trim.StartsWith("#") && trim.Contains("not_empty"))
                        continue;

                    if (block[i].Length > minIndent)
                        block[i] = block[i].Substring(minIndent);
                }
            }

            //Return the Vertex Lines with the arguments replaced with their proper names
            public string[] VertexLines(List<string> arguments, string key = "")
            {
                Argument[] args;
                VerticesArgs.TryGetValue(key, out args);
                return ArgumentLines(Vertices[key], args, arguments);
            }

            //Return the Fragment Lines with the arguments replaced with their proper names
            public string[] FragmentLines(List<string> arguments, string key = "")
            {
                Argument[] args;
                string[] lines;
                FragmentsArgs.TryGetValue(key, out args);
                Fragments.TryGetValue(key, out lines);

                if (lines == null)
                {
                    Debug.LogError(ShaderGenerator2.ErrorMsg(string.Format(
                        "Can't find #FRAGMENT/#LIGHTING for Module '{0}{1}'", name,
                        string.IsNullOrEmpty(key) ? "" : ":" + key)));
                    return null;
                }

                return ArgumentLines(lines, args, arguments);
            }

            private string[] ArgumentLines(string[] array, Argument[] arguments, List<string> suppliedArguments)
            {
                if (arguments == null || arguments.Length == 0) return array;

                if (suppliedArguments.Count != arguments.Length)
                    Debug.LogError(ShaderGenerator2.ErrorMsg(string.Format(
                        "[Module {4}] Invalid number of arguments provided: got <b>{0}</b>, expected <b>{1}</b>:\nExpected: {2}\nSupplied: {3}",
                        suppliedArguments.Count,
                        arguments.Length,
                        string.Join(", ", Array.ConvertAll(arguments, a => a.ToString())),
                        string.Join(", ", suppliedArguments.ToArray()),
                        name)));

                List<string> list = new();
                foreach (string line in array)
                {
                    string lineWithArgs = line;
                    for (int i = 0; i < arguments.Length; i++)
                        lineWithArgs = Regex.Replace(lineWithArgs, @"\b" + arguments[i].name + @"\b",
                            suppliedArguments[i]);
                    list.Add(lineWithArgs);
                }

                return list.ToArray();
            }

            public class Argument
            {
                public string name;

                //Variable type is parsed but we actually don't care about it in the code, it's just an indication in the Module file for proper integration into the Template
                public string variable;

                public override string ToString()
                {
                    return string.Format("{0} : {1}", name, variable);
                }
            }
        }
    }
}
