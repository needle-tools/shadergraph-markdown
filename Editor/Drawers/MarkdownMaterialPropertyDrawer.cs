using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.ShaderGraphMarkdown
{
    public abstract class MarkdownMaterialPropertyDrawer : ScriptableObject
    {
        public struct DrawerParameters
        {
            public override string ToString()
            {
                return string.Join(",", parts);
            }

            private string[] parts;

            public DrawerParameters(string[] parts) {
                this.parts = parts;
            }

            public int Count => parts.Length - 2;

            public string Get(int i, string defaultValue)
            {
                // first 2 parts are "!DRAWER" and the drawer object name
                if (parts.Length > 2 + i)
                    return parts[2 + i];
                    
                return defaultValue;
            }

            public MaterialProperty Get(int i, MaterialProperty[] properties)
            {
                if (parts.Length < 2 + i + 1)
                    return null;
                
                var propertyName = parts[2 + i];
                var property = properties.FirstOrDefault(x => x.name.Equals(propertyName, StringComparison.Ordinal));
                return property;
            }
        }
        
        /// Renders a custom drawer. Parts 0 and 1 of the parts array are "!DRAWER" and the drawer name,
        /// the others are optional parameters passed to the drawer.
        public abstract void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters);

        public virtual void OnInlineDrawerGUI(Rect rect, MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            
        }

        public virtual bool SupportsInlineDrawing => false;
        
        public virtual IEnumerable<MaterialProperty> GetReferencedProperties(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            return null;
        }

        public override string ToString()
        {
            return this.GetType().ToString();
        }
    }
}