using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using Needle.ShaderGraphMarkdown;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
#if HDRP_7_OR_NEWER
using UnityEditor.Rendering.HighDefinition;
#endif

// we're making an exception here: only Needle namespace, because full namespace
// has to be included in the ShaderGraph custom ui field.
namespace Needle
{
    public class MarkdownShaderGUI : ShaderGUI
    {
        private class HeaderGroup
        {
            public string name;
            public List<MaterialProperty> properties;
            public Action customDrawer = null;
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

        private static string refFormat = "!REF";
        private static string noteFormat = "!NOTE";
        private static string alternateNoteFormat = "* ";
        private static string drawerFormat = "!DRAWER";
        private static string foldoutHeaderFormat = "#";
        private static string foldoutHeaderFormatStart = foldoutHeaderFormat + " ";
        private static string headerFormatStart = "##" + " ";

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
            if (display.StartsWith(headerFormatStart))
                return MarkdownProperty.Header;
            return MarkdownProperty.None;
        }
        
        private static Dictionary<string, MarkdownProperty> propertyTypeCache = new Dictionary<string, MarkdownProperty>();
        internal static MarkdownProperty GetMarkdownType(string display)
        {
            if (propertyTypeCache.ContainsKey(display)) return propertyTypeCache[display];
            
            // markdown blockquote: >
            // markdown footnote: [^MyNote] Hello I'm a footnote

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
        
        // property drawer debugging
        private bool debugPropertyDrawers = false;
        private static Type MaterialPropertyHandler;
        private static MethodInfo GetHandler;
        private static FieldInfo m_PropertyDrawer, m_DecoratorDrawers;

        private new static MaterialProperty FindProperty(string keywordRef, MaterialProperty[] properties)
        {
            var keywordProp = ShaderGUI.FindProperty(keywordRef, properties, false);
            
            // special case: bool properties have to be named MY_PROP_ON in ShaderGraph to be exposed,
            // but the actual property is named "MY_PROP" and the keyword is still "MY_PROP_ON".
            if (keywordProp == null && keywordRef.EndsWith("_ON", StringComparison.Ordinal))
                keywordProp = ShaderGUI.FindProperty(keywordRef.Substring(0, keywordRef.Length - "_ON".Length), properties, false);
            
            return keywordProp;
        }

        private static MarkdownMaterialPropertyDrawer GetCachedDrawer(string objectName)
        {
            if(!drawerCache.ContainsKey(objectName)) {
                var objectPath = AssetDatabase.FindAssets($"t:{nameof(MarkdownMaterialPropertyDrawer)} {objectName}").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
                var scriptableObject = AssetDatabase.LoadAssetAtPath<MarkdownMaterialPropertyDrawer>(objectPath);
                if (scriptableObject == null) {
                    // create a drawer instance in memory
                    var drawerType = TypeCache
                        .GetTypesDerivedFrom<MarkdownMaterialPropertyDrawer>()
                        .FirstOrDefault(x => x.Name.Equals(objectName, StringComparison.Ordinal));
                    scriptableObject = (MarkdownMaterialPropertyDrawer) ScriptableObject.CreateInstance(drawerType);

                    if (scriptableObject == null && !objectName.EndsWith("Drawer", StringComparison.Ordinal))
                    {
                        var longName = objectName + "Drawer";
                        if (drawerCache.ContainsKey(longName))
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
            
            int GetHashCode()
            {
                var hashCode = targetMat.name.GetHashCode();
                hashCode = (hashCode * 397) ^ (targetMat.shader ? targetMat.shader.name.GetHashCode() : 0);
                foreach(var prop in properties)
                {
                    hashCode = (hashCode * 397) ^ prop.name.GetHashCode();
                    hashCode = (hashCode * 397) ^ prop.displayName.GetHashCode();
                    hashCode = (hashCode * 397) ^ prop.rangeLimits.GetHashCode();
                    hashCode = (hashCode * 397) ^ prop.flags.GetHashCode();
                }
                return hashCode;
            }

            int currentHash = GetHashCode();
            if (lastHash != currentHash) {
                lastHash = currentHash;
                headerGroups = null;
            }
            
            // split by header properties
            if(headerGroups == null)
            {
                headerGroups = new List<HeaderGroup>();
                headerGroups.Add(new HeaderGroup() { name = "Default" });
                
                foreach (var prop in properties)
                {
                    if(prop.displayName.StartsWith(foldoutHeaderFormatStart) || prop.displayName.Equals(foldoutHeaderFormat, StringComparison.Ordinal))
                    {
                        if (prop.displayName.Equals(foldoutHeaderFormat, StringComparison.Ordinal)  || 
                            (prop.displayName.StartsWith(foldoutHeaderFormatStart + "(", StringComparison.Ordinal) && prop.displayName.EndsWith(")", StringComparison.Ordinal))) // for multiple ## (1) foldout breakers)
                            headerGroups.Add(new HeaderGroup() { name = null });
                        else
                            headerGroups.Add(new HeaderGroup() { name = prop.displayName.Substring(prop.displayName.IndexOf(' ') + 1) });
                    }
                    else
                    {
                        var last = headerGroups.Last();
                        if (last.properties == null) 
                            last.properties = new List<MaterialProperty>();
                        
                        // need to process REF/DRAWER properties early so we can hide them properly if needed
                        var display = prop.displayName.TrimStart('-');
                        
                        if (display.StartsWith(refFormat))
                        {
                            var split = display.Split(' ');
                            if(split.Length > 1) {
                                var keywordRef = split[1];
                                var keywordProp = FindProperty(keywordRef, properties);
                                if(keywordProp != null)
                                    referencedProperties.Add(keywordProp);
                            }
                        }

                        if (display.StartsWith(drawerFormat))
                        {
                            var split = display.Split(' ');
                            if (split.Length > 1)
                            {
                                var objectName = split[1];
                                var drawer = GetCachedDrawer(objectName);
                                if (drawer != null)
                                {
                                    var referencedProps = drawer.GetReferencedProperties(materialEditor, properties, new MarkdownMaterialPropertyDrawer.DrawerParameters(split));
                                    if(referencedProps != null)
                                        referencedProperties.AddRange(referencedProps);
                                }
                            }
                        }

                        last.properties.Add(prop);
                    }
                }
                headerGroups.Add(new HeaderGroup() { name = null, properties = null, customDrawer = DrawCustomGUI });
                headerGroups.Add(new HeaderGroup() { name = "Debug", properties = null, customDrawer = DrawDebugGroupContent });
            }
            string GetBetween(string str, char start, char end, bool last = false)
            {
                var i0 = last ? str.LastIndexOf(start) : str.IndexOf(start);
                var i1 = last ? str.LastIndexOf(end)   : str.IndexOf(end);
                if (i0 < 0 || i1 < 0) return null;
                return str.Substring(i0 + 1, i1 - i0 - 1);
            }
            
            void DrawDebugGroupContent()
            {
                EditorGUI.BeginChangeCheck();
                showOriginalPropertyList = EditorGUILayout.Toggle("Show Original Properties", showOriginalPropertyList);
                if (EditorGUI.EndChangeCheck())
                    InitializeCustomGUI(targetMat);    
                debugConditionalProperties = EditorGUILayout.Toggle("Debug Conditional Properties", debugConditionalProperties);
                debugReferencedProperties = EditorGUILayout.Toggle("Debug Referenced Properties", debugReferencedProperties);
                
                EditorGUILayout.Space();
                if (GUILayout.Button("Clear Inspector Cache")) {
                    drawerCache.Clear();
                    headerGroups = null;
                }                
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Shader Keywords", EditorStyles.boldLabel);
                foreach (var kw in targetMat.shaderKeywords)
                {
                    EditorGUILayout.LabelField(kw, EditorStyles.miniLabel);
                }

                if (GUILayout.Button("Clear Keywords"))
                {
                    foreach (var kw in targetMat.shaderKeywords)
                        targetMat.DisableKeyword(kw);
                }

                #if HDRP_7_OR_NEWER
                if (GUILayout.Button("Reset Keywords"))
                {
                    HDShaderUtils.ResetMaterialKeywords(targetMat);
                }
                EditorGUILayout.Space();
                #endif

                
                EditorGUILayout.Space();

                debugPropertyDrawers = EditorGUILayout.Foldout(debugPropertyDrawers, "Property Drawers and Decorators");
                if(debugPropertyDrawers)
                {
                    foreach (var prop in properties)
                    {
                        // TextureScaleOffsetProperty

                        if(MaterialPropertyHandler == null) MaterialPropertyHandler = typeof(MaterialProperty).Assembly.GetType("UnityEditor.MaterialPropertyHandler");
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
                // #if HDRP_7_OR_NEWER
                // EditorGUILayout.LabelField("ShaderGraph Info", EditorStyles.boldLabel);
                // if (GUILayout.Button("Refresh"))
                // {
                //     Debug.Log(MarkdownHDExtensions.GetDefaultCustomInspectorFromShader(targetMat.shader));
                // }
                // #endif
            }
            
            void DrawCustomGUI()
            {
                if (!haveSearchedForCustomGUI)
                    InitializeCustomGUI(targetMat);
            
                if(baseShaderGui != null) {
                    EditorGUILayout.Space();
                    CoreEditorUtils.DrawSplitter();
                    EditorGUILayout.LabelField("Additional Options", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                
                    // only pass in the properties that are hidden - this allows us to render all custom HDRP ShaderGraph UIs.
                    baseShaderGui?.OnGUI(materialEditor, showOriginalPropertyList ? properties : properties
                        .Where(x => x.flags.HasFlag(MaterialProperty.PropFlags.HideInInspector))
                        .ToArray());
                    CoreEditorUtils.DrawSplitter();
                }
            }

            void DrawGroup(HeaderGroup group)
            {
                bool previousPropertyWasDrawn = true;
                
                for(int i = 0; i < group.properties.Count; i++)
                {
                    var prop = group.properties[i];
                    var display = prop.displayName;
                    var isDisabled = false;
                    var hasCondition = !display.StartsWith("[", StringComparison.Ordinal) && display.Contains('[') && display.EndsWith("]", StringComparison.Ordinal);
                    if(hasCondition) {
                        var condition = GetBetween(display, '[', ']', true);
                        if (!string.IsNullOrEmpty(condition))
                        {
                            var keywordIsSet = Array.IndexOf(targetMat.shaderKeywords, condition) >= 0;
                            var boolIsSet = false;
                            var textureIsSet = false;
                            
                            // support for using bool/float/texture values as conditionals
                            if(!keywordIsSet && targetMat.shader.FindPropertyIndex(condition) > -1) {
                                var propertyIndex = targetMat.shader.FindPropertyIndex(condition);
                                var propertyType = targetMat.shader.GetPropertyType(propertyIndex);
                                
                                if(propertyType == ShaderPropertyType.Float && targetMat.GetFloat(condition) > 0.5f)
                                    boolIsSet = true;

                                if (propertyType == ShaderPropertyType.Texture && targetMat.GetTexture(condition))
                                    textureIsSet = true;
                            }
                            
                            var conditionIsFulfilled = keywordIsSet || boolIsSet || textureIsSet;
                            if (!conditionIsFulfilled)
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
                                    try {
                                        drawer.OnDrawerGUI(materialEditor, properties, new MarkdownMaterialPropertyDrawer.DrawerParameters(parts));
                                    }
                                    catch (Exception e) {
                                        EditorGUILayout.HelpBox("Error in Custom Drawer \"" + objectName + "\": " + e, MessageType.Error);
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
                            var labelName = display.Substring(display.IndexOf(' ') + 1);
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField(labelName, EditorStyles.boldLabel);
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
                        case MarkdownProperty.None:
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

                            // drawer shorthands
                            if (display.EndsWith("&", StringComparison.Ordinal))
                            {
                                bool shouldDrawMultiplePropertiesInline = display.EndsWith("&&", StringComparison.Ordinal);
                                var trimmedDisplay = display.Trim(' ', '&');
                                if(prop.type == MaterialProperty.PropType.Texture)
                                {
                                    // special drawer for inline textures: InlineTextureDrawer
                                    var drawer = (InlineTextureDrawer) GetCachedDrawer(nameof(InlineTextureDrawer));
                                    if (drawer)
                                    {
                                        MaterialProperty extraProperty = null;
                                        if(shouldDrawMultiplePropertiesInline)
                                        {
                                            extraProperty = (i + 1 < group.properties.Count) ? group.properties[i + 1] : null;
                                            if(extraProperty != null)
                                            {
                                                // check if this is a special property, not supported right now
                                                if (GetMarkdownType(extraProperty.displayName) != MarkdownProperty.None)
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
                                        drawer.OnDrawerGUI(materialEditor, prop, trimmedDisplay, extraProperty);
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
                                materialEditor.ShaderProperty(prop, display);
                            }
                            previousPropertyWasDrawn = true;
                            break;
                    }
                    EditorGUI.indentLevel = currentIndent;
                    
                    if(isDisabled)
                        EditorGUI.EndDisabledGroup();
                }    
                EditorGUILayout.Space();
            }
            
            foreach(var group in headerGroups)
            {
                if (group.properties == null && group.customDrawer == null) continue;

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
                    var keyName = $"{nameof(MarkdownShaderGUI)}.{group.name}";
                    var state = SessionState.GetBool(keyName, true);
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
            }
        }

        private ShaderGUI baseShaderGui = null;
        private bool haveSearchedForCustomGUI = false;
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
             
            #if HDRP_7_OR_NEWER
            var defaultCustomInspector = MarkdownSGExtensions.GetDefaultCustomInspectorFromShader(targetMat.shader);
            if(!string.IsNullOrEmpty(defaultCustomInspector))
            {
                if (!defaultCustomInspector.StartsWith("UnityEditor."))
                    defaultCustomInspector = "UnityEditor." + defaultCustomInspector;
                var litGui = typeof(HDShaderUtils).Assembly.GetType(defaultCustomInspector);
                baseShaderGui = (ShaderGUI) Activator.CreateInstance(litGui);
            }
            // remove the "ShaderGraphUIBlock" uiBlock ("Exposed Properties") as we're rendering that ourselves
            // if(!showOriginalPropertyList)
            //     MarkdownHDExtensions.RemoveShaderGraphUIBlock(baseShaderGui);
            #endif
            
            haveSearchedForCustomGUI = true;
        }
    }
}