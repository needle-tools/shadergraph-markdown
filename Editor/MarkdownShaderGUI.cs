using System;
using System.Collections.Generic;
using System.Linq;
using Needle.ShaderGraphMarkdown;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

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
        
        private readonly Dictionary<string, bool> headerGroupStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, MarkdownMaterialPropertyDrawer> drawerCache = new Dictionary<string, MarkdownMaterialPropertyDrawer>();
        private readonly List<MaterialProperty> referencedProperties = new List<MaterialProperty>();
        private readonly List<string> excludedProperties = new List<string>() {"unity_Lightmaps", "unity_LightmapsInd", "unity_ShadowMasks" };

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

        internal static MarkdownProperty GetMarkdownType(string display)
        {
            if (display.StartsWith("#REF"))
                return MarkdownProperty.Reference;
            if (display.StartsWith("#LINK"))
                return MarkdownProperty.Link;
            if (display.StartsWith("#NOTE"))
                return MarkdownProperty.Note;
            if (display.StartsWith("#DRAWER"))
                return MarkdownProperty.Drawer;
            if (display.StartsWith("## ") || display.Equals("##", StringComparison.Ordinal))
                return MarkdownProperty.Foldout;
            if (display.StartsWith("### "))
                return MarkdownProperty.Header;
            return MarkdownProperty.None;
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
                if(prop.displayName.StartsWith("## ") || prop.displayName.Equals("##", StringComparison.Ordinal))
                {
                    if (prop.displayName.Equals("##", StringComparison.Ordinal))
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
                    if (display.StartsWith("#REF"))
                    {
                        var keywordRef = display.Split(' ')[1];
                        try {
                            var keywordProp = FindProperty(keywordRef, properties);
                            referencedProperties.Add(keywordProp);
                        }
                        catch (ArgumentException) {
                            // EditorGUILayout.HelpBox(e.Message, MessageType.Error);
                        }
                    }

                    last.properties.Add(prop);
                }
            }
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
                EditorGUILayout.Space();

                // EditorGUILayout.LabelField("Shader Properties", EditorStyles.boldLabel);
                // var shader = targetMat.shader;
                // var propertyCount = ShaderUtil.GetPropertyCount(shader);
                // for (int i = 0; i < propertyCount; i++) {
                //     EditorGUILayout.LabelField(ShaderUtil.GetPropertyName(shader, i), ShaderUtil.GetPropertyType(shader, i) + (ShaderUtil.IsShaderPropertyHidden(shader, i) ? " (hidden)" : ""));
                // }
                
                // ShaderUtil.GetShaderGlobalKeywords(shader);
                // ShaderUtil.GetShaderLocalKeywords(shader);
            }

            void DrawGroup(HeaderGroup group)
            {
                bool previousPropertyWasDrawn = true;
                
                foreach (var prop in group.properties)
                {
                    var display = prop.displayName;
                    var hasCondition = display.Contains('[') && display.EndsWith("]", StringComparison.Ordinal);
                    if(hasCondition) {
                        var condition = GetBetween(display, '[', ']', true);
                        if (!string.IsNullOrEmpty(condition))
                        {
                            if (Array.IndexOf(targetMat.shaderKeywords, condition) < 0)
                            {
                                previousPropertyWasDrawn = false;
                                continue;
                            }
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
                            continue;
                        case MarkdownProperty.Note:
                            if (!previousPropertyWasDrawn) continue;
                            var noteText = display.Substring(display.IndexOf(' ') + 1);
                            EditorGUILayout.LabelField(noteText, CenteredGreyMiniLabel);
                            continue;
                        case MarkdownProperty.Drawer:
                            var parts = display.Split(' ');
                            var objectName = parts[1];
                            if(!drawerCache.ContainsKey(objectName)) {
                                var objectPath = AssetDatabase.FindAssets($"t:{nameof(MarkdownMaterialPropertyDrawer)} {objectName}").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
                                var scriptableObject = AssetDatabase.LoadAssetAtPath<MarkdownMaterialPropertyDrawer>(objectPath);
                                drawerCache.Add(objectName, scriptableObject);
                            }
                            if(drawerCache[objectName])
                                drawerCache[objectName].OnDrawerGUI(materialEditor, properties);
                            else
                                EditorGUILayout.HelpBox("Custom Drawer for property " + objectName + " not found.", MessageType.Error);
                            continue;
                        case MarkdownProperty.Header:
                            var labelName = display.Substring(display.IndexOf(' ') + 1);
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField(labelName, EditorStyles.boldLabel);
                            continue;
                        case MarkdownProperty.Reference:
                            var keywordRef = display.Split(' ')[1];
                            try
                            {
                                var keywordProp = FindProperty(keywordRef, properties);
                                // referencedProperties.Add(keywordProp);
                                materialEditor.ShaderProperty(keywordProp, keywordProp.displayName);
                            }
                            catch (ArgumentException e)
                            {
                                EditorGUILayout.HelpBox(e.Message, MessageType.Error);
                            }
                            continue;
                        case MarkdownProperty.None:
                        default:
                            if(referencedProperties.Contains(prop))
                            {
                                previousPropertyWasDrawn = false;
                                continue;
                            }

                            var idx = Shader.PropertyToID(prop.name);
                            if(targetMat.shader && idx >= 0 && idx < ShaderUtil.GetPropertyCount(targetMat.shader) && ShaderUtil.IsShaderPropertyHidden(targetMat.shader, idx))
                                continue;

                            // excluded properties
                            if (excludedProperties.Contains(prop.name))
                                continue;
                        
                            materialEditor.ShaderProperty(prop, display);
                            previousPropertyWasDrawn = true;
                            break;
                    }
                }    
                EditorGUILayout.Space();
            }
            
            foreach(var group in headerGroups)
            {
                if (group.properties == null && group.customDrawer == null) continue;

                if (group.name == null || group.name.Equals("Default", StringComparison.OrdinalIgnoreCase)) {
                    DrawGroup(group);
                }
                else {
                    if (!headerGroupStates.ContainsKey(group.name)) headerGroupStates.Add(group.name, false);
                    headerGroupStates[group.name] = CoreEditorUtils.DrawHeaderFoldout(group.name, headerGroupStates[group.name]);
                    if (headerGroupStates[group.name])
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.Space();
                        if (group.customDrawer != null)
                            group.customDrawer.Invoke();
                        else
                            DrawGroup(group);
                        EditorGUI.indentLevel--;
                    }
                    CoreEditorUtils.DrawSplitter();
                }
            }
        }
    }
}