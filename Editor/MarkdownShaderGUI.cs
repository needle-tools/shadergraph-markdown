#if UNITY_2021_2_OR_NEWER
#define HAVE_VALIDATE_MATERIAL
#if RP_CORE_7_OR_NEWER
#define HAVE_HEADER_FOLDOUT_WITH_DOCS
#endif
#if SHADERGRAPH_7_OR_NEWER
#define SRP12_SG_REFACTORED
#endif
#endif
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using Needle.ShaderGraphMarkdown;
using Needle.ShaderGraphMarkdown.LogicExpressionParser;
using Unity.Profiling;
using UnityEditorInternal;
#if SHADERGRAPH_7_OR_NEWER
using UnityEditor.ShaderGraph;
#endif
using UnityEngine.Rendering;
#if HDRP_7_OR_NEWER
using UnityEditor.Rendering.HighDefinition;
#endif

// we're making an exception here: only Needle namespace, because full namespace
// has to be included in the ShaderGraph custom ui field.
namespace Needle
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MarkdownShaderGUI : ShaderGUI
    {
        private class HeaderGroup
        {
            public readonly string name;
            public readonly string condition;
            public List<MaterialProperty> properties;
            public Action customDrawer = null;
            public bool expandedByDefault = true;
            private int hash = 0;
            public string FoldoutStateKeyName => $"{nameof(MarkdownShaderGUI)}.{name}.{hash}";
            
            public HeaderGroup(string displayName, string condition = null, int hash = 0)
            {
                if (!string.IsNullOrEmpty(displayName) && displayName.EndsWith("-", StringComparison.Ordinal))
                {
                    expandedByDefault = false;
                    name = displayName.Substring(0, displayName.Length - 1);
                }
                else
                {
                    name = displayName;    
                }

                this.condition = condition;
                this.hash = hash;
            }
        }

        private static GUIStyle centeredGreyMiniLabel;
        private static GUIStyle CenteredGreyMiniLabel {
            get {
                if (centeredGreyMiniLabel == null)
                {
                    centeredGreyMiniLabel = new GUIStyle(GUI.skin.FindStyle("MiniLabel") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("MiniLabel")) {
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = Color.gray }
                    };
                }
                return centeredGreyMiniLabel;
            }
        }
        
        private static readonly Dictionary<string, MarkdownMaterialPropertyDrawer> drawerCache = new Dictionary<string, MarkdownMaterialPropertyDrawer>();
        private readonly List<MaterialProperty> referencedProperties = new List<MaterialProperty>();

        internal enum MarkdownProperty
        {
            None,
            Reference,
            Link,
            Note,
            Drawer,
            Header,
            Foldout,
            Tooltip,
        }

        private static readonly string refFormat = "!REF";
        private static readonly string noteFormat = "!NOTE";
        private static readonly string alternateNoteFormat = "* ";
        private static readonly string tooltipFormat = "!TIP";
        private static readonly string alternateTooltipFormat = "!TOOLTIP";
        private static readonly string drawerFormat = "!DRAWER";
        private static readonly string foldoutHeaderFormat = "#";
        private static readonly string foldoutHeaderFormatStart = foldoutHeaderFormat + " ";
        private static readonly string headerFormat = "##";
        private static readonly string headerFormatStart = headerFormat + " ";
        private static readonly string headerFormatStartLabel = headerFormat + "# ";

        private static MarkdownProperty GetMarkdownTypeUncached(string display)
        {
            if (display.StartsWith(refFormat))
                return MarkdownProperty.Reference;
            if (display.StartsWith("[") && display.IndexOf("]", StringComparison.Ordinal) > -1 && display.IndexOf("(", StringComparison.Ordinal) > -1 && display.EndsWith(")"))
                return MarkdownProperty.Link;
            if (display.StartsWith(noteFormat) || display.StartsWith(alternateNoteFormat))
                return MarkdownProperty.Note;
            if (display.StartsWith(tooltipFormat) || display.StartsWith(alternateTooltipFormat))
                return MarkdownProperty.Tooltip;
            if (display.StartsWith(drawerFormat))
                return MarkdownProperty.Drawer;
            if (display.StartsWith(foldoutHeaderFormatStart) || display.Equals(foldoutHeaderFormat, StringComparison.Ordinal))
                return MarkdownProperty.Foldout;
            if (display.StartsWith(headerFormatStart) || display.StartsWith(headerFormatStartLabel) || display.Equals(headerFormat, StringComparison.Ordinal))
                return MarkdownProperty.Header;
            return MarkdownProperty.None;
        }
        
        private static Dictionary<string, MarkdownProperty> propertyTypeCache = new Dictionary<string, MarkdownProperty>();
        internal static MarkdownProperty GetMarkdownType(string display)
        {
            if (propertyTypeCache.ContainsKey(display)) return propertyTypeCache[display];
            var type = GetMarkdownTypeUncached(display);
            propertyTypeCache.Add(display, type);
            return type;
        }

        internal static int GetIndentLevel(string display)
        {
            int indent = 0;
            for(int i = 0; i < display.Length; i++) {
                if (display[i].Equals('-')) indent++;
                else break;
            }

            return indent;
        }

        private bool
            showOriginalPropertyList = false,
            _showPropertyNames = false,
            debugConditionalProperties = false,
            debugReferencedProperties = false;

        private bool shortcutShowPropertyNames = false;
        private bool showPropertyNames => _showPropertyNames || shortcutShowPropertyNames;

        private bool
            debugPropertyDrawers = false,
            debugMarkdownGroups = false,
            showBaseShaderGuiOptions = true;
        
        // Reflection Access
        // ReSharper disable InconsistentNaming
        private static Type MaterialPropertyHandler;
        private static MethodInfo GetHandler;
        private static FieldInfo m_PropertyDrawer, m_DecoratorDrawers;
        private bool debugLocalAndGlobalKeywords = false;

#if UNITY_2021_2_OR_NEWER
        private Dictionary<string, LocalKeyword> localKeywords;
        private Dictionary<string, GlobalKeyword> globalKeywords;
#else
        private List<string> localKeywords;
        private List<string> globalKeywords;
        private static MethodInfo getShaderLocalKeywords;
        private static MethodInfo GetShaderLocalKeywords {
            get {
                if (getShaderLocalKeywords  == null) getShaderLocalKeywords  = typeof(ShaderUtil).GetMethod("GetShaderLocalKeywords",  (BindingFlags) (-1));
                return getShaderLocalKeywords;
            }
        }

        private static MethodInfo getShaderGlobalKeywords;
        private static MethodInfo GetShaderGlobalKeywords {
            get {
                if (getShaderGlobalKeywords == null) getShaderGlobalKeywords = typeof(ShaderUtil).GetMethod("GetShaderGlobalKeywords", (BindingFlags) (-1));
                return getShaderGlobalKeywords;
            }
        }
#endif
        // ReSharper restore InconsistentNaming
             
        private static MaterialProperty FindKeywordProperty(string keywordRef, MaterialProperty[] properties)
        {
            var keywordProp = ShaderGUI.FindProperty(keywordRef, properties, false);
            
            // special case: bool properties have to be named MY_PROP_ON in ShaderGraph to be exposed,
            // but the actual property is named "MY_PROP" and the keyword is still "MY_PROP_ON".
            if (keywordProp == null && keywordRef.EndsWith("_ON", StringComparison.Ordinal))
                keywordProp = ShaderGUI.FindProperty(keywordRef.Substring(0, keywordRef.Length - "_ON".Length), properties, false);
            
            return keywordProp;
        }

        internal static MarkdownMaterialPropertyDrawer GetCachedDrawer(string objectName)
        {
            if(!drawerCache.ContainsKey(objectName) || !drawerCache[objectName])
            {
                var objectPath = AssetDatabase.FindAssets($"t:{nameof(MarkdownMaterialPropertyDrawer)} {objectName}").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
                var scriptableObject = default(MarkdownMaterialPropertyDrawer);
                
                if(!string.IsNullOrEmpty(objectPath))
                    scriptableObject = AssetDatabase.LoadAssetAtPath<MarkdownMaterialPropertyDrawer>(objectPath);
                
                if (!scriptableObject)
                {
                    // create a drawer instance in memory
                    var drawerType = TypeCache
                        .GetTypesDerivedFrom<MarkdownMaterialPropertyDrawer>()
                        .FirstOrDefault(x => x.Name.Equals(objectName, StringComparison.Ordinal));
                    scriptableObject = (MarkdownMaterialPropertyDrawer) ScriptableObject.CreateInstance(drawerType);

                    if (!scriptableObject && !objectName.EndsWith("Drawer", StringComparison.Ordinal))
                    {
                        var longName = objectName + "Drawer";
                        if (drawerCache.ContainsKey(longName) && drawerCache[longName])
                            scriptableObject = drawerCache[longName];
                        else
                            scriptableObject = GetCachedDrawer(longName);
                    }
                }
                
                if (drawerCache.ContainsKey(objectName)) drawerCache[objectName] = scriptableObject;
                else drawerCache.Add(objectName, scriptableObject);
            }

            return drawerCache[objectName];
        }

        public override void OnClosed(Material material)
        {
            headerGroups?.Clear();
            headerGroups = null;
            lastHash = -1;
            base.OnClosed(material);
        }
        
        private int lastHash;
        private List<HeaderGroup> headerGroups = null;
#if SRP12_SG_REFACTORED
        private Dictionary<string, int> propertyToCategory = null;
        private Dictionary<int, HeaderGroup> categoryToHeaderGroup = null;
#endif
        
        private string nextTooltip = null;
        private string UseTooltip()
        {
            if (nextTooltip == null) return null;
            var val = nextTooltip;
            nextTooltip = null;
            return val;
        }
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var targetMat = materialEditor.target as Material;
            if (!targetMat) return;

            OnGUIMarker.Begin();
            
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;
            // proper widths for texture and label fields, same as ShaderGUI
            EditorGUIUtility.fieldWidth = 64f;
            // keyboard shortcut overrides
            shortcutShowPropertyNames = Event.current.shift;
            
            int GetTargetMaterialHashCode()
            {
                var hashCode = targetMat.name.GetHashCode();
                hashCode = (hashCode * 397) ^ (targetMat.shader ? targetMat.shader.name.GetHashCode() : 0);
                foreach(var prop in properties)
                {
                    hashCode = (hashCode * 397) ^ prop.name.GetHashCode();
                    hashCode = (hashCode * 397) ^ prop.displayName.GetHashCode();
                    hashCode = (hashCode * 397) ^ prop.rangeLimits.GetHashCode();
                    hashCode = (hashCode * 397) ^ prop.flags.GetHashCode();
                    
                    // need to hash values as well since it seems MaterialProperties are changing whenever a value is changed/undone etc.
                    hashCode = (hashCode * 397) ^ prop.floatValue.GetHashCode();
                    hashCode = (hashCode * 397) ^ prop.colorValue.GetHashCode();
                    hashCode = (hashCode * 397) ^ prop.vectorValue.GetHashCode();
                    if(prop.textureValue) hashCode = (hashCode * 397) ^ prop.textureValue.GetHashCode();
                    hashCode = (hashCode * 397) ^ prop.textureScaleAndOffset.GetHashCode();
                }
                return hashCode;
            }

            GetHashCodeMarker.Begin();
            int currentHash = GetTargetMaterialHashCode();
            if (lastHash != currentHash) {
                lastHash = currentHash;
                MaterialChanged(materialEditor, properties);
                headerGroups?.Clear();
            }
            GetHashCodeMarker.End();

            if (headerGroups == null)
                headerGroups = new List<HeaderGroup>();
            
            // split by header properties
            if(headerGroups.Count < 1)
            {
                GenerateHeaderGroupsMarker.Begin();
                headerGroups.Add(new HeaderGroup("Default"));
                
#if SRP12_SG_REFACTORED
                // on 2021.2+, we need to also collect Blackboard Categories, which are stored in a sub asset
                var blackboardCategories = MarkdownSGExtensions.CollectCategories(targetMat.shader)?.ToList();
                propertyToCategory = new Dictionary<string, int>();
                categoryToHeaderGroup = new Dictionary<int, HeaderGroup>();
                if(blackboardCategories != null)
                {
                    foreach (var cat in blackboardCategories)
                    {
                        var firstPropertyInCategory = cat.properties.FirstOrDefault();
                        if(firstPropertyInCategory != null)
                            propertyToCategory.Add(firstPropertyInCategory, cat.categoryHash);
                    }
                    
                    foreach (var cat in blackboardCategories)
                    {
                        var display = cat.categoryName;
                        var condition = GetBetween(display, '[', ']', true);
                        if (!string.IsNullOrWhiteSpace(condition))
                            display = display.Substring(0, display.LastIndexOf('[')).TrimEnd();
                        var group = new HeaderGroup(display, condition, cat.categoryHash);
                        categoryToHeaderGroup.Add(cat.categoryHash, group);
                    }
                }
#endif
                
                foreach (var prop in properties)
                {
                    var display = prop.displayName;
                    if(display.StartsWith(foldoutHeaderFormatStart) || display.Equals(foldoutHeaderFormat, StringComparison.Ordinal))
                    {
                        var condition = GetBetween(display, '[', ']', true);
                        if (!string.IsNullOrWhiteSpace(condition))
                            display = display.Substring(0, display.LastIndexOf('[')).TrimEnd();
                        if (display.Equals(foldoutHeaderFormat, StringComparison.Ordinal)  || 
                            (display.StartsWith(foldoutHeaderFormatStart + "(", StringComparison.Ordinal) && display.EndsWith(")", StringComparison.Ordinal))) // for multiple # (1) foldout breakers)
                            headerGroups.Add(new HeaderGroup(null, condition));
                        else
                        {
                            // remove "# " from start
                            display = display.Substring(display.IndexOf(' ') + 1);
                            headerGroups.Add(new HeaderGroup(display, condition));
                        }
                    }
                    else
                    {
#if SRP12_SG_REFACTORED
                        if (propertyToCategory.ContainsKey(prop.name))
                        {
                            var categoryHeaderGroup = categoryToHeaderGroup[propertyToCategory[prop.name]];
                            headerGroups.Add(categoryHeaderGroup);
                        }
#endif
                        var headerGroup = headerGroups.Last();
                        
                        if (headerGroup.properties == null) 
                            headerGroup.properties = new List<MaterialProperty>();
                        
                        // need to process REF/DRAWER properties early so we can hide them properly if needed
                        display = display.TrimStart('-');
                        var searchForReferencedProperties = false;
                        
                        // don't collect properties for drawers/references that are conditionally excluded
                        var markdownType = GetMarkdownType(display);
                        switch (markdownType)
                        {
                            case MarkdownProperty.Foldout:
                            case MarkdownProperty.Header:
                            case MarkdownProperty.Link:
                            case MarkdownProperty.Note:
                            case MarkdownProperty.Tooltip:
                            case MarkdownProperty.None:
                                break;
                            case MarkdownProperty.Reference:
                            case MarkdownProperty.Drawer:
                                searchForReferencedProperties = true;
                                break;
                        }
                        
                        if(searchForReferencedProperties)
                        {
                            // TODO unsure about this: there are cases where both excluding and including such properties seems desired.
                            // var condition = GetBetween(display, '[', ']', true);
                            // if (!string.IsNullOrEmpty(condition) && !ConditionIsFulfilled(targetMat, condition))
                            //     continue;

                            var split = display.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                            if (split.Length < 2)
                                continue;
                            
                            switch (markdownType)
                            {
                                case MarkdownProperty.Reference:
                                    var keywordRef = split[1];
                                    var keywordProp = FindKeywordProperty(keywordRef, properties);
                                    if(keywordProp != null)
                                        referencedProperties.Add(keywordProp);
                                    break;
                                case MarkdownProperty.Drawer:
                                    var objectName = split[1];
                                    var drawer = GetCachedDrawer(objectName);
                                    if (drawer != null)
                                    {
                                        var referencedProps = drawer.GetReferencedProperties(materialEditor, properties, new MarkdownMaterialPropertyDrawer.DrawerParameters(split));
                                        if(referencedProps != null)
                                            referencedProperties.AddRange(referencedProps);
                                    }
                                    break;
                            }
                        }
                        headerGroup.properties.Add(prop);
                    }
                }
                headerGroups.Add(new HeaderGroup(null) { properties = null, customDrawer = DrawCustomGUI });
                headerGroups.Add(new HeaderGroup(MarkdownToolsLabel) { properties = null, customDrawer = DrawDebugGroupContent, expandedByDefault = false});
                
                GenerateHeaderGroupsMarker.End();
            }
            
            void DrawDebugGroupContent()
            {
                DrawDebugGroupContentMarker.Begin();

                EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
                if (shortcutShowPropertyNames)
                {
                    EditorGUI.showMixedValue = true;
                    EditorGUILayout.Toggle(ShowPropertyNames, _showPropertyNames);
                    EditorGUI.showMixedValue = false;
                }
                else
                {
                    _showPropertyNames = EditorGUILayout.Toggle(ShowPropertyNames, _showPropertyNames);
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Refactoring");
                if (GUILayout.Button(RenameShaderProperties, EditorStyles.miniButton))
                    ShowRefactoringWindow(AssetDatabase.GetAssetPath(targetMat.shader), "");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Markdown Debugging", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                showOriginalPropertyList = EditorGUILayout.Toggle(ShowOriginalProperties, showOriginalPropertyList);
                if (EditorGUI.EndChangeCheck())
                    InitializeCustomGUI(targetMat);
                debugConditionalProperties = EditorGUILayout.Toggle(DebugConditionalProperties, debugConditionalProperties);
                debugReferencedProperties = EditorGUILayout.Toggle(DebugReferencedProperties, debugReferencedProperties);
                
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(ShaderKeywords, EditorStyles.boldLabel);
                foreach (var kw in targetMat.shaderKeywords)
                {
                    EditorGUILayout.TextField(kw, EditorStyles.miniLabel);
                }

                EditorGUILayout.Space();

                var newVal = EditorGUILayout.Foldout(debugLocalAndGlobalKeywords, LocalAndGlobalKeywords);
                if (newVal != debugLocalAndGlobalKeywords)
                {
                    debugLocalAndGlobalKeywords = newVal;
                    
                    // enforce refresh when opening / closing the foldout
                    if (debugLocalAndGlobalKeywords)
                        CollectLocalAndGlobalKeywords(targetMat);
                }
                if(debugLocalAndGlobalKeywords)
                {
                    EditorGUILayout.LabelField(LocalKeywords, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUI.indentLevel++;
                    foreach (var kw in localKeywords)
                    {
#if UNITY_2021_2_OR_NEWER
                        var title = kw.Value.name + "  " + (kw.Value.type != ShaderKeywordType.UserDefined ? "[" + kw.Value.type + "]" : "") + (kw.Value.isOverridable ? " [Overridable]" : "");
                        var isOn = targetMat.shaderKeywords.Contains(kw.Value.name);
#else
                        var title = kw;
                        var isOn = targetMat.shaderKeywords.Contains(kw);
#endif
                        EditorGUILayout.TextField(title, EditorStyles.miniLabel);
                        var lastRect = GUILayoutUtility.GetLastRect();
                        lastRect.xMin -= 32;
                        lastRect.xMax = lastRect.xMin += 16;
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUI.ToggleLeft(lastRect, GUIContent.none, isOn);   
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;
                    EditorGUILayout.LabelField(GlobalKeywords, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUI.indentLevel++;
                    foreach (var kw in globalKeywords)
                    {
#if UNITY_2021_2_OR_NEWER
                        var title = kw.Value.name;
                        var isOn = Shader.IsKeywordEnabled(kw.Value);
#else
                        var title = kw;
                        var isOn = Shader.IsKeywordEnabled(kw);
#endif
                        EditorGUILayout.TextField(title, EditorStyles.miniLabel);
                        var lastRect = GUILayoutUtility.GetLastRect();
                        lastRect.xMin -= 32;
                        lastRect.xMax = lastRect.xMin += 16;
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUI.ToggleLeft(lastRect, GUIContent.none, isOn);  
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;
                }
                
                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Reset");
                if (GUILayout.Button(ClearKeywords))
                {
                    foreach (var kw in targetMat.shaderKeywords)
                        targetMat.DisableKeyword(kw);
                    
#if HDRP_7_OR_NEWER
                    try
                    {
                        HDShaderUtils.ResetMaterialKeywords(targetMat);
                    }
                    catch (ArgumentException)
                    {
                        // ignore, not a HDRP shader probably
                    }
#endif
                    ValidateMaterial(targetMat);
                    
                    // resetting the shader seems to trigger keyword sanitization for ShaderGraph shaders and some other Unity shaders 
                    baseShaderGui?.AssignNewShaderToMaterial(targetMat, targetMat.shader, targetMat.shader);
                }
                GUILayout.EndHorizontal();
                
                if (IsDeveloperMode())
                {
                    DrawDeveloperModeContent();
                }
                EditorGUILayout.Space();
                
                // #if HDRP_7_OR_NEWER
                // EditorGUILayout.LabelField("ShaderGraph Info", EditorStyles.boldLabel);
                // if (GUILayout.Button("Refresh"))
                // {
                //     Debug.Log(MarkdownHDExtensions.GetDefaultCustomInspectorFromShader(targetMat.shader));
                // }
                // #endif
                
                DrawDebugGroupContentMarker.End();
            }

            void DrawDeveloperModeContent()
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(DevelopmentOptionsLabel, EditorStyles.boldLabel);
                showBaseShaderGuiOptions = EditorGUILayout.Foldout(showBaseShaderGuiOptions, BaseShaderGUIOptions);
                if (showBaseShaderGuiOptions)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.TextField("Type", baseShaderGui != null ? baseShaderGui.GetType().ToString() : "none", EditorStyles.miniLabel);
                    if(baseShaderGui != null)
                    {
                        if (GUILayout.Button("Validate Material")) ValidateMaterial(targetMat);
                        if (GUILayout.Button("Re-Assign Shader")) baseShaderGui.AssignNewShaderToMaterial(targetMat, targetMat.shader, targetMat.shader);
                    }
                    EditorGUI.indentLevel--;
                }
                
                debugPropertyDrawers = EditorGUILayout.Foldout(debugPropertyDrawers, PropertyDrawersAndDecorators);
                if (debugPropertyDrawers)
                {
                    foreach (var prop in properties)
                    {
                        if (MaterialPropertyHandler == null) MaterialPropertyHandler = typeof(MaterialProperty).Assembly.GetType("UnityEditor.MaterialPropertyHandler");
                        if (MaterialPropertyHandler != null)
                        {
                            if(GetHandler         == null) GetHandler         = MaterialPropertyHandler.GetMethod("GetHandler", (BindingFlags) (-1));
                            var handler = GetHandler.Invoke(null, new object[] {((Material) materialEditor.target).shader, prop.name});
                            if(m_PropertyDrawer   == null) m_PropertyDrawer   = MaterialPropertyHandler.GetField("m_PropertyDrawer", (BindingFlags) (-1));
                            if(m_DecoratorDrawers == null) m_DecoratorDrawers = MaterialPropertyHandler.GetField("m_DecoratorDrawers", (BindingFlags) (-1));

                            if(handler != null)
                            {
                                MaterialPropertyDrawer propertyDrawer = (MaterialPropertyDrawer) m_PropertyDrawer.GetValue(handler);
                                List<MaterialPropertyDrawer> decoratorDrawers = (List<MaterialPropertyDrawer>) m_DecoratorDrawers.GetValue(handler);
                                if (propertyDrawer != null || decoratorDrawers != null)
                                {
                                    EditorGUILayout.LabelField($@"{prop.name}(""{prop.displayName}"", {prop.type})");
                                }
                                EditorGUI.indentLevel++;
                                if(propertyDrawer != null)
                                {
                                    EditorGUILayout.LabelField("Property Drawer", EditorStyles.miniLabel);
                                    EditorGUILayout.LabelField(propertyDrawer.GetType() + " (height: " + propertyDrawer.GetPropertyHeight(prop, prop.displayName, materialEditor) + "px)");
                                }
                                if(decoratorDrawers != null)
                                {
                                    EditorGUILayout.LabelField("Decorator Drawers", EditorStyles.miniLabel);
                                    foreach (var d in decoratorDrawers)
                                    {
                                        EditorGUILayout.LabelField(d.GetType() + " (height: " + d.GetPropertyHeight(prop, prop.displayName, materialEditor) + "px)");
                                    }
                                }
                                EditorGUI.indentLevel--;
                            }
                        }
                        // MaterialPropertyHandler handler = MaterialPropertyHandler.GetHandler(((Material) materialEditor.target).shader, prop.name);
                    }
                }
                
                debugMarkdownGroups = EditorGUILayout.Foldout(debugMarkdownGroups, GroupsAndCategories);
                if (debugMarkdownGroups)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    foreach (var group in headerGroups)
                    {
                        EditorGUILayout.LabelField(group.name == null ? "<null>" : string.IsNullOrWhiteSpace(group.name) ? "<whitespace>" : group.name, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Condition", group.condition == null ? "<null>" : group.condition);
                        EditorGUILayout.LabelField("Custom Drawer", group.customDrawer?.Method?.Name ?? "<null>");
                        EditorGUILayout.LabelField("Property Count", "" + (group.properties?.Count ?? 0));
                        EditorGUILayout.Space();
                    }
                    EditorGUI.EndDisabledGroup();
                }
                
                EditorGUILayout.Space();
                
                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Inspector");
                if (GUILayout.Button(RedrawInspector))
                {
                    drawerCache.Clear();
                    headerGroups.Clear();
                    GUIUtility.ExitGUI();
                }                
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Foldouts");
                if (GUILayout.Button(ResetFoldoutSessionState))
                {
                    foreach(var group in headerGroups)
                    {
                        SessionState.EraseBool(group.FoldoutStateKeyName);
                    }
                }
                GUILayout.EndHorizontal();
            }
            
            void DrawCustomGUI()
            {
                DrawCustomGUIMarker.Begin();
                
                if (!haveSearchedForCustomGUI)
                    InitializeCustomGUI(targetMat);
            
                // This assumes that all base shader GUIs draw the advanced options themselves (e.g. instancing, queue, ...)
                if(baseShaderGui == null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(AdditionalOptions, EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    
                    // from MaterialEditor.PropertiesDefaultGUI(properties);
                    if (SupportedRenderingFeatures.active.editableMaterialRenderQueue)
                        materialEditor.RenderQueueField();
                    materialEditor.EnableInstancingField();
                    materialEditor.DoubleSidedGIField();
                    
                    // if the material exposes _EMISSION as user-changeable property, we only want to draw the lightmap settings when its on.
                    // otherwise, we always draw it when the known emission properties exist
                    if (targetMat.HasProperty(EmissionColorPropertyName) || targetMat.HasProperty(EmissionMapPropertyName) || targetMat.HasProperty(EmissionColorPropertyName2) || targetMat.HasProperty(EmissionMapPropertyName2))
                        if (!(targetMat.HasProperty(EmissionKeyword) || targetMat.HasProperty("_Emission")) || targetMat.IsKeywordEnabled(EmissionKeyword))
                            materialEditor.LightmapEmissionFlagsProperty(0, true, true);
                    
                    EditorGUILayout.Space();
                    CoreEditorUtils.DrawSplitter();
                }

                try
                {
                    if (baseShaderGui != null)
                    {
                        // only pass in the properties that are hidden - this allows us to render all custom HDRP ShaderGraph UIs.
                        baseShaderGui?.OnGUI(materialEditor, showOriginalPropertyList 
                            ? properties
                            : properties
                                .Where(x => x.flags.HasFlag(MaterialProperty.PropFlags.HideInInspector))
                                .ToArray());
                        CoreEditorUtils.DrawSplitter();
                    }
                }
                catch (Exception e)
                {
                    EditorGUILayout.HelpBox("Exception when drawing base shader GUI of type " + baseShaderGui?.GetType() + ":\n" + e, MessageType.Error);
                }
                
                DrawCustomGUIMarker.End();
            }
            
            void DrawGroup(HeaderGroup group)
            {
                DrawGroupMarker.Begin();

                bool previousPropertyWasDrawn = true;
                bool nextPropertyDrawerShouldBeInline = false;
                
                for(int i = 0; i < group.properties.Count; i++)
                {
                    var prop = group.properties[i];
                    var display = prop.displayName;
                    var isDisabled = false;
                    var hasCondition = !display.StartsWith("[", StringComparison.Ordinal) && display.Contains('[') && display.EndsWith("]", StringComparison.Ordinal);
                    if (hasCondition)
                    {
                        var condition = GetBetween(display, '[', ']', true);
                        if (!string.IsNullOrEmpty(condition))
                        {
                            if (!ConditionIsFulfilled(targetMat, condition))
                            {
                                // consume tooltip for conditionally excluded properties
                                UseTooltip();
                                if(!debugConditionalProperties)
                                {
                                    previousPropertyWasDrawn = false;
                                    continue;
                                }
                                
                                isDisabled = true;
                                EditorGUI.BeginDisabledGroup(true);
                            }
                            if(!debugConditionalProperties)
                                display = display.Substring(0, display.IndexOf('[') - 1);
                        }
                    }

                    var currentIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = currentIndent + GetIndentLevel(display);
                    display = display.TrimStart('-');
                    switch (GetMarkdownType(display))
                    {
                        case MarkdownProperty.Link:
                            if (!previousPropertyWasDrawn) continue;
                            var linkText = GetBetween(display, '[', ']');
                            var linkHref = GetBetween(display, '(', ')');
                            if (GUILayout.Button(new GUIContent(linkText, UseTooltip()), CenteredGreyMiniLabel)) Application.OpenURL(linkHref);
                            break;
                        case MarkdownProperty.Note:
                            if (!previousPropertyWasDrawn) continue;
                            var noteText = display.Substring(display.IndexOf(' ') + 1);
                            EditorGUILayout.LabelField(new GUIContent(noteText, UseTooltip()), CenteredGreyMiniLabel);
                            break;
                        case MarkdownProperty.Tooltip:
                            nextTooltip = display.Substring(display.IndexOf(' ') + 1);
                            break;
                        case MarkdownProperty.Drawer:
                            var parts = display.Split(' ');
                            if(parts.Length > 1) {
                                var objectName = parts[1];
                                var drawer = GetCachedDrawer(objectName);
                                if(drawer) {
                                    try
                                    {
                                        if (nextPropertyDrawerShouldBeInline)
                                        {
                                            if(drawer.SupportsInlineDrawing)
                                            {
                                                var rect = InlineTextureDrawer.LastInlineTextureRect;
                                                rect.xMin += EditorGUI.indentLevel * 15f;
                                                var previousIndent = EditorGUI.indentLevel;
                                                EditorGUI.indentLevel = 0;
                                                drawer.OnInlineDrawerGUI(InlineTextureDrawer.LastInlineTextureRect, materialEditor, properties, new MarkdownMaterialPropertyDrawer.DrawerParameters(parts, UseTooltip()));
                                                EditorGUI.indentLevel = previousIndent;
                                            }
                                            else
                                            {
                                                EditorGUI.LabelField(InlineTextureDrawer.LastInlineTextureRect, drawer + " doesn't support inline drawing", EditorStyles.miniLabel);
                                                drawer.OnDrawerGUI(materialEditor, properties, new MarkdownMaterialPropertyDrawer.DrawerParameters(parts, UseTooltip()));
                                            }
                                        }
                                        else
                                        {
                                            drawer.OnDrawerGUI(materialEditor, properties, new MarkdownMaterialPropertyDrawer.DrawerParameters(parts, UseTooltip(), showPropertyNames));
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        EditorGUILayout.HelpBox("Error in Custom Drawer \"" + objectName + "\":\n" + e.Message, MessageType.Error);
                                    }
                                    finally
                                    {
                                        nextPropertyDrawerShouldBeInline = false;
                                    }
                                }
                                else
                                    EditorGUILayout.HelpBox("Custom Drawer \"" + objectName + "\" not found.", MessageType.Error);
                            }
                            else {
                                EditorGUILayout.HelpBox("No Drawer specified.", MessageType.Warning);
                            }
                            previousPropertyWasDrawn = true;
                            break;
                        case MarkdownProperty.Header:
                            var stringIndex = display.IndexOf(' ') + 1;
                            if(stringIndex > 0 && !(display.StartsWith(headerFormatStart + "(", StringComparison.Ordinal) && display.EndsWith(")", StringComparison.Ordinal)))
                            {
                                var labelName = display.Substring(stringIndex);
                                if(display.StartsWith(headerFormatStartLabel))
                                {
                                    EditorGUILayout.LabelField(new GUIContent(labelName, UseTooltip()));
                                }
                                else
                                {
                                    EditorGUILayout.Space();
                                    EditorGUILayout.LabelField(new GUIContent(labelName, UseTooltip()), EditorStyles.boldLabel);
                                }
                            }
                            else
                            {
                                EditorGUILayout.Space();
                            }
                            previousPropertyWasDrawn = true;
                            break;
                        case MarkdownProperty.Reference:
                            var split = display.Split(' ');
                            if (split.Length > 1)
                            {
                                var keywordRef = split[1];
                                var keywordProp = FindKeywordProperty(keywordRef, properties);
                                var foundKeywordToDraw = false;
                                
#if SHADERGRAPH_7_OR_NEWER
                                // special case: the keyword might be defined in a subgraph
                                if (keywordProp == null)
                                {
                                    var keyword = MarkdownSGExtensions.FindKeywordData(targetMat.shader, keywordRef);
                                    if (keyword != null)
                                    {
                                        MarkdownSGExtensions.DrawShaderKeywordProperty(materialEditor, keyword, UseTooltip(), showPropertyNames);
                                        foundKeywordToDraw = true;
                                    }
                                }
#endif
                                if (!foundKeywordToDraw)
                                {
                                    if (keywordProp == null)
                                    {
#if UNITY_2021_2_OR_NEWER
                                        if (globalKeywords.ContainsKey(keywordRef))   
#else
                                        if (globalKeywords.Contains(keywordRef))
#endif
                                        {
                                            EditorGUI.BeginChangeCheck();
                                            var newVal = EditorGUILayout.Toggle(keywordRef, Shader.IsKeywordEnabled(keywordRef));
                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                if (newVal) Shader.EnableKeyword(keywordRef);
                                                else Shader.DisableKeyword(keywordRef);
                                            }
                                        }
                                        else
                                        {
                                            EditorGUILayout.HelpBox("Could not find MaterialProperty: '" + keywordRef, MessageType.Error);
                                        }
                                    }
                                    else
                                        materialEditor.ShaderProperty(keywordProp, new GUIContent(showPropertyNames ? keywordProp.name : keywordProp.displayName, UseTooltip()));
                                }
                            }
                            previousPropertyWasDrawn = true;
                            break;
                        // case MarkdownProperty.None:
                        default:
                            if(referencedProperties.Contains(prop))
                            {
                                if (debugReferencedProperties)
                                {
                                    EditorGUI.BeginDisabledGroup(true);
                                    materialEditor.ShaderProperty(prop, display);
                                    EditorGUI.EndDisabledGroup();
                                }
                                previousPropertyWasDrawn = false;
                                break;
                            }

                            if(prop.flags.HasFlag(MaterialProperty.PropFlags.HideInInspector))
                                break;
                            
                            if(prop.flags.HasFlag(MaterialProperty.PropFlags.PerRendererData))
                                break;
                            
                            // check if drawer shorthand + parameters
                            var indexOfShorthand = display.IndexOf("&&", StringComparison.Ordinal);
                            var isShorthandWithParameters = indexOfShorthand > 0 && indexOfShorthand < display.Length - 2;
                            
                            // drawer shorthands
                            if (isShorthandWithParameters || display.EndsWith("&", StringComparison.Ordinal) ||
                                (prop.type == MaterialProperty.PropType.Texture && prop.flags.HasFlag(MaterialProperty.PropFlags.NonModifiableTextureData))) // display non-modifiable textures inlined by default
                            {
                                bool shouldDrawMultiplePropertiesInline = display.EndsWith("&&", StringComparison.Ordinal);
                                var parameters = default(MarkdownMaterialPropertyDrawer.DrawerParameters);
                                if (isShorthandWithParameters)
                                {
                                    // extract parameters
                                    var parameterRemainder = display.Substring(indexOfShorthand + 2).Trim();
                                    parameters = new MarkdownMaterialPropertyDrawer.DrawerParameters(("!DRAWER Dummy " + prop.name + " " + parameterRemainder)
                                        .Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries), UseTooltip());
                                    display = display.Substring(0, indexOfShorthand);
                                }
                                
                                var trimmedDisplay = display.Trim(' ', '&');
                                if(prop.type == MaterialProperty.PropType.Texture)
                                {
                                    var drawer = (InlineTextureDrawer) GetCachedDrawer(nameof(InlineTextureDrawer));
                                    if (drawer)
                                    {
                                        if (isShorthandWithParameters)
                                        {
                                            drawer.OnDrawerGUI(materialEditor, properties, parameters);
                                            
                                            // exclude parameter properties from further regular rendering
                                            for (int k = 1; k < parameters.Count; k++)
                                            {
                                                var j = k;
                                                var referencedProperty = properties.FirstOrDefault(x => x.name == parameters.Get(j, ""));
                                                if (referencedProperty != null && !referencedProperties.Contains(referencedProperty))
                                                    referencedProperties.Add(referencedProperty);
                                            }
                                        }
                                        else
                                        {
                                            MaterialProperty extraProperty = null;
                                            if (shouldDrawMultiplePropertiesInline)
                                            {
                                                extraProperty = (i + 1 < group.properties.Count) ? group.properties[i + 1] : null;

                                                if (extraProperty != null)
                                                {
                                                    if (extraProperty.flags.HasFlag(MaterialProperty.PropFlags.HideInInspector))
                                                    {
                                                        extraProperty = null;
                                                    }
                                                    else if (GetMarkdownType(extraProperty.displayName) == MarkdownProperty.Drawer)
                                                    {
                                                        extraProperty = null;
                                                        nextPropertyDrawerShouldBeInline = true;
                                                    }
                                                    // check if this is a special property, not supported right now
                                                    // also we don't want to draw if hidden (e.g. unity_Lightmaps)
                                                    else if (GetMarkdownType(extraProperty.displayName) != MarkdownProperty.None)
                                                    {
                                                        extraProperty = null;
                                                    }

                                                    // add this to the referenced list so we don't draw it twice
                                                    if (extraProperty != null && !referencedProperties.Contains(extraProperty))
                                                    {
                                                        referencedProperties.Add(extraProperty);
                                                    }
                                                }
                                            }

                                            drawer.OnDrawerGUI(materialEditor, properties, prop, new GUIContent(showPropertyNames ? prop.name + (extraProperty != null ? " & " + extraProperty.name : "") : trimmedDisplay, UseTooltip()), extraProperty);
                                        }
                                    }
                                }
                                else if (prop.type == MaterialProperty.PropType.Vector)
                                {
                                    var drawer = (VectorSliderDrawer) GetCachedDrawer(nameof(VectorSliderDrawer)); 
                                    if (drawer)
                                    {
                                        drawer.OnDrawerGUI(materialEditor, prop, new GUIContent(showPropertyNames ? prop.name : trimmedDisplay, UseTooltip()));
                                    }
                                }
                            }
                            else
                            {
                                materialEditor.ShaderPropertyWithTooltip(prop, new GUIContent(showPropertyNames ? prop.name : display, UseTooltip()));
                            }
                            previousPropertyWasDrawn = true;
                            break;
                    }
                    EditorGUI.indentLevel = currentIndent;
                    
                    if(isDisabled)
                        EditorGUI.EndDisabledGroup();
                }    
                EditorGUILayout.Space();
                
                DrawGroupMarker.End();
            }

            foreach(var group in headerGroups)
            {
                if (group.properties == null && group.customDrawer == null) continue;
                bool isDisabled = false;
                if (group.condition != null)
                {
                    if (!ConditionIsFulfilled(targetMat, group.condition))
                    {
                        if (debugConditionalProperties)
                            isDisabled = true;
                        else
                            continue;
                    }
                }

                DrawHeaderGroupMarker.Begin();
                if (isDisabled)
                    EditorGUI.BeginDisabledGroup(true);
                EditorGUI.BeginChangeCheck();
                if (string.IsNullOrEmpty(group.name) || group.name.Equals("Default", StringComparison.OrdinalIgnoreCase)) {
                    if(group.customDrawer != null) {
                        group.customDrawer.Invoke();
                    }
                    else {
                        DrawGroup(group);
                        CoreEditorUtils.DrawSplitter();
                    }
                }
                else
                {
                    var keyName = group.FoldoutStateKeyName;
                    var state = SessionState.GetBool(keyName, group.expandedByDefault);
                    bool newState;
                    if (group.customDrawer == DrawDebugGroupContent || group.name == MarkdownToolsLabel)
                    {
                        newState = DrawHeaderFoldout(new GUIContent(group.name), state, false, () => true, null, AttributeDocumentationUrl, pos =>
                            {
                                var menu = new GenericMenu();
                                menu.AddItem(new GUIContent("Show Development Options"), ShowDevelopmentOptions, () => { ShowDevelopmentOptions = !ShowDevelopmentOptions; });
                                menu.DropDown(new Rect(pos, Vector2.zero));
                            });
                    }
                    else {
                        newState = CoreEditorUtils.DrawHeaderFoldout(group.name, state);
                    }
                    
                    if(newState != state) SessionState.SetBool(keyName, newState);
                    state = newState;
                    if (state)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.Space();
                        if (group.customDrawer != null) {
                            group.customDrawer.Invoke();
                            if(group != headerGroups.Last())
                                CoreEditorUtils.DrawSplitter();
                        }
                        else
                            DrawGroup(group);
                        EditorGUI.indentLevel--;
                    }
                    
                    if(group != headerGroups.Last())
                        CoreEditorUtils.DrawSplitter();
                }

                if (EditorGUI.EndChangeCheck())
                    MaterialChanged(materialEditor, properties);

                if(isDisabled)
                    EditorGUI.EndDisabledGroup();
                
                DrawHeaderGroupMarker.End();
            }
            
            OnGUIMarker.End();
        }

        private bool IsDeveloperMode()
        {
            return Unsupported.IsDeveloperMode() || ShowDevelopmentOptions;
        }

        public static string GetBetween(string str, char start, char end, bool last = false)
        {
            var i0 = last ? str.LastIndexOf(start) : str.IndexOf(start);
            var i1 = last ? str.LastIndexOf(end)   : str.IndexOf(end);
            if (i0 < 0 || i1 < 0) return null;
            return str.Substring(i0 + 1, i1 - i0 - 1);
        }

        private static Parser parser;
        // private static Dictionary<string, LogicExpression> expressionCache = new Dictionary<string, LogicExpression>();
        private static bool ConditionIsFulfilled(Material targetMaterial, string conditionExpression)
        {
            bool SetValueForCondition(ExpressionVariable variable, string condition)
            {
                // check if keyword is set
                var keywordIsSet = Array.IndexOf(targetMaterial.shaderKeywords, condition) >= 0;
                if (keywordIsSet)
                {
                    variable.Set(true);
                    return true;
                }

                // also check for global keywords
                if (Shader.IsKeywordEnabled(condition))
                {
                    variable.Set(true);
                    return true;
                }

                var propertyIndex = targetMaterial.shader.FindPropertyIndex(condition);
                if (propertyIndex <= -1)
                {
                    // property not found -
                    // we could search harder (e.g. assume Vector.x/y/z/w or Color.r/g/b/a)
                    // or we warn.
                    // we should still set the value here
                    // variable.Set(0);
                    return false;
                }
                
                var propertyType = targetMaterial.shader.GetPropertyType(propertyIndex);

                switch (propertyType)
                {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        variable.Set(targetMaterial.GetFloat(condition));
                        break;
                    case ShaderPropertyType.Texture:
                        variable.Set(targetMaterial.GetTexture(condition));
                        break;
                    case ShaderPropertyType.Vector:
                        variable.Set(targetMaterial.GetVector(condition).magnitude);
                        break;
                    case ShaderPropertyType.Color:
                        variable.Set(targetMaterial.GetColor(condition).maxColorComponent);
                        break;
                    default:
                        // weird property type in comparison
                        return false;
                }

                return true;
            }

            ConditionCheckMarker.Begin();
            
            // TODO Cache properly
            bool result;
            try
            {
                parser = new Parser(new ParsingContext(false), new ExpressionContext(false));
                var expression = parser.Parse(conditionExpression);

                bool allConditionsResolved = true;
                foreach (var v in expression.Context.Variables)
                {
                    allConditionsResolved &= SetValueForCondition(v.Value, v.Key);
                }

                if (!allConditionsResolved)
                {
                    // TODO show warning
                    // TODO how to see which expressions are actually needed?
                }

                result = expression.GetResult();
            }
            catch (ParseException)
            {
                Debug.LogWarning("Expression parse error for Shader condition: " + conditionExpression + " on " + targetMaterial, targetMaterial);
                result = false;
            }

            ConditionCheckMarker.End();
            return result;
        }
        
        private void CollectLocalAndGlobalKeywords(Material material)
        {
            // TODO properly support keywordSpace and local/global keywords and overrides on 2021.2+            
#if UNITY_2021_2_OR_NEWER
            localKeywords = material.shader.keywordSpace.keywords.ToDictionary(x => x.name, x => x);
            globalKeywords = Shader.globalKeywords.ToDictionary(x => x.name, x => x);
#else
            localKeywords = ((string[]) GetShaderLocalKeywords.Invoke(null, new object[] { material.shader })).ToList();
            globalKeywords = ((string[]) GetShaderGlobalKeywords.Invoke(null, new object[] { material.shader })).ToList();
#endif
        }
        
        protected virtual void MaterialChanged(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialChangedMarker.Begin();
            foreach(var mat in materialEditor.targets)
            {
                var material = (Material) mat;
                if (!material)
                    throw new ArgumentNullException(nameof(materialEditor), "Target material is null for " + materialEditor);

                if (!material.shader) return;
                
                // get keywords for this shader.
                CollectLocalAndGlobalKeywords(material);
                
                // loop through texture properties
                foreach (var materialProperty in properties.Where(x => x.type == MaterialProperty.PropType.Texture))
                {
                    var uppercaseName = materialProperty.name.ToUpperInvariant();
#if UNITY_2021_2_OR_NEWER
                    if (localKeywords.ContainsKey(uppercaseName))
#else
                    if (localKeywords.Contains(uppercaseName))
#endif
                    {
                        if (material.GetTexture(materialProperty.name))
                            material.EnableKeyword(uppercaseName);
                        else
                            material.DisableKeyword(uppercaseName);
                    }
                }
            }
            
            MaterialChangedMarker.End();
        }

        private ShaderGUI baseShaderGui = null;
        private bool haveSearchedForCustomGUI = false;

        // ReSharper disable InconsistentNaming
        private static readonly GUIContent AdditionalOptions = new GUIContent("Additional Options", "Options from the shader that are not part of the usual shader properties, e.g. instancing.");
        private static readonly GUIContent ShowOriginalProperties = new GUIContent("Show Original Properties", "Enable this option to show all properties, even those marked with [HideInInspector].");
        private static readonly GUIContent ShowPropertyNames = new GUIContent("Show Reference Names", "Shortcut: Hold the <shift> key.\nEnable this option to see the actual reference names of all properties. This helps with access from code or animation.");
        private static readonly GUIContent RenameShaderProperties = new GUIContent("Rename Shader Properties", "Change shader property names across your project. Finds and renames property names in existing materials, animations, and scripts using that shader.");
        private static readonly GUIContent DebugConditionalProperties = new GUIContent("Debug Conditional Properties", "Enable this option to show properties that are conditionally filtered out with [SOME_CONDITION].");
        private static readonly GUIContent DebugReferencedProperties = new GUIContent("Debug Referenced Properties", "Enable this option to show properties that are filtered out because they are referenced by drawers (!DRAWER) or inline properties (&&).");
        private static readonly GUIContent RedrawInspector = new GUIContent("Reset Inspector", "After updating some properties and drawers, Unity caches some editor/inspector details. Clicking this button forces a regeneration of the Shader Inspector in such cases.");
        private static readonly GUIContent ShaderKeywords = new GUIContent("Enabled Keywords");
        private static readonly GUIContent ClearKeywords = new GUIContent("Reset Keywords", "Reset keywords currently set on this shader.");
        private static readonly GUIContent LocalKeywords = new GUIContent("Local Keywords");
        private static readonly GUIContent GlobalKeywords = new GUIContent("Global Keywords");
        private static readonly GUIContent DevelopmentOptionsLabel = new GUIContent("Development Options");
        private static readonly GUIContent BaseShaderGUIOptions = new GUIContent("Base Shader GUI");
        private static readonly GUIContent PropertyDrawersAndDecorators = new GUIContent("Property Drawers and Decorators");
        private static readonly GUIContent GroupsAndCategories = new GUIContent("Groups and Categories");
        private static readonly GUIContent ResetFoldoutSessionState = new GUIContent("Reset Foldout SessionState");
        private static readonly GUIContent LocalAndGlobalKeywords = new GUIContent("All Keywords", "All keywords that are defined/used by this shader and globally.");
        private static readonly string MarkdownToolsLabel = "Markdown Tools";
        private static readonly string AttributeDocumentationUrl = "https://github.com/needle-tools/shadergraph-markdown#attribute-reference";
        internal static readonly string PropertyRefactorDocumentationUrl = "https://github.com/needle-tools/shadergraph-markdown#refactoring-shader-properties";
        
        private static ProfilerMarker OnGUIMarker = new ProfilerMarker("OnGUI");
        private static ProfilerMarker GetHashCodeMarker = new ProfilerMarker("Get Hash Code");
        private static ProfilerMarker GenerateHeaderGroupsMarker = new ProfilerMarker("Generate Header Groups");
        private static ProfilerMarker DrawGroupMarker = new ProfilerMarker("Draw Group");
        private static ProfilerMarker DrawCustomGUIMarker = new ProfilerMarker("Draw Custom GUI");
        private static ProfilerMarker DrawDebugGroupContentMarker = new ProfilerMarker("Draw Debug Group Content");
        private static ProfilerMarker DrawHeaderGroupMarker = new ProfilerMarker("Draw Header Groups");
        private static ProfilerMarker MaterialChangedMarker = new ProfilerMarker("Material Changed");
        private static ProfilerMarker ConditionCheckMarker = new ProfilerMarker("Condition Check");
        
        private const string ShowDevelopmentOptionsKey = nameof(MarkdownShaderGUI) + "." + nameof(ShowDevelopmentOptions);
        private const string EmissionKeyword = "_EMISSION";
        private const string EmissionColorPropertyName = "_EmissionColor";
        private const string EmissionMapPropertyName = "_EmissionMap";
        private const string EmissionColorPropertyName2 = "emissiveFactor";
        private const string EmissionMapPropertyName2 = "emissiveTexture";

        private bool ShowDevelopmentOptions
        {
            get => SessionState.GetBool(ShowDevelopmentOptionsKey, false);
            set => SessionState.SetBool(ShowDevelopmentOptionsKey, value);
        }
        // ReSharper restore InconsistentNaming
        
        private void InitializeCustomGUI(Material targetMat)
        {
            // instead of calling base, we need to draw the right inspector here.
            // to figure out which one that is, we need to get info from the ShaderGraph directly - 
            // at least in HDRP, different custom editors are used depending on what modes are selected in ShaderGraph.
            
            // also see HDShaderUtils.cs
            
            //// HDRP 10
            // Decal        Rendering.HighDefinition.DecalGUI
            // Eye          Rendering.HighDefinition.LightingShaderGraphGUI
            // Fabric       Rendering.HighDefinition.LightingShaderGraphGUI
            // Hair         Rendering.HighDefinition.LightingShaderGraphGUI
            // Lit          Rendering.HighDefinition.LitShaderGraphGUI
            // StackLit     Rendering.HighDefinition.LightingShaderGraphGUI
            // LayeredLit   Rendering.HighDefinition.LayeredLitGUI
            // Unlit        Rendering.HighDefinition.HDUnlitGUI
            // TerrainLitGUI
            // AxFGUI
            
            //// HDRP 8
            // UnityEditor.Rendering.HighDefinition.HDLitGUI
            // UnityEditor.Rendering.HighDefinition.HDUnlitGUI
            // UnityEditor.Rendering.HighDefinition.DecalGUI
#if SHADERGRAPH_7_OR_NEWER
            var defaultCustomInspector = MarkdownSGExtensions.GetDefaultCustomInspectorFromShader(targetMat.shader);
            if(!string.IsNullOrEmpty(defaultCustomInspector))
            {
                baseShaderGui = MarkdownSGExtensions.CreateShaderGUI(defaultCustomInspector);
            }
            // remove the "ShaderGraphUIBlock" uiBlock ("Exposed Properties") as we're rendering that ourselves
            // if(!showOriginalPropertyList)
            //     MarkdownHDExtensions.RemoveShaderGraphUIBlock(baseShaderGui);
#endif
            
            haveSearchedForCustomGUI = true;
        }

#if HAVE_VALIDATE_MATERIAL
        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
            if(baseShaderGui != null) baseShaderGui.ValidateMaterial(material);
#else
        public void ValidateMaterial(Material material)
        {
#endif
            // if the material has an emission keyword, this is explicitly controlled;
            // if it doesn't, GI only works when _EmissionColor is present
            if (!(material.HasProperty(EmissionKeyword) || material.HasProperty("_Emission")) && (material.HasProperty(EmissionColorPropertyName) || material.HasProperty(EmissionColorPropertyName2)))
            {
                MaterialEditor.FixupEmissiveFlag(material);
                bool state = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == MaterialGlobalIlluminationFlags.None;
                if (state) material.EnableKeyword(EmissionKeyword);
                else material.DisableKeyword(EmissionKeyword);
            }
        }

        public static void ShowRefactoringWindow(string shaderAssetPath, string inputReferenceName)
        {
            ShaderRefactoringWindow.Show(shaderAssetPath, inputReferenceName);
        }

        internal static bool DrawHeaderFoldout(GUIContent title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null, string documentationURL = "", Action<Vector2> contextAction = null)
        {
#if HAVE_HEADER_FOLDOUT_WITH_DOCS
#if UNITY_2023_1_OR_NEWER
            return CoreEditorUtils.DrawHeaderFoldout(title, state, isBoxed, hasMoreOptions, toggleMoreOptions, false, documentationURL, contextAction);
#else
            return CoreEditorUtils.DrawHeaderFoldout(title, state, isBoxed, hasMoreOptions, toggleMoreOptions, documentationURL, contextAction);
#endif
#else
            return CoreEditorUtilsShim.DrawHeaderFoldout(title, state, isBoxed, hasMoreOptions, toggleMoreOptions, documentationURL, contextAction);
#endif
        }
    }
}
