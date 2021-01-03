#if !NO_HDRP_INTERNAL && UNITY_2019_4_OR_NEWER

using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.Rendering.HighDefinition.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
#endif
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    public static class MarkdownHDExtensions
    {
        private const string CustomEditorGUI = "Needle.MarkdownShaderGUI";
        
        [MenuItem("CONTEXT/Material/Toggle ShaderGraph Markdown", true)]
        static bool ToggleShaderGraphMarkdownValidate(MenuCommand command)
        {
            if (command.context is Material mat)
                return GetGraphData(mat.shader) != null;
            return false;
        }
        
        [MenuItem("CONTEXT/Material/Toggle ShaderGraph Markdown", false, 1000)]
        static void ToggleShaderGraphMarkdown(MenuCommand command)
        {
            if (command.context is Material mat)
            {
                var graphData = GetGraphData(mat.shader);
                
#if UNITY_2020_2_OR_NEWER
                foreach (var target in graphData.activeTargets)
                {
                    if (target is HDTarget hdTarget)
                    {
                        if (hdTarget.customEditorGUI.Equals(CustomEditorGUI, StringComparison.Ordinal))
                            hdTarget.customEditorGUI = null; // GetDefaultCustomInspectorFromShader(mat.shader);
                        else
                            hdTarget.customEditorGUI = CustomEditorGUI;
                        
                        File.WriteAllText(AssetDatabase.GetAssetPath(mat.shader), MultiJson.Serialize(graphData));
                    }
                }
#else
                if (graphData.outputNode is MasterNode masterNode)
                {
                    var canChangeShaderGUI = masterNode as ICanChangeShaderGUI;
                    GenerationUtils.FinalCustomEditorString(canChangeShaderGUI);
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
                    
                    File.WriteAllText(AssetDatabase.GetAssetPath(mat.shader), JsonUtility.ToJson(graphData));
                }
#endif
                AssetDatabase.Refresh();
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
#if UNITY_2020_2_OR_NEWER
            var graph = new GraphData {
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

        internal static GraphData GetGraphData(Shader shader)
        {
            if (!AssetDatabase.Contains(shader)) return null;
            var assetPath = AssetDatabase.GetAssetPath(shader);
            var assetImporter = AssetImporter.GetAtPath(assetPath);
            if (assetImporter is ShaderGraphImporter shaderGraphImporter)
                return GetGraphData(shaderGraphImporter);

            return null;
        }
        
        public static string GetDefaultCustomInspectorFromShader(Shader shader)
        {
            var graphData = GetGraphData(shader);
            if (graphData == null) return null;
            string customInspector = null;
            
#if UNITY_2020_2_OR_NEWER
            foreach(var target in graphData.activeTargets)
            {
                // Debug.Log("Active Target: " + target.displayName);
                if (target is HDTarget hdTarget)
                {
                    // currently set editor: hdTarget.customEditorGUI
                    
                    // Possible SubTargets  
                    // foreach (var subTarget in TargetUtils.GetSubTargets(hdTarget)) {
                    //     Debug.Log(subTarget);
                    // }

                    var activeSubTarget = ((JsonData<SubTarget>) typeof(HDTarget).GetField("m_ActiveSubTarget", (BindingFlags)(-1)).GetValue(hdTarget)).value;
                    if (activeSubTarget is HDSubTarget hdSubTarget)
                    {
                        customInspector = (string) typeof(HDSubTarget).GetProperty("customInspector", (BindingFlags) (-1))?.GetValue(hdSubTarget);
                        // Debug.Log($"Active SubTarget: {activeSubTarget.displayName}, Custom Editor: " + customInspector);
                    }
                }
            }
#else
            if (graphData.outputNode is MasterNode masterNode)
            {
                switch (masterNode)
                {
                    case HDLitMasterNode hdLitMasterNode:
                        customInspector = "UnityEditor.Rendering.HighDefinition.HDLitGUI";
                        break;
                    case HDUnlitMasterNode hdUnlitMasterNode:
                        customInspector = "UnityEditor.Rendering.HighDefinition.HDUnlitGUI";
                        break;
                    case DecalMasterNode sub:
                        customInspector = "UnityEditor.Rendering.HighDefinition.DecalGUI";
                        break;
                    case EyeMasterNode sub:
                        customInspector = "UnityEditor.Rendering.HighDefinition.EyeGUI";
                        break;
                    case FabricMasterNode sub:
                        customInspector = "UnityEditor.Rendering.HighDefinition.FabricGUI";
                        break;
                    case HairMasterNode sub:
                        customInspector = "UnityEditor.Rendering.HighDefinition.HairGUI";
                        break;
                    case PBRMasterNode sub:
                        customInspector = "UnityEditor.Rendering.HighDefinition.HDPBRLitGUI";
                        break;
                    case StackLitMasterNode sub:
                        customInspector = "UnityEditor.Rendering.HighDefinition.StackLitGUI";
                        break;
                    case UnlitMasterNode sub:
                        customInspector = "UnityEditor.Rendering.HighDefinition.UnlitUI";
                        break;
                }
            } 
#endif
            // foreach (var target in graphData.allPotentialTargets)
            //     Debug.Log("Potential Target: " + target.displayName);
            
            // HDShaderUtils.IsHDRPShader
            // var shaderID = HDShaderUtils.GetShaderEnumFromShader(shader);
            
            // var hdMetadata = AssetDatabase.LoadAssetAtPath<HDMetadata>(assetPath);
            // Debug.Log("Shader ID: " + shaderID);
            
            // HDSubTarget.Setup

            return customInspector;
        }

        public static void RemoveShaderGraphUIBlock(ShaderGUI baseShaderGui)
        {
            MaterialUIBlockList blockList = null;
            
#if UNITY_2020_2_OR_NEWER
            if (baseShaderGui is LightingShaderGraphGUI hdGui)
            {
                blockList = (MaterialUIBlockList) typeof(LightingShaderGraphGUI).GetProperty("uiBlocks", (BindingFlags) (-1))?.GetValue(hdGui);
            }
#else
            if (baseShaderGui is HDLitGUI hdLitGUI)
            {
                blockList = (MaterialUIBlockList) typeof(HDLitGUI).GetField("uiBlocks", (BindingFlags) (-1))?.GetValue(hdLitGUI); 
            }
            else if (baseShaderGui is HDUnlitGUI hdUnlitGUI)
            {
                blockList = (MaterialUIBlockList) typeof(HDUnlitGUI).GetField("uiBlocks", (BindingFlags) (-1))?.GetValue(hdUnlitGUI); 
            }
#endif
            else if (baseShaderGui is DecalGUI decalGUI)
            {
                blockList = (MaterialUIBlockList) typeof(DecalGUI).GetField("uiBlocks", (BindingFlags) (-1))?.GetValue(decalGUI); 
            }
            else
            {
                blockList = (MaterialUIBlockList) baseShaderGui.GetType().GetField("uiBlocks", (BindingFlags) (-1))?.GetValue(baseShaderGui);
                if(blockList == null)
                    blockList = (MaterialUIBlockList) baseShaderGui.GetType().GetProperty("uiBlocks", (BindingFlags) (-1))?.GetValue(baseShaderGui);
            }

            if (blockList == null) return;
            var shaderGraphBlock = blockList.FetchUIBlock<ShaderGraphUIBlock>();
                
            // // check if there are special features active and so we need to keep the block
            // var m_Features = typeof(ShaderGraphUIBlock).GetField("m_Features", (BindingFlags) (-1));
            // var shaderGraphFeatures = (ShaderGraphUIBlock.Features) (m_Features?.GetValue(shaderGraphBlock) ?? 0);
            // if(shaderGraphFeatures == 0)
            blockList?.Remove(shaderGraphBlock);
            
            // Debug.Log(shaderGraphFeatures);
        }
    }
}

#endif