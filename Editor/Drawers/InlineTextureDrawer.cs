using System.Collections;
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
                throw new System.ArgumentException("No parameters to " + nameof(InlineTextureDrawer) + ". Please provide _TextureProperty and optional _Float or _Color property names.");
            var textureProperty = parameters.Get(0, properties);
            if (textureProperty == null)
                throw new System.ArgumentNullException("No property named " + parameters.Get(0, ""));
            
            var extraProperty = parameters.Get(1, properties);
            // var extraProperty2 = parameters.Get(2, properties);
            
            if(extraProperty == null)
                materialEditor.TexturePropertySingleLine(new GUIContent(textureProperty.displayName), textureProperty);
            // else if (extraProperty2 != null)
            //     materialEditor.TexturePropertyTwoLines(new GUIContent(textureProperty.displayName), textureProperty, extraProperty, new GUIContent(extraProperty2.displayName), extraProperty2);
            else
                materialEditor.TexturePropertySingleLine(new GUIContent(textureProperty.displayName), textureProperty, extraProperty);
            
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