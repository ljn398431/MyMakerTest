using BloxEngine.DataBinding;
using plyLibEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BloxEditor.Databinding
{
	[plyCustomEd(typeof(ComparisonCheck))]
	public class ComparisonCheckEd : DataProviderEd
	{
		private class Data
		{
			public ComparisonCheck obj;

			public int idx;
		}

		public static readonly string[] S_ComparisonOpt = new string[6]
		{
			"==",
			"!=",
			"<",
			">",
			"<=",
			">="
		};

		private static GUIContent[] GC_Operator = null;

		private static readonly GUIContent GC_Head = new GUIContent("Condition");

		private static readonly GUIContent GC_Param1 = new GUIContent("Comparison Value 1");

		private static readonly GUIContent GC_Param2 = new GUIContent("Comparison Value 2");

		private static readonly GUIContent GC_TriggerOnce = new GUIContent(" Trigger only once on result change", "If this is set then the event will only be triggered once for when the value changed from one state to another. If not then the events will be triggered every frame until die condition result changes state back from a 'successful result' (true to false)");

		private static GenericMenu optMenu = null;

		public override void DrawInspector(DataProvider target, SerializedObject serializedObject)
		{
			SerializedProperty property = serializedObject.FindProperty("onConditionSuccess");
			GUILayout.Label(ComparisonCheckEd.GC_Head);
			Rect rect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
			this.DrawEditor(rect, target);
			EditorGUILayout.Space();
			ComparisonCheck comparisonCheck = target as ComparisonCheck;
			comparisonCheck.triggerOnceOnValueChange = EditorGUILayout.ToggleLeft(ComparisonCheckEd.GC_TriggerOnce, comparisonCheck.triggerOnceOnValueChange);
			serializedObject.Update();
			EditorGUILayout.PropertyField(property);
			serializedObject.ApplyModifiedProperties();
		}

		public override void DrawEditor(Rect rect, DataProvider target)
		{
			ComparisonCheck comparisonCheck = target as ComparisonCheck;
			if (ComparisonCheckEd.GC_Operator == null)
			{
				ComparisonCheckEd.GC_Operator = new GUIContent[ComparisonCheckEd.S_ComparisonOpt.Length];
				for (int i = 0; i < ComparisonCheckEd.GC_Operator.Length; i++)
				{
					ComparisonCheckEd.GC_Operator[i] = new GUIContent(ComparisonCheckEd.S_ComparisonOpt[i]);
				}
			}
			Rect position = rect;
			Rect position2 = rect;
			position2.width = 25f;
			position.width = (float)((position.width - position2.width) / 2.0);
			position2.x += position.width;
			if (GUI.Button(position, base.GetDataBindingLabel(comparisonCheck.param1), EditorStyles.miniButtonLeft))
			{
				DataBindingWindow.Show_DataBindingWindow(ComparisonCheckEd.GC_Param1, comparisonCheck.param1, comparisonCheck);
			}
			if (GUI.Button(position2, ComparisonCheckEd.GC_Operator[(uint)comparisonCheck.comparisonOpt], EditorStyles.miniButtonMid))
			{
				ComparisonCheckEd.optMenu = new GenericMenu();
				for (int j = 0; j < ComparisonCheckEd.GC_Operator.Length; j++)
				{
					ComparisonCheckEd.optMenu.AddItem(ComparisonCheckEd.GC_Operator[j], false, this.OnOptChosen, new Data
					{
						obj = comparisonCheck,
						idx = j
					});
				}
				ComparisonCheckEd.optMenu.DropDown(position2);
			}
			position.x = position.x + position.width + position2.width;
			if (GUI.Button(position, base.GetDataBindingLabel(comparisonCheck.param2), EditorStyles.miniButtonRight))
			{
				DataBindingWindow.Show_DataBindingWindow(ComparisonCheckEd.GC_Param2, comparisonCheck.param2, comparisonCheck);
			}
		}

		private void OnOptChosen(object arg)
		{
			Data data = (Data)arg;
			data.obj.comparisonOpt = (ComparisonCheck.ComparisonOpt)(byte)data.idx;
			EditorUtility.SetDirty(data.obj);
		}

		private void SaveDatabindingChanges(object[] args)
		{
			EditorUtility.SetDirty((ComparisonCheck)args[0]);
			EditorSceneManager.MarkAllScenesDirty();
		}
	}
}
