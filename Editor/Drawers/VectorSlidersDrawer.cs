using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Needle.ShaderGraphMarkdown
{
    public class VectorSlidersDrawer : MarkdownMaterialPropertyDrawer
    {
        static string[] defaultParts = new[] {"X", "Y", "Z", "W"};
        
        public override void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            if (parameters.Count < 1)
                throw new System.ArgumentException("No parameters to " + nameof(InlineTextureDrawer) + ". Please provide _TextureProperty and optional _Float or _Color property names.");
            var vectorProperty = parameters.Get(0, properties);
            if (vectorProperty == null)
                throw new System.ArgumentNullException("No property named " + parameters.Get(0, ""));

            if (vectorProperty.type != MaterialProperty.PropType.Vector)
                throw new System.ArgumentException("Property " + vectorProperty + " isn't a vector property, can't draw sliders for it.");

            var display = vectorProperty.displayName;
            var firstParen = display.IndexOf('(');
            var lastParen = vectorProperty.displayName.LastIndexOf(')');
            string[] parts = null;
            if (firstParen >= 0 && lastParen >= 0 && lastParen > firstParen)
            {
                var betweenParens = display.Substring(firstParen + 1, lastParen - firstParen - 1);
                parts = betweenParens.Split(',', ';');
                display = display.Substring(0, firstParen).TrimEnd();
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
                value[i] = EditorGUILayout.Slider(parts[i], value[i], 0, 1);
            }

            if (EditorGUI.EndChangeCheck())
                vectorProperty.vectorValue = value;
            EditorGUI.indentLevel--;
        }

        public override IEnumerable<MaterialProperty> GetReferencedProperties(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            var vectorProperty = parameters.Get(0, properties);
            if (vectorProperty == null) return null;
            return new[] { vectorProperty };
        }
    }
}