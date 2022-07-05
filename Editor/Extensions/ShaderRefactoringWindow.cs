#if UNITY_2020_3_OR_NEWER
#define HAVE_UITOOLKIT_HELPBOX
#endif

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
                refactoringData = new List<ShaderPropertyRefactoringData>()
                {
                    new ShaderPropertyRefactoringData()
                    {
                        sourceReferenceName = inputReferenceName, 
                        targetReferenceName = inputReferenceName
                    }
                },
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
            public StringPropertyFieldWithDropdown(ShaderRefactoringWindow window, SerializedObject so, SerializedProperty prop, string label, Action dropdownEvent, Action changeEvent)
            {
                style.flexDirection = FlexDirection.Row;
                var refactorFrom = new PropertyField(prop, label) { style = { flexGrow = 1 } };
#if UNITY_2020_2_OR_NEWER
                refactorFrom.RegisterValueChangeCallback(evt => changeEvent?.Invoke());
#else
                refactorFrom.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt => changeEvent?.Invoke());
#endif
                refactorFrom.Bind(so);
                Add(refactorFrom);
                var btn = new Button(dropdownEvent) { tooltip = "Select Property", style = { paddingLeft = 1, paddingRight = 1 } };
                var btnContent = new VisualElement();
                btnContent.AddToClassList("unity-base-popup-field__arrow");
                btn.Add(btnContent);
                Add(btn);
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

        private void AddPropertyItem()
        {
            
        }
        
        private void CreateGUI()
        {
            titleContent = new GUIContent("Refactor Shader Properties");
            var splitter = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            var left = new VisualElement() { style = { width = Length.Percent(50), marginRight = 20 } };
            var right = new VisualElement() { style = { width = Length.Percent(50), marginLeft = 20  } };
            
            var rightActionsContainer = new VisualElement();
            var leftActionsContainer = new VisualElement();
            
            if (so == null || so.targetObject != this)
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
            var splitter2 = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            splitter2.Add(propField);
            
            // List implementation
            var list = new ListView(data.refactoringData, 24, 
            makeItem: () =>
            {
                var splitter3 = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                var left2 = new VisualElement() { style = { width = Length.Percent(50), marginRight = 20 }, name = "LeftSection" };
                var right2 = new VisualElement() { style = { width = Length.Percent(50), marginLeft = 20  }, name = "RightSection"  };
                // left.Add(new TextField() { name = "From" });
                // right.Add(new TextField() { name = "To" });
                splitter3.Add(left2);
                splitter3.Add(right2);
                return splitter3;
            }, 
            bindItem: (element, i) =>
            {
                while (data.refactoringData.Count < i)
                    data.refactoringData.Add(new ShaderPropertyRefactoringData());
                
                var el = data.refactoringData[i];
                if (el == null)
                {
                    el = new ShaderPropertyRefactoringData() { };
                    data.refactoringData[i] = el;
                    so.Update();
                }
                var from = element.Q<VisualElement>("LeftSection");
                var to = element.Q<VisualElement>("RightSection");
                var p1 = prop.FindPropertyRelative(nameof(ShaderRefactoringData.refactoringData)).GetArrayElementAtIndex(i);
                
                var refactorFrom = new StringPropertyFieldWithDropdown(this, so, p1.FindPropertyRelative(nameof(ShaderPropertyRefactoringData.sourceReferenceName)), "Source Property",(() =>
                {
                    var properties = new List<(string referenceName, string displayName)>();
                    if(data.shader)
                    {
                        var propertyCount = data.shader.GetPropertyCount();
                        for (int j = 0; j < propertyCount; j++)
                        {
                            if(ShaderUtil.IsShaderPropertyHidden(data.shader, j)) continue;
                            properties.Add((data.shader.GetPropertyName(j), data.shader.GetPropertyDescription(j)));
                        }
                    }

                    var menu = new GenericMenu();

                    if (!data.shader)
                        menu.AddItem(new GUIContent("Set a Shader to pick properties from"), false, null);
                    else 
                        menu.AddItem(new GUIContent("Shader Properties"), false, null);
                
                    foreach(var tuple in properties)
                    {
                        menu.AddItem(new GUIContent(tuple.referenceName + " (" + tuple.displayName + ")"), tuple.referenceName == el.sourceReferenceName, o =>
                        {
                            var t = o as string;
                            el.sourceReferenceName = t;
                        }, tuple.referenceName);
                    }
                    menu.ShowAsContext();
                }), () =>
                {
                    leftActionsContainer.SetEnabled(
                        !string.IsNullOrWhiteSpace(el.sourceReferenceName) && el.sourceReferenceName.Length >= 2);
                });
                
                var refactorTo = new StringPropertyFieldWithDropdown(this, so, p1.FindPropertyRelative(nameof(ShaderPropertyRefactoringData.targetReferenceName)), "Replace With", () =>
                {
                    var shaderInfo = ShaderUtil.GetAllShaderInfo();
                    List<(string referenceName, string displayName)> properties = shaderInfo.SelectMany(x =>
                    {
                        // exclude hidden shaders?
                        if (x.name.StartsWith("Hidden/")) return Enumerable.Empty<(string, string)>();
                        
                        var shader = Shader.Find(x.name);
                        var propertyCount = ShaderUtil.GetPropertyCount(shader);
                        return Enumerable.Range(0, propertyCount)
                            .Where(idx => !ShaderUtil.IsShaderPropertyHidden(shader, idx))
                            // .Where(x =>
                            // {
                            //     if (ShaderUtil.GetPropertyName(shader, x) == "_A")
                            //     {
                            //         Debug.Log("Shader has property " + "_A" + ": " + shader, shader);
                            //     }
                            //     return true;
                            // })
                            .Select(idx => (ShaderUtil.GetPropertyName(shader, idx), ShaderUtil.GetPropertyDescription(shader, idx)));
                    })
                        .ToLookup(x => x.Item1)
                        .OrderByDescending(x => x.Count())
                        // .Where(x =>
                        // {
                        //     if(x.Count() > 10)
                        //         Debug.Log("Property " + x.Key + " found in " + string.Join("\n", x));
                        //     return true;
                        // })
                        .Select(x => (x.Key, x.Count().ToString()))
                        .ToList();
                    
                    const int ItemsInMainSection = 25;
                    var menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Common Names"), false, null);
                    foreach (var tuple in commonNamePairs)
                    {
                        menu.AddItem(new GUIContent(tuple.referenceName + " (" + tuple.displayName + ")"), tuple.referenceName == el.targetReferenceName, o =>
                        {
                            var t = o as string;
                            el.targetReferenceName = t;
                        }, tuple.referenceName);
                    }
                    
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Most Used Properties"), false, null);
                    foreach(var tuple in properties.Take(ItemsInMainSection))
                    {
                        menu.AddItem(new GUIContent(tuple.referenceName + "\t(" + tuple.displayName + ")"), tuple.referenceName == el.targetReferenceName, o =>
                        {
                            var t = o as string;
                            el.targetReferenceName = t;
                        }, tuple.referenceName);
                    }
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("All Properties"), false, null);
                    foreach (var tuple in properties.Select(x => (char.ToUpperInvariant(x.referenceName.TrimStart('_').FirstOrDefault()),x)).OrderBy(x => x.Item1))
                    {
                        menu.AddItem(new GUIContent("Alphabetic/" + tuple.Item1 + "/" + tuple.x.referenceName + "\t(" + tuple.x.displayName + ")"), tuple.x.referenceName == el.targetReferenceName, o =>
                        {
                            var t = o as string;
                            el.targetReferenceName = t;
                        }, tuple.x.referenceName);
                    }
                    menu.ShowAsContext();
                }, () =>
                {
                    rightActionsContainer.SetEnabled(
                        !string.IsNullOrWhiteSpace(el.sourceReferenceName) && el.sourceReferenceName.Length >= 2 &&
                        !string.IsNullOrWhiteSpace(el.targetReferenceName) && el.targetReferenceName.Length >= 2);
                });
                
                from.Clear();
                to.Clear();
                from.Add(refactorFrom);
                to.Add(refactorTo);
            }) { style = { flexShrink = 500, flexGrow = 1 }};
