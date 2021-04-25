#if !NO_INTERNALS_ACCESS && UNITY_2019_4_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.ShaderGraph.Serialization;
#else
#endif

namespace UnityEditor.ShaderGraph
{
    public static partial class MarkdownSGExtensions
    {
        internal static GraphData GetGraphData(AssetImporter importer)
        {
            try
            {
                var path = importer.assetPath;
                var textGraph = File.ReadAllText(path, Encoding.UTF8);

#if UNITY_2020_2_OR_NEWER
                var graph = new GraphData
                {
                    assetGuid = AssetDatabase.AssetPathToGUID(path)
                };
                MultiJson.Deserialize(graph, textGraph);
#else
                var graph = JsonUtility.FromJson<GraphData>(textGraph);
#endif

                graph.OnEnable();
                graph.ValidateGraph();
                return graph;
            }
            catch (ArgumentException e)
            {
                Debug.LogError("Couldn't get graph data for " + importer.assetPath + ": " + e.ToString());
                return null;
            }
        }

        internal static GraphData GetGraphData(Shader shader)
        {
            if (!AssetDatabase.Contains(shader)) return null;
            var assetPath = AssetDatabase.GetAssetPath(shader);
            var assetImporter = AssetImporter.GetAtPath(assetPath);
            if (assetImporter is ShaderGraphImporter shaderGraphImporter)
                return GetGraphData(shaderGraphImporter);

            return null;
        }

        internal static void WriteShaderGraphToDisk(Shader shader, GraphData graphData)
        {
#if UNITY_2020_2_OR_NEWER
            File.WriteAllText(AssetDatabase.GetAssetPath(shader), MultiJson.Serialize(graphData));
#else
            File.WriteAllText(AssetDatabase.GetAssetPath(shader), JsonUtility.ToJson(graphData));
#endif
            AssetDatabase.Refresh();
        }

        public static string GetDefaultCustomInspectorFromShader(Shader shader)
        {
            var graphData = GetGraphData(shader);
            if (graphData == null) return null;
            string customInspector = null;

            foreach (var getter in CustomInspectorGetters)
            {
                var inspector = getter(graphData);
                if (!string.IsNullOrEmpty(inspector))
                {
                    customInspector = inspector;
                    break;
                }
            }

            // foreach (var target in graphData.allPotentialTargets)
            //     Debug.Log("Potential Target: " + target.displayName);

            // HDShaderUtils.IsHDRPShader
            // var shaderID = HDShaderUtils.GetShaderEnumFromShader(shader);

            // var hdMetadata = AssetDatabase.LoadAssetAtPath<HDMetadata>(assetPath);
            // Debug.Log("Shader ID: " + shaderID);

            // HDSubTarget.Setup

            return customInspector;
        }

        private const string CustomEditorGUI = "Needle.MarkdownShaderGUI";

        [MenuItem("CONTEXT/Material/Toggle ShaderGraph Markdown", true)]
        static bool ToggleShaderGraphMarkdownValidate(MenuCommand command)
        {
            if (command.context is Material mat)
                return MarkdownSGExtensions.GetGraphData(mat.shader) != null;
            return false;
        }

        [MenuItem("CONTEXT/Material/Toggle ShaderGraph Markdown", false, 1000)]
        static void ToggleShaderGraphMarkdown(MenuCommand command)
        {
            if (command.context is Material mat)
            {
                var graphData = MarkdownSGExtensions.GetGraphData(mat.shader);

#if UNITY_2020_2_OR_NEWER
                var haveSetInspector = false;
                foreach (var target in graphData.activeTargets)
                {
                    foreach (var setter in CustomInspectorSetters)
                    {
                        if (setter(target, CustomEditorGUI))
                        {
                            haveSetInspector = true;
                            break;
                        }

                        if (haveSetInspector) break;
                    }
                }
#else
                if (graphData.outputNode is UnityEditor.ShaderGraph.MasterNode masterNode)
                {
                    var canChangeShaderGUI = masterNode as UnityEditor.Graphing.ICanChangeShaderGUI;
                    // GenerationUtils.FinalCustomEditorString(canChangeShaderGUI);
                    if (canChangeShaderGUI.OverrideEnabled) 
                    {
                        canChangeShaderGUI.OverrideEnabled = false;
                        canChangeShaderGUI.ShaderGUIOverride = null;
                    }
                    else 
                    {
                        canChangeShaderGUI.OverrideEnabled = true;
                        canChangeShaderGUI.ShaderGUIOverride = CustomEditorGUI;
                    }
                }
#endif
                MarkdownSGExtensions.WriteShaderGraphToDisk(mat.shader, graphData);
            }
        }

        private static readonly List<Func<GraphData, string>> CustomInspectorGetters =
            new List<Func<GraphData, string>>();

        internal static void RegisterCustomInspectorGetter(Func<GraphData, string> func)
        {
            CustomInspectorGetters.Add(func);
        }

#if UNITY_2020_2_OR_NEWER
        private static readonly List<Func<Target, string, bool>> CustomInspectorSetters =
            new List<Func<Target, string, bool>>();

        internal static void RegisterCustomInspectorSetter(Func<Target, string, bool> func)
        {
            CustomInspectorSetters.Add(func);
        }
#endif
    }
}

#endif