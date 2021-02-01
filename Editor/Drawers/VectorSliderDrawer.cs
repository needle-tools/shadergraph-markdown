using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Needle.ShaderGraphMarkdown
{
    public class VectorSliderDrawer : MarkdownMaterialPropertyDrawer
    {
        public float minValue = 0, maxValue = 1;
        
        static string[] defaultParts = new[] {"X", "Y", "Z", "W"};
        
        public override void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            if (parameters.Count < 1)
                throw new System.ArgumentException("No parameters to " + nameof(InlineTextureDrawer) + ". Please provide a vector property name or one or more float property names.");
            var vectorProperty = parameters.Get(0, properties);
            if (vectorProperty == null)
                throw new System.ArgumentNullException("No property named " + parameters.Get(0, ""));

            switch (vectorProperty.type)
            {
                case MaterialProperty.PropType.Vector:
                    OnDrawerGUI(materialEditor, vectorProperty, vectorProperty.displayName);
                    break;
                case MaterialProperty.PropType.Float:
                case MaterialProperty.PropType.Range:
                    EditorGUILayout.LabelField("Vector Group");
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        var param = parameters.Get(i, properties);
                        if (param == null) {
                            EditorGUILayout.HelpBox("Parameter " + parameters.Get(i, (string)null) + " does not exist.", MessageType.Error);
                            continue;
                        }
                        materialEditor.ShaderProperty(param, param.displayName);
                    }
                    EditorGUI.indentLevel--;
                    break;
                default:
                    throw new System.ArgumentException("Property " + vectorProperty + " isn't a vector or float property, can't draw sliders for it.");
            }
        }

        public void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty vectorProperty, string display)
        {
            var firstParen = display.IndexOf('(');
            var lastParen = vectorProperty.displayName.LastIndexOf(')');
            string[] parts = null;
            if (firstParen >= 0 && lastParen >= 0 && lastParen > firstParen)
            {
                var betweenParens = display.Substring(firstParen + 1, lastParen - firstParen - 1);
                parts = betweenParens.Split(',', ';');
                display = display.Substring(0, firstParen).Trim();
            }
            else
            {
                parts = defaultParts;
            }
            
            EditorGUILayout.LabelField(display);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            var value = vectorProperty.vectorValue;
            for (int i = 0; i < Mathf.Min(parts.Length, 4); i++)
            {
                value[i] = EditorGUILayout.Slider(parts[i], value[i], minValue, maxValue);
            }

            if (EditorGUI.EndChangeCheck())
                vectorProperty.vectorValue = value;
            EditorGUI.indentLevel--;
        }
        
        public override IEnumerable<MaterialProperty> GetReferencedProperties(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            var vectorProperty = parameters.Get(0, properties);
            if (vectorProperty == null) return null;

            switch (vectorProperty.type)
            {
                case MaterialProperty.PropType.Vector:
                    return new[] { vectorProperty };
                case MaterialProperty.PropType.Float:
                case MaterialProperty.PropType.Range:
                    var parameterList = new List<MaterialProperty>();
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        var param = parameters.Get(i, properties);
                        if (param != null)
                            parameterList.Add(param);
                    }

                    return parameterList;
                default:
                    return null;
            }
        }
    }
}