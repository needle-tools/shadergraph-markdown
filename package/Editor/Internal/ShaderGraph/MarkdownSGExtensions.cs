#if !NO_INTERNALS_ACCESS && UNITY_2019_4_OR_NEWER
#if UNITY_2021_2_OR_NEWER
#define SRP12_SG_REFACTORED
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
#if UNITY_2020_2_OR_NEWER
#if SRP12_SG_REFACTORED
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
#else
#if !UNITY_2021_2_OR_NEWER
using UnityEditor.ShaderGraph.Drawing.Views.Blackboard;
#endif
#endif
using UnityEditor.ShaderGraph.Serialization;
#else
#endif

namespace UnityEditor.ShaderGraph
{
    public static partial class MarkdownSGExtensions
    {
#if SRP12_SG_REFACTORED
        [InitializeOnLoadMethod]
        static void RegisterMarkdownHelpers()
        {
            MarkdownSGExtensions.RegisterCustomInspectorSetter(SetDefaultCustomInspector);
        }
        
        static bool SetDefaultCustomInspector(Target target, string customInspector)
        {
            if (target is BuiltInTarget builtInTarget)
            {
                if (builtInTarget.customEditorGUI.Equals(customInspector, StringComparison.Ordinal))
                    builtInTarget.customEditorGUI = null;
                else
                    builtInTarget.customEditorGUI = customInspector;
                return true;
            }

            return false;
        }
#endif
        
        internal static GraphData GetGraphData(AssetImporter importer, bool initializeGraph = false)
        {
            try
            {
                var path = importer.assetPath;
                var textGraph = File.ReadAllText(path, Encoding.UTF8);

#if UNITY_2020_2_OR_NEWER
                var graph = new GraphData
                {
                    assetGuid = AssetDatabase.AssetPathToGUID(path)
                };
                MultiJson.Deserialize(graph, textGraph);
#else
                var graph = JsonUtility.FromJson<GraphData>(textGraph);
#endif

                if(initializeGraph)
                {
                    graph.OnEnable();
                    graph.ValidateGraph();
                }
                return graph;
            }
            catch (ArgumentException e)
            {
                Debug.LogError("Couldn't get graph data for " + importer.assetPath + ": " + e);
                return null;
            }
        }

        internal static GraphData GetGraphData(Shader shader, bool initializeGraph = false)
        {
            if (!AssetDatabase.Contains(shader)) return null;
            var assetPath = AssetDatabase.GetAssetPath(shader);
            var assetImporter = AssetImporter.GetAtPath(assetPath);
            if (assetImporter is ShaderGraphImporter shaderGraphImporter)
                return GetGraphData(shaderGraphImporter, initializeGraph);

            return null;
        }

        internal static void WriteShaderGraphToDisk(Shader shader, GraphData graphData)
        {
#if UNITY_2020_2_OR_NEWER
            File.WriteAllText(AssetDatabase.GetAssetPath(shader), MultiJson.Serialize(graphData));
#else
            File.WriteAllText(AssetDatabase.GetAssetPath(shader), JsonUtility.ToJson(graphData));
#endif
            AssetDatabase.Refresh();
        }

