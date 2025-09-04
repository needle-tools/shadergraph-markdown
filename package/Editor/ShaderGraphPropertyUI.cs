#if SHADERGRAPH_7_OR_NEWER

using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using System.Reflection;
using UnityEditor.ShaderGraph;
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

        private static void EditorUpdate()
        {
            if(!ShaderGraphMarkdownSettings.instance.showMarkdownInBlackboard) return;
            i = (i + 1) % 100; // only run every 100 frames for now
            if (i != 0) return;

            #if SHADERGRAPH_10_OR_NEWER
            var shaderGraphAssembly = typeof(UnityEditor.ShaderGraph.PositionControl).Assembly; // does not exist pre-2020.2
            #else
            var shaderGraphAssembly = typeof(UnityEditor.ShaderGraph.KeywordDefinition).Assembly; // went internal in 2020.2+
            #endif
            var windowType = shaderGraphAssembly.GetType("UnityEditor.ShaderGraph.Drawing.MaterialGraphEditWindow");
            var windows = Resources.FindObjectsOfTypeAll(windowType);
            foreach (var windowObject in windows)
            {
                var wnd = (EditorWindow) windowObject;
                
                if (_graphEditorView == null) _graphEditorView = windowType.GetProperty("graphEditorView", (BindingFlags) (-1));
                var graphEditorView = _graphEditorView?.GetValue(wnd);
                if (graphEditorView == null) continue;

                var GraphEditorView = graphEditorView.GetType();

                if (_blackboardProvider == null) _blackboardProvider = GraphEditorView.GetProperty("blackboardProvider", (BindingFlags) (-1));
                var blackboardProvider = _blackboardProvider?.GetValue(graphEditorView);
                if (blackboardProvider == null)
                {
                    _blackboardProvider = GraphEditorView.GetProperty("blackboardController", (BindingFlags) (-1));
                    blackboardProvider = _blackboardProvider?.GetValue(graphEditorView);
                    if(blackboardProvider == null)
                        continue;
                }

                var BlackboardProvider = blackboardProvider.GetType();

                if (_blackboard == null) _blackboard = BlackboardProvider.GetProperty("blackboard", (BindingFlags) (-1));
                var blackboard = (VisualElement) _blackboard?.GetValue(blackboardProvider);
                if (blackboard == null) continue;
                
#if UNITY_2020_2_OR_NEWER
                // get inputRows as well so we can access actual shader property data, not just UI
                var inputRows = MarkdownSGExtensions.GetInputRowDictionaryFromController(blackboardProvider);
                if(inputRows == null)
                    continue;
                
                var shaderInputs = inputRows
                    .Where(x => x.Value != null)
                    .ToDictionary(x => x.Value, x => x.Key);
#endif
                if (!styleSheet)
                    styleSheet = Resources.Load<StyleSheet>("Styles/ShaderGraphMarkdown");

                var blackboardElements = blackboard.Query<BlackboardField>().Visible().ToList().Cast<VisualElement>().ToList();
                if (!blackboardElements.Any())
                    blackboardElements = MarkdownSGExtensions.GetBlackboardElements(blackboard);
                    
#if !UNITY_2020_2_OR_NEWER
                BlackboardRow GetRowForElement(VisualElement fieldView)
                {
                    if (fieldView == null) return null;
                    var p = fieldView.parent;
                    while (p != null && !(p is BlackboardRow))
                        p = p.parent;
                    if (p is BlackboardRow row)
                        return row;
                    return null;
                }
                
                void ToggleExpand(EventBase eventBase)
                {
                    var target = eventBase.target as Button;
                    var currentRow = GetRowForElement(target);
                    if (target == null || currentRow == null || blackboardElements == null) return;
                    
                    // expanded state has already been updated at this point since our event is always appended later as the default event
                    var currentState = currentRow.expanded;

                    // only react to Alt click
                    if (!(eventBase is MouseUpEvent downEvent) || !downEvent.altKey) return;
                    
                    foreach (var fieldView in blackboardElements)
                    {
                        var row = GetRowForElement(fieldView);
                        if(row != null)
                            row.expanded = currentState;
                    }
                }
#endif
                
                bool nextFieldShouldBeIndented = false;
                foreach(var fieldView in blackboardElements)
                {
#if UNITY_2020_2_OR_NEWER
                    bool usesDefaultReferenceName = false;
                    bool usesRecommendedReferenceName = true;
                    bool usesInlineTextureDrawerShorthand = false;
                    bool isTextureProperty = false;
                    
                    // get shaderInput for this field
                    var element = (VisualElement) fieldView;
                    while (!MarkdownSGExtensions.ElementIsBlackboardRow(element) && element.parent != null)
                        element = element.parent;
                    
                    if(MarkdownSGExtensions.ElementIsBlackboardRow(element)) // element is BlackboardRow blackboardRow)
                    {
                        var blackboardRow = element;
                        
                        if (shaderInputs.ContainsKey(blackboardRow))
                        {
                            var shaderInput = shaderInputs[blackboardRow];
                            
#if UNITY_2021_1_OR_NEWER
                            if (shaderInput.IsUsingNewDefaultRefName() || shaderInput.IsUsingOldDefaultRefName())
#else
                            if (shaderInput.referenceName.Equals(shaderInput.GetDefaultReferenceName(), StringComparison.Ordinal))
#endif
                                usesDefaultReferenceName = true;

                            if (!usesDefaultReferenceName && ReferenceNameLooksLikeADefaultReference(shaderInput.referenceName))
                                usesDefaultReferenceName = true;
                            
                            // some properties already have "good" default reference names, we shouldn't warn in that case.
                            if (usesDefaultReferenceName && shaderInput.referenceName.StartsWith("_", StringComparison.Ordinal))
                                usesDefaultReferenceName = false;

                            if (!shaderInput.referenceName.StartsWith("_", StringComparison.Ordinal))
                                usesRecommendedReferenceName = false;

                            if ((shaderInput is AbstractShaderProperty abstractShaderProperty && 
                                 (abstractShaderProperty.propertyType == PropertyType.Texture2D || 
                                  abstractShaderProperty.propertyType == PropertyType.Texture3D || 
                                  abstractShaderProperty.propertyType == PropertyType.Texture2DArray || 
                                  abstractShaderProperty.propertyType == PropertyType.VirtualTexture)))
                                isTextureProperty = true;

                            var display = shaderInput.displayName;
                            var hasCondition = !display.StartsWith("[", StringComparison.Ordinal) && display.Contains('[') && display.EndsWith("]", StringComparison.Ordinal);
                            if (hasCondition)
                            {
                                display = display.Substring(0, display.IndexOf('[') - 1).TrimEnd();
                            }
                            if (display.EndsWith("&&", StringComparison.Ordinal))
                            {
                                usesInlineTextureDrawerShorthand = true;
                            }
                            
                            // make sure our context menu handler is registered
                            element.UnregisterCallback<ContextualMenuPopulateEvent>(MenuPopulateEvent);
                            element.RegisterCallback<ContextualMenuPopulateEvent>(MenuPopulateEvent);
                            
                            // refactoring
                            if (markedForRefactor != null && markedForRefactor == blackboardRow)
                            {
                                markedForRefactor = null;
                                MarkdownShaderGUI.ShowRefactoringWindow(MarkdownSGExtensions.GetShaderPathForWindow(wnd), shaderInput.referenceName);
                            }
                        }
                    }
#endif
                    
                    var displayName = MarkdownSGExtensions.GetBlackboardFieldText(fieldView);
                    var contentItem = fieldView.Q("contentItem");

                    var indentLevel = MarkdownShaderGUI.GetIndentLevel(displayName);
                    if (nextFieldShouldBeIndented) {
                        indentLevel += 2;
                        nextFieldShouldBeIndented = false;
                    }
                    displayName = displayName.TrimStart('-');
                    var markdownType = MarkdownShaderGUI.GetMarkdownType(displayName);
                    contentItem.ClearClassList();
                    if (!fieldView.styleSheets.Contains(styleSheet) && styleSheet)
                    {
                        fieldView.styleSheets.Add(styleSheet);
                        
#if !UNITY_2020_2_OR_NEWER
                        // attach to the Blackboard expandButton for each item and add the typical Alt+Click handler
                        // to expand/collapse all items.
                        var row = GetRowForElement(fieldView);
                        if (row != null)
                        {
                            var expand = row.Q<Button>("expandButton");
                            expand.clickable.activators.Remove(ExpandManipulatorFilter);
                            expand.clickable.activators.Add(ExpandManipulatorFilter);
                            expand.clickable.clickedWithEventInfo -= ToggleExpand;
                            expand.clickable.clickedWithEventInfo += ToggleExpand;
                        }
#endif
                    }

                    switch (markdownType)
                    {
                        case MarkdownShaderGUI.MarkdownProperty.None:
#if UNITY_2020_2_OR_NEWER
                            if (!MarkdownSGExtensions.IsSubGraph(wnd))
                            {
                                if (usesDefaultReferenceName && ShaderGraphMarkdownSettings.instance.showDefaultReferenceNameWarning)
                                    contentItem.AddToClassList("__markdown_DefaultReferenceWarning");
                                else if (!usesRecommendedReferenceName && ShaderGraphMarkdownSettings.instance.showNamingRecommendationHint)
                                    contentItem.AddToClassList("__markdown_NonRecommendedReferenceHint");
                            }
                            
                            if (isTextureProperty && usesInlineTextureDrawerShorthand)
                                nextFieldShouldBeIndented = true;
#endif
                            break;
                        default:
                            contentItem.AddToClassList("__markdown_" + markdownType);
                    
                            if (!fieldView.ClassListContains("__markdown"))
                                fieldView.AddToClassList("__markdown");

                            MarkdownSGExtensions.SetBlackboardFieldTypeText(fieldView, markdownType.ToString());
                            break;
                    }
                    
                    if(indentLevel > 0)
                        contentItem.AddToClassList("__markdown_indent_" + indentLevel);
                }
            }
        }

        private static VisualElement markedForRefactor;
        private static void MenuPopulateEvent(ContextualMenuPopulateEvent evt)
        {
            var currentTarget = evt.currentTarget;
            evt.menu.AppendAction("Refactor Property", dropdownAction =>
            {
                markedForRefactor = (VisualElement) currentTarget;
                i = -1; // enforce refresh on next frame
            });
        }

        private static bool ReferenceNameLooksLikeADefaultReference(string referenceName)
        {
            // alternative would be a RegEx check
            // ^.+?_(?<maybe_guid>\w{7,8}|\w{32})(_\w+?)?$
            // https://regex101.com/r/fT2iWo/1
            var parts = referenceName.Split('_');
            var l = parts.Length;
            if (parts.Length < 2)
                return false;
            var first = parts[l - 1];
            if ((first.Length >= 6 && first.Length <= 8 && int.TryParse(first, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)) || Guid.TryParse(first, out _))
                return true;
            if (parts.Length < 3)
                return false;
            var second = parts[l - 2];
            if ((second.Length >= 6 && second.Length <= 8 && int.TryParse(second, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)) || Guid.TryParse(second, out _))
                return true;
            return false;
        }
        
#if !UNITY_2020_2_OR_NEWER
        private static readonly ManipulatorActivationFilter ExpandManipulatorFilter = new ManipulatorActivationFilter()
        {
            button = MouseButton.LeftMouse,
            modifiers = EventModifiers.Alt
        };
#endif
    }
}

#endif