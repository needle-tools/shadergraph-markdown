#if !NO_INTERNALS_ACCESS && UNITY_2019_4_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.Rendering.HighDefinition.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
#endif
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    public static class MarkdownHDExtensions
    {
        private static readonly Dictionary<Type, Type> TypeToHDPropertyTypeMap = new Dictionary<Type, Type>()
        {
#if UNITY_2020_2_OR_NEWER
            { typeof(LightmapData),   typeof(LightmappingShaderProperties.LightmapTextureArrayProperty) },
            { typeof(DiffusionProfileSettings), typeof(DiffusionProfileShaderProperty) },
#endif
        };
        
        [InitializeOnLoadMethod]
        static void RegisterMarkdownHelpers()
        {
            MarkdownSGExtensions.RegisterTypeMap(TypeToHDPropertyTypeMap);
            MarkdownSGExtensions.RegisterCustomInspectorGetter(GetDefaultCustomInspectorFromGraphData);
#if UNITY_2020_2_OR_NEWER
            MarkdownSGExtensions.RegisterCustomInspectorSetter(SetDefaultCustomInspector);
#endif
        }

#if UNITY_2020_2_OR_NEWER
        static bool SetDefaultCustomInspector(Target target, string customInspector)
        {
            if (target is HDTarget hdTarget)
            {
                if (hdTarget.customEditorGUI.Equals(customInspector, StringComparison.Ordinal))
                    hdTarget.customEditorGUI = null; // GetDefaultCustomInspectorFromShader(mat.shader);
                else
                    hdTarget.customEditorGUI = customInspector;
                return true;
            }

            return false;
        }
#endif
        
        static string GetDefaultCustomInspectorFromGraphData(GraphData graphData)
        {
            string customInspector = null;
            
            #if UNITY_2020_2_OR_NEWER
            foreach(var target in graphData.activeTargets)
            {
                // Debug.Log("Active Target: " + target.displayName);
                if (target is HDTarget hdTarget)
                {
                    var activeSubTarget = ((JsonData<SubTarget>) typeof(HDTarget).GetField("m_ActiveSubTarget", (BindingFlags)(-1)).GetValue(hdTarget)).value;
                    if (activeSubTarget is HDSubTarget hdSubTarget)
                    {
                        customInspector = (string) typeof(HDSubTarget).GetProperty("customInspector", (BindingFlags) (-1))?.GetValue(hdSubTarget);
                        break;
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
#if !UNITY_2021_1_OR_NEWER
            else if (baseShaderGui is DecalGUI decalGUI)
            {
                blockList = (MaterialUIBlockList) typeof(DecalGUI).GetField("uiBlocks", (BindingFlags) (-1))?.GetValue(decalGUI); 
            }
#endif
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