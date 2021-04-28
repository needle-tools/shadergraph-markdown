#if !NO_INTERNALS_ACCESS && UNITY_2019_4_OR_NEWER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.ShaderGraph.Serialization;
#else
#endif

namespace UnityEditor.ShaderGraph
{
    public static partial class MarkdownSGExtensions
    {
#region ShaderGraph Property Manipulation API
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

        [InitializeOnLoadMethod]
        static void RegisterBaseTypeMap()
        {
            RegisterTypeMap(TypeToPropertyTypeMap);
        }

        private static readonly List<Dictionary<Type, Type>> TypeMaps = new List<Dictionary<Type, Type>>(); 
        public static void RegisterTypeMap(Dictionary<Type,Type> typeToPropertyTypeMap)
        {
            if (!TypeMaps.Contains(typeToPropertyTypeMap)) TypeMaps.Add(typeToPropertyTypeMap);
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
        }

        private static
#if UNITY_2020_2_OR_NEWER
            List<JsonData<AbstractShaderProperty>>
#else
            List<AbstractShaderProperty>
#endif
        GetProperties(GraphData graphData)
        {
            if (m_Properties == null) m_Properties = typeof(GraphData).GetField("m_Properties", (BindingFlags) (-1));
            var properties = 
#if UNITY_2020_2_OR_NEWER
                (List<JsonData<AbstractShaderProperty>>)
#else
                (List<AbstractShaderProperty>)
#endif
                m_Properties.GetValue(graphData);
            return properties;
        }

        internal static void AddMaterialPropertyInternal(GraphData graphData, Type propertyType, string displayName, string referenceName = null)
        {
            var properties = GetProperties(graphData);

            var prop = (AbstractShaderProperty) Activator.CreateInstance(propertyType, true);
            prop.displayName = displayName;
            if (!string.IsNullOrEmpty(referenceName))
                prop.overrideReferenceName = referenceName;
            
            // JsonData has an implicit conversion operator
            properties.Add(prop);
        }

        public static void RemoveMaterialProperty(Shader shader, string referenceName)
        {
            var graphData = GetGraphData(shader);
            if (graphData == null) return;
            RemoveMaterialProperty(graphData, referenceName);
            WriteShaderGraphToDisk(shader, graphData);
        }

        internal static void RemoveMaterialProperty(GraphData graphData, string referenceName)
        {
            var properties = GetProperties(graphData);
            properties.RemoveAll(x => ((AbstractShaderProperty) x).referenceName.Equals(referenceName, StringComparison.Ordinal));
        }
        
        public static void AddMaterialPropertyInternal(Shader shader, Type propertyType, string displayName, string referenceName = null)
        {
            var graphData = GetGraphData(shader);
            if (graphData == null) return;
            if (propertyType == null) return;
            
            AddMaterialPropertyInternal(graphData, propertyType, displayName, referenceName);

            WriteShaderGraphToDisk(shader, graphData);
        }
        
        public static void AddMaterialPropertyInternal<T>(Shader shader, string displayName, string referenceName = null) where T: AbstractShaderProperty
        {
            AddMaterialPropertyInternal(shader, typeof(T), displayName, referenceName);
        }

        internal static void AddMaterialProperty(GraphData graphData, Type propertyType, string displayName, string referenceName = null)
        {
            AddMaterialPropertyInternal(graphData, FindTypeInTypeMap(propertyType), displayName, referenceName);
        }

        private static Type FindTypeInTypeMap(Type propertyType)
        {
            Type foundType = null;
            foreach(var dict in TypeMaps)
            {
                if (dict == null) continue;
                if (dict.ContainsKey(propertyType)) {
                    foundType = dict[propertyType];
                    break;
                }
            }
            
            if(foundType == null)
            {
                Debug.LogError($"Can't add property of type {propertyType}: not found in type map. Allowed types: {string.Join("\n", TypeToPropertyTypeMap.Select(x => x.Key + " (" + x.Value + ")"))}");
                return null;
            }

            return foundType;
        }
        
        public static void AddMaterialProperty(Shader shader, Type propertyType, string displayName, string referenceName = null)
        {
            AddMaterialPropertyInternal(shader, FindTypeInTypeMap(propertyType), displayName, referenceName);
        }
        
        public static void AddMaterialProperty<T>(Shader shader, string displayName, string referenceName = null)
        {
            AddMaterialProperty(shader, typeof(T), displayName, referenceName);
        }
    }

    [Serializable]
    class PropertyWizard : ScriptableWizard
    {
        public Shader targetShader;
        public Material sourceMaterial;
        public Shader sourceShader;
        public bool hideMatchingProperties = false;

        private SerializedProperty _targetShader;
        private SerializedProperty _sourceMaterial, _sourceShader;
        
        private SerializedObject serializedObject;
        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            createButtonName = "Apply Properties";

            _targetShader = serializedObject.FindProperty("targetShader");
            _sourceMaterial = serializedObject.FindProperty("sourceMaterial");
            _sourceShader = serializedObject.FindProperty("sourceShader");
        }

        [Serializable]
        public class ShaderProperty
        {
            public string name;
            public ShaderUtil.ShaderPropertyType propertyType;
            public string description;
            public bool isHidden = false;
        }

        private Dictionary<string, ShaderProperty> sourceProperties = new Dictionary<string, ShaderProperty>();
        private Dictionary<string, ShaderProperty> targetProperties = new Dictionary<string, ShaderProperty>();

