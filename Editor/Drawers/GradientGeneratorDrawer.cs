using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.ShaderGraphMarkdown
{
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

            // already cached
            bool gradientWasFound = mappedGradients.ContainsKey(targetMat) && mappedGradients[targetMat].ContainsKey(targetPropertyName);

            void AddToCache(Gradient gradient)
            {
                if(!mappedGradients.ContainsKey(targetMat)) mappedGradients.Add(targetMat, new Dictionary<string, Gradient>());
                mappedGradients[targetMat][targetPropertyName] = gradient;
                gradientWasFound = true;
            }
            
            // check for user data in importer
            if (!gradientWasFound && targetProperty?.textureValue is Texture2D tex)
            {
                if (AssetDatabase.Contains(tex))
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
                    var userData = importer.userData;
                    if (!string.IsNullOrEmpty(userData))
                    {
                        var metaData = new GradientUserData();
                        JsonUtility.FromJsonOverwrite(userData, metaData);
                        if (metaData.isSet && metaData.gradient != null)
                            AddToCache(metaData.gradient);
                    } 
                }
            }
            
            // fallback: generate gradient from texture
            if (!gradientWasFound && targetProperty?.textureValue is Texture2D texture)
            {
                var gradient = GenerateFromTexture(texture);
                if(gradient != null)
                    AddToCache(gradient);
            }
            
            EditorGUILayout.BeginHorizontal();
            var displayName = targetProperty != null ? targetProperty.displayName : "Ramp";
            
            EditorGUI.BeginChangeCheck();
            var existingGradient = gradientWasFound ? mappedGradients[targetMat][targetPropertyName] : new Gradient();
            var newGradient = EditorGUILayout.GradientField(displayName, existingGradient);
            if (EditorGUI.EndChangeCheck())
            {
                Debug.Log("Changed Gradient");
                Undo.RecordObject(this, "Changed Gradient");
                mappedGradients[targetMat][targetPropertyName] = newGradient;
                // ApplyRampTexture(targetMat, parameters.Get(0, targetPropertyName)); // immediately apply gradient - experimental
                OnBeforeSerialize();
                Undo.FlushUndoRecordObjects();
                // EditorUtility.SetDirty(this);
            }

            var placeholderContent = new GUIContent(targetProperty?.textureValue ? "ApplyMM" : "CreateMM");
            var buttonRect = GUILayoutUtility.GetRect(placeholderContent, GUI.skin.button);
            // draw texture picker next to button
            if(targetProperty != null && buttonRect.width > 46 + 18)
            {
                var controlRect = buttonRect;
                controlRect.height = 16;
                controlRect.y += 2;
                controlRect.xMin = controlRect.xMax - 33;
                var newObj = (Texture2D) EditorGUI.ObjectField(controlRect, targetProperty.textureValue, typeof(Texture2D), true);
                if(newObj != targetProperty.textureValue)
                {
                    Undo.RecordObjects(new Object[] { this, targetMat }, "Applied Gradient");
                    targetProperty.textureValue = newObj;
                    mappedGradients[targetMat].Remove(targetPropertyName);
                    GUIUtility.ExitGUI(); // reset GUI loop so the gradient can be picked up properly
                }
                buttonRect.xMax -= 18;
            }
            if (GUI.Button(buttonRect, targetProperty?.textureValue ? "Apply" : "Create")) 
                ApplyRampTexture(targetMat, parameters.Get(0, targetPropertyName));
            
            EditorGUILayout.EndHorizontal();
            
            // show gradient fallback when no gradient was found - 
            // this shouldn't happen, ever, since we're generating the gradient from the texture
            if (!gradientWasFound && targetProperty != null)
            {
                if(targetProperty.textureValue)
                {
                    var rect = EditorGUILayout.GetControlRect(true, 8);
                    rect.xMin += EditorGUIUtility.labelWidth + 3;
                    rect.height -= 2;
                    var existingTexture = targetProperty.textureValue;
                    GUI.DrawTexture(rect, existingTexture);
                }
                else
                {
                    EditorGUILayout.LabelField(" ", "None Assigned", EditorStyles.miniLabel);
                }
            }
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

        private Gradient GenerateFromTexture(Texture2D texture)
        {
            try
            {
                // sample texture in 8 places and make a gradient from that as fallback.
                var rt = new RenderTexture(texture.width, texture.height, 0);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex2 = new Texture2D(texture.width, texture.height);
                Graphics.Blit(texture, rt);
                tex2.wrapMode = TextureWrapMode.Clamp;
                tex2.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                tex2.Apply();
                RenderTexture.active = prev;
                rt.Release();

                var grad = new Gradient();
                var colorKeys = new GradientColorKey[8];
                var alphaKeys = new GradientAlphaKey[8];
                
                for (int i = 0; i < 8; i++)
                {
                    var x = i / (8.0f - 1);
                    var color = tex2.GetPixelBilinear(x, 0.5f, 0);
                    colorKeys[i] = new GradientColorKey(color, x);
                    alphaKeys[i] = new GradientAlphaKey(color.a, x);
                }

                grad.SetKeys(colorKeys, alphaKeys);
                return grad;
            }
            catch
            {
                return null;
            }
        }

        private static int width = 256;
        private static int height = 4; // needs to be multiple of 4 for DXT1 format compression

        [System.Serializable]
        private class GradientUserData
        {
            public Gradient gradient;
            public bool isSet;
        }
        
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
            
            // store gradient meta-info as user data
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(gradientTexture));
            importer.userData = JsonUtility.ToJson(new GradientUserData() { gradient = gradient, isSet = true }, false);
            EditorUtility.SetDirty(gradientTexture);
            importer.SaveAndReimport();
                
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
