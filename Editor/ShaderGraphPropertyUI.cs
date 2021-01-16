using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using System.Reflection;
using UnityEditor.ShaderGraph.Internal;

namespace Needle.ShaderGraphMarkdown
{
    internal static class ShaderGraphPropertyUI
    {
        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.update += EditorUpdate;
        }

        private static int i = 0;
        private static StyleSheet styleSheet;
        private static PropertyInfo _graphEditorView, _blackboardProvider, _blackboard;
        private static FieldInfo m_InputRows;
        
        private static void EditorUpdate()
        {
            i = (i + 1) % 100; // only run every 100 frames for now
            if (i != 0) return;

            #if SHADERGRAPH_10_OR_NEWER
            var shaderGraphAssembly = typeof(UnityEditor.ShaderGraph.PositionControl).Assembly; // does not exist pre-2020.2
            #else
            var shaderGraphAssembly = typeof(UnityEditor.ShaderGraph.KeywordDefinition).Assembly; // went internal in 2020.2+
            #endif
            var windowType = shaderGraphAssembly.GetType("UnityEditor.ShaderGraph.Drawing.MaterialGraphEditWindow");
            var windows = Resources.FindObjectsOfTypeAll(windowType);
            foreach (var wnd in windows)
            {
                if (_graphEditorView == null) _graphEditorView = windowType.GetProperty("graphEditorView", (BindingFlags) (-1));
                var graphEditorView = _graphEditorView?.GetValue(wnd);
                if (graphEditorView == null) continue;

                var GraphEditorView = graphEditorView.GetType();

                if (_blackboardProvider == null) _blackboardProvider = GraphEditorView.GetProperty("blackboardProvider", (BindingFlags) (-1));
                var blackboardProvider = _blackboardProvider?.GetValue(graphEditorView);
                if (blackboardProvider == null) continue;

                var BlackboardProvider = blackboardProvider.GetType();

                if (_blackboard == null) _blackboard = BlackboardProvider.GetProperty("blackboard", (BindingFlags) (-1));
                var blackboard = (VisualElement) _blackboard?.GetValue(blackboardProvider);
                if (blackboard == null) continue;
                
#if UNITY_2020_2_OR_NEWER
                // get inputRows as well so we can access actual shader property data, not just UI
                if(m_InputRows == null) m_InputRows = BlackboardProvider.GetField("m_InputRows", (BindingFlags) (-1));
                var inputRows = (Dictionary<ShaderInput, BlackboardRow>) m_InputRows.GetValue(blackboardProvider);
                var shaderInputs = inputRows.ToDictionary(x => x.Value, x => x.Key);
#endif
                if (!styleSheet)
                    styleSheet = Resources.Load<StyleSheet>("Styles/ShaderGraphMarkdown");
                
                var blackboardElements = blackboard.Query<BlackboardField>().Visible().ToList();
                foreach(var fieldView in blackboardElements)
                {
#if UNITY_2020_2_OR_NEWER
                    bool usesDefaultReferenceName = false;
                    bool usesRecommendedReferenceName = true;
                    
                    // get shaderInput for this field
                    var element = (VisualElement) fieldView;
                    while (!(element is BlackboardRow) && element.parent != null)
                        element = element.parent;
                    
                    if(element is BlackboardRow blackboardRow)
                    {
                        if (shaderInputs.ContainsKey(blackboardRow))
                        {
                            var shaderInput = shaderInputs[blackboardRow];
                            if (shaderInput.referenceName.Equals(shaderInput.GetDefaultReferenceName(), StringComparison.Ordinal))
                                usesDefaultReferenceName = true;

                            if (!shaderInput.referenceName.StartsWith("_"))
                                usesRecommendedReferenceName = false;
                        }
                    }
#endif
                    
                    var displayName = fieldView.text;
                    var contentItem = fieldView.Q("contentItem");
                    var markdownType = MarkdownShaderGUI.GetMarkdownType(displayName);
                    switch (markdownType)
                    {
                        case MarkdownShaderGUI.MarkdownProperty.None:
                            contentItem.ClearClassList();
#if UNITY_2020_2_OR_NEWER
                            if (usesDefaultReferenceName && ShaderGraphMarkdownSettings.instance.showDefaultReferenceNameWarning)
                            {
                                if (!fieldView.styleSheets.Contains(styleSheet))
                                    fieldView.styleSheets.Add(styleSheet);
                                
                                contentItem.AddToClassList("__markdown_DefaultReferenceWarning");
                            }
                            else if (!usesRecommendedReferenceName && ShaderGraphMarkdownSettings.instance.showNamingRecommendationHint)
                            {
                                if (!fieldView.styleSheets.Contains(styleSheet))
                                    fieldView.styleSheets.Add(styleSheet);
                                
                                contentItem.AddToClassList("__markdown_NonRecommendedReferenceHint");
                            }
#endif
                            break;
                        default:
                            if (!fieldView.styleSheets.Contains(styleSheet))
                                fieldView.styleSheets.Add(styleSheet);
                    
                            if (!fieldView.ClassListContains("__markdown"))
                                fieldView.AddToClassList("__markdown");
                            
                            contentItem.ClearClassList();
                            contentItem.AddToClassList("__markdown_" + markdownType);
                            
                            fieldView.typeText = markdownType.ToString();
                            break;
                    }
                }
            }
        }
    }
}