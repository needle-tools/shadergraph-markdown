using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.ShaderGraphMarkdown
{
    [CreateAssetMenu(menuName = "ShaderGraph Markdown/Gradient Generator Drawer", fileName = nameof(GradientGeneratorDrawer) + ".asset")]
    public class GradientGeneratorDrawer : MarkdownMaterialPropertyDrawer, ISerializationCallbackReceiver
    {
        public string texturePropertyName = "_RampTexture";

        [System.Serializable]
        private class Map
        {
            public Material material;
            public Gradient gradient;
        }

        [HideInInspector] [SerializeField] private List<Map> mappedGradientStore = new List<Map>();
        private Dictionary<Material, Gradient> mappedGradients;

        public override void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var targetMat = materialEditor.target as Material;
            if (!targetMat) return;

            if (!mappedGradients.ContainsKey(targetMat))
            {
                mappedGradients.Add(targetMat, new Gradient());
                EditorUtility.SetDirty(this);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.GradientField("Ramp", mappedGradients[targetMat]);
            if (GUILayout.Button("Apply")) ApplyRampTexture(targetMat);
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyRampTexture(Material targetMat)
        {
            targetMat.SetTexture(texturePropertyName, CreateGradientTexture(targetMat, mappedGradients[targetMat]));
        }

        private static int width = 256;
        private static int height = 4; // needs to be multiple of 4 for DXT1 format compression

        private static Texture2D CreateGradientTexture(Material targetMaterial, Gradient gradient)
        {
            Texture2D gradientTexture = new Texture2D(width, height, TextureFormat.ARGB32, false, false)
            {
                name = "_LUT",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                alphaIsTransparency = true,

            };

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                    gradientTexture.SetPixel(i, j, gradient.Evaluate((float) i / width));
            }

            gradientTexture.Apply(false);
            gradientTexture = SaveAndGetTexture(targetMaterial, gradientTexture);
            return gradientTexture;
        }

        private static Texture2D SaveAndGetTexture(Material targetMaterial, Texture2D sourceTexture)
        {
            string targetFolder = AssetDatabase.GetAssetPath(targetMaterial);
            targetFolder = targetFolder.Replace(targetMaterial.name + ".mat", string.Empty);

            targetFolder += "Color Ramp Textures/";

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                AssetDatabase.Refresh();
            }

            string path = targetFolder + targetMaterial.name + sourceTexture.name + ".asset";
            // File.WriteAllBytes(path, sourceTexture.EncodeToPNG());

            AssetDatabase.CreateAsset(sourceTexture, path);
            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
            // AssetDatabase.ImportAsset(path, ImportAssetOptions.Default);
            sourceTexture = (Texture2D) AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));

            return sourceTexture;
        }

        public void OnBeforeSerialize()
        {
            mappedGradientStore = mappedGradients?
                .Where(x => x.Key && x.Value != null)
                .Select(x => new Map() {gradient = x.Value, material = x.Key})
                .ToList();
        }

        public void OnAfterDeserialize()
        {
            mappedGradients = mappedGradientStore?
                .Where(x => x.material && x.gradient != null)
                .ToDictionary(x => x.material, x => x.gradient);
        }
    }
}