#if UNITY_2021_2_OR_NEWER
            list.showAddRemoveFooter = true;
            list.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            list.reorderMode = ListViewReorderMode.Simple;
            list.showBoundCollectionSize = true;
            list.showBorder = true;
#else
            var addRemove = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            addRemove.Add(new Button(() =>
            {
#if UNITY_2020_1_OR_NEWER
                var selected = list.selectedItems;
                foreach (ShaderPropertyRefactoringData sel in selected)
                    data.refactoringData.Remove(sel);
#else
                data.refactoringData.Remove(list.selectedItem as ShaderPropertyRefactoringData);
#endif
                so.Update();
                list.Bind(so);
                list.Refresh();
            }) { text = "-" });
            addRemove.Add(new Button(() =>
            {
                data.refactoringData.Add(new ShaderPropertyRefactoringData());
                so.Update();
                list.Bind(so);
                list.Refresh();
            }) { text = "+" });
#endif
            list.Bind(so);

            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(() =>
            {
                GUIUtility.systemCopyBuffer = string.Join("\n", data.refactoringData.Select(x => x.sourceReferenceName + ", " + x.targetReferenceName));
            }) { text = "Copy Property List to Clipboard" });
            toolbar.Add(new ToolbarButton(() =>
            {
                var str = GUIUtility.systemCopyBuffer;
                var lines = str.Split('\n');
                data.refactoringData.Clear();
                foreach (var t in lines)
                {
                    var parts = t.Split(new char[] { ',', ';' });
                    if (parts.Length != 2) continue;
                    data.refactoringData.Add(new ShaderPropertyRefactoringData() { sourceReferenceName = parts[0].Trim(), targetReferenceName = parts[1].Trim() });
                }
                so.Update();
#if UNITY_2021_2_OR_NEWER
                list.Rebuild();
#else
                list.Bind(so);
#endif
            }) { text = "Paste Property List from Clipboard", tooltip = "Plain text with comma separation: \"source, target\""});
            rootVisualElement.Add(toolbar);
            
            rootVisualElement.Add(splitter2);
            
            rootVisualElement.Add(list);
