#if !NO_INTERNALS_ACCESS && UNITY_2019_4_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.ShaderGraph.Serialization;
using UnityEditorInternal;

#else
using UnityEngine.Rendering;
#endif

namespace UnityEditor.ShaderGraph
{
    public static class MarkdownSGExtensions
    {
        
        [MenuItem("internal:CONTEXT/Shader/Remove All Properties", true)]
        static bool RemoveAllPropertiesValidate(MenuCommand command)
        {
            return GetGraphData((Shader) command.context) != null;
        }

        [MenuItem("internal:CONTEXT/Shader/Remove All Properties", false)]
        static void RemoveAllProperties(MenuCommand command)
        {
            var shader = (Shader) command.context;
                
            var graphData = GetGraphData(shader);
            if (graphData == null) return;
                
            if (m_Keywords == null) m_Keywords = typeof(GraphData).GetField("m_Keywords", (BindingFlags) (-1));
            if (m_Properties == null) m_Properties = typeof(GraphData).GetField("m_Properties", (BindingFlags) (-1));
            #if UNITY_2020_2_OR_NEWER
            var keywords = (List<JsonData<ShaderKeyword>>) m_Keywords.GetValue(graphData);
            var properties = (List<JsonData<AbstractShaderProperty>>) m_Properties.GetValue(graphData);
            #else
            var keywords = (List<ShaderKeyword>) m_Keywords.GetValue(graphData);
            var properties = (List<AbstractShaderProperty>) m_Properties.GetValue(graphData);
            #endif
            //#else
            //#endif
            keywords.Clear();
            properties.Clear();
                
            WriteShaderGraphToDisk(shader, graphData);
            AssetDatabase.Refresh();
        }
        
        [MenuItem("internal:CONTEXT/Shader/Add Test Property", true)]
        static bool AddTestPropertyValidate(MenuCommand command)
        {
            return GetGraphData((Shader) command.context) != null;
        }

        [MenuItem("internal:CONTEXT/Shader/Add Test Properties", false)]
        static void AddTestProperty(MenuCommand command)
        {
            var shader = (Shader) command.context;
            
            AddMaterialProperty<bool>(shader, "My Bool");
            AddMaterialProperty<Color>(shader, "My Color");
            AddMaterialProperty<Texture2D>(shader, "My Texture");
            AddMaterialProperty<Vector4>(shader, "My Vector");
            AddMaterialProperty<float>(shader, "My Float");

            AddMaterialKeyword<bool>(shader, "Some Boolean Keyword");
            AddMaterialKeyword<Enum>(shader, "Some Enum Keyword");
        }

        [MenuItem("CONTEXT/Shader/Add Properties Wizard", true)]
        static bool AddPropertiesWizardValidate(MenuCommand command)
        {
            return GetGraphData((Shader) command.context) != null;
        }
        
        [MenuItem("CONTEXT/Shader/Add Properties Wizard", false)]
        static void AddPropertiesWizard(MenuCommand command)
        {
            // open wizard window
            var wizard = ScriptableWizard.DisplayWizard<PropertyWizard>("Add Properties from Material", "Add Properties");
            wizard.targetShader = (Shader) command.context;
        }
        
        // private static PropertyInfo _graphEditorView;
        // static void Stuff()
        // {
        //     var shaderGraphWindows = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
        //     foreach (var window in shaderGraphWindows)
        //     {
        //         if (_graphEditorView == null) _graphEditorView = typeof(MaterialGraphEditWindow).GetProperty("graphEditorView", (BindingFlags) (-1));
        //         var graphEditorView = (GraphEditorView) _graphEditorView?.GetValue(window);
        //         if (graphEditorView == null) continue;
        //
        //         var blackboardProvider = graphEditorView.blackboardProvider;
        //         var blackboard = blackboardProvider.blackboard;
        //     }
        // }

        internal static GraphData GetGraphData(AssetImporter importer)
        {
            var path = importer.assetPath;
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            
    #if UNITY_2020_2_OR_NEWER
            var graph = new GraphData {
                assetGuid = AssetDatabase.AssetPathToGUID(path)
            };
            MultiJson.Deserialize(graph, textGraph);
    #else
            var graph = JsonUtility.FromJson<GraphData>(textGraph);
    #endif
            
            graph.OnEnable();
            graph.ValidateGraph();
            return graph;
        }

