using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Needle.ShaderGraphMarkdown
{
    public class ShaderRefactoringWindow : EditorWindow
    {
        public ShaderRefactoringData data;

        [MenuItem("Window/Needle/Refactor Shader Properties")]
        private static void ShowWindow()
        {
            Show("", "");
        }

        private static Texture2D iconHelp;
        private void ShowButton(Rect rect)
        {
            if(!iconHelp) iconHelp = EditorGUIUtility.IconContent("icons/_help" + (EditorGUIUtility.pixelsPerPoint > 1.0 ? "@2x" : "") + ".png").image as Texture2D;
            if (GUI.Button(rect, new GUIContent(iconHelp), "IconButton"))
                EditorUtility.OpenWithDefaultApp(MarkdownShaderGUI.PropertyRefactorDocumentationUrl);
        }
        
        [MenuItem("CONTEXT/Material/Refactor Shader Properties", false, 701)]
        [MenuItem("CONTEXT/Shader/Refactor Shader Properties", false, 701)]
        private static void ShowFromContextMenu(MenuCommand command)
        {
            // TODO can we figure out which material property is currently selected?
            // Debug.Log("Keyboard Control: " + GUIUtility.keyboardControl);
            
            // TODO can we add a context menu to each property?
            if(command.context is Material mat0)
                Show(AssetDatabase.GetAssetPath(mat0.shader), "");
            if(command.context is Shader shader0)
                Show(AssetDatabase.GetAssetPath(shader0), "");
            if (Selection.activeObject is Material material1)
                Show(AssetDatabase.GetAssetPath(material1.shader), "");
            if (Selection.activeObject is Shader shader1)
                Show(AssetDatabase.GetAssetPath(shader1), "");
        }
        
        public static void Show(string shaderAssetPath, string inputReferenceName)
        {
            var wnd = GetWindow<ShaderRefactoringWindow>();
            wnd.data = new ShaderRefactoringData()
            {
                shaderPath = shaderAssetPath,
                shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderAssetPath),
                sourceReferenceName = inputReferenceName,
                targetReferenceName = inputReferenceName,
            };
            wnd.minSize = new Vector2(800, 200);
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

        class StringPropertyFieldWithDropdown : VisualElement
        {
            public StringPropertyFieldWithDropdown(ShaderRefactoringWindow window, SerializedObject so, SerializedProperty prop, string label, Action dropdownEvent)
            {
                var data = window.data;
                style.flexDirection = FlexDirection.Row;
                var refactorFrom = new PropertyField(prop, label) { style = { flexGrow = 1, fontSize = 16 } }; 
                refactorFrom.Bind(so);
                Add(refactorFrom);
                var btn = new Button(dropdownEvent) { tooltip = "Select Property", style = { paddingLeft = 1, paddingRight = 1 } };
                var btnContent = new VisualElement();
                btnContent.AddToClassList("unity-base-popup-field__arrow");
                btn.Add(btnContent);
                this.Add(btn);
            }
        }        
        
        private VisualElement popupFieldContainer;
        private SerializedObject so;

        private static readonly List<(string referenceName, string displayName)> commonNamePairs = new List<(string, string)>()
        {
            ("BiRP/_MainTex", "Albedo"),
            ("BiRP/_BumpMap", "Normal Map"),
            ("BiRP/_Color", "Color"),
            ("URP/_BaseMap", "Albedo"),
            ("URP/_BumpMap", "Normal Map"),
            ("URP/_BaseColor", "Color"),
            ("HDRP/_BaseColorMap", "Albedo"),
            ("HDRP/_BumpMap", "Normal Map"),
            ("HDRP/_BaseColor", "Color"),
            ("_Smoothness", "Smoothness"),
            ("_Metallic", "Metallic"),
            ("_Cutoff", "Alpha Cutoff"),
        };
        
        private void CreateGUI()
        {
            titleContent = new GUIContent("Refactor Shader Properties");
            var splitter = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            var left = new VisualElement() { style = { width = Length.Percent(50), marginRight = 20 } };
            var right = new VisualElement() { style = { width = Length.Percent(50), marginLeft = 20  } };
            var center = new VisualElement() { style = { position = Position.Absolute, left = Length.Percent(50), top = 22, fontSize = 16, marginLeft = -8 } };
            center.Add(new Label("→"));
            rootVisualElement.Add(splitter);
            rootVisualElement.Add(center);
            splitter.Add(left);
            splitter.Add(right);
            
            if(so == null || so.targetObject != this)
                so = new SerializedObject(this);
            so.Update();
            var prop = so.FindProperty(nameof(data));
            var propField = new PropertyField(prop.FindPropertyRelative(nameof(ShaderRefactoringData.shader)));
// #if UNITY_2020_2_OR_NEWER
//             propField.RegisterValueChangeCallback(evt => UpdatePopupField());
// #else
//             propField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt => UpdatePopupField());
// #endif
            propField.Bind(so);
            left.Add(propField);
            
            // refactorArea.Add(popupFieldContainer);
            left.Add(new StringPropertyFieldWithDropdown(this, so, prop.FindPropertyRelative(nameof(ShaderRefactoringData.sourceReferenceName)), "Source Property",(() =>
            {
                var properties = new List<(string referenceName, string displayName)>();
                if(data.shader)
                {
                    var propertyCount = data.shader.GetPropertyCount();
                    for (int i = 0; i < propertyCount; i++)
                    {
                        if(ShaderUtil.IsShaderPropertyHidden(data.shader, i)) continue;
                        properties.Add((data.shader.GetPropertyName(i), data.shader.GetPropertyDescription(i)));
                    }
                }

                var menu = new GenericMenu();
                foreach(var tuple in properties)
                {
                    menu.AddItem(new GUIContent(tuple.referenceName + " (" + tuple.displayName + ")"), tuple.referenceName == data.sourceReferenceName, o =>
                    {
                        var t = o as string;
                        data.sourceReferenceName = t;
                    }, tuple.referenceName);
                }
                menu.ShowAsContext();
            })));
            
            var refactorTo = new StringPropertyFieldWithDropdown(this, so, prop.FindPropertyRelative(nameof(ShaderRefactoringData.targetReferenceName)), "Replace With", () =>
            {
                var shaderInfo = ShaderUtil.GetAllShaderInfo();
                List<(string referenceName, string displayName)> properties = shaderInfo.SelectMany(x =>
                {
                    var shader = Shader.Find(x.name);
                    var propertyCount = ShaderUtil.GetPropertyCount(shader);
                    return Enumerable.Range(0, propertyCount)
                        .Where(idx => !ShaderUtil.IsShaderPropertyHidden(shader, idx))
                        .Select(idx => (ShaderUtil.GetPropertyName(shader, idx), ShaderUtil.GetPropertyDescription(shader, idx)));
                })
                    .ToLookup(x => x.Item1)
                    .OrderByDescending(x => x.Count())
                    .Select(x => (x.Key, x.Count().ToString()))
                    .ToList();
                
                const int ItemsInMainSection = 25;
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("Common Names"), false, null);
                foreach (var tuple in commonNamePairs)
                {
                    menu.AddItem(new GUIContent(tuple.referenceName + " (" + tuple.displayName + ")"), tuple.referenceName == data.targetReferenceName, o =>
                    {
                        var t = o as string;
                        data.targetReferenceName = t;
                    }, tuple.referenceName);
                }
                
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Most Used Properties"), false, null);
                foreach(var tuple in properties.Take(ItemsInMainSection))
                {
                    menu.AddItem(new GUIContent(tuple.referenceName + "\t(" + tuple.displayName + ")"), tuple.referenceName == data.targetReferenceName, o =>
                    {
                        var t = o as string;
                        data.targetReferenceName = t;
                    }, tuple.referenceName);
                }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("All Properties"), false, null);
                foreach (var tuple in properties.Select(x => (char.ToUpperInvariant(x.referenceName.TrimStart('_').FirstOrDefault()),x)).OrderBy(x => x.Item1))
                {
                    menu.AddItem(new GUIContent("Alphabetic/" + tuple.Item1 + "/" + tuple.x.referenceName + "\t(" + tuple.x.displayName + ")"), tuple.x.referenceName == data.targetReferenceName, o =>
                    {
                        var t = o as string;
                        data.targetReferenceName = t;
                    }, tuple.x.referenceName);
                }
                menu.ShowAsContext();
            });

            right.Add(new VisualElement() { style = { height = 20 }});
            right.Add(refactorTo);

            so.ApplyModifiedProperties();
            
            right.Add(new Label("Replace") { style = { marginTop = 10, unityFontStyleAndWeight = FontStyle.Bold }});
            right.Add(new Button(FixMaterialsAndShaders) { text = "Replace in materials and shaders" });
            right.Add(new Button(FixAnimationClips) { text = "Replace in animations" });

            left.Add(new Label("Find") { style = { marginTop = 10, unityFontStyleAndWeight = FontStyle.Bold }});
            left.Add(new Button(() =>
            {
                
            }) { text = "Find Usages"});
            left.Add(new Button(FindMaterials) { text = "Find materials using this property" });
            left.Add(new Button(FindAnimationClips) { text = "Find animations targeting this property" });
            left.Add(new Button(FindScripts) { text = "Find scripts targeting this property" });
        }

        private void FindAnimationClips()
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
        }

        private void FindScripts()
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
        }
        
        private void FindMaterials()
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
        }

        private void FixMaterialsAndShaders()
        {
            if (string.IsNullOrWhiteSpace(data.sourceReferenceName))
            {
                Debug.LogError("Can't refactor: source reference name is empty. Please select a source property.");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(data.targetReferenceName))
            {
                Debug.LogError("Can't refactor: target reference name is empty. Please set a new reference name for " + data.sourceReferenceName);
                return;
            }
            
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
                    (selectedShadersNeedUpdating ? "This property exists in this shader that is selected for updating:" + ObjectNames(shadersThatNeedUpdating) : "This property doesn't exist in the selected shader(s).") +
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
            var materialsThatNeedUpdating = GetAllAssets<Material>()
                .Where(x => x.shader && shadersThatNeedUpdating.Contains(x.shader))
                .ToList();

            var materialsThatCannotBeUpdated = materialsThatNeedUpdating.Where(x => !AssetDatabase.IsMainAsset(x)).ToList();
            var materialsThatCanBeUpdated = materialsThatNeedUpdating.Except(materialsThatCannotBeUpdated).ToList();

            if (materialsThatCannotBeUpdated.Any())
            {
                Debug.LogWarning($"Some materials couldn't be updated since they are sub assets [{materialsThatCannotBeUpdated.Count}]: {ObjectNames(materialsThatCannotBeUpdated, false, false, true)}");
            }
            
            if(!shadersThatNeedUpdating.Any() && !materialsThatCanBeUpdated.Any())
            {
                Debug.Log("Property hasn't been found in any shaders and materials. No changes necessary.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Refactor shader property " + data.sourceReferenceName + " → " + data.targetReferenceName,
                "Shaders that will be changed [" + shadersThatNeedUpdating.Count + "]:" + ObjectNames(shadersThatNeedUpdating) + "\n\n" +
                "Materials that will be changed [" + materialsThatCanBeUpdated.Count + "]:" + ObjectNames(materialsThatCanBeUpdated),
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
                // The general case is more complex! We'd have to parse the file properly and check if something is "_VarName=" or "_VarName2" (the former would be replaced, the latter is a separate field)
                text = text.Replace($"\"{data.sourceReferenceName}\"", $"\"{data.targetReferenceName}\"");
                File.WriteAllText(path, text);
            }

            foreach (var mat in materialsThatCanBeUpdated)
            {
                var path = AssetDatabase.GetAssetPath(mat);
                var text = File.ReadAllText(path);
                // we're directly operating on the serialized YAML here, what could possibly go wrong
                text = text.Replace("- " + data.sourceReferenceName + ":", "- " + data.targetReferenceName + ":");
                File.WriteAllText(path, text);
            }
            
            AssetDatabase.Refresh();
            // UpdatePopupField();
        }

        private void FixAnimationClips()
        {
            
        }
        
        private static string ObjectNames<T>(IReadOnlyCollection<T> objects, bool singleLine = false, bool returnShortStringIfTooManyElements = true, bool groupByName = false) where T : UnityEngine.Object
        {
            var strings = (groupByName ? objects.ToLookup(x => x.name).Select(x => $"{x.Key} [{x.Count()}]") : objects.Select(x => x.name)).OrderBy(x => x).ToList();
            
            if (returnShortStringIfTooManyElements && strings.Count > 150)
                return singleLine ? " " : "\n" + "(too many items to display: " + strings.Count + ")";
            
            const int MaxPrettyObjectsCount = 40;
            var makeSingleLineBecauseOfTooManyElements = returnShortStringIfTooManyElements && strings.Count > MaxPrettyObjectsCount;
            var firstSeparator = singleLine ? "" : makeSingleLineBecauseOfTooManyElements ? "\n" : "\n  · ";
            var separator = singleLine || makeSingleLineBecauseOfTooManyElements ? ", " : "\n  · ";
            return firstSeparator + string.Join(separator, strings);
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