#if !UNITY_2021_2_OR_NEWER
            rootVisualElement.Add(addRemove);
#endif

            rootVisualElement.Add(splitter);
            splitter.Add(left);
            splitter.Add(right);
            
            so.ApplyModifiedProperties();

            rightActionsContainer.Add(new Label("Replace") { style = { marginTop = 10, marginLeft = 3, unityFontStyleAndWeight = FontStyle.Bold}});
            rightActionsContainer.Add(new Button(() =>
            {
                FixMaterialsAndShaders();
                FixAnimationClips();
                FixScripts();
            }) { text = "Replace all Usages", tooltip = "Replace in materials, shaders and animation clips", style = { height = 30 } });
            rightActionsContainer.Add(new Button(FixMaterialsAndShaders) { text = "Replace in materials and shaders" });
            rightActionsContainer.Add(new Button(FixAnimationClips) { text = "Replace in animations" });
            rightActionsContainer.Add(new Button(FixScripts) { text = "Replace in scripts" });
#if HAVE_UITOOLKIT_HELPBOX
            rightActionsContainer.Add(new HelpBox("Please make sure to have a backup before running these operations!", HelpBoxMessageType.Warning));
#endif
            right.Add(rightActionsContainer);

            leftActionsContainer.Add(new Label("Find") { style = { marginTop = 10, marginLeft = 3, unityFontStyleAndWeight = FontStyle.Bold }});
            leftActionsContainer.Add(new Button(() =>
            {
                FindMaterials();
                FindAnimationClips();
                FindScripts();
            }) { text = "Find all Usages", tooltip = "Find materials, animation clips and scripts using this property", style = { height = 30 } } );
            leftActionsContainer.Add(new Button(FindMaterials) { text = "Find materials and shaders using this property" });
            leftActionsContainer.Add(new Button(FindAnimationClips) { text = "Find animations targeting this property" });
            leftActionsContainer.Add(new Button(FindScripts) { text = "Find scripts targeting this property" });
#if HAVE_UITOOLKIT_HELPBOX
            leftActionsContainer.Add(new HelpBox("Non-Shadergraph shaders and scripts cannot be updated automatically. Please use the Find buttons and update them manually.", HelpBoxMessageType.None));
