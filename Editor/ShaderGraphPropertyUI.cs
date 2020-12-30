using Needle;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using System.Reflection;

public static class ShaderGraphPropertyUI
{
    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorApplication.update += EditorUpdate;
    }

    private static int i = 0;
    private static StyleSheet styleSheet;
    
    private static void EditorUpdate()
    {
        i = (i + 1) % 100; // only run every 100 frames for now
        if (i != 0) return;

        #if UNITY_2020_2_OR_NEWER
        var shaderGraphAssembly = typeof(UnityEditor.ShaderGraph.PositionControl).Assembly; // does not exist earlier
        #else
        var shaderGraphAssembly = typeof(UnityEditor.ShaderGraph.KeywordDefinition).Assembly; // is internal in 2020.2+
        #endif
        var windowType = shaderGraphAssembly.GetType("UnityEditor.ShaderGraph.Drawing.MaterialGraphEditWindow");
        var windows = Resources.FindObjectsOfTypeAll(windowType);
        foreach (var wnd in windows)
        {
            var _graphEditorView = windowType.GetProperty("graphEditorView", (BindingFlags) (-1));
            var graphEditorView = _graphEditorView?.GetValue(wnd);
            if(graphEditorView == null) continue;

            var GraphEditorView = graphEditorView.GetType();

            var _blackboardProvider = GraphEditorView.GetProperty("blackboardProvider", (BindingFlags) (-1));
            var blackboardProvider = _blackboardProvider?.GetValue(graphEditorView);
            if(blackboardProvider == null) continue;

            var BlackboardProvider = blackboardProvider.GetType();

            var _blackboard = BlackboardProvider.GetProperty("blackboard", (BindingFlags) (-1));
            var blackboard = (Blackboard) _blackboard?.GetValue(blackboardProvider);
            if(blackboard == null) continue;
            
            if (!styleSheet)
                styleSheet = Resources.Load<StyleSheet>("Styles/ShaderGraphMarkdown");
            
            var blackboardElements = blackboard.Query<BlackboardField>().Visible().ToList();
            foreach(var fieldView in blackboardElements)
            {
                var displayName = fieldView.text;
                    
                var markdownType = MarkdownShaderGUI.GetMarkdownType(displayName);
                switch (markdownType)
                {
                    case MarkdownShaderGUI.MarkdownProperty.None:
                        break;
                    default:
                        if (!fieldView.styleSheets.Contains(styleSheet))
                            fieldView.styleSheets.Add(styleSheet);
                
                        if (!fieldView.ClassListContains("__markdown"))
                            fieldView.AddToClassList("__markdown");
                        var contentItem = fieldView.Q("contentItem");
                        contentItem.ClearClassList();
                        contentItem.AddToClassList("__markdown_" + markdownType);
                        
                        fieldView.typeText = markdownType.ToString();
                        break;
                }
            }
        }
    }
}
