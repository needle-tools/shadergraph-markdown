using System;
using UnityEditor;
using UnityEngine;

namespace Needle
{
#if !SHADERGRAPH_7_OR_NEWER
    // From CoreEditorUtils, shimmed here for Built-In support
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
    
    internal static class CoreEditorUtilsShim
    {
        private static class CoreEditorStyles
        {
            public static Texture2D paneOptionsIcon;
            public static Texture2D iconHelp;
            public static readonly GUIContent contextMenuIcon;

            static CoreEditorStyles()
            {
                paneOptionsIcon = EditorGUIUtility.IconContent("pane options").image as Texture2D;
                contextMenuIcon = new GUIContent(paneOptionsIcon, "");
                iconHelp = EditorGUIUtility.IconContent("icons/_help" + (EditorGUIUtility.pixelsPerPoint > 1.0 ? "@2x" : "") + ".png").image as Texture2D;
            }
            
            static Lazy<GUIStyle> m_ContextMenuStyle = new Lazy<GUIStyle>(() => new GUIStyle("IconButton"));
            public static GUIStyle contextMenuStyle => m_ContextMenuStyle.Value;
            
            public static GUIStyle iconHelpStyle => GUI.skin.FindStyle("IconButton") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("IconButton");
        }
        
#if !HAVE_HEADER_FOLDOUT_WITH_DOCS
        /// <summary> Draw a foldout header </summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="state"> The state of the header </param>
        /// <param name="isBoxed"> [optional] is the eader contained in a box style ? </param>
        /// <param name="hasMoreOptions"> [optional] Delegate used to draw the right state of the advanced button. If null, no button drawn. </param>
        /// <param name="toggleMoreOptions"> [optional] Callback call when advanced button clicked. Should be used to toggle its state. </param>
        /// <param name="documentationURL">[optional] The URL that the Unity Editor opens when the user presses the help button on the header.</param>
        /// <param name="contextAction">[optional] The callback that the Unity Editor executes when the user presses the burger menu on the header.</param>
        /// <returns>return the state of the foldout header</returns>
        public static bool DrawHeaderFoldout(GUIContent title, bool state, bool isBoxed = false, Func<bool> hasMoreOptions = null, Action toggleMoreOptions = null, string documentationURL = "", Action<Vector2> contextAction = null)
        {
            const float height = 17f;
            var backgroundRect = GUILayoutUtility.GetRect(1f, height);
            float xMin = backgroundRect.xMin;

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

            if (isBoxed)
            {
                labelRect.xMin += 5;
                foldoutRect.xMin += 5;
                backgroundRect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
                backgroundRect.width -= 1;
            }

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            // Context menu
            var menuIcon = CoreEditorStyles.paneOptionsIcon;
            var menuRect = new Rect(labelRect.xMax + 3f, labelRect.y + 1f, 16, 16);

            // // Add context menu for "Additional Properties"
            // if (contextAction == null && hasMoreOptions != null)
            // {
            //     contextAction = pos => OnContextClick(pos, hasMoreOptions, toggleMoreOptions);
            // }

            if (contextAction != null)
            {
                if (GUI.Button(menuRect, CoreEditorStyles.contextMenuIcon, CoreEditorStyles.contextMenuStyle))
                    contextAction(new Vector2(menuRect.x, menuRect.yMax));
            }

            // Documentation button
            ShowHelpButton(menuRect, documentationURL, title);

            var e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (backgroundRect.Contains(e.mousePosition))
                {
                    if (e.button != 0 && contextAction != null)
                        contextAction(e.mousePosition);
                    else if (e.button == 0)
                    {
                        state = !state;
                        e.Use();
                    }

                    e.Use();
                }
            }

            return state;
        }
        
        static void ShowHelpButton(Rect contextMenuRect, string documentationURL, GUIContent title)
        {
            if (string.IsNullOrEmpty(documentationURL))
                return;

            var documentationRect = contextMenuRect;
            documentationRect.x -= 16 + 2;

            var documentationIcon = new GUIContent(CoreEditorStyles.iconHelp, $"Open Reference for {title.text}.");

            if (GUI.Button(documentationRect, documentationIcon, CoreEditorStyles.iconHelpStyle))
                Help.BrowseURL(documentationURL);
        }
#endif
    }
}    