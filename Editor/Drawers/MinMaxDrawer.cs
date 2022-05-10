using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.ShaderGraphMarkdown
{
    public class MinMaxDrawer : MarkdownMaterialPropertyDrawer
    {
        float GetValue(MaterialProperty property, char swizzle)
        {
            switch (property.type)
            {
                case MaterialProperty.PropType.Float:
                case MaterialProperty.PropType.Range:
                    return property.floatValue;
                case MaterialProperty.PropType.Vector:
                    return GetValue(property.vectorValue, swizzle);
                default:
                    return 0;
            }
        }

        void SetValue(MaterialProperty property, char swizzle, float value)
        {
            switch (property.type)
            {
                case MaterialProperty.PropType.Float:
                case MaterialProperty.PropType.Range:
                    property.floatValue = value;
                    break;
                case MaterialProperty.PropType.Vector:
                    var val = property.vectorValue;
                    SetValue(ref val, swizzle, value);
                    property.vectorValue = val;
                    break;
                default:
                    return;
            }
        }
        
        float GetValue(Vector4 vector, char swizzle)
        {
            switch (swizzle)
            {
                case 'x': case 'r': return vector.x;
                case 'y': case 'g': return vector.y;
                case 'z': case 'b': return vector.z;
                case 'w': case 'a': return vector.w;
                default: return 0;
            }
        }

        void SetValue(ref Vector4 vector, char swizzle, float value)
        {
            switch (swizzle)
            {
                case 'x': case 'r': vector.x = value; return;
                case 'y': case 'g': vector.y = value; return;
                case 'z': case 'b': vector.z = value; return;
                case 'w': case 'a': vector.w = value; return;
                default: return;
            }
        }

        void GetPropertyNameAndSwizzle(string parameterName, out string propertyName, out char swizzle)
        {
            var indexOfDot = parameterName.LastIndexOf('.');
            if (indexOfDot > 0 && indexOfDot == parameterName.Length - 2)
            {
                swizzle = parameterName[parameterName.Length - 1];
                propertyName = parameterName.Substring(0, parameterName.Length - 2);
            }
            else
            {
                swizzle = ' ';
                propertyName = parameterName;
            }
        }

        private bool isInline = false;
        private Rect inlineRect;
        
        public override void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            // check if these are vectors, and get the "base property"
            var parameterName1 = parameters.Get(0, (string) null);
            var parameterName2 = parameters.Get(1, (string) null);
            var displayName = parameters.Get(2, (string) null);
            if (parameterName2 != null && parameterName2.StartsWith("[", StringComparison.Ordinal))
                parameterName2 = null;

            if (displayName != null && displayName.StartsWith("[", StringComparison.Ordinal))
                displayName = null;

            // allow third parameter to be used for display name override
            // use second parameter if it's not referencing a property
            if (parameterName2 != null && displayName == null)
            {
                GetPropertyNameAndSwizzle(parameterName2, out var prop, out var sw);
                var p2 = properties.FirstOrDefault(x => x.name.Equals(prop, StringComparison.Ordinal));
                if (p2 == null)
                {
                    displayName = parameterName2;
                    parameterName2 = null;
                }
            }
            
            if (parameterName2 == null)
            {
                // parameter 1 must be a vector, no swizzles
                // we're using zw as limits
                var vectorProp = properties.FirstOrDefault(x => x.name.Equals(parameterName1, StringComparison.Ordinal));
                if (vectorProp == null || vectorProp.type != MaterialProperty.PropType.Vector)
                {
                    if(!isInline)
                        EditorGUILayout.HelpBox(nameof(MinMaxDrawer) + ": Parameter is not a Vector property (" + parameterName1 + ")", MessageType.Error);
                    return;
                }

                EditorGUI.showMixedValue = vectorProp.hasMixedValue;
                var vec = vectorProp.vectorValue;
                EditorGUI.BeginChangeCheck();

                if (displayName == null)
                    displayName = vectorProp.displayName;
                
                if(isInline)
                    EditorGUI.MinMaxSlider(inlineRect, ref vec.x, ref vec.y, vec.z, vec.w);
                else
                    EditorGUILayout.MinMaxSlider(new GUIContent(parameters.ShowPropertyNames ? vectorProp.name + ".xy (z-w)" : displayName, parameters.Tooltip), ref vec.x, ref vec.y, vec.z, vec.w);
                
                if (EditorGUI.EndChangeCheck())
                {
                    vectorProp.vectorValue = vec;
                }

                EditorGUI.showMixedValue = false;
                return;
            }

            if (parameterName1 == null || parameterName2 == null) {
                if(!isInline)
                    EditorGUILayout.HelpBox(nameof(MinMaxDrawer) + ": Parameter names are incorrect (" + parameterName1 + ", " + parameterName2 + "), all: " + string.Join(",", parameters), MessageType.Error);
                return;
            }
            
            GetPropertyNameAndSwizzle(parameterName1, out var propertyName1, out var swizzle1);
            GetPropertyNameAndSwizzle(parameterName2, out var propertyName2, out var swizzle2);
            
            var param1 = properties.FirstOrDefault(x => x.name.Equals(propertyName1, StringComparison.Ordinal));
            var param2 = properties.FirstOrDefault(x => x.name.Equals(propertyName2, StringComparison.Ordinal));

            if (param1 == null || param2 == null) {
                if(!isInline)
                    EditorGUILayout.HelpBox(nameof(MinMaxDrawer) + ": Parameter names are incorrect (" + propertyName1 + ", " + propertyName2 + "), all: " + string.Join(",", parameters), MessageType.Error);
                return;
            }
            
            float value1 = GetValue(param1, swizzle1);
            float value2 = GetValue(param2, swizzle2);

            if (displayName == null)
                displayName = param1 == param2 ? param1.displayName : param1.displayName + "-" + param2.displayName;
            
            EditorGUI.BeginChangeCheck();
            
            if(isInline)
                EditorGUI.MinMaxSlider(inlineRect, ref value1, ref value2, 0.0f, 1.0f);
            else
                EditorGUILayout.MinMaxSlider(new GUIContent(parameters.ShowPropertyNames 
                    ? (param1 == param2 
                        ? param1.name + "." + swizzle1 + swizzle2 
                        : param1.name + "." + swizzle1 + "-" + param2.name + "." + swizzle2) 
                    : displayName, parameters.Tooltip), ref value1, ref value2, 0.0f, 1.0f);
            
            if (EditorGUI.EndChangeCheck())
            {
                SetValue(param1, swizzle1, value1);
                SetValue(param2, swizzle2, value2);
            }
        }

        public override bool SupportsInlineDrawing => true;

        public override void OnInlineDrawerGUI(Rect rect, MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            isInline = true;
            inlineRect = rect;
            OnDrawerGUI(materialEditor, properties, parameters);
            isInline = false;
        }

        public override IEnumerable<MaterialProperty> GetReferencedProperties(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            var parameterName1 = parameters.Get(0, (string) null);
            var parameterName2 = parameters.Get(1, (string) null);
            if (parameterName2 != null && parameterName2.StartsWith("[", StringComparison.Ordinal))
                parameterName2 = null;
            
            if (parameterName1 != null && parameterName2 == null)
            {
                var vectorProp = properties.FirstOrDefault(x => x.name.Equals(parameterName1, StringComparison.Ordinal));
                if (vectorProp == null || vectorProp.type != MaterialProperty.PropType.Vector)
                    return null;

                return new [] { vectorProp };
            }

            if (parameterName1 == null)
                return null;
            
            GetPropertyNameAndSwizzle(parameterName1, out var propertyName1, out _);
            GetPropertyNameAndSwizzle(parameterName2, out var propertyName2, out _);
            
            var param1 = properties.FirstOrDefault(x => x.name.Equals(propertyName1, StringComparison.Ordinal));
            var param2 = properties.FirstOrDefault(x => x.name.Equals(propertyName2, StringComparison.Ordinal));
            
            if (param1 != null && param2 != null)
                return new[] { param1, param2 };
            if (param1 != null)
                return new[] { param1 };
            if (param2 != null)
                return new[] { param2 };
            return null;
        }
    }
}