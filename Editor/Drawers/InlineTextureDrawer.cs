using System;
using System.Collections.Generic;
using System.Reflection;
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
            var displayName = textureProperty.displayName;
            // strip condition
            var lastIndex = displayName.LastIndexOf('[');
            if (lastIndex > 0)
                displayName = displayName.Substring(0, lastIndex);
            // strip inlining
            var inliningIndex = displayName.IndexOf("&&", StringComparison.Ordinal);
            if (inliningIndex > 0)
                displayName = displayName.Substring(0, inliningIndex);
            
            OnDrawerGUI(materialEditor, properties, textureProperty, new GUIContent(parameters.ShowPropertyNames ? textureProperty.name : displayName, parameters.Tooltip), extraProperty);
        }

        private static Rect lastInlineTextureRect; 
        internal static Rect LastInlineTextureRect => lastInlineTextureRect;
        private static MethodInfo _GetPropertyRect = null;
        
        internal void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty[] properties, MaterialProperty textureProperty, GUIContent displayContent, MaterialProperty extraProperty)
        {
            lastInlineTextureRect = Rect.zero;
            if(extraProperty == null)
            {
                var rect = materialEditor.TexturePropertySingleLine(displayContent, textureProperty);
                lastInlineTextureRect = rect;
                lastInlineTextureRect.x += EditorGUIUtility.labelWidth;
                lastInlineTextureRect.width -= EditorGUIUtility.labelWidth;
            }
            else if(extraProperty.type == MaterialProperty.PropType.Vector && (extraProperty.name.Equals(textureProperty.name + "_ST", StringComparison.Ordinal)))
            {
                if (_GetPropertyRect == null)
                {
                    _GetPropertyRect = typeof(MaterialEditor).GetMethod("GetPropertyRect", (BindingFlags)(-1), null, new[] { typeof(MaterialProperty), typeof(string), typeof(bool) }, null);
                    if (_GetPropertyRect == null)
                    {
                        EditorGUILayout.HelpBox("Oh no, looks like an API change for MaterialEditor.GetPropertyRect – please report a bug! Thanks!", MessageType.Error);
                    }
                }
                if (_GetPropertyRect == null)
                {
                    materialEditor.TexturePropertyWithTooltip(textureProperty, displayContent, true);
                }
                else
                {
                    var rect = (Rect) _GetPropertyRect.Invoke(materialEditor, new object[] { textureProperty, displayContent.text, true });
                    materialEditor.TextureProperty(rect, textureProperty, displayContent.text, displayContent.tooltip, true);
                }
            }
            else
            {
                materialEditor.TexturePropertySingleLine(displayContent, textureProperty, extraProperty);
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