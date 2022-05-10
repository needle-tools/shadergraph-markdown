using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Needle.ShaderGraphMarkdown
{
    public class MultiPropertyDrawer : MarkdownMaterialPropertyDrawer
    {
        public override void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            var controlRect = EditorGUILayout.GetControlRect(false, 18f);
            
            controlRect.xMin += EditorGUI.indentLevel * 15f;
            var previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            OnInlineDrawerGUI(controlRect, materialEditor, properties, parameters);
            EditorGUI.indentLevel = previousIndent;
        }

        public override IEnumerable<MaterialProperty> GetReferencedProperties(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                yield return parameters.Get(i, properties);
            }
        }

        public override bool SupportsInlineDrawing => true;

        public override void OnInlineDrawerGUI(Rect rect, MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            // GUI.DrawTexture(rect, Texture2D.whiteTexture);

            var entryCount = parameters.Count;
            var partRect = rect;
            partRect.width = partRect.width / entryCount - 5;
            var previousWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = partRect.width / 3;
            
            for(int i = 0; i < parameters.Count; i++)
            {
                var param = parameters.Get(i, properties);
                if (param == null)
                {
                    throw new System.ArgumentException("Parameter " + i + " is invalid: " + parameters.Get(i, ""));
                }
                if (param.type == MaterialProperty.PropType.Texture)
                {
                    var miniTexRect = partRect;
                    miniTexRect.width += 100;
                    var prevWidth2 = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = partRect.width;
                    materialEditor.TexturePropertyMiniThumbnail(miniTexRect, param, parameters.ShowPropertyNames ? param.name : param.displayName, null);
                    EditorGUIUtility.labelWidth = prevWidth2;
                }
                else
                {
                    materialEditor.ShaderProperty(partRect, param, parameters.ShowPropertyNames ? param.name : param.displayName);
                }
                partRect.x += partRect.width + 5;
            }

            EditorGUIUtility.labelWidth = previousWidth;
        }
    }
}