        internal static GraphData GetGraphData(Shader shader)
        {
            if (!AssetDatabase.Contains(shader)) return null;
            var assetPath = AssetDatabase.GetAssetPath(shader);
            var assetImporter = AssetImporter.GetAtPath(assetPath);
            if (assetImporter is ShaderGraphImporter shaderGraphImporter)
                return GetGraphData(shaderGraphImporter);

            return null;
        }
        
        internal static void WriteShaderGraphToDisk(Shader shader, GraphData graphData)
        {
    #if UNITY_2020_2_OR_NEWER
            File.WriteAllText(AssetDatabase.GetAssetPath(shader), MultiJson.Serialize(graphData));
    #else
            File.WriteAllText(AssetDatabase.GetAssetPath(shader), JsonUtility.ToJson(graphData));
    #endif
        }

        private static readonly Dictionary<Type, Type> TypeToPropertyTypeMap = new Dictionary<Type, Type>()
        {
            { typeof(Gradient),       typeof(GradientShaderProperty) },
            // { typeof(Matrix),         typeof(Matrix2ShaderProperty) },
            // { typeof(null),           typeof(Matrix3ShaderProperty) },
            { typeof(Matrix4x4),      typeof(Matrix4ShaderProperty) },
            // { typeof(Matrix4x4),      typeof(MatrixShaderProperty) },
            { typeof(TextureSamplerState), typeof(SamplerStateShaderProperty) },
            // { typeof(VirtualTexture), typeof(VirtualTextureShaderProperty) },
            // { typeof(null),           typeof(AbstractShaderProperty) },
            { typeof(bool),           typeof(BooleanShaderProperty) },
            { typeof(Color),          typeof(ColorShaderProperty) },
            { typeof(Cubemap),        typeof(CubemapShaderProperty) },
            { typeof(Texture2DArray), typeof(Texture2DArrayShaderProperty) },
            { typeof(Texture2D),      typeof(Texture2DShaderProperty) },
            { typeof(Texture3D),      typeof(Texture3DShaderProperty) },
            { typeof(float),          typeof(Vector1ShaderProperty) },
            { typeof(Vector2),        typeof(Vector2ShaderProperty) },
            { typeof(Vector3),        typeof(Vector3ShaderProperty) },
            { typeof(Vector4),        typeof(Vector4ShaderProperty) },
            // { typeof(float),          typeof(VectorShaderProperty) },
            // { typeof(null),           typeof(MultiJsonInternal.UnknownShaderPropertyType) },
        };


        private static FieldInfo m_Properties, m_Keywords;
        
        public static void AddMaterialKeyword<T>(Shader shader, string displayName, string referenceName = null)
        {
            var keywordType = typeof(T);
            
            var graphData = GetGraphData(shader);
            if (graphData == null) return;
            if (m_Keywords == null) m_Keywords = typeof(GraphData).GetField("m_Keywords", (BindingFlags) (-1));
            var keywords =
#if UNITY_2020_2_OR_NEWER
                (List<JsonData<ShaderKeyword>>)
#else
                (List<ShaderKeyword>)
#endif
                m_Keywords.GetValue(graphData);

            ShaderKeyword keyword = null;
            if(keywordType == typeof(Enum))
                keyword = new ShaderKeyword(KeywordType.Enum);
            else if (keywordType == typeof(bool))
                keyword = new ShaderKeyword(KeywordType.Boolean);
            else {
                Debug.LogError($"Can't create keyword of type {keywordType}, allowed types are Enum and bool");
                return;
            }
            
            keyword.displayName = displayName;
            if (!string.IsNullOrEmpty(referenceName))
                keyword.overrideReferenceName = referenceName;
            
            // JsonData has an implicit conversion operator
            keywords.Add(keyword);
            
            WriteShaderGraphToDisk(shader, graphData);
            AssetDatabase.Refresh();
        }

        public static void AddMaterialPropertyInternal(Shader shader, Type propertyType, string displayName, string referenceName = null)
        {
            var graphData = GetGraphData(shader);
            if (graphData == null) return;
            if (m_Properties == null) m_Properties = typeof(GraphData).GetField("m_Properties", (BindingFlags) (-1));
            
            var properties = 
#if UNITY_2020_2_OR_NEWER
                (List<JsonData<AbstractShaderProperty>>)
#else
                (List<AbstractShaderProperty>)
#endif
                m_Properties.GetValue(graphData);

            var prop = (AbstractShaderProperty) Activator.CreateInstance(propertyType, true);
            prop.displayName = displayName;
            if (!string.IsNullOrEmpty(referenceName))
                prop.overrideReferenceName = referenceName;
            
            // JsonData has an implicit conversion operator
            properties.Add(prop);

            WriteShaderGraphToDisk(shader, graphData);
            AssetDatabase.Refresh();
        }
        
