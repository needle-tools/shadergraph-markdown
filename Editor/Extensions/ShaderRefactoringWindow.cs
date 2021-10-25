using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.ShaderGraphMarkdown
{
    public class ShaderRefactoringWindow : EditorWindow
    {
        public ShaderRefactoringData data;
        
        public static void Show(string shaderAssetPath, string inputReferenceName)
        {
            var wnd = CreateWindow<ShaderRefactoringWindow>();
            wnd.data = new ShaderRefactoringData()
            {
                shaderPath = shaderAssetPath,
                shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderAssetPath),
                sourceReferenceName = inputReferenceName,
                targetReferenceName = inputReferenceName,
            };
            
            wnd.Show();
        }

        private static List<T> GetAllAssets<T>() where T:UnityEngine.Object
        {
            return AssetDatabase
                .FindAssets("t:" + typeof(T).Name)
                .Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .Where(x => x && x.GetType() == typeof(T))
                .Select(x => (T) x)
                .ToList();
        }
        
        private void CreateGUI()
        {
            titleContent = new GUIContent("Shader Refactor");
            
            var so = new SerializedObject(this);
            so.Update();
            var prop = so.FindProperty(nameof(data));

            var propField = new PropertyField(prop.FindPropertyRelative(nameof(ShaderRefactoringData.shader))); propField.Bind(so);
            rootVisualElement.Add(propField);
            var refactorFrom = new PropertyField(prop.FindPropertyRelative(nameof(ShaderRefactoringData.sourceReferenceName))); refactorFrom.Bind(so);
            rootVisualElement.Add(refactorFrom);
            var refactorTo = new PropertyField(prop.FindPropertyRelative(nameof(ShaderRefactoringData.targetReferenceName))); refactorTo.Bind(so);
            rootVisualElement.Add(refactorTo);

            so.ApplyModifiedProperties();
            
            rootVisualElement.Add(new Button(() =>
            {
                var allShaders = GetAllAssets<Shader>();
                var shadersThatNeedUpdating = new List<Shader>();
                var shadersThatWouldNeedUpdatingButAreExcluded = new List<Shader>();
                var i = 0;
                var count = allShaders.Count;
                foreach (var shader in allShaders)
                {
                    i++;
                    EditorUtility.DisplayProgressBar("Parsing Shader", "Shader " + i + "/" + count + ": " + shader.name, (float)i / count);
                    if(!shader) continue;
                    var propertyIndex = shader.FindPropertyIndex(data.sourceReferenceName);
                    if (propertyIndex >= 0)
                    {
                        // if the shader field is set, we want to explicitly only upgrade that shader;
                        // if the shader field isn't set, we want to upgrade all shaders.
                        var shouldUpdateThisShader = !data.shader || shader == data.shader;
                        if (shouldUpdateThisShader)
                        {
                            Debug.Log($"Shader has property {data.sourceReferenceName} and will be updated: {shader.name}", shader);
                            shadersThatNeedUpdating.Add(shader);                            
                        }
                        else
                        {
                            Debug.LogWarning($"Shader has property {data.sourceReferenceName} but will NOT be updated: {shader.name}. Clear the shader field if you want to update all shaders with this property.", shader);
                            shadersThatWouldNeedUpdatingButAreExcluded.Add(shader);
                        }
                    }
                }
                EditorUtility.ClearProgressBar();

                if(!shadersThatNeedUpdating.Any() && !shadersThatWouldNeedUpdatingButAreExcluded.Any())
                {
                    Debug.Log($"Property {data.sourceReferenceName} hasn't been found in any shaders. No changes necessary.");
                    return;
                }

                if (shadersThatWouldNeedUpdatingButAreExcluded.Any())
                {
                    var selectedShadersNeedUpdating = shadersThatNeedUpdating.Any();
                    switch (EditorUtility.DisplayDialogComplex(
                        "Property " + data.sourceReferenceName + " also exists in other shaders!",
                        selectedShadersNeedUpdating ? "This property exists in this shader that is selected for updating:" + ObjectNames(shadersThatNeedUpdating) : "This property doesn't exist in the selected shader(s)." +
                        "\n\n" +
                        "but also exists in these shaders that won't be updated because they're not selected:" + ObjectNames(shadersThatWouldNeedUpdatingButAreExcluded) +
                        ".\n\n" +
                        "Are you sure you want to update just these shaders? Press \"Update All\", or Cancel and empty the \"Shader\" field to update all shaders.",
                        selectedShadersNeedUpdating ? "Update only " + ObjectNames(shadersThatNeedUpdating, true) : "Do nothing",
                        "Cancel", "Update all " + (shadersThatNeedUpdating.Count + shadersThatWouldNeedUpdatingButAreExcluded.Count) + " shaders"))
                    {
                        case 0:
                            break;
                        case 1:
                            return;
                        case 2:
                            shadersThatNeedUpdating.AddRange(shadersThatWouldNeedUpdatingButAreExcluded);
                            shadersThatWouldNeedUpdatingButAreExcluded.Clear();
                            break;
                    }
                }

                // find all materials that are using any of these shaders
                var materialsThatNeedUpdating = GetAllAssets<Material>().Where(x => x.shader && shadersThatNeedUpdating.Contains(x.shader)).ToList();
                
                if(!shadersThatNeedUpdating.Any() && !materialsThatNeedUpdating.Any())
                {
                    Debug.Log("Property hasn't been found in any shaders and materials. No changes necessary.");
                    return;
                }

                if (!EditorUtility.DisplayDialog(
                    "Refactor shader property " + data.sourceReferenceName + " → " + data.targetReferenceName,
                    "Shaders that will be changed [" + shadersThatNeedUpdating.Count + "]:" + ObjectNames(shadersThatNeedUpdating) + "\n\n" +
                    "Materials that will be changed [" + materialsThatNeedUpdating.Count + "]:" + ObjectNames(materialsThatNeedUpdating),
                    "OK",
                    "Cancel")) return;
                
                foreach (var shader in shadersThatNeedUpdating)
                {
                    // upgrade file
                    var path = AssetDatabase.GetAssetPath(shader);
                    if (!path.EndsWith(".shadergraph", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.LogWarning("Only .shadergraph files can be auto-updated right now. Please update the file manually: " + path, shader);
                        continue;
                    }
                    var text = File.ReadAllText(path);
                    // TODO this will only properly work for ShaderGraph files right now, as these have "" around properties.
                    // The general case is more complex! We'd have to parse the file properly and check if something is "_Dissolve=" or "_Dissolve2" (the former would be replaced, the latter is a separate field)
                    text = text.Replace($"\"{data.sourceReferenceName}\"", $"\"{data.targetReferenceName}\"");
                    File.WriteAllText(path, text);
                }

                foreach (var mat in materialsThatNeedUpdating)
                {
                    var path = AssetDatabase.GetAssetPath(mat);
                    var text = File.ReadAllText(path);
                    // we're directly operating on the serialized YAML here, what could possibly go wrong
                    text = text.Replace("- " + data.sourceReferenceName + ":", "- " + data.targetReferenceName + ":");
                    File.WriteAllText(path, text);
                }
                
                AssetDatabase.Refresh();
            }) { text = "Find and update materials and shaders using this property" });

            rootVisualElement.Add(new Label("Helpers") { style = { marginTop = 10, unityFontStyleAndWeight = FontStyle.Bold }});
            rootVisualElement.Add(new Button(() =>
            {
                var allMaterials = GetAllAssets<Material>();
                var i = 0;
                var count = allMaterials.Count;
                foreach (var material in allMaterials)
                {
                    i++;
                    EditorUtility.DisplayProgressBar("Parsing Material and Shader", "Material " + i + "/" + count + ": " + material.name, (float)i / count);
                    if(!material) continue;
                    var shader = material.shader;
                    if(!shader) continue;
                    var propertyIndex = shader.FindPropertyIndex(data.sourceReferenceName);
                    if (propertyIndex >= 0)
                    {
                        Debug.Log($"Material uses shader with property {data.sourceReferenceName}: {material.name} ({shader.name})", material);
                    }
                }
                EditorUtility.ClearProgressBar();
            }) { text = "Find materials using this property" });
            
            rootVisualElement.Add(new Button(() =>
            {
                var clipsThatNeedUpdating = new List<AnimationClip>();
                var allClips = GetAllAssets<AnimationClip>();
                var i = 0;
                var count = allClips.Count;
                foreach (var clip in allClips)
                {
                    i++;
                    EditorUtility.DisplayProgressBar("Parsing AnimationClips", "Clip " + i + "/" + count + ": " + clip.name, (float)i / count);
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (binding.propertyName == "material." + data.sourceReferenceName) // TODO what if the animated material is not at index 0?
                        {
                            if(clipsThatNeedUpdating.Contains(clip))
                                continue;
                            
                            clipsThatNeedUpdating.Add(clip);
                            Debug.Log($"AnimationClip targets this shader property: {clip.name} ({binding.path})", clip);
                        }
                    }   
                }
                EditorUtility.ClearProgressBar();
            }) { text = "Find animations targeting this property" });
            
            rootVisualElement.Add(new Button(() =>
            {
                // find all scripts
                var scripts = GetAllAssets<MonoScript>();
                var i = 0;
                var count = scripts.Count;
                foreach (var script in scripts)
                {
                    i++;
                    if(!AssetDatabase.IsOpenForEdit(script)) continue;
                    EditorUtility.DisplayProgressBar("Parsing Scripts", "Script " + i + "/" + count + ": " + script.name, (float)i / count);
                    var fullText = File.ReadAllLines(AssetDatabase.GetAssetPath(script));
                    for(int line = 0; line < fullText.Length; line++)
                    {
                        var text = fullText[line];
                        if (text.Contains("\"" + data.sourceReferenceName + "\""))
                        {
                            Debug.Log($"Script contains reference to this property: {script.name} at line {line}", script);                        
                        }
                        else if (text.Contains(data.sourceReferenceName))
                        {
                            // Debug.Log($"Script may contain reference to this property: ) (at {Path.GetFullPath(AssetDatabase.GetAssetPath(script)).Replace("\\","/")}:{line})", script);
                            Debug.Log($"Script may contain reference to this property: <a href=\"{AssetDatabase.GetAssetPath(script)}:{line}\">{AssetDatabase.GetAssetPath(script)}:{line}</a>", script);
                        }
                    }
                }
                EditorUtility.ClearProgressBar();
            }) { text = "Find scripts targeting this property" });
        }
                
        private static string ObjectNames<T>(IReadOnlyCollection<T> objects, bool singleLine = false) where T:UnityEngine.Object
        {
            const int MaxPrettyObjectsCount = 40;
            var firstSeparator = singleLine ? "" : objects.Count > MaxPrettyObjectsCount ? "\n" : "\n  · ";
            var separator = singleLine || objects.Count > MaxPrettyObjectsCount ? ", " : "\n  · ";
            return firstSeparator + string.Join(separator, objects.Select(x => x.name));
        }
    }

    [Serializable]
    public class ShaderRefactoringData
    {
        public string shaderPath;
        public Shader shader;
        public string sourceReferenceName;
        public string targetReferenceName;
    }
}