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
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var targetMat = materialEditor.target as Material;
            if (!targetMat) return;
            
            // split by header properties
            var headerGroups = new List<HeaderGroup>();
            headerGroups.Add(new HeaderGroup() { name = "Default" });
            
            foreach (var prop in properties)
            {
                if(prop.displayName.StartsWith("## ")) {
                    headerGroups.Add(new HeaderGroup() { name = prop.displayName.Substring(prop.displayName.IndexOf(' ') + 1) });
                }
                else
                {
                    var last = headerGroups.Last();
                    if (last.properties == null) 
                        last.properties = new List<MaterialProperty>();
                    last.properties.Add(prop);
                }
            }

            string GetBetween(string str, char start, char end, bool last = false)
            {
                var i0 = last ? str.LastIndexOf(start) : str.IndexOf(start);
                var i1 = last ? str.LastIndexOf(end)   : str.IndexOf(end);
                if (i0 < 0 || i1 < 0) return null;
                return str.Substring(i0 + 1, i1 - i0 - 1);
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

                    if (display.StartsWith("#LINK"))
                    {
                        if (!previousPropertyWasDrawn) continue;
                        var linkText = GetBetween(display, '[', ']');
                        var linkHref = GetBetween(display, '(', ')');
                        if (GUILayout.Button(linkText, CenteredGreyMiniLabel)) Application.OpenURL(linkHref);
                        continue;
                    }

                    if (display.StartsWith("#NOTE"))
                    {
                        if (!previousPropertyWasDrawn) continue;
                        var noteText = display.Substring(display.IndexOf(' ') + 1);
                        EditorGUILayout.LabelField(noteText, CenteredGreyMiniLabel);
                        continue;
                    }

                    if (display.StartsWith("#DRAWER"))
                    {
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
                    }
                        
                    if (display.StartsWith("### "))
                    {
                        var labelName = display.Substring(display.IndexOf(' ') + 1);
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField(labelName, EditorStyles.boldLabel);
                        continue;
                    }
                        
                    if (display.StartsWith("#REF"))
                    {
                        var keywordRef = display.Split(' ')[1];
                        try
                        {
                            var keywordProp = FindProperty(keywordRef, properties);
                            materialEditor.ShaderProperty(keywordProp, keywordProp.displayName);
                        }
                        catch (ArgumentException e)
                        {
                            EditorGUILayout.HelpBox(e.Message, MessageType.Error);
                        }
                    }
                    else
                    {
                        materialEditor.ShaderProperty(prop, display);
                        previousPropertyWasDrawn = true;
                    }
                }    
                EditorGUILayout.Space();
            }
            
            foreach(var group in headerGroups)
            {
                if (group.properties == null) continue;

                if (group.name.Equals("Default", StringComparison.OrdinalIgnoreCase)) {
                    DrawGroup(group);
                }
                else {
                    if (!headerGroupStates.ContainsKey(group.name)) headerGroupStates.Add(group.name, false);
                    headerGroupStates[group.name] = CoreEditorUtils.DrawHeaderFoldout(group.name, headerGroupStates[group.name]);
                    if (headerGroupStates[group.name])
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.Space();
                        DrawGroup(group);
                        EditorGUI.indentLevel--;
                    }
                    CoreEditorUtils.DrawSplitter();
                }
            }
        }
    }
}