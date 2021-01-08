#if !NO_INTERNALS_ACCESS && UNITY_2019_4_OR_NEWER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using UnityEditorInternal;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.Rendering;

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

        // commented out entries are either duplicates or don't have matching Unity types
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

        public static void AddMaterialProperty(Shader shader, Type propertyType, string displayName, string referenceName = null)
        {
            Type foundType = null;
            foreach(var dict in TypeMaps)
            {
                if (dict.ContainsKey(propertyType)) {
                    foundType = dict[propertyType];
                    break;
                }
            }
            
            if(foundType == null)
            {
                Debug.LogError($"Can't add property of type {propertyType}: not found in type map. Allowed types: {string.Join("\n", TypeToPropertyTypeMap.Select(x => x.Key + " (" + x.Value + ")"))}");
                return;
            }
            
            AddMaterialPropertyInternal(shader, TypeToPropertyTypeMap[propertyType], displayName, referenceName);
        }
        
        public static void AddMaterialProperty<T>(Shader shader, string displayName, string referenceName = null)
        {
            AddMaterialProperty(shader, typeof(T), displayName, referenceName);
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

        [Serializable]
        public class ShaderProperty
        {
            public string name;
            public ShaderUtil.ShaderPropertyType propertyType;
            public string description;
            public bool isHidden = false;
        }

        private ReorderableList propertyList;
        [SerializeField]
        private List<ShaderProperty> properties;

        void OnGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(_targetShader);
            EditorGUILayout.PropertyField(_sourceMaterial);

            void Refresh() {
                if (!sourceMaterial) return;
                
                // rebuild reorderable list
                // propertyList = new ReorderableList()

                if (properties == null) properties = new List<ShaderProperty>();
                properties.Clear();
                var shader = sourceMaterial.shader;
                var propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propertyCount; i++)
                {
                    properties.Add(new ShaderProperty()
                    {
                        name = ShaderUtil.GetPropertyName(shader, i),
                        description = ShaderUtil.GetPropertyDescription(shader, i),
                        propertyType = ShaderUtil.GetPropertyType(shader, i),
                        isHidden = ShaderUtil.IsShaderPropertyHidden(shader, i)
                    });
                }

                propertyList = new ReorderableList(serializedObject, serializedObject.FindProperty("properties"), true, true, true, true);
                propertyList.elementHeight = 110;
                propertyList.drawElementCallback += (rect, index, active, focused) =>
                {
                    var element = propertyList.serializedProperty.GetArrayElementAtIndex(index);
                    var name = element.FindPropertyRelative("name");
                    var propertyType = element.FindPropertyRelative("propertyType");
                    var description = element.FindPropertyRelative("description");
                    var isHidden = element.FindPropertyRelative("isHidden");
                    rect.height = 20;
                    EditorGUI.PropertyField(rect, name);
                    rect.y += rect.height;
                    EditorGUI.PropertyField(rect, description);
                    rect.y += rect.height;
                    EditorGUI.PropertyField(rect, propertyType);
                    rect.y += rect.height;
                    EditorGUI.PropertyField(rect, isHidden);
                    rect.width *= 0.5f;
                    
                    if (GUI.Button(rect, "Add to Shader"))
                    {
                        Type ShaderPropertyTypeToType(ShaderUtil.ShaderPropertyType shaderPropertyType)
                        {
                            switch (shaderPropertyType)
                            {
                                case ShaderUtil.ShaderPropertyType.Color:
                                    return typeof(Color);
                                case ShaderUtil.ShaderPropertyType.Float:
                                    return typeof(float);
                                case ShaderUtil.ShaderPropertyType.Range:
                                    return typeof(float);
                                case ShaderUtil.ShaderPropertyType.TexEnv:
                                    return typeof(Texture2D);
                                case ShaderUtil.ShaderPropertyType.Vector:
                                    return typeof(Vector4);
                                default:
                                    return null;
                            }
                        }
                        
                        MarkdownSGExtensions.AddMaterialProperty(targetShader, ShaderPropertyTypeToType(properties[index].propertyType), description.stringValue, name.stringValue);
                    }
                };
            }
            
            GUILayout.Label("Property Wizard");
            if (GUILayout.Button("Refresh"))
                Refresh();

            sp = EditorGUILayout.BeginScrollView(sp);
            if(propertyList != null)
                propertyList.DoLayoutList();
            EditorGUILayout.EndScrollView();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                Refresh();
            }
        }

        private Vector2 sp;
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