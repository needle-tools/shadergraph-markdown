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
        private const string DefaultTexturePropertyName = "_RampTexture"; 
        public string texturePropertyName = DefaultTexturePropertyName;

        [System.Serializable]
        private class Map
        {
            public Material material;
            public string propertyName = "_RampTexture";
            public Gradient gradient;
        }

        [HideInInspector] [SerializeField] private List<Map> mappedGradientStore = new List<Map>();
        private Dictionary<Material, Dictionary<string, Gradient>> mappedGradients;

        public override void OnDrawerGUI(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            var targetProperty = parameters.Get(0, properties);
            var targetPropertyName = texturePropertyName;
            if(targetProperty != null && targetProperty.type == MaterialProperty.PropType.Texture)
            {
                targetPropertyName = targetProperty.name;
                if (string.IsNullOrEmpty(targetPropertyName)) targetPropertyName = texturePropertyName;
            }
            var targetMat = materialEditor.target as Material;
            if (!targetMat) return;

            if (mappedGradients == null)
                mappedGradients = new Dictionary<Material, Dictionary<string, Gradient>>();
            
            if (!mappedGradients.ContainsKey(targetMat))
            {
                mappedGradients.Add(targetMat, new Dictionary<string, Gradient>());
                EditorUtility.SetDirty(this);
            }

            if (!mappedGradients[targetMat].ContainsKey(targetPropertyName))
                mappedGradients[targetMat].Add(targetPropertyName, new Gradient());

            EditorGUILayout.BeginHorizontal();
            var displayName = targetProperty != null ? targetProperty.displayName : "Ramp";
            mappedGradients[targetMat][targetPropertyName] = EditorGUILayout.GradientField(displayName, mappedGradients[targetMat][targetPropertyName]);
            
            if (GUILayout.Button("Apply")) ApplyRampTexture(targetMat, parameters.Get(0, targetPropertyName));
            EditorGUILayout.EndHorizontal();
        }

        public override IEnumerable<MaterialProperty> GetReferencedProperties(MaterialEditor materialEditor, MaterialProperty[] properties, DrawerParameters parameters)
        {
            var textureProperty = parameters.Get(0, properties);
            if (textureProperty != null && textureProperty.type == MaterialProperty.PropType.Texture)
                yield return textureProperty;
        }

        private void ApplyRampTexture(Material targetMat, string propertyName)
        {
            targetMat.SetTexture(propertyName, CreateGradientTexture(targetMat, mappedGradients[targetMat][propertyName], propertyName));
        }

        private static int width = 256;
        private static int height = 4; // needs to be multiple of 4 for DXT1 format compression

        private static Texture2D CreateGradientTexture(Material targetMaterial, Gradient gradient, string propertyName)
        {
            Texture2D gradientTexture = new Texture2D(width, height, TextureFormat.ARGB32, false, false)
            {
                name = propertyName + "_Gradient",
                filterMode = FilterMode.Bilinear,
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

            targetFolder += "Gradient Textures/";

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                AssetDatabase.Refresh();
            }

            string path = targetFolder + targetMaterial.name + sourceTexture.name + ".png";
            File.WriteAllBytes(path, sourceTexture.EncodeToPNG());

            // AssetDatabase.CreateAsset(sourceTexture, path);
            // AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            sourceTexture = (Texture2D) AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));

            return sourceTexture;
        }

        public void OnBeforeSerialize()
        {
            if (mappedGradients == null || !mappedGradients.Any())
            {
                mappedGradientStore = new List<Map>();
                return;
            }
            
            var store = new List<Map>();
            foreach (var x in mappedGradients)
            {
                foreach (var y in x.Value)
                {
                    store.Add(new Map()
                    {
                        material = x.Key,
                        propertyName = y.Key,
                        gradient = y.Value,
                    });
                }
            }

            mappedGradientStore = store;
        }

        public void OnAfterDeserialize()
        {
            if (mappedGradients == null)
                mappedGradients = new Dictionary<Material, Dictionary<string, Gradient>>();
            
            foreach (var entry in mappedGradientStore)
            {
                if (!mappedGradients.ContainsKey(entry.material))
                    mappedGradients.Add(entry.material, new Dictionary<string, Gradient>());
                
                if (!mappedGradients[entry.material].ContainsKey(entry.propertyName))
                    mappedGradients[entry.material].Add(entry.propertyName, entry.gradient);
            }
        }
    }
}