        enum DrawMode
        {
            AddToTarget,
            RemoveFromTarget
        }
        
        void OnGUI()
        {
            serializedObject.Update();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(_sourceMaterial);
            EditorGUILayout.PropertyField(_sourceShader);
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(_targetShader);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            void AddProperty(GraphData graphData, ShaderUtil.ShaderPropertyType shaderPropertyType, string displayName, string referenceName)
            {
                Type ShaderPropertyTypeToType(ShaderUtil.ShaderPropertyType propertyType)
                {
                    switch (propertyType)
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
                        
                MarkdownSGExtensions.AddMaterialProperty(graphData, ShaderPropertyTypeToType(shaderPropertyType), displayName, referenceName);
            }

            void FillPropertyList(Shader shader, ref Dictionary<string, ShaderProperty> props)
            {
                if (props == null) props = new Dictionary<string, ShaderProperty>();
                props.Clear();
                var propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propertyCount; i++)
                {
                    var propName = ShaderUtil.GetPropertyName(shader, i);
                    if (props.ContainsKey(propName))
                    {
                        Debug.LogWarning("Duplicate property: " + propName, shader);
                        return;
                    }
                    
                    props.Add(propName, new ShaderProperty()
                    {
                        name = propName,
                        description = ShaderUtil.GetPropertyDescription(shader, i),
                        propertyType = ShaderUtil.GetPropertyType(shader, i),
                        isHidden = ShaderUtil.IsShaderPropertyHidden(shader, i)
                    });
                }
            }
            
            void Refresh() {
                if (!sourceMaterial && !sourceShader) return;
                
                FillPropertyList(sourceMaterial ? sourceMaterial.shader : sourceShader, ref sourceProperties);
                FillPropertyList(targetShader, ref targetProperties);
            }

            void DrawElement(Rect rect, ShaderProperty property, bool existsInSource, DrawMode mode)
            {
                var c = GUI.color;
                var baseColor = existsInSource ? Color.green : Color.white;
                if (property.isHidden)
                    baseColor.a = 0.5f;
                GUI.color = baseColor;
                rect.height = 20;
                property.name = EditorGUI.TextField(rect, property.name);
                rect.y += rect.height;
                property.description = EditorGUI.TextField(rect, property.description);
                rect.y += rect.height;
                property.propertyType = (ShaderUtil.ShaderPropertyType) EditorGUI.EnumPopup(rect, property.propertyType);
                rect.y += rect.height;
                // property.isHidden = EditorGUI.Toggle(rect, property.isHidden);
                // rect.y += rect.height;
                // rect.width *= 0.5f;
                switch (mode)
                {
                    case DrawMode.AddToTarget:
                        if (GUI.Button(rect, "Add to Target"))
                        {
                            if (!targetProperties.ContainsKey(property.name))
                            {
                                var graphData = MarkdownSGExtensions.GetGraphData(targetShader);
                                AddProperty(graphData, property.propertyType, property.description, property.name);
                                MarkdownSGExtensions.WriteShaderGraphToDisk(targetShader, graphData);
                            }
                            else
                            {
                                Debug.LogWarning("Property already exists: " + property.name + ", can't add again.");
                            }
                        }
                        break;
                    case DrawMode.RemoveFromTarget:
                        if (GUI.Button(rect, "Remove from Target"))
                        {
                            var graphData = MarkdownSGExtensions.GetGraphData(targetShader);
                            MarkdownSGExtensions.RemoveMaterialProperty(graphData, property.name);
                            MarkdownSGExtensions.WriteShaderGraphToDisk(targetShader, graphData);
                        }
                        break;
                }
                GUI.color = c;
            }
            
            GUILayout.Label("Property Wizard");
            if (GUILayout.Button("Refresh"))
                Refresh();
            if(GUILayout.Button("Add All Source Properties to Target"))
            {
                var graphData = MarkdownSGExtensions.GetGraphData(targetShader);
                
                foreach (var kvp in sourceProperties)
                {
                    var prop = kvp.Value;
                    if(!targetProperties.ContainsKey(prop.name))
                        AddProperty(graphData, prop.propertyType, prop.description, prop.name);
                }
                
                MarkdownSGExtensions.WriteShaderGraphToDisk(targetShader, graphData);
            }

            hideMatchingProperties = EditorGUILayout.Toggle("Hide Matching Properties", hideMatchingProperties);

            sp = EditorGUILayout.BeginScrollView(sp);
            var draw = GUILayoutUtility.GetRect(position.width, Mathf.Max(sourceProperties.Count, targetProperties.Count) * 110);
            draw.width /= 2;
            int j = 0;
            foreach(var prop in sourceProperties)
            {
                if(hideMatchingProperties && targetProperties.ContainsKey(prop.Key))
                    continue;
                
                DrawElement(new Rect(draw.x, j * 110, draw.width, 110), prop.Value, targetProperties.ContainsKey(prop.Key), DrawMode.AddToTarget);
                j++;
            }

            draw.x += draw.width;
            j = 0;
            foreach(var prop in targetProperties)
            {
                if(hideMatchingProperties && sourceProperties.ContainsKey(prop.Key))
                    continue;

                DrawElement(new Rect(draw.x, j * 110, draw.width, 110), prop.Value, sourceProperties.ContainsKey(prop.Key), DrawMode.RemoveFromTarget);
                j++;
            }
            
            EditorGUILayout.EndScrollView();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                Refresh();
            }
        }

        private Vector2 sp;
    }
    
#endregion
}
#endif