        public static string GetDefaultCustomInspectorFromShader(Shader shader)
        {
            var graphData = GetGraphData(shader);
            string customInspector = null;
            
            if (graphData != null)
            {
                foreach (var getter in CustomInspectorGetters)
                {
                    var inspector = getter(graphData);
                    if (!string.IsNullOrEmpty(inspector))
                    {
                        customInspector = inspector;
                        break;
                    }
                }
            }

            foreach (var customGetter in CustomBaseShaderGUIGetters)
            {
                if(customGetter == null) continue;
                var inspector = customGetter(shader);
                if (!string.IsNullOrEmpty(inspector))
                {
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

        [MenuItem("CONTEXT/Material/Toggle ShaderGraph Markdown", false, 700)]
        static void ToggleShaderGraphMarkdown(MenuCommand command)
        {
            if (command.context is Material mat)
            {
                var graphData = MarkdownSGExtensions.GetGraphData(mat.shader);

#if UNITY_2020_2_OR_NEWER
                foreach (var target in graphData.activeTargets)
                {
                    foreach (var setter in CustomInspectorSetters)
                    {
                        setter(target, CustomEditorGUI);
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
            }
        }

        private static readonly List<Func<GraphData, string>> CustomInspectorGetters = new List<Func<GraphData, string>>();

        internal static void RegisterCustomInspectorGetter(Func<GraphData, string> func)
        {
            CustomInspectorGetters.Add(func);
        }

        private static readonly List<Func<Shader, string>> CustomBaseShaderGUIGetters = new List<Func<Shader, string>>();

        public static void RegisterCustomBaseShaderGUI(Func<Shader, string> func)
        {
            CustomBaseShaderGUIGetters.Add(func);
        }
        
#if UNITY_2020_2_OR_NEWER
        private static readonly List<Func<Target, string, bool>> CustomInspectorSetters = new List<Func<Target, string, bool>>();

        internal static void RegisterCustomInspectorSetter(Func<Target, string, bool> func)
        {
            CustomInspectorSetters.Add(func);
        }
#endif
        
#region Blackboard Access Helpers
        
        private static FieldInfo m_InputRows;
        
        public static Dictionary<ShaderInput, VisualElement> GetInputRowDictionaryFromController(object controllerObject)
        {
#if !UNITY_2021_2_OR_NEWER
            var provider = (BlackboardProvider) controllerObject;
            if (provider != null)
            {
                if(m_InputRows == null) m_InputRows = typeof(BlackboardProvider).GetField("m_InputRows", (BindingFlags) (-1));
                var inputRows = (Dictionary<ShaderInput, BlackboardRow>) m_InputRows?.GetValue(provider);
                return inputRows?.ToDictionary(x => x.Key, x => (VisualElement) x.Value);
            }
#endif
            
#if UNITY_2021_2_OR_NEWER
            var controller = (BlackboardController) controllerObject;
            if(controller != null)
            {
                return controller.Model.properties.ToDictionary(x => (ShaderInput) x, x => (VisualElement) controller.GetBlackboardRow(x));
            }
#endif
            return null;
        }
        
        public static List<VisualElement> GetBlackboardElements(VisualElement blackboard)
        {
            var blackboardFieldQuery = blackboard.Query<BlackboardField>().Visible().ToList(); 
            if(blackboardFieldQuery.Count > 0)
                return blackboardFieldQuery.Cast<VisualElement>().ToList();
#if SRP12_SG_REFACTORED
            return blackboard.Query<SGBlackboardField>().Visible().ToList().Cast<VisualElement>().ToList();
#else
            return new List<VisualElement>();
#endif
        }
        
        public static string GetBlackboardFieldText(VisualElement fieldView)
        {
            if (fieldView is BlackboardField field1) return field1.text;
#if SRP12_SG_REFACTORED
            if (fieldView is SGBlackboardField field2) return field2.text;
#endif
            return "unknown";
        }
        
        public static void SetBlackboardFieldTypeText(VisualElement fieldView, string toString)
        {
            if (fieldView is BlackboardField field1) field1.typeText = toString;
#if SRP12_SG_REFACTORED
            if (fieldView is SGBlackboardField field2) field2.typeText = toString;
#endif
        }
        
        public static bool ElementIsBlackboardRow(VisualElement element)
        {
            if (element is BlackboardRow) return true;
#if UNITY_2021_2_OR_NEWER
            if (element is SGBlackboardRow) return true;
#endif
            return false;
        }
        
#endregion

        public static ShaderGUI CreateShaderGUI(string defaultCustomInspector)
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.ShaderGUIUtility");
            var method = type?.GetMethod("CreateShaderGUI", (BindingFlags)(-1));
            if (method != null)
                return (ShaderGUI) method.Invoke(null, new object[]{defaultCustomInspector});
            return null;
        }

#if SRP12_SG_REFACTORED
        public static IEnumerable<(string categoryName, int categoryHash, IEnumerable<string> properties)> CollectCategories(Shader s)
        {
            string path = AssetDatabase.GetAssetPath(s);
            ShaderGraphMetadata metadata = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is ShaderGraphMetadata meta)
                {
                    metadata = meta;
                    var categories = metadata.categoryDatas
                        .Select(x => (x.categoryName, x.GetHashCode(), x.propertyDatas.Select(x => x.referenceName)));
                    return categories;
                }
            }

            return null;
        }
#endif

        public class WrappedShaderKeyword
        {
            internal ShaderKeyword keyword;
        }
        
        public static WrappedShaderKeyword FindKeywordData(Shader shader, string keywordRef)
        {
            // collect keywords from graph and first level of child sub graphs
            // (shader graph only generates shader_feature / multi_compile for that first level it seems)
            var graphData = GetGraphData(shader);
            if (graphData == null) return null;
            
            var shaderKeywords = new KeywordCollector();
            foreach (var node in graphData.GetNodes<SubGraphNode>())
                node.CollectShaderKeywords(shaderKeywords, GenerationMode.ForReals);
            graphData.CollectShaderKeywords(shaderKeywords, GenerationMode.ForReals);

            var keyword = shaderKeywords.keywords.FirstOrDefault(x => x.referenceName == keywordRef);
            if (keyword == null) return null;
            return new WrappedShaderKeyword() { keyword = keyword };
        }

        public static void DrawShaderKeywordProperty(MaterialEditor editor, WrappedShaderKeyword keyword, string tooltip, bool showPropertyNames = false)
        {
            var mat = (Material) editor.target;
            switch (keyword.keyword.keywordType)
            {
                case KeywordType.Boolean:
                    var isSet = mat.IsKeywordEnabled(keyword.keyword.referenceName);
                    var newValue = EditorGUILayout.Toggle(new GUIContent(showPropertyNames ? keyword.keyword.referenceName : keyword.keyword.displayName, tooltip), isSet);
                    if (isSet != newValue)
                    {
                        Undo.RegisterCompleteObjectUndo(mat, "Set " + keyword.keyword.displayName + " to " + newValue);
                        if (newValue) mat.EnableKeyword(keyword.keyword.referenceName);
                        else mat.DisableKeyword(keyword.keyword.referenceName);
                    }
                    break;
                case KeywordType.Enum:
                    var keywordEntries = keyword.keyword.entries;
                    var keywordReferenceName = keyword.keyword.referenceName + "_";
            
                    // get selected index based on set keywords
                    var currentIndex = 0;
                    
                    for (int i = 0; i < keywordEntries.Count; i++)
                    {
                        if (mat.IsKeywordEnabled(keywordReferenceName + keywordEntries[i].referenceName))
                        {
                            currentIndex = i;
                            break;
                        }
                    }
                    var newIndex = EditorGUILayout.Popup(new GUIContent(showPropertyNames ? keyword.keyword.referenceName : keyword.keyword.displayName, tooltip), currentIndex, keywordEntries.Select(x => x.displayName).ToArray());
                    if (newIndex != currentIndex)
                    {
                        Undo.RegisterCompleteObjectUndo(mat, "Set " + keyword.keyword.displayName + " to " + keywordReferenceName + keywordEntries[newIndex].referenceName);
                        for (int i = 0; i < keywordEntries.Count; i++)
                        {
                            if (i == newIndex) mat.EnableKeyword(keywordReferenceName + keywordEntries[i].referenceName);
                            else mat.DisableKeyword(keywordReferenceName + keywordEntries[i].referenceName);
                        }
                    }
                    break;
            }
        }

        public static string GetShaderPathForWindow(EditorWindow wnd)
        {
            if (!(wnd is MaterialGraphEditWindow editWindow)) return null;
            return AssetDatabase.GUIDToAssetPath(editWindow.selectedGuid);
        }

        public static bool IsSubGraph(EditorWindow wnd)
        {
            if (!(wnd is MaterialGraphEditWindow editWindow)) return false;
#if UNITY_2020_3_OR_NEWER
            return editWindow.graphObject != null && editWindow.graphObject.graph != null && editWindow.graphObject.graph.isSubGraph;
#else
            return false; // graphObject not easily accessible (would need reflection)
#endif
        }
    }
}

#endif