using UnityEditor;
using UnityEngine;

namespace Needle.Editors
{
	internal static class MarkdownNeedleIcons
	{
		private const string _iconGuid = "f4f8d9f0d70d0d447b9083d86319ca3d";
		private static Texture2D _logo;

		private const string _iconDarkModeGuid = "c581d781060cfa4479726b9c6009399d";
		private static Texture2D _logoDarkMode;
		
		public static Texture2D Logo
		{
			get
			{
				if (_logo) return _logo;
				var path = AssetDatabase.GUIDToAssetPath(_iconGuid);
				if (path != null)
				{
					return _logo = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				}

				_logo = Texture2D.blackTexture;
				return _logo;
			}
		}
		
		public static Texture2D LogoDarkMode
		{
			get
			{
				if (_logoDarkMode) return _logoDarkMode;
				var path = AssetDatabase.GUIDToAssetPath(_iconDarkModeGuid);
				if (path != null)
				{
					return _logoDarkMode = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				}

				_logoDarkMode = Texture2D.blackTexture;
				return _logoDarkMode;
			}
		}		

		private const string _iconButtonGuid = "6d6418d8e5b6a4c44955be2c5c84ffa5";
		private static Texture2D _logo_button;

		public static Texture2D LogoButton
		{
			get
			{
				if (_logo) return _logo;
				var path = AssetDatabase.GUIDToAssetPath(_iconButtonGuid);
				if (path != null)
				{
					return _logo_button = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				}

				_logo_button = Texture2D.blackTexture;
				return _logo_button;
			}
		}

		public static void DrawGUILogo()
		{
			var logo = EditorGUIUtility.isProSkin ? LogoDarkMode : Logo;
			if (logo)
			{
				EditorGUILayout.Space(20f);
				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				
				EditorGUILayout.BeginVertical();
				var rect = GUILayoutUtility.GetRect(80, 40);

				if (Event.current.type == EventType.Repaint)
					GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit);

				GUILayout.Label(new GUIContent("ShaderGraph Markdown by Needle"), EditorStyles.miniLabel);

				var linkRect = new Rect(rect);
				linkRect.height += EditorGUIUtility.singleLineHeight;
				
				EditorGUIUtility.AddCursorRect(linkRect, MouseCursor.Link);
				if (Event.current.type == EventType.MouseUp && linkRect.Contains(Event.current.mousePosition))
					Application.OpenURL("https://needle.tools");
				EditorGUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
			}
		}
	}
}