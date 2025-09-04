#if !NO_INTERNALS_ACCESS && UNITY_2019_4_OR_NEWER

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.Rendering.Universal.ShaderGraph;
#endif

namespace UnityEditor.Rendering.Universal
{
    public static class MarkdownURPExtensions
    {
        [InitializeOnLoadMethod]
        static void RegisterMarkdownHelpers()
        {
            MarkdownSGExtensions.RegisterCustomInspectorGetter(GetDefaultCustomInspectorFromGraphData);
            #if UNITY_2020_2_OR_NEWER
            MarkdownSGExtensions.RegisterCustomInspectorSetter(SetDefaultCustomInspector);
            #endif
        }

#if UNITY_2021_2_OR_NEWER
        internal struct MaterialHeaderScopeItem
        {
            public GUIContent headerTitle { get; set; }
            public uint expandable { get; set; }
            public Action<Material> drawMaterialScope { get; set; }
        }
        
        internal class MarkdownShaderGraphLitGUI : ShaderGraphLitGUI
        {
            // needs to stay in sync with BaseShaderGUI:240.OnOpenGUI
            public override void OnOpenGUI(Material material, MaterialEditor materialEditor)
            {
                var m_MaterialScopeList = (MaterialHeaderScopeList) typeof(BaseShaderGUI).GetField("m_MaterialScopeList", (BindingFlags)(-1))?.GetValue(this);
                m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceOptions, (uint)Expandable.SurfaceOptions, DrawSurfaceOptions);
                // m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceInputs, (uint)Expandable.SurfaceInputs, DrawSurfaceInputs);
                FillAdditionalFoldouts(m_MaterialScopeList);
                m_MaterialScopeList.RegisterHeaderScope(Styles.AdvancedLabel, (uint)Expandable.Advanced, DrawAdvancedOptions);
            }
        }

        internal class MarkdownShaderGraphUnlitGUI : ShaderGraphUnlitGUI
        {
            // needs to stay in sync with BaseShaderGUI:240.OnOpenGUI
            public override void OnOpenGUI(Material material, MaterialEditor materialEditor)
            {
                var m_MaterialScopeList = (MaterialHeaderScopeList) typeof(BaseShaderGUI).GetField("m_MaterialScopeList", (BindingFlags)(-1))?.GetValue(this);
                m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceOptions, (uint)Expandable.SurfaceOptions, DrawSurfaceOptions);
                // m_MaterialScopeList.RegisterHeaderScope(Styles.SurfaceInputs, (uint)Expandable.SurfaceInputs, DrawSurfaceInputs);
                FillAdditionalFoldouts(m_MaterialScopeList);
                m_MaterialScopeList.RegisterHeaderScope(Styles.AdvancedLabel, (uint)Expandable.Advanced, DrawAdvancedOptions);
            }
        }
#endif
        
        private static string GetDefaultCustomInspectorFromGraphData(GraphData arg)
        {
#if UNITY_2021_2_OR_NEWER
            foreach (var target in arg.activeTargets)
            {
                if (target is UniversalTarget universalTarget)
                {
                    switch (universalTarget.activeSubTarget)
                    {
                        case UniversalLitSubTarget litSubTarget:
                            return typeof(MarkdownShaderGraphLitGUI).FullName;
                        case UniversalUnlitSubTarget unlitSubTarget:
                            return typeof(MarkdownShaderGraphUnlitGUI).FullName;
                    }
                }
            }
#endif

            return null;
        }

#if UNITY_2020_2_OR_NEWER
        static bool SetDefaultCustomInspector(Target target, string customInspector)
        {
            if (target is UniversalTarget universalTarget)
            {
                if (universalTarget.customEditorGUI.Equals(customInspector, StringComparison.Ordinal))
                    universalTarget.customEditorGUI = null;
                else
                    universalTarget.customEditorGUI = customInspector;
                return true;
            }

            return false;
        }
#endif
    }
}
#endif