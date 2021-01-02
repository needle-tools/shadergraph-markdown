
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class MarkdownHDLitGUI : LitShaderGraphGUI
    {
        public MarkdownHDLitGUI() : base()
        {
            // remove default ShaderGraphUI block because we're rendering this ourselves
            uiBlocks.Remove(uiBlocks.FetchUIBlock<ShaderGraphUIBlock>());
        }
        
        public T GetBlock<T>() where T: MaterialUIBlock
        {
            return uiBlocks.FetchUIBlock<T>();
        }

        public MaterialUIBlockList blocks => uiBlocks;
    }

    public static class MarkdownHDExtensions
    {
        public static void DrawBlock(ShaderGUI shaderGUI)
        {
            switch (shaderGUI)
            {
                case MarkdownHDLitGUI markdownHdLitGUI:
                    break;
            }
        }

        public static int GetMaterialIdExt(this Material material)
        {
            return (int) material.GetMaterialId();
        }
        
        internal static GraphData GetGraphData(AssetImporter importer)
        {
            var path = importer.assetPath;
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            var graph = new GraphData {
                assetGuid = AssetDatabase.AssetPathToGUID(path)
            };
            MultiJson.Deserialize(graph, textGraph);
            graph.OnEnable();
            graph.ValidateGraph();
            return graph;
        }

        public static string GetDefaultCustomInspectorFromShader(Shader shader)
        {
            if (!AssetDatabase.Contains(shader)) return null;
            string customInspector = null;
            
            var assetPath = AssetDatabase.GetAssetPath(shader);
            var assetImporter = AssetImporter.GetAtPath(assetPath);
            if (assetImporter is ShaderGraphImporter shaderGraphImporter)
            {
                var graphData = GetGraphData(shaderGraphImporter);
                foreach(var target in graphData.activeTargets) {
                    Debug.Log("Active Target: " + target.displayName);
                    if (target is HDTarget hdTarget)
                    {
                        // Possible SubTargets  
                        // foreach (var subTarget in TargetUtils.GetSubTargets(hdTarget)) {
                        //     Debug.Log(subTarget);
                        // }

                        var activeSubTarget = ((JsonData<SubTarget>) typeof(HDTarget).GetField("m_ActiveSubTarget", (BindingFlags)(-1)).GetValue(hdTarget)).value;
                        if (activeSubTarget is HDSubTarget hdSubTarget)
                        {
                            customInspector = (string) typeof(HDSubTarget).GetProperty("customInspector", (BindingFlags) (-1))?.GetValue(hdSubTarget);
                            Debug.Log($"Active SubTarget: {activeSubTarget.displayName}, Custom Editor: " + customInspector);
                        }
                    }
                }
                foreach (var target in graphData.allPotentialTargets)
                    Debug.Log("Potential Target: " + target.displayName);
            }
            
            // HDShaderUtils.IsHDRPShader
            var shaderID = HDShaderUtils.GetShaderEnumFromShader(shader);
            
            // var hdMetadata = AssetDatabase.LoadAssetAtPath<HDMetadata>(assetPath);
            Debug.Log("Shader ID: " + shaderID);
            
            // HDSubTarget.Setup

            return customInspector;
        }

        public static void RemoveShaderGraphUIBlock(ShaderGUI baseShaderGui)
        {
            if (baseShaderGui is LightingShaderGraphGUI hdGui)
            {
                var blockList = (MaterialUIBlockList) typeof(LightingShaderGraphGUI).GetProperty("uiBlocks", (BindingFlags) (-1))?.GetValue(hdGui);
                blockList?.Remove(blockList.FetchUIBlock<ShaderGraphUIBlock>());
            }
        }
    }
}