#endif
            left.Add(leftActionsContainer);
        }

        private void FindAnimationClips()
        {
            FindAnimationClips(data.refactoringData);
        }

        private void FindAnimationClips(List<ShaderPropertyRefactoringData> dat)
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
                    foreach (var data in dat)
                    {
                        if (binding.propertyName.StartsWith("material." + data.sourceReferenceName, StringComparison.Ordinal)) // TODO what if the animated material is not at index 0?
                        {
                            if(clipsThatNeedUpdating.Contains(clip))
                                continue;
                                
                            clipsThatNeedUpdating.Add(clip);
                            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, clip, $"AnimationClip targets this shader property: {clip.name} ({binding.path})");
                        }
                    }
                }   
            }
            EditorUtility.ClearProgressBar();
        }

        private void FindScripts()
        {
            foreach (var dat in data.refactoringData)
            {
                FindScripts(dat);
            }
        }

        private void FindScripts(ShaderPropertyRefactoringData data)
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
                    var scriptPath = $"<a href=\"{AssetDatabase.GetAssetPath(script)}:{line}\">{AssetDatabase.GetAssetPath(script)}:{line}</a>";
                    if (text.Contains("\"" + data.sourceReferenceName + "\""))
                    {
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, script, "{0}", "Script contains reference to this property: " + scriptPath);                        
                    }
                    else if (text.Contains(data.sourceReferenceName))
                    {
                        // Debug.Log($"Script may contain reference to this property: ) (at {Path.GetFullPath(AssetDatabase.GetAssetPath(script)).Replace("\\","/")}:{line})", script);
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, script, "{0}", "Script may contain reference to this property: " + scriptPath);
                    }
                }
            }
            EditorUtility.ClearProgressBar();
        }

        void FindMaterials()
        {
            foreach (var dat in data.refactoringData)
            {
                FindMaterials(dat);
            }
        }
        
        private void FindMaterials(ShaderPropertyRefactoringData data)
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
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, material, $"Material uses shader with property {data.sourceReferenceName}: {material.name} ({shader.name})");
                }
            }
            EditorUtility.ClearProgressBar();
        }

        private void FixMaterialsAndShaders()
        {
            FixMaterialsAndShaders(data.shader, data.refactoringData);
        }
        
        private void FixMaterialsAndShaders(Shader sourceShader, List<ShaderPropertyRefactoringData> propertyList)
        {
            var allShaders = GetAllAssets<Shader>();
            var shadersThatNeedUpdating = new List<Shader>();
            var shadersThatCannotBeUpdated = new List<Shader>();
            var shadersThatWouldNeedUpdatingButAreExcluded = new List<Shader>();
            var i = 0;
            var count = allShaders.Count;
            foreach (var shader in allShaders)
            {
                i++;
                EditorUtility.DisplayProgressBar("Parsing Shader", "Shader " + i + "/" + count + ": " + shader.name, (float)i / count);
                if(!shader) continue;
                
                foreach(var data in propertyList)
                {   
                    if (string.IsNullOrWhiteSpace(data.sourceReferenceName))
                    {
                        // Debug.LogWarning("Can't refactor: source reference name is empty. Please select a source property.");
                        continue;
                    }
            
                    if (string.IsNullOrWhiteSpace(data.targetReferenceName))
                    {
                        // Debug.LogWarning("Can't refactor: target reference name is empty. Please set a new reference name for " + data.sourceReferenceName);
                        continue;
                    }
                    
                    var propertyIndex = shader.FindPropertyIndex(data.sourceReferenceName);
                    if (propertyIndex >= 0)
                    {
                        var path = AssetDatabase.GetAssetPath(shader);
                        // if the shader field is set, we want to explicitly only upgrade that shader;
                        // if the shader field isn't set, we want to upgrade all shaders.
                        var shouldUpdateThisShader = !sourceShader || shader == sourceShader;
                        var couldUpdateThisShader = path.EndsWith(".shadergraph", StringComparison.OrdinalIgnoreCase) && AssetDatabase.IsOpenForEdit(path);
                        if (!couldUpdateThisShader)
                        {
                            shadersThatCannotBeUpdated.Add(shader);
                        }
                        else if (shouldUpdateThisShader)
                        {
                            // Debug.Log($"Shader has property {data.sourceReferenceName} and will be updated: {shader.name}", shader);
                            shadersThatNeedUpdating.Add(shader);                            
                        }
                        else
                        {
                            // Debug.LogWarning($"Shader has property {data.sourceReferenceName} but will NOT be updated: {shader.name}. Clear the shader field if you want to update all shaders with this property.", shader);
                            shadersThatWouldNeedUpdatingButAreExcluded.Add(shader);
                        }
                    }
                }
            }
            EditorUtility.ClearProgressBar();

            if(!shadersThatNeedUpdating.Any() && !shadersThatWouldNeedUpdatingButAreExcluded.Any() && !shadersThatCannotBeUpdated.Any())
            {
                Debug.Log($"Properties haven't been found in any shaders. No changes necessary.");
                return;
            }

            if (shadersThatWouldNeedUpdatingButAreExcluded.Any())
            {
                var selectedShadersNeedUpdating = shadersThatNeedUpdating.Any();
                switch (EditorUtility.DisplayDialogComplex(
                    "Properties also exist in other shaders!",
                    (selectedShadersNeedUpdating ? "This property exists in this shader that is selected for updating:" + ObjectNames(shadersThatNeedUpdating) : "This property doesn't exist in the selected shader(s).") +
                    "\n\n" +
                    "but also exists in these shaders that won't be updated because they're not selected:" + ObjectNames(shadersThatWouldNeedUpdatingButAreExcluded) +
                    ".\n\n" +
                    "It also exists in " + shadersThatCannotBeUpdated.Count + " shaders that cannot be updated (read-only or not checked out)" +
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

            var materialsThatCannotBeUpdated = materialsThatNeedUpdating.Where(x => !AssetDatabase.IsMainAsset(x) || !AssetDatabase.IsOpenForEdit(x)).ToList();
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
                "Refactor shader properties",
                "Shaders that will be changed [" + shadersThatNeedUpdating.Count + "]:" + ObjectNames(shadersThatNeedUpdating) + "\n\n" +
                "Materials that will be changed [" + materialsThatCanBeUpdated.Count + "]:" + ObjectNames(materialsThatCanBeUpdated) + "\n\n" +
                (shadersThatCannotBeUpdated.Any() ? ("Shaders that can't be changed [" + shadersThatCannotBeUpdated.Count + "]:" + ObjectNames(shadersThatCannotBeUpdated) + "\n\n") : "") +
                (materialsThatCannotBeUpdated.Any() ? ("Materials that can't be changed [" + materialsThatCannotBeUpdated.Count + "]:" + ObjectNames(materialsThatCannotBeUpdated)) : ""),
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
                
                var lines = File.ReadAllLines(path);
                for (int l = 0; l < lines.Length; l++)
                {
                    var start = lines[l].TrimStart();
                    if (start.StartsWith("\"m_DisplayName") || start.StartsWith("\"m_ShaderOutputName") || start.StartsWith("\"m_Name"))
                    {
                        continue;
                    }

                    foreach (var data in propertyList)
                    {
                        if (string.IsNullOrWhiteSpace(data.sourceReferenceName))
                            continue;
            
                        if (string.IsNullOrWhiteSpace(data.targetReferenceName))
                            continue;
                        
                        lines[l] = lines[l].Replace($"\"{data.sourceReferenceName}\"", $"\"{data.targetReferenceName}\"");
                    }
                }
                File.WriteAllLines(path, lines);
            }

            foreach (var mat in materialsThatCanBeUpdated)
            {
                var path = AssetDatabase.GetAssetPath(mat);
                var lines = File.ReadAllLines(path);
                for(int l = 0; l < lines.Length; l++)
                {
                    var start = lines[l].TrimStart();

                    foreach (var data in propertyList)
                    {
                        if (string.IsNullOrWhiteSpace(data.sourceReferenceName))
                            continue;
            
                        if (string.IsNullOrWhiteSpace(data.targetReferenceName))
                            continue;
                        
                        // migrate shader keywords
                        if (start.StartsWith("m_ShaderKeywords: ", StringComparison.Ordinal))
                        {
                            lines[l] = lines[l].Replace(" " + data.sourceReferenceName + " ", " " + data.targetReferenceName + " ");
                            lines[l] = lines[l].Replace(" " + data.sourceReferenceName + "\n", " " + data.targetReferenceName + "\n"); // last item
                        }
                        else
                        {
                            // we're directly operating on the serialized YAML here, what could possibly go wrong
                            lines[l] = lines[l].Replace("- " + data.sourceReferenceName + ":", "- " + data.targetReferenceName + ":");
                        }
                    }
                }
                File.WriteAllLines(path, lines);
            }
            
            AssetDatabase.Refresh();
            // UpdatePopupField();
        }

        private void FixAnimationClips()
        {
            FixAnimationClips(data.refactoringData);
        }

        private void FixAnimationClips(List<ShaderPropertyRefactoringData> dat)
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
                    foreach(var data in dat)
                    {
                        if (binding.propertyName.StartsWith("material." + data.sourceReferenceName, StringComparison.Ordinal)) // TODO what if the animated material is not at index 0?
                        {
                            if(clipsThatNeedUpdating.Contains(clip))
                                continue;
                                
                            clipsThatNeedUpdating.Add(clip);
                        }
                    }
                }   
            }
            EditorUtility.ClearProgressBar();

            var clipsThatCannotBeUpdated = clipsThatNeedUpdating.Where(x => !AssetDatabase.IsOpenForEdit(x)).ToList();
            foreach (var clip in clipsThatNeedUpdating)
            {
                var currentBindings = AnimationUtility.GetCurveBindings(clip);
                for(int j = 0; j < currentBindings.Length; j++)
                {
                    var binding = currentBindings[j];

                    foreach (var data in dat)
                    {
                        if (binding.propertyName.StartsWith("material." + data.sourceReferenceName, StringComparison.Ordinal)) // TODO what if the animated material is not at index 0?
                        {
                            var curve = AnimationUtility.GetEditorCurve(clip, binding);
                            AnimationUtility.SetEditorCurve(clip, binding, null);
                            binding.propertyName = binding.propertyName.Replace("material." + data.sourceReferenceName, "material." + data.targetReferenceName);
                            AnimationUtility.SetEditorCurve(clip, binding, curve);
                        }
                    }

                    // AnimationUtility.SetEditorCurve(clip, binding, null);
                }

                // var path = AssetDatabase.GetAssetPath(clip);
                // var text = File.ReadAllText(path);
                // // we're directly operating on the serialized YAML here, what could possibly go wrong
                // text = text.Replace("- " + data.sourceReferenceName + ":", "- " + data.targetReferenceName + ":");
                // File.WriteAllText(path, text);
            }

            if(clipsThatNeedUpdating.Any())
            {
                Debug.Log("Clips that have been updated [" + clipsThatNeedUpdating.Count + "]: " + ObjectNames(clipsThatNeedUpdating));
                if (clipsThatCannotBeUpdated.Any())
                {
                    Debug.Log("Clips that cannot be updated (read-only) [" + clipsThatCannotBeUpdated.Count + "]: " + ObjectNames(clipsThatCannotBeUpdated));
                }
            }
        }
        
        private void FixScripts()
        {
            // find all scripts
            var scripts = GetAllAssets<MonoScript>();
            var i = 0;
            var count = scripts.Count;
            foreach (var script in scripts)
            {
                i++;
                if (!AssetDatabase.IsOpenForEdit(script)) continue;
                EditorUtility.DisplayProgressBar("Parsing Scripts", "Script " + i + "/" + count + ": " + script.name, (float)i / count);
                var path = AssetDatabase.GetAssetPath(script);
                var fullText = File.ReadAllLines(path);
                bool didAChange = false;
                for (int line = 0; line < fullText.Length; line++)
                {
                    var text = fullText[line];
                    foreach (var data in data.refactoringData)
                        text = text.Replace("\"" + data.sourceReferenceName + "\"", "\"" + data.targetReferenceName + "\"");
                    if (fullText[line] != text)
                        didAChange = true;
                    fullText[line] = text;
                }
                if (didAChange)
                    File.WriteAllLines(path, fullText);
            }
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        private static string ObjectNames<T>(IReadOnlyCollection<T> objects, bool singleLine = false, bool returnShortStringIfTooManyElements = true, bool groupByName = false) where T : UnityEngine.Object
        {
            var strings = (groupByName ? objects.ToLookup(x => x.name).Select(x => $"{x.Key} [{x.Count()}]") : objects.Select(x => x.name)).OrderBy(x => x).ToList();
            
            if (returnShortStringIfTooManyElements && strings.Count > 5)
                return singleLine ? " " : "\n" + "(too many items to display: " + strings.Count + ")";
            
            const int MaxPrettyObjectsCount = 10;
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
        public List<ShaderPropertyRefactoringData> refactoringData = new List<ShaderPropertyRefactoringData>();
        // public string sourceReferenceName;
        // public string targetReferenceName;
    }

    [Serializable]
    public class ShaderPropertyRefactoringData
    {
        public string sourceReferenceName;
        public string targetReferenceName;
    }
}