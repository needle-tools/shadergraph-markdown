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
            public string FoldoutStateKeyName => $"{nameof(MarkdownShaderGUI)}.{name}";
            
            public HeaderGroup(string displayName, string condition = null)
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
            Foldout
        }

        private static readonly string refFormat = "!REF";
        private static readonly string noteFormat = "!NOTE";
        private static readonly string alternateNoteFormat = "* ";
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
            debugConditionalProperties = false,
            debugReferencedProperties = false;
        
        private bool debugPropertyDrawers = false;
        
        // Reflection Access
        // ReSharper disable InconsistentNaming
        private static Type MaterialPropertyHandler;
        private static MethodInfo GetHandler;
        private static FieldInfo m_PropertyDrawer, m_DecoratorDrawers;
        private bool debugLocalAndGlobalKeywords = false;

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
        // ReSharper restore InconsistentNaming
             
        private new static MaterialProperty FindProperty(string keywordRef, MaterialProperty[] properties)
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
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var targetMat = materialEditor.target as Material;
            if (!targetMat) return;

            OnGUIMarker.Begin();
            
            // proper widths for texture and label fields, same as ShaderGUI
            EditorGUIUtility.fieldWidth = 64f;
            
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
                        var last = headerGroups.Last();
                        if (last.properties == null) 
                            last.properties = new List<MaterialProperty>();
                        
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
                                    var keywordProp = FindProperty(keywordRef, properties);
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
                        last.properties.Add(prop);
                    }
                }
                headerGroups.Add(new HeaderGroup(null) { properties = null, customDrawer = DrawCustomGUI });
                headerGroups.Add(new HeaderGroup("Debug") { properties = null, customDrawer = DrawDebugGroupContent, expandedByDefault = false});
                
                GenerateHeaderGroupsMarker.End();
            }
            
            void DrawDebugGroupContent()
            {
                DrawDebugGroupContentMarker.Begin();
                
                EditorGUI.BeginChangeCheck();
                showOriginalPropertyList = EditorGUILayout.Toggle(ShowOriginalProperties, showOriginalPropertyList);
                if (EditorGUI.EndChangeCheck())
                    InitializeCustomGUI(targetMat);    
                debugConditionalProperties = EditorGUILayout.Toggle(DebugConditionalProperties, debugConditionalProperties);
                debugReferencedProperties = EditorGUILayout.Toggle(DebugReferencedProperties, debugReferencedProperties);
                
                EditorGUILayout.Space();
                if (GUILayout.Button(RedrawInspector))
                {
                    drawerCache.Clear();
                    headerGroups.Clear();
                    GUIUtility.ExitGUI();
                }                
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(ShaderKeywords, EditorStyles.boldLabel);
                foreach (var kw in targetMat.shaderKeywords)
                {
                    EditorGUILayout.LabelField(kw, EditorStyles.miniLabel);
                }

                if (GUILayout.Button(ClearKeywords))
                {
                    foreach (var kw in targetMat.shaderKeywords)
                        targetMat.DisableKeyword(kw);
                    
#if HDRP_7_OR_NEWER
                    try
                    {
                        HDShaderUtils.ResetMaterialKeywords(targetMat);
                    }
                    catch (ArgumentException _)
                    {
                        // ignore, not a HDRP shader probably
                    }
#endif
                }

                EditorGUILayout.Space();

                debugLocalAndGlobalKeywords = EditorGUILayout.Foldout(debugLocalAndGlobalKeywords, LocalAndGlobalKeywords);
                if(debugLocalAndGlobalKeywords)
                {
                    EditorGUILayout.LabelField(LocalKeywords, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    var localKeywords = (string[]) GetShaderLocalKeywords.Invoke(null, new object[] { targetMat.shader });
                    foreach (var kw in localKeywords)
                    {
                        EditorGUILayout.LabelField(kw, EditorStyles.miniLabel);
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.LabelField(GlobalKeywords, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    var globalKeywords = (string[]) GetShaderGlobalKeywords.Invoke(null, new object[] { targetMat.shader });
                    foreach (var kw in globalKeywords)
                    {
                        EditorGUILayout.LabelField(kw, EditorStyles.miniLabel);
                    }
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
                
                if(Unsupported.IsDeveloperMode())
                {
                    debugPropertyDrawers = EditorGUILayout.Foldout(debugPropertyDrawers, PropertyDrawersAndDecorators);
                    if(debugPropertyDrawers)
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
                    
                    EditorGUILayout.Space();
                    if (GUILayout.Button(ResetFoldoutSessionState))
                    {
                        foreach(var group in headerGroups)
                        {
                            SessionState.EraseBool(group.FoldoutStateKeyName);
                        }
                    }
                }
                
                // #if HDRP_7_OR_NEWER
                // EditorGUILayout.LabelField("ShaderGraph Info", EditorStyles.boldLabel);
                // if (GUILayout.Button("Refresh"))
                // {
                //     Debug.Log(MarkdownHDExtensions.GetDefaultCustomInspectorFromShader(targetMat.shader));
                // }
                // #endif
                
                DrawDebugGroupContentMarker.End();
            }
            
            void DrawCustomGUI()
            {
                DrawCustomGUIMarker.Begin();
                
                if (!haveSearchedForCustomGUI)
                    InitializeCustomGUI(targetMat);
            
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(AdditionalOptions, EditorStyles.boldLabel);
                EditorGUILayout.Space();
                
                // from MaterialEditor.PropertiesDefaultGUI(properties);
                if (SupportedRenderingFeatures.active.editableMaterialRenderQueue)
                    materialEditor.RenderQueueField();
                materialEditor.EnableInstancingField();
                materialEditor.DoubleSidedGIField();
                EditorGUILayout.Space();
                CoreEditorUtils.DrawSplitter();
        
#if HDRP_7_OR_NEWER
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
                    EditorGUILayout.HelpBox("Exception when drawing base shader GUI of type " + baseShaderGui.GetType() + ":\n" + e, MessageType.Error);
                }
#endif        
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
                    if(hasCondition)
                    {
                        var condition = GetBetween(display, '[', ']', true);
                        if (!string.IsNullOrEmpty(condition))
                        {
                            if (!ConditionIsFulfilled(targetMat, condition))
                            {
                                if(!debugConditionalProperties) {
                                    previousPropertyWasDrawn = false;
                                    continue;
                                }
                                else {
                                    isDisabled = true;
                                    EditorGUI.BeginDisabledGroup(true);
                                }
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
                            if (GUILayout.Button(linkText, CenteredGreyMiniLabel)) Application.OpenURL(linkHref);
                            break;
                        case MarkdownProperty.Note:
                            if (!previousPropertyWasDrawn) continue;
                            var index = display.IndexOf(' ');
                            var noteText = display.Substring(index + 1);
                            EditorGUILayout.LabelField(noteText, CenteredGreyMiniLabel);
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
                                                drawer.OnInlineDrawerGUI(InlineTextureDrawer.LastInlineTextureRect, materialEditor, properties, new MarkdownMaterialPropertyDrawer.DrawerParameters(parts));
                                                EditorGUI.indentLevel = previousIndent;
                                            }
                                            else
                                            {
                                                EditorGUI.LabelField(InlineTextureDrawer.LastInlineTextureRect, drawer + " doesn't support inline drawing", EditorStyles.miniLabel);
                                                drawer.OnDrawerGUI(materialEditor, properties, new MarkdownMaterialPropertyDrawer.DrawerParameters(parts));
                                            }
                                        }
                                        else
                                        {
                                            drawer.OnDrawerGUI(materialEditor, properties, new MarkdownMaterialPropertyDrawer.DrawerParameters(parts));
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
                                    EditorGUILayout.LabelField(labelName);
                                }
                                else
                                {
                                    EditorGUILayout.Space();
                                    EditorGUILayout.LabelField(labelName, EditorStyles.boldLabel);
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
                            if(split.Length > 1) {
                                var keywordRef = split[1];
                                var keywordProp = FindProperty(keywordRef, properties);
                                // special case: this is a texture prop. 2nd argument could be a color or float, and we want to draw it inline
                                // if it's a texture prop without 2nd arg, we still draw the "small texture" version
                                if(keywordProp == null)
                                    EditorGUILayout.HelpBox("Could not find MaterialProperty: '" + keywordRef, MessageType.Error);
                                else
                                    materialEditor.ShaderProperty(keywordProp, keywordProp.displayName);
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
                                        .Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries));
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
                                                var referencedProperty = properties.FirstOrDefault(x => x.name == parameters.Get(k, ""));
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

                                            drawer.OnDrawerGUI(materialEditor, properties, prop, trimmedDisplay, extraProperty);
                                        }
                                    }
                                }
                                else if (prop.type == MaterialProperty.PropType.Vector)
                                {
                                    var drawer = (VectorSliderDrawer) GetCachedDrawer(nameof(VectorSliderDrawer)); 
                                    if (drawer)
                                    {
                                        drawer.OnDrawerGUI(materialEditor, prop, trimmedDisplay);
                                    }
                                }
                            }
                            else
                            {
                                materialEditor.ShaderProperty(prop, new GUIContent(display));
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
                if (group.name == null || group.name.Equals("Default", StringComparison.OrdinalIgnoreCase)) {
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
                    var newState = CoreEditorUtils.DrawHeaderFoldout(group.name, state);
                    if(newState != state) SessionState.SetBool(keyName, newState);
                    state = newState;
                    if (state)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.Space();
                        if (group.customDrawer != null) {
                            group.customDrawer.Invoke();
                            CoreEditorUtils.DrawSplitter();
                        }
                        else
                            DrawGroup(group);
                        EditorGUI.indentLevel--;
                    }
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
        
        private static string GetBetween(string str, char start, char end, bool last = false)
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
                var keywordIsSet = Array.IndexOf(targetMaterial.shaderKeywords, condition) >= 0;
                if (keywordIsSet)
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
            var result = false;
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
        
        protected virtual void MaterialChanged(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialChangedMarker.Begin();
            foreach(var mat in materialEditor.targets)
            {
                var material = (Material) mat;
                if (!material)
                    throw new ArgumentNullException(nameof(materialEditor), "Target material is null for " + materialEditor);

                if (!material.shader) return;
                
                // set keywords based on texture names
                var localKeywords = ((string[]) GetShaderLocalKeywords.Invoke(null, new object[] { material.shader })).ToList();
                
                // loop through texture properties
                foreach (var materialProperty in properties.Where(x => x.type == MaterialProperty.PropType.Texture))
                {
                    var uppercaseName = materialProperty.name.ToUpperInvariant();
                    if (localKeywords.Contains(uppercaseName))
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
        private static readonly GUIContent DebugConditionalProperties = new GUIContent("Debug Conditional Properties", "Enable this option to show properties that are conditionally filtered out with [SOME_CONDITION].");
        private static readonly GUIContent DebugReferencedProperties = new GUIContent("Debug Referenced Properties", "Enable this option to show properties that are filtered out because they are referenced by drawers (!DRAWER) or inline properties (&&).");
        private static readonly GUIContent RedrawInspector = new GUIContent("Redraw Shader Inspector", "After updating some properties and drawers, Unity caches some editor/inspector details. Clicking this button forces a regeneration of the Shader Inspector in such cases.");
        private static readonly GUIContent ShaderKeywords = new GUIContent("Shader Keywords");
        private static readonly GUIContent ClearKeywords = new GUIContent("Clear Keywords", "Reset keywords currently set on this shader.");
        private static readonly GUIContent LocalKeywords = new GUIContent("Local Keywords");
        private static readonly GUIContent GlobalKeywords = new GUIContent("Global Keywords");
        private static readonly GUIContent PropertyDrawersAndDecorators = new GUIContent("Property Drawers and Decorators");
        private static readonly GUIContent ResetFoldoutSessionState = new GUIContent("Reset Foldout SessionState");
        private static readonly GUIContent LocalAndGlobalKeywords = new GUIContent("Local and Global Keywords", "All keywords that are defined/used by this shader.");

        private static ProfilerMarker OnGUIMarker = new ProfilerMarker("OnGUI");
        private static ProfilerMarker GetHashCodeMarker = new ProfilerMarker("Get Hash Code");
        private static ProfilerMarker GenerateHeaderGroupsMarker = new ProfilerMarker("Generate Header Groups");
        private static ProfilerMarker DrawGroupMarker = new ProfilerMarker("Draw Group");
        private static ProfilerMarker DrawCustomGUIMarker = new ProfilerMarker("Draw Custom GUI");
        private static ProfilerMarker DrawDebugGroupContentMarker = new ProfilerMarker("Draw Debug Group Content");
        private static ProfilerMarker DrawHeaderGroupMarker = new ProfilerMarker("Draw Header Groups");
        private static ProfilerMarker MaterialChangedMarker = new ProfilerMarker("Material Changed");
        private static ProfilerMarker ConditionCheckMarker = new ProfilerMarker("Condition Check");
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
             
            var defaultCustomInspector = MarkdownSGExtensions.GetDefaultCustomInspectorFromShader(targetMat.shader);
            if(!string.IsNullOrEmpty(defaultCustomInspector))
            {
                baseShaderGui = MarkdownSGExtensions.CreateShaderGUI(defaultCustomInspector);
            }
            // remove the "ShaderGraphUIBlock" uiBlock ("Exposed Properties") as we're rendering that ourselves
            // if(!showOriginalPropertyList)
            //     MarkdownHDExtensions.RemoveShaderGraphUIBlock(baseShaderGui);
            
            haveSearchedForCustomGUI = true;
        }
    }
    
#if !SHADERGRAPH_7_OR_NEWER
    // From CoreEditorTools, shimmed here for Built-In support
    internal static class CoreEditorUtils
    {
        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            float xMin = rect.xMin;

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;

            if (isBoxed)
            {
                rect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }
        
        public static bool DrawHeaderFoldout(string title, bool state)
        {
            const float height = 17f;
            var backgroundRect = GUILayoutUtility.GetRect(1f, height);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;
            foldoutRect.x = labelRect.xMin + 15 * (EditorGUI.indentLevel - 1); //fix for presset

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }

            return state;
        }
    }
#endif
}