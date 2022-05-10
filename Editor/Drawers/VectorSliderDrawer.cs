using System.Collections.Generic;
using System.Reflection;
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
                throw new System.ArgumentException("No parameters to " + nameof(VectorSliderDrawer) + ". Please provide a vector property name or one or more float property names.");
            var vectorProperty = parameters.Get(0, properties);
            if (vectorProperty == null)
                throw new System.ArgumentNullException("No property named " + parameters.Get(0, ""));

            switch (vectorProperty.type)
            {
                case MaterialProperty.PropType.Vector:
                    OnDrawerGUI(materialEditor, vectorProperty, new GUIContent(parameters.ShowPropertyNames ? vectorProperty.name : vectorProperty.displayName, parameters.Tooltip));
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
                        materialEditor.ShaderProperty(param, parameters.ShowPropertyNames ? param.name : param.displayName);
                    }
                    EditorGUI.indentLevel--;
                    break;
                default:
                    throw new System.ArgumentException("Property " + vectorProperty + " isn't a vector or float property, can't draw sliders for it.");
            }
        }

        public void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty vectorProperty, GUIContent display)
        {
            if (vectorProperty.name.StartsWith("_Tile", System.StringComparison.Ordinal) || vectorProperty.name.StartsWith("_Tiling", System.StringComparison.Ordinal) || vectorProperty.name.EndsWith("_ST", System.StringComparison.Ordinal))
            {
                var rect = EditorGUILayout.GetControlRect(true, 18 * 2);
                materialEditor.BeginAnimatedCheck(rect, vectorProperty);
                EditorGUI.BeginChangeCheck();
                var newVector = MaterialEditor.TextureScaleOffsetProperty(rect, vectorProperty.vectorValue);
                if (EditorGUI.EndChangeCheck())
                    vectorProperty.vectorValue = newVector;
                materialEditor.EndAnimatedCheck();
                EditorGUILayout.Space(1);
                return;
            }
            
            var firstParen = display.text.IndexOf('(');
            var lastParen = display.text.LastIndexOf(')');
            string[] parts;
            if (firstParen >= 0 && lastParen >= 0 && lastParen > firstParen)
            {
                var betweenParens = display.text.Substring(firstParen + 1, lastParen - firstParen - 1);
                parts = betweenParens.Split(new []{',', ';'}, System.StringSplitOptions.RemoveEmptyEntries);
                display.text = display.text.Substring(0, firstParen).Trim();
            }
            else
            {
                parts = defaultParts;
            }

            EditorGUILayout.LabelField(display);
            EditorGUI.indentLevel++;
            materialEditor.BeginAnimatedCheck(vectorProperty);
            // we need to do our own OverridePropertyColor check as this seems to be broken for Vector4 properties in Unity.
            if (OverridePropertyColor(materialEditor, "material." + vectorProperty.name + ".x", out var bgColor))
                GUI.backgroundColor = bgColor;
            EditorGUI.BeginChangeCheck();
            var value = vectorProperty.vectorValue;
            for (int i = 0; i < Mathf.Min(parts.Length, 4); i++)
            {
                materialEditor.BeginAnimatedCheck(vectorProperty);
                var rect = EditorGUILayout.GetControlRect(true, 18f);
                value[i] = EditorGUI.Slider(rect, parts[i].Trim(), value[i], minValue, maxValue);
                materialEditor.EndAnimatedCheck();
            }

            if (EditorGUI.EndChangeCheck())
                vectorProperty.vectorValue = value;
            materialEditor.EndAnimatedCheck();
            EditorGUI.indentLevel--;
        }

        // ReSharper disable InconsistentNaming
        private static MethodInfo InAnimationRecording, IsPropertyCandidate;
        // ReSharper restore InconsistentNaming
        private static PropertyInfo rendererForAnimationMode; 
        // This is incredibly ugly, but seems Vector4 animation colors are broken in Unity,
        // this gets them to work for our custom editor here.
        bool OverridePropertyColor(MaterialEditor editor, string path, out Color color)
        {
            if (rendererForAnimationMode == null) rendererForAnimationMode = typeof(MaterialEditor).GetProperty("rendererForAnimationMode", (BindingFlags) (-1));
            if (InAnimationRecording == null) InAnimationRecording = typeof(AnimationMode).GetMethod("InAnimationRecording", (BindingFlags) (-1));
            if (IsPropertyCandidate == null) IsPropertyCandidate = typeof(AnimationMode).GetMethod("IsPropertyCandidate", (BindingFlags) (-1));
            
            if (rendererForAnimationMode == null || InAnimationRecording == null || IsPropertyCandidate == null)
            {
                color = Color.white;
                return false;
            }
            
            var target = (Object) rendererForAnimationMode.GetValue(editor);
            if (AnimationMode.IsPropertyAnimated(target, path))
            {
                color = AnimationMode.animatedPropertyColor;
                if ((bool) InAnimationRecording.Invoke(null, null))
                    color = AnimationMode.recordedPropertyColor;
                else if ((bool) IsPropertyCandidate.Invoke(null, new object[]{ target, path }))
                    color = AnimationMode.candidatePropertyColor;
                return true;
            }
            
            color = Color.white;
            return false;
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