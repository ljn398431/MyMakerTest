using BloxGameSystems;
using UnityEditor;
using UnityEngine;

namespace BloxEditor.GameSystems
{
	[CustomEditor(typeof(Bootstrap))]
	public class BootstrapInspector : Editor
	{
		private SerializedProperty prop_onBootstrapDone;

		private static readonly string STR_Descr = "OnBootstrapDone is triggered when bootstrap is done loading all scenes marked for startup/ auto-loading";

		protected void OnEnable()
		{
			this.prop_onBootstrapDone = base.serializedObject.FindProperty("onBootstrapDone");
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox(BootstrapInspector.STR_Descr, MessageType.None);
			EditorGUILayout.PropertyField(this.prop_onBootstrapDone);
			if (GUI.changed)
			{
				base.serializedObject.ApplyModifiedProperties();
			}
		}
	}
}