        public static void AddMaterialPropertyInternal<T>(Shader shader, string displayName, string referenceName = null) where T: AbstractShaderProperty
        {
            AddMaterialPropertyInternal(shader, typeof(T), displayName, referenceName);
        }
        
        public static void AddMaterialProperty<T>(Shader shader, string displayName, string referenceName = null)
        {
            Type foundType = null;
            foreach(var dict in TypeMaps)
            {
                if (dict.ContainsKey(typeof(T))) {
                    foundType = dict[typeof(T)];
                    break;
                }
            }
            
            if(foundType == null)
            {
                Debug.LogError($"Can't add property of type {typeof(T)}: not found in type map. Allowed types: {string.Join("\n", TypeToPropertyTypeMap.Select(x => x.Key + " (" + x.Value + ")"))}");
                return;
            }
            
            AddMaterialPropertyInternal(shader, TypeToPropertyTypeMap[typeof(T)], displayName, referenceName);
        }
        
        public static string GetDefaultCustomInspectorFromShader(Shader shader)
        {
            var graphData = GetGraphData(shader);
            if (graphData == null) return null;
            string customInspector = null;

            foreach (var getter in CustomInspectorGetters)
            {
                var inspector = getter(graphData);
                if (!string.IsNullOrEmpty(inspector)) {
                    customInspector = inspector;
                    break;
                }
            }
            
            // foreach (var target in graphData.allPotentialTargets)
            //     Debug.Log("Potential Target: " + target.displayName);
            
            // HDShaderUtils.IsHDRPShader
            // var shaderID = HDShaderUtils.GetShaderEnumFromShader(shader);
            
            // var hdMetadata = AssetDatabase.LoadAssetAtPath<HDMetadata>(assetPath);
            // Debug.Log("Shader ID: " + shaderID);
            
            // HDSubTarget.Setup

            return customInspector;
        }
        
        private const string CustomEditorGUI = "Needle.MarkdownShaderGUI";
        
        [MenuItem("CONTEXT/Material/Toggle ShaderGraph Markdown", true)]
        static bool ToggleShaderGraphMarkdownValidate(MenuCommand command)
        {
            if (command.context is Material mat)
                return MarkdownSGExtensions.GetGraphData(mat.shader) != null;
            return false;
        }
        
        [MenuItem("CONTEXT/Material/Toggle ShaderGraph Markdown", false, 1000)]
        static void ToggleShaderGraphMarkdown(MenuCommand command)
        {
            if (command.context is Material mat)
            {
                var graphData = MarkdownSGExtensions.GetGraphData(mat.shader);
                
#if UNITY_2020_2_OR_NEWER
                var haveSetInspector = false;
                foreach (var target in graphData.activeTargets)
                {
                    foreach (var setter in CustomInspectorSetters)
                    {
                        if (setter(target, CustomEditorGUI))
                        {
                            haveSetInspector = true;
                            break;
                        }

                        if (haveSetInspector) break;
                    }
                }
#else
                if (graphData.outputNode is UnityEditor.ShaderGraph.MasterNode masterNode)
                {
                    var canChangeShaderGUI = masterNode as UnityEditor.Graphing.ICanChangeShaderGUI;
                    // GenerationUtils.FinalCustomEditorString(canChangeShaderGUI);
                    if (canChangeShaderGUI.OverrideEnabled) 
                    {
                        canChangeShaderGUI.OverrideEnabled = false;
                        canChangeShaderGUI.ShaderGUIOverride = null;
                    }
                    else 
                    {
                        canChangeShaderGUI.OverrideEnabled = true;
                        canChangeShaderGUI.ShaderGUIOverride = CustomEditorGUI;
                    }
                }
#endif
                MarkdownSGExtensions.WriteShaderGraphToDisk(mat.shader, graphData);
                AssetDatabase.Refresh();
            }
        }


        private static readonly List<Dictionary<Type, Type>> TypeMaps = new List<Dictionary<Type, Type>>() { TypeToPropertyTypeMap };
        public static void RegisterTypeMap(Dictionary<Type,Type> typeToPropertyTypeMap)
        {
            if (!TypeMaps.Contains(typeToPropertyTypeMap)) TypeMaps.Add(typeToPropertyTypeMap);
        }

        private static readonly List<Func<GraphData, string>> CustomInspectorGetters = new List<Func<GraphData, string>>();
        
        internal static void RegisterCustomInspectorGetter(Func<GraphData, string> func) 
        {
            CustomInspectorGetters.Add(func);
        }
        
#if UNITY_2020_2_OR_NEWER
        private static readonly List<Func<Target, string, bool>> CustomInspectorSetters = new List<Func<Target, string, bool>>();
        internal static void RegisterCustomInspectorSetter(Func<Target, string, bool> func)
        {
            CustomInspectorSetters.Add(func);
        }
#endif
    }

    [Serializable]
    class PropertyWizard : ScriptableWizard
    {
        public Shader targetShader;
        public Material sourceMaterial;

        private SerializedProperty _targetShader;
        private SerializedProperty _sourceMaterial;
        
        private SerializedObject serializedObject;
        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            createButtonName = "Apply Properties";

            _targetShader = serializedObject.FindProperty("targetShader");
            _sourceMaterial = serializedObject.FindProperty("sourceMaterial");
        }

        private ReorderableList propertyList;

        void OnGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(_targetShader);
            EditorGUILayout.PropertyField(_sourceMaterial);

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();

                if (!sourceMaterial) return;
                
                // rebuild reorderable list
                // propertyList = new ReorderableList()

                var shader = sourceMaterial.shader;
                var propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propertyCount; i++)
                {
                    var propertyLog = ShaderUtil.GetPropertyName(shader, i) + " " + 
                                      "[" + ShaderUtil.GetPropertyType(shader, i) + "] " + 
                                      "(" + ShaderUtil.GetPropertyDescription(shader, i) + ")" + 
                                      (ShaderUtil.IsShaderPropertyHidden(shader, i) ? " (hidden)" : "");
                    Debug.Log(propertyLog);
                }
            }
            
            GUILayout.Label("Property Wizard");
        }

        private void OnWizardCreate()
        {
            
        }
    }
    
    // [Serializable]
    // [BlackboardInputInfo(30000, name = "Markdown/Foldout Header")]
    // public class MarkdownFoldout : MarkdownShaderProperty
    // {
    //     internal override string DisplayName => "# My Foldout";
    // }
    //
    // [Serializable]
    // [BlackboardInputInfo(30001, name = "Markdown/Header")]
    // public class MarkdownHeader : MarkdownShaderProperty
    // {
    //     internal override string DisplayName => "## My Header";
    // }
    //
    // [Serializable]
    // [BlackboardInputInfo(30002, name = "Markdown/Note")]
    // public class MarkdownNote : MarkdownShaderProperty
    // {
    //     internal override string DisplayName => "!NOTE My Note";
    // }
    //
    // public abstract class MarkdownShaderProperty : AbstractShaderProperty<bool>
    // {
    //      internal abstract string DisplayName { get; }
    //      
    //      internal MarkdownShaderProperty()
    //      {
    //          displayName = DisplayName;
    //      }
    //
    //      public override PropertyType propertyType => PropertyType.Boolean;
    //
    //      internal override bool isExposable => true;
    //      internal override bool isRenamable => true;
    //
    //      internal override string GetPropertyAsArgumentString()
    //      {
    //          return $"{concreteShaderValueType.ToShaderString(concretePrecision.ToShaderString())} {referenceName}";
    //      }
    //
    //      internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
    //      {
    //          HLSLDeclaration decl = GetDefaultHLSLDeclaration();
    //          action(new HLSLProperty(HLSLType._float, referenceName, decl, concretePrecision));
    //      }
    //
    //      internal override string GetPropertyBlockString()
    //      {
    //          return $"{hideTagString}[ToggleUI]{referenceName}(\"{displayName}\", Float) = {(value == true ? 1 : 0)}";
    //      }
    //
    //      internal override AbstractMaterialNode ToConcreteNode()
    //      {
    //          return new BooleanNode() { value = new ToggleData(value) };
    //      }
    //
    //      internal override PreviewProperty GetPreviewMaterialProperty()
    //      {
    //          return new PreviewProperty(propertyType)
    //          {
    //              name = referenceName,
    //              booleanValue = value
    //          };
    //      }
    //
    //      internal override ShaderInput Copy()
    //      {
    //          return new BooleanShaderProperty()
    //          {
    //              displayName = displayName,
    //              hidden = hidden,
    //              value = value,
    //              precision = precision,
    //          };
    //      }
    // }
}

#endif