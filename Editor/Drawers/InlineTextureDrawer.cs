using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Needle.ShaderGraphMarkdown
{
    public class InlineTextureDrawer : MarkdownMaterialPropertyDrawer
    {
        public override void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            if (parameters.Count < 1)
                throw new ArgumentException("No parameters to " + nameof(InlineTextureDrawer) + ". Please provide _TextureProperty and optional _Float or _Color property names.");
            var textureProperty = parameters.Get(0, properties);
            if (textureProperty == null)
                throw new ArgumentNullException("No property named " + parameters.Get(0, ""));
            
            var extraProperty = parameters.Get(1, properties);
            
            OnDrawerGUI(materialEditor, properties, textureProperty, textureProperty.displayName, extraProperty);
        }

        private static Rect lastInlineTextureRect; 
        internal static Rect LastInlineTextureRect => lastInlineTextureRect;
        
        internal void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty[] properties, MaterialProperty textureProperty, string displayName, MaterialProperty extraProperty)
        {
            lastInlineTextureRect = Rect.zero;
            if(extraProperty == null)
            {
                var rect = materialEditor.TexturePropertySingleLine(new GUIContent(displayName), textureProperty);
                lastInlineTextureRect = rect;
                lastInlineTextureRect.xMin += EditorGUIUtility.labelWidth;
            }
            else if(extraProperty.type == MaterialProperty.PropType.Vector && (extraProperty.name.Equals(textureProperty.name + "_ST", StringComparison.Ordinal)))
            {
                materialEditor.TextureProperty(textureProperty, displayName, true);
            }
            else
            {
                materialEditor.TexturePropertySingleLine(new GUIContent(displayName), textureProperty, extraProperty);
            }
            
            // workaround for Unity being weird
            if(extraProperty != null && extraProperty.type == MaterialProperty.PropType.Texture) {
                EditorGUILayout.Space(45);
            }
        }

        public override IEnumerable<MaterialProperty> GetReferencedProperties(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            var textureProperty = parameters.Get(0, properties);
            var extraProperty = parameters.Get(1, properties);
            
            return new[] { textureProperty, extraProperty };
        }
    }
}