using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if !UNITY_2020_2_OR_NEWER
using System.IO;
using System.Linq;
using UnityEditorInternal;
#endif

namespace Needle.ShaderGraphMarkdown
{
    internal class ShaderGraphMarkdownSettingsProvider : SettingsProvider
    {
        private SerializedObject m_SerializedObject;
        private SerializedProperty showDefaultReferenceNameWarning, showNamingRecommendationHint, showMarkdownInBlackboard;
        
        public override void OnGUI(string searchContext)
        {
            if (m_SerializedObject == null) {
                ShaderGraphMarkdownSettings.instance.hideFlags = HideFlags.DontSave;
                m_SerializedObject = new SerializedObject(ShaderGraphMarkdownSettings.instance);
                showMarkdownInBlackboard = m_SerializedObject.FindProperty(nameof(ShaderGraphMarkdownSettings.instance.showMarkdownInBlackboard));
                showDefaultReferenceNameWarning = m_SerializedObject.FindProperty(nameof(ShaderGraphMarkdownSettings.instance.showDefaultReferenceNameWarning));
                showNamingRecommendationHint = m_SerializedObject.FindProperty(nameof(ShaderGraphMarkdownSettings.instance.showNamingRecommendationHint));
            }
            
            EditorGUILayout.LabelField("Blackboard Hints", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showMarkdownInBlackboard, new GUIContent("Show Markdown in Blackboard"));
            EditorGUILayout.PropertyField(showDefaultReferenceNameWarning, new GUIContent("Default Name Warning"));
            EditorGUILayout.PropertyField(showNamingRecommendationHint, new GUIContent("Recommendations Hint"));
            
            if(m_SerializedObject.hasModifiedProperties) {
                m_SerializedObject.ApplyModifiedProperties();
            }
        }
        
        // we don't need the SettingsProvider before 2020.2 as the only setting for now
        // are the blackboard hints, which are available on 2020.2+ only
#if UNITY_2020_2_OR_NEWER
        [SettingsProvider]
        public static SettingsProvider CreateShaderGraphMarkdownSettingsProvider()
        {
            ShaderGraphMarkdownSettings.instance.Save();
            return new ShaderGraphMarkdownSettingsProvider("Project/Graphics/ShaderGraph Markdown", SettingsScope.Project);
        }
#endif
        
        public ShaderGraphMarkdownSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }
    }

    [FilePath("ProjectSettings/ShaderGraphMarkdownSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class ShaderGraphMarkdownSettings : ScriptableSingleton<ShaderGraphMarkdownSettings>
    {
        public bool showMarkdownInBlackboard = true;
        [Tooltip("Shows a red bar next to properties that still have the default reference name. This is not recommended, as it will be hard to change to another shader in the future. Try to align your property names to Unity conventions (e.g. \"_BaseColor\")")]
        public bool showDefaultReferenceNameWarning = true;
        [Tooltip("Shows a yellow bar next to properties that don't follow recommended reference naming. Reference names should start with \"_\" and be in CamelCase.")]
        public bool showNamingRecommendationHint = true;

        public void Save() => Save(true);
    }

#if !UNITY_2020_1_OR_NEWER
#region FilePath/ScriptableSingleton Shim
    internal class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject {
        static T s_Instance;
        public static T instance {
            get {
                if (!s_Instance) CreateAndLoad();
                return s_Instance;
            }
        }

        protected ScriptableSingleton() {
            if (s_Instance)
                Debug.LogError("ScriptableSingleton already exists. Did you query the singleton in a constructor?");
            else {
                object casted = this;
                s_Instance = casted as T;
                System.Diagnostics.Debug.Assert(s_Instance != null);
            }
        }

        private static void CreateAndLoad() {
            System.Diagnostics.Debug.Assert(s_Instance == null);

            // Load
            string filePath = GetFilePath();
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(filePath))
                InternalEditorUtility.LoadSerializedFileAndForget(filePath);
#endif
            if (s_Instance == null) {
                T t = CreateInstance<T>();
                t.hideFlags = HideFlags.HideAndDontSave;
            }

            System.Diagnostics.Debug.Assert(s_Instance != null);
        }

        protected virtual void Save(bool saveAsText) {
            if (!s_Instance) {
                Debug.Log("Cannot save ScriptableSingleton: no instance!");
                return;
            }

            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath)) {
                string folderPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
#if UNITY_EDITOR
                InternalEditorUtility.SaveToSerializedFileAndForget(new[] {s_Instance}, filePath, saveAsText);
#endif
            }
        }

        protected static string GetFilePath() {
            System.Type type = typeof(T);
            object[] attributes = type.GetCustomAttributes(true);
            return attributes.OfType<FilePathAttribute>()
                .Select(f => f.filepath)
                .FirstOrDefault();
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    internal sealed class FilePathAttribute : System.Attribute
    {
        public enum Location {
            PreferencesFolder,
            ProjectFolder
        }

        public string filepath { get; set; }

        public FilePathAttribute(string relativePath, FilePathAttribute.Location location) {
            if (string.IsNullOrEmpty(relativePath)) {
                Debug.LogError("Invalid relative path! (its null or empty)");
                return;
            }

            if (relativePath[0] == '/')
                relativePath = relativePath.Substring(1);
#if UNITY_EDITOR
            if (location == FilePathAttribute.Location.PreferencesFolder)
                this.filepath = InternalEditorUtility.unityPreferencesFolder + "/" + relativePath;
            else
#endif
                this.filepath = relativePath;
        }
    }
#endregion
#endif
}