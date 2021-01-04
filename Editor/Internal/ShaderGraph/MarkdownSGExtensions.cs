#if !NO_INTERNALS_ACCESS && UNITY_2019_4_OR_NEWER

// using System;
// using System.Reflection;
// using UnityEditor.ShaderGraph;
// using UnityEngine;
// using UnityEditor.ShaderGraph.Drawing;
// using UnityEditor.ShaderGraph.Drawing.Controls;
// using UnityEditor.ShaderGraph.Internal;

// public static class MarkdownSGExtensions
// {
//     private static PropertyInfo _graphEditorView;
//     
//     static void Stuff()
//     {
//         var shaderGraphWindows = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
//         foreach (var window in shaderGraphWindows)
//         {
//             if (_graphEditorView == null) _graphEditorView = typeof(MaterialGraphEditWindow).GetProperty("graphEditorView", (BindingFlags) (-1));
//             var graphEditorView = (GraphEditorView) _graphEditorView?.GetValue(window);
//             if (graphEditorView == null) continue;
//
//             var blackboardProvider = graphEditorView.blackboardProvider;
//             var blackboard = blackboardProvider.blackboard;
//         }
//     }
// }

// namespace Needle.ShaderGraphMarkdown
// {
//     [Serializable]
//     [BlackboardInputInfo(30000, name = "Markdown/Foldout Header")]
//     public class MarkdownFoldout : MarkdownShaderProperty
//     {
//         internal override string DisplayName => "# My Foldout";
//     }
//
//     [Serializable]
//     [BlackboardInputInfo(30001, name = "Markdown/Header")]
//     public class MarkdownHeader : MarkdownShaderProperty
//     {
//         internal override string DisplayName => "## My Header";
//     }
//     
//     [Serializable]
//     [BlackboardInputInfo(30002, name = "Markdown/Note")]
//     public class MarkdownNote : MarkdownShaderProperty
//     {
//         internal override string DisplayName => "!NOTE My Note";
//     }
//     
//     public abstract class MarkdownShaderProperty : AbstractShaderProperty<bool>
//     {
//          internal abstract string DisplayName { get; }
//          
//          internal MarkdownShaderProperty()
//          {
//              displayName = DisplayName;
//          }
//
//          public override PropertyType propertyType => PropertyType.Boolean;
//
//          internal override bool isExposable => true;
//          internal override bool isRenamable => true;
//
//          internal override string GetPropertyAsArgumentString()
//          {
//              return $"{concreteShaderValueType.ToShaderString(concretePrecision.ToShaderString())} {referenceName}";
//          }
//
//          internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
//          {
//              HLSLDeclaration decl = GetDefaultHLSLDeclaration();
//              action(new HLSLProperty(HLSLType._float, referenceName, decl, concretePrecision));
//          }
//
//          internal override string GetPropertyBlockString()
//          {
//              return $"{hideTagString}[ToggleUI]{referenceName}(\"{displayName}\", Float) = {(value == true ? 1 : 0)}";
//          }
//
//          internal override AbstractMaterialNode ToConcreteNode()
//          {
//              return new BooleanNode() { value = new ToggleData(value) };
//          }
//
//          internal override PreviewProperty GetPreviewMaterialProperty()
//          {
//              return new PreviewProperty(propertyType)
//              {
//                  name = referenceName,
//                  booleanValue = value
//              };
//          }
//
//          internal override ShaderInput Copy()
//          {
//              return new BooleanShaderProperty()
//              {
//                  displayName = displayName,
//                  hidden = hidden,
//                  value = value,
//                  precision = precision,
//              };
//          }
//     }   
// }
//
#endif