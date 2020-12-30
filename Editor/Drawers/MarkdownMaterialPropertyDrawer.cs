using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.ShaderGraphMarkdown
{
    public abstract class MarkdownMaterialPropertyDrawer : ScriptableObject
    {
        public abstract void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty[] properties);
    }
}