using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using Needle.ShaderGraphMarkdown;
using UnityEditor.ShaderGraph;
#if HDRP_7_OR_NEWER
using UnityEditor.Rendering.HighDefinition;
#endif

// we're making an exception here: only Needle namespace, because full namespace
// has to be included in the ShaderGraph custom ui field.
namespace Needle
{
    public class MarkdownShaderGUI : ShaderGUI
    {
        private class HeaderGroup
        {
            public string name;
            public List<MaterialProperty> properties;
            public Action customDrawer = null;
        }

        private static GUIStyle centeredGreyMiniLabel;
        private static GUIStyle CenteredGreyMiniLabel {
            get {
                if (centeredGreyMiniLabel == null)
                {
                    centeredGreyMiniLabel = new GUIStyle(GUI.skin.FindStyle("MiniLabel") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("MiniLabel")) {
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = Color.gray }
                    };
                }
                return centeredGreyMiniLabel;
            }
        }
        
        private readonly Dictionary<string, MarkdownMaterialPropertyDrawer> drawerCache = new Dictionary<string, MarkdownMaterialPropertyDrawer>();
        private readonly List<MaterialProperty> referencedProperties = new List<MaterialProperty>();

        internal enum MarkdownProperty
        {
            None,
            Reference,
            Link,
            Note,
            Drawer,
            Header,
            Foldout
        }

        private static string refFormat = "!REF";
        private static string noteFormat = "!NOTE";
        private static string alternateNoteFormat = "* ";
        private static string drawerFormat = "!DRAWER";
        private static string foldoutHeaderFormat = "#";
        private static string foldoutHeaderFormatStart = foldoutHeaderFormat + " ";
        private static string headerFormatStart = "##" + " ";
        
        internal static MarkdownProperty GetMarkdownType(string display)
        {
            // markdown blockquote: >
            // markdown footnote: [^MyNote] Hello I'm a footnote

            if (display.StartsWith(refFormat))
                return MarkdownProperty.Reference;
            if (display.StartsWith("[") && display.IndexOf("]", StringComparison.Ordinal) > -1 && display.IndexOf("(", StringComparison.Ordinal) > -1 && display.EndsWith(")"))
                return MarkdownProperty.Link;
            if (display.StartsWith(noteFormat) || display.StartsWith(alternateNoteFormat))
                return MarkdownProperty.Note;
            if (display.StartsWith(drawerFormat))
                return MarkdownProperty.Drawer;
            if (display.StartsWith(foldoutHeaderFormatStart) || display.Equals(foldoutHeaderFormat, StringComparison.Ordinal))
                return MarkdownProperty.Foldout;
            if (display.StartsWith(headerFormatStart))
                return MarkdownProperty.Header;
            return MarkdownProperty.None;
        }

        private bool showOriginalPropertyList = false, debugConditionalProperties = false;

        private new static MaterialProperty FindProperty(string keywordRef, MaterialProperty[] properties)
        {
            var keywordProp = ShaderGUI.FindProperty(keywordRef, properties, false);
            
            // special case: bool properties have to be named MY_PROP_ON in ShaderGraph to be exposed,
            // but the actual property is named "MY_PROP" and the keyword is still "MY_PROP_ON".
            if (keywordProp == null && keywordRef.EndsWith("_ON", StringComparison.Ordinal))
                keywordProp = ShaderGUI.FindProperty(keywordRef.Substring(0, keywordRef.Length - "_ON".Length), properties, false);
            
            return keywordProp;
        }
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var targetMat = materialEditor.target as Material;
            if (!targetMat) return;
            
            // referencedProperties.Clear();
            
            // split by header properties
            var headerGroups = new List<HeaderGroup>();
            headerGroups.Add(new HeaderGroup() { name = "Default" });
            
            foreach (var prop in properties)
            {
                if(prop.displayName.StartsWith(foldoutHeaderFormatStart) || prop.displayName.Equals(foldoutHeaderFormat, StringComparison.Ordinal))
                {
                    if (prop.displayName.Equals(foldoutHeaderFormat, StringComparison.Ordinal)  || 
                        (prop.displayName.StartsWith(foldoutHeaderFormatStart + "(", StringComparison.Ordinal) && prop.displayName.EndsWith(")", StringComparison.Ordinal))) // for multiple ## (1) foldout breakers)
                        headerGroups.Add(new HeaderGroup() { name = null });
                    else
                        headerGroups.Add(new HeaderGroup() { name = prop.displayName.Substring(prop.displayName.IndexOf(' ') + 1) });
                }
                else
                {
                    var last = headerGroups.Last();
                    if (last.properties == null) 
                        last.properties = new List<MaterialProperty>();
                    
                    // need to process REF properties early so we can hide them properly if needed
                    var display = prop.displayName;
                    if (display.StartsWith(refFormat))
                    {
                        var split = display.Split(' ');
                        if(split.Length > 1) {
                            var keywordRef = split[1];
                            var keywordProp = FindProperty(keywordRef, properties);
                            if(keywordProp != null)
                                referencedProperties.Add(keywordProp);
                        }
                    }

                    last.properties.Add(prop);
                }
            }
            headerGroups.Add(new HeaderGroup() { name = null, properties = null, customDrawer = DrawCustomGUI });
            headerGroups.Add(new HeaderGroup() { name = "Debug", properties = null, customDrawer = DrawDebugGroupContent });

            string GetBetween(string str, char start, char end, bool last = false)
            {
                var i0 = last ? str.LastIndexOf(start) : str.IndexOf(start);
                var i1 = last ? str.LastIndexOf(end)   : str.IndexOf(end);
                if (i0 < 0 || i1 < 0) return null;
                return str.Substring(i0 + 1, i1 - i0 - 1);
            }
            
            void DrawDebugGroupContent()
            {
                EditorGUI.BeginChangeCheck();
                showOriginalPropertyList = EditorGUILayout.Toggle("Show Original Properties", showOriginalPropertyList);
                if (EditorGUI.EndChangeCheck())
                    InitializeCustomGUI(targetMat);    
                debugConditionalProperties = EditorGUILayout.Toggle("Debug Conditional Properties", debugConditionalProperties);
                
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("Shader Keywords", EditorStyles.boldLabel);
                foreach (var kw in targetMat.shaderKeywords)
                {
                    EditorGUILayout.LabelField(kw, EditorStyles.miniLabel);
                }

                if (GUILayout.Button("Clear Keywords"))
                {
                    foreach (var kw in targetMat.shaderKeywords)
                        targetMat.DisableKeyword(kw);
                }

                #if HDRP_7_OR_NEWER
                if (GUILayout.Button("Reset Keywords"))
                {
                    HDShaderUtils.ResetMaterialKeywords(targetMat);
                }
                EditorGUILayout.Space();
                #endif
                
                // #if HDRP_7_OR_NEWER
                // EditorGUILayout.LabelField("ShaderGraph Info", EditorStyles.boldLabel);
                // if (GUILayout.Button("Refresh"))
                // {
                //     Debug.Log(MarkdownHDExtensions.GetDefaultCustomInspectorFromShader(targetMat.shader));
                // }
                // #endif
            }
            
            void DrawCustomGUI()
            {
                if (!haveSearchedForCustomGUI)
                    InitializeCustomGUI(targetMat);
            
                if(baseShaderGui != null) {
                    EditorGUILayout.Space();
                    CoreEditorUtils.DrawSplitter();
                    EditorGUILayout.LabelField("Additional Options", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                
                    // only pass in the properties that are hidden - this allows us to render all custom HDRP ShaderGraph UIs.
                    baseShaderGui?.OnGUI(materialEditor, showOriginalPropertyList ? properties : properties
                        .Where(x => x.flags.HasFlag(MaterialProperty.PropFlags.HideInInspector))
                        .ToArray());
                    CoreEditorUtils.DrawSplitter();
                }
            }

            void DrawGroup(HeaderGroup group)
            {
                bool previousPropertyWasDrawn = true;
                
                foreach (var prop in group.properties)
                {
                    var display = prop.displayName;
                    var isDisabled = false;
                    var hasCondition = !display.StartsWith("[", StringComparison.Ordinal) && display.Contains('[') && display.EndsWith("]", StringComparison.Ordinal);
                    if(hasCondition) {
                        var condition = GetBetween(display, '[', ']', true);
                        if (!string.IsNullOrEmpty(condition))
                        {
                            var keywordIsSet = Array.IndexOf(targetMat.shaderKeywords, condition) >= 0;
                            var boolIsSet = false;
                            
                            // support for using bool/float values as conditionals
                            if(!keywordIsSet && targetMat.shader.FindPropertyIndex(condition) > -1) {
                                var propertyIndex = targetMat.shader.FindPropertyIndex(condition);
                                var propertyType = targetMat.shader.GetPropertyType(propertyIndex);
                                if(propertyType == ShaderPropertyType.Float && targetMat.GetFloat(condition) > 0.5f)
                                    boolIsSet = true;
                            }
                            
                            var conditionIsFulfilled = keywordIsSet || boolIsSet;
                            if (!conditionIsFulfilled)
                            {
                                if(!debugConditionalProperties) {
                                    previousPropertyWasDrawn = false;
                                    continue;
                                }
                                else {
                                    isDisabled = true;
                                    EditorGUI.BeginDisabledGroup(true);
                                }
                            }
                            if(!debugConditionalProperties)
                                display = display.Substring(0, display.IndexOf('[') - 1);
                        }
                    }

                    switch (GetMarkdownType(display))
                    {
                        case MarkdownProperty.Link:
                            if (!previousPropertyWasDrawn) continue;
                            var linkText = GetBetween(display, '[', ']');
                            var linkHref = GetBetween(display, '(', ')');
                            if (GUILayout.Button(linkText, CenteredGreyMiniLabel)) Application.OpenURL(linkHref);
                            break;
                        case MarkdownProperty.Note:
                            if (!previousPropertyWasDrawn) continue;
                            var index = display.IndexOf(' ');
                            var noteText = display.Substring(index + 1);
                            EditorGUILayout.LabelField(noteText, CenteredGreyMiniLabel);
                            break;
                        case MarkdownProperty.Drawer:
                            var parts = display.Split(' ');
                            if(parts.Length > 1) {
                                var objectName = parts[1];
                                if(!drawerCache.ContainsKey(objectName)) {
                                    var objectPath = AssetDatabase.FindAssets($"t:{nameof(MarkdownMaterialPropertyDrawer)} {objectName}").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
                                    var scriptableObject = AssetDatabase.LoadAssetAtPath<MarkdownMaterialPropertyDrawer>(objectPath);
                                    drawerCache.Add(objectName, scriptableObject);
                                }
                                if(drawerCache[objectName]) {
                                    try {
                                        drawerCache[objectName].OnDrawerGUI(materialEditor, properties);
                                    }
                                    catch (Exception e) {
                                        EditorGUILayout.HelpBox("Error in Custom Drawer \"" + objectName + "\": " + e, MessageType.Error);
                                    }
                                }
                                else
                                    EditorGUILayout.HelpBox("Custom Drawer \"" + objectName + "\" not found.", MessageType.Error);
                            }
                            else {
                                EditorGUILayout.HelpBox("No Drawer specified.", MessageType.Warning);
                            }
                            previousPropertyWasDrawn = true;
                            break;
                        case MarkdownProperty.Header:
                            var labelName = display.Substring(display.IndexOf(' ') + 1);
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField(labelName, EditorStyles.boldLabel);
                            previousPropertyWasDrawn = true;
                            break;
                        case MarkdownProperty.Reference:
                            var split = display.Split(' ');
                            if(split.Length > 1) {
                                var keywordRef = display.Split(' ')[1];
                                var keywordProp = FindProperty(keywordRef, properties);
                                if(keywordProp == null)
                                    EditorGUILayout.HelpBox("Could not find MaterialProperty: '" + keywordRef, MessageType.Error);
                                else
                                    materialEditor.ShaderProperty(keywordProp, keywordProp.displayName);
                            }
                            previousPropertyWasDrawn = true;
                            break;
                        case MarkdownProperty.None:
                        default:
                            if(referencedProperties.Contains(prop))
                            {
                                previousPropertyWasDrawn = false;
                                break;
                            }

                            if(prop.flags.HasFlag(MaterialProperty.PropFlags.HideInInspector))
                                break;
                            
                            if(prop.flags.HasFlag(MaterialProperty.PropFlags.PerRendererData))
                                break;

                            materialEditor.ShaderProperty(prop, display);
                            previousPropertyWasDrawn = true;
                            break;
                    }
                    
                    if(isDisabled)
                        EditorGUI.EndDisabledGroup();
                }    
                EditorGUILayout.Space();
            }
            
            foreach(var group in headerGroups)
            {
                if (group.properties == null && group.customDrawer == null) continue;

                if (group.name == null || group.name.Equals("Default", StringComparison.OrdinalIgnoreCase)) {
                    if(group.customDrawer != null) {
                        group.customDrawer.Invoke();
                    }
                    else {
                        DrawGroup(group);
                        CoreEditorUtils.DrawSplitter();
                    }
                }
                else
                {
                    var keyName = $"{nameof(MarkdownShaderGUI)}.{group.name}";
                    var state = SessionState.GetBool(keyName, true);
                    var newState = CoreEditorUtils.DrawHeaderFoldout(group.name, state);
                    if(newState != state) SessionState.SetBool(keyName, newState);
                    state = newState;
                    if (state)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.Space();
                        if (group.customDrawer != null) {
                            group.customDrawer.Invoke();
                            CoreEditorUtils.DrawSplitter();
                        }
                        else
                            DrawGroup(group);
                        EditorGUI.indentLevel--;
                    }
                    CoreEditorUtils.DrawSplitter();
                }
            }
        }

        private ShaderGUI baseShaderGui = null;
        private bool haveSearchedForCustomGUI = false;
        private void InitializeCustomGUI(Material targetMat)
        {
            // instead of calling base, we need to draw the right inspector here.
            // to figure out which one that is, we need to get info from the ShaderGraph directly - 
            // at least in HDRP, different custom editors are used depending on what modes are selected in ShaderGraph.
            
            // also see HDShaderUtils.cs
            
            //// HDRP 10
            // Decal        Rendering.HighDefinition.DecalGUI
            // Eye          Rendering.HighDefinition.LightingShaderGraphGUI
            // Fabric       Rendering.HighDefinition.LightingShaderGraphGUI
            // Hair         Rendering.HighDefinition.LightingShaderGraphGUI
            // Lit          Rendering.HighDefinition.LitShaderGraphGUI
            // StackLit     Rendering.HighDefinition.LightingShaderGraphGUI
            // LayeredLit   Rendering.HighDefinition.LayeredLitGUI
            // Unlit        Rendering.HighDefinition.HDUnlitGUI
            // TerrainLitGUI
            // AxFGUI
            
            //// HDRP 8
            // UnityEditor.Rendering.HighDefinition.HDLitGUI
            // UnityEditor.Rendering.HighDefinition.HDUnlitGUI
            // UnityEditor.Rendering.HighDefinition.DecalGUI
             
            #if HDRP_7_OR_NEWER
            var defaultCustomInspector = MarkdownSGExtensions.GetDefaultCustomInspectorFromShader(targetMat.shader);
            if (!defaultCustomInspector.StartsWith("UnityEditor."))
                defaultCustomInspector = "UnityEditor." + defaultCustomInspector;
            var litGui = typeof(HDShaderUtils).Assembly.GetType(defaultCustomInspector);
            baseShaderGui = (ShaderGUI) Activator.CreateInstance(litGui);

            // remove the "ShaderGraphUIBlock" uiBlock ("Exposed Properties") as we're rendering that ourselves
            // if(!showOriginalPropertyList)
            //     MarkdownHDExtensions.RemoveShaderGraphUIBlock(baseShaderGui);
            #endif
            
            haveSearchedForCustomGUI = true;
        }
    }
}