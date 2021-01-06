#if !NO_INTERNALS_ACCESS && UNITY_2019_4_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.Rendering.Universal.ShaderGraph;
#endif
using UnityEditor.ShaderGraph;
using UnityEngine;

public class MarkdownURPExtensions
{
    [InitializeOnLoadMethod]
    static void RegisterMarkdownHelpers()
    {
        // MarkdownSGExtensions.RegisterTypeMap(TypeToHDPropertyTypeMap);
        // MarkdownSGExtensions.RegisterCustomInspectorGetter(GetDefaultCustomInspectorFromGraphData);
        #if UNITY_2020_2_OR_NEWER
        MarkdownSGExtensions.RegisterCustomInspectorSetter(SetDefaultCustomInspector);
        #endif
    }
    
#if UNITY_2020_2_OR_NEWER
    static bool SetDefaultCustomInspector(Target target, string customInspector)
    {
        if (target is UniversalTarget universalTarget)
        {
            if (universalTarget.customEditorGUI.Equals(customInspector, StringComparison.Ordinal))
                universalTarget.customEditorGUI = null; // GetDefaultCustomInspectorFromShader(mat.shader);
            else
                universalTarget.customEditorGUI = customInspector;
            return true;
        }

        return false;
    }
#endif
}

#endif