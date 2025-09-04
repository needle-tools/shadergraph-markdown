using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Needle.ShaderGraphMarkdown
{
    public static class CommonInternalExtensions
    {
        public static void ShaderPropertyWithTooltip(this MaterialEditor materialEditor, MaterialProperty prop, GUIContent label)
        {
            // special case: we want to draw textures and vectors differently, since they discard tooltips in the MaterialEditor implementation...
            switch (prop.type)
            {
                case MaterialProperty.PropType.Vector:
                    if (_GetPropertyRect == null)
                    {
                        materialEditor.VectorProperty(prop, label.text);
                    }
                    else
                    {
                        var position = (Rect)_GetPropertyRect.Invoke(materialEditor, new object[] { prop, label.text, true });
                        _VectorProperty(position, prop, label);
                    }
                    break;
                case MaterialProperty.PropType.Texture:
                    bool scaleOffset = (prop.flags & MaterialProperty.PropFlags.NoScaleOffset) == MaterialProperty.PropFlags.None;
                    materialEditor.TexturePropertyWithTooltip(prop, label, scaleOffset);
                    break;
                default:
                    materialEditor.ShaderProperty(prop, label);
                    break;
            }
        }
        
        // from MaterialEditor.VectorProperty, plus support for GUIContent
        public static Vector4 _VectorProperty(Rect position, MaterialProperty prop, GUIContent label)
        {
#if UNITY_2022_1_OR_NEWER
            MaterialEditor.BeginProperty(position, prop);
#endif
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0.0f;
            Vector4 vector4 = EditorGUI.Vector4Field(position, label, prop.vectorValue);
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.vectorValue = vector4;
#if UNITY_2022_1_OR_NEWER
            MaterialEditor.EndProperty();
#endif
            return prop.vectorValue;
        }

        private static MethodInfo _getPropertyRect = null;
        private static MethodInfo _GetPropertyRect
        {
            get
            {
                if (_getPropertyRect == null)
                    _getPropertyRect = typeof(MaterialEditor).GetMethod("GetPropertyRect", (BindingFlags)(-1), null, CallingConventions.Any, new Type[] { typeof(MaterialProperty), typeof(string), typeof(bool) }, null);

                return _getPropertyRect;
            }
        }
        
        public static void TexturePropertyWithTooltip(this MaterialEditor materialEditor, MaterialProperty textureProperty, GUIContent displayContent, bool scaleOffset)
        {
            if (_GetPropertyRect == null)
            {
                materialEditor.TextureProperty(textureProperty, displayContent.text, scaleOffset);
            }
            else 
            {
                var rect = (Rect)_GetPropertyRect.Invoke(materialEditor, new object[] { textureProperty, displayContent.text, true });
                materialEditor.TextureProperty(rect, textureProperty, displayContent.text, displayContent.tooltip, scaleOffset);
            }
        }
    }
}