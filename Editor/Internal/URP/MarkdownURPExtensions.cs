#if !NO_INTERNALS_ACCESS && UNITY_2019_4_OR_NEWER

using System;
using UnityEditor;
using UnityEditor.ShaderGraph;
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
                            return typeof(ShaderGraphLitGUI).FullName;
                        case UniversalUnlitSubTarget unlitSubTarget:
                            return typeof(ShaderGraphUnlitGUI).FullName;
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