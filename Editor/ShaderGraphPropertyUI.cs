using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Needle;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

public static class ShaderGraphPropertyUI
{
    [InitializeOnLoadMethod]
    static void Init()
    {
        // find all matching windows
        // type is MaterialGraphEditWindow
        
        // find the correct view VisualElement
        // type is MaterialGraphView
        
        // get the graph editor view
        // graphEditorView
        
        // attach to the changed event
        // m_GraphView.graphViewChanged
        
        // Debug.Log("start");
        EditorApplication.update += EditorUpdate;
    }

    private static int i = 0;
    private static StyleSheet styleSheet;
    
    private static void EditorUpdate()
    {
        i = (i + 1) % 100; // only run every 100 frames for now
        if (i != 0) return;

        var shaderGraphAssembly = typeof(UnityEditor.ShaderGraph.PositionControl).Assembly;
        var windowType = shaderGraphAssembly.GetType("UnityEditor.ShaderGraph.Drawing.MaterialGraphEditWindow");
        var windows = Resources.FindObjectsOfTypeAll(windowType);
        foreach (var wnd in windows)
        {
            var _graphEditorView = windowType.GetProperty("graphEditorView", (BindingFlags) (-1));
            var graphEditorView = _graphEditorView?.GetValue(wnd);
            if(graphEditorView == null) continue;

            var GraphEditorView = graphEditorView.GetType();
            // var _graphView = GraphEditorView.GetProperty("graphView", (BindingFlags) (-1));
            // var graphView = _graphView?.GetValue(graphEditorView);

            var _blackboardProvider = GraphEditorView.GetProperty("blackboardProvider", (BindingFlags) (-1));
            var blackboardProvider = _blackboardProvider?.GetValue(graphEditorView);
            if(blackboardProvider == null) continue;

            var BlackboardProvider = blackboardProvider.GetType();
            var _m_InputRows = BlackboardProvider.GetField("m_InputRows", (BindingFlags) (-1));
            var m_InputRows = (Dictionary<ShaderInput, BlackboardRow>) _m_InputRows?.GetValue(blackboardProvider);
            if(m_InputRows == null) continue;

            var _blackboard = BlackboardProvider.GetProperty("blackboard", (BindingFlags) (-1));
            var blackboard = (Blackboard) _blackboard?.GetValue(blackboardProvider);
            if(blackboard == null) continue;
            
            if (!styleSheet)
                styleSheet = Resources.Load<StyleSheet>("Styles/ShaderGraphMarkdown");
            // if (!blackboard.styleSheets.Contains(styleSheet))
            //     blackboard.styleSheets.Add(styleSheet); 
            
            // blackboard.RegisterCallback(ChangeEvent<>);
            
            foreach(var kvp in m_InputRows) {
                // Debug.Log(kvp.Key.referenceName + " (" + kvp.Key.referenceName + ") => " + kvp.Value.name);
                var row = kvp.Value;

                var markdownType = MarkdownShaderGUI.GetMarkdownType(kvp.Key.displayName);
                switch (markdownType)
                {
                    case MarkdownShaderGUI.MarkdownProperty.None:
                        break;
                    default:
                        var fieldView = row.Q<BlackboardField>();
                
                        if (!fieldView.styleSheets.Contains(styleSheet))
                            fieldView.styleSheets.Add(styleSheet);
                
                        if (!row.ClassListContains("__markdown"))
                            row.AddToClassList("__markdown");
                        var contentItem = row.Q("contentItem");
                        contentItem.ClearClassList();
                        contentItem.AddToClassList("__markdown_" + markdownType);
                        
                        fieldView.Q<Label>("typeLabel").text = markdownType.ToString();
                        break;
                }
            }
        }
    }
}
