using plyLibEditor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BloxEditor.GameSystems
{
	public class MainEditorWindow : EditorWindow
	{
		public static float PanelContentWidth = 450f;

		private static List<MainEdChild> editors = new List<MainEdChild>();

		private static GUIContent[] labels = new GUIContent[0];

		private static int edIdx = 0;

		public static void Show_MainEditorWindow()
		{
			BloxEdGlobal.CheckAllData();
			MainEditorWindow window = EditorWindow.GetWindow<MainEditorWindow>("BGS");
			Texture2D image = plyEdGUI.LoadTextureResource("BloxEditor.res.icons.bgs" + (EditorGUIUtility.isProSkin ? "_p" : "") + ".png", typeof(MainEditorWindow).Assembly, FilterMode.Point, TextureWrapMode.Clamp);
			window.titleContent = new GUIContent("BGS", image);
		}

		public static void AddChildEditor(MainEdChild ed)
		{
			MainEditorWindow.editors.Add(ed);
			MainEditorWindow.editors.Sort((MainEdChild a, MainEdChild b) => a.order.CompareTo(b.order));
			MainEditorWindow.labels = new GUIContent[MainEditorWindow.editors.Count];
			for (int i = 0; i < MainEditorWindow.editors.Count; i++)
			{
				MainEditorWindow.labels[i] = new GUIContent(MainEditorWindow.editors[i].label);
			}
		}

		protected void OnEnable()
		{
			Texture2D image = plyEdGUI.LoadTextureResource("BloxEditor.res.icons.bgs" + (EditorGUIUtility.isProSkin ? "_p" : "") + ".png", typeof(MainEditorWindow).Assembly, FilterMode.Point, TextureWrapMode.Clamp);
			base.titleContent = new GUIContent("BGS", image);
		}

		protected void OnFocus()
		{
			MainEditorWindow.editors[MainEditorWindow.edIdx].editorWindow = this;
			MainEditorWindow.editors[MainEditorWindow.edIdx].OnFocus();
		}

		protected void OnGUI()
		{
			if (plyEdGUI.Tabbar(ref MainEditorWindow.edIdx, MainEditorWindow.labels))
			{
				MainEditorWindow.editors[MainEditorWindow.edIdx].editorWindow = this;
				MainEditorWindow.editors[MainEditorWindow.edIdx].OnFocus();
			}
			MainEditorWindow.editors[MainEditorWindow.edIdx].editorWindow = this;
			MainEditorWindow.editors[MainEditorWindow.edIdx].OnGUI();
		}
	}
}
