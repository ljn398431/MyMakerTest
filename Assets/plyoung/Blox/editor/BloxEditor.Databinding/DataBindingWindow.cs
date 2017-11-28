using BloxEditor.Variables;
using BloxEngine;
using BloxEngine.DataBinding;
using BloxEngine.Variables;
using plyLibEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BloxEditor.Databinding
{
	public class DataBindingWindow : EditorWindow
	{
		private class BindableData
		{
			public string visibleName;

			public BloxMemberInfo mi;
		}

		private DataBinding databinding;

		private DataBinding target;

		private UnityEngine.Object owningObject;

		private GUIContent label;

		private bool accepted;

		private static readonly GUIContent GC_DataContext = new GUIContent("Data Context");

		private static readonly GUIContent GC_ValueType = new GUIContent("Value Type");

		private static readonly GUIContent GC_DataSource = new GUIContent("Data Source");

		private static GUIContent[] varEdLabels = null;

		private static List<plyVarEd> varEditors = null;

		private plyVarEd currVarEd;

		private int varTypeIdx = -1;

		private static List<BindableData> staticBindables = null;

		private static GUIContent[] staticBindableLabels = null;

		private int staticBindIdx = -1;

		public static void Show_DataBindingWindow(GUIContent label, DataBinding databinding, UnityEngine.Object owningObject)
		{
			DataBindingWindow window = EditorWindow.GetWindow<DataBindingWindow>(true, "Data Binding", true);
			window.label = label;
			window.target = databinding;
			window.databinding = databinding.Copy();
			window.owningObject = owningObject;
			window.Init();
			window.minSize = new Vector2(270f, 150f);
			window.ShowUtility();
		}

		private void Init()
		{
			this.InitConstant();
			this.InitGlobalProperty();
		}

		private void Update()
		{
			if (this.target == null || this.databinding == null)
			{
				base.Close();
			}
			if (this.accepted)
			{
				Undo.RecordObject(this.owningObject, "Change Data Binding");
				this.databinding.CopyTo(this.target);
				EditorUtility.SetDirty(this.owningObject);
				base.Close();
			}
		}

		private void OnGUI()
		{
			EditorGUILayout.Space();
			GUILayout.Label(this.label, EditorStyles.boldLabel);
			EditorGUIUtility.labelWidth = 85f;
			EditorGUI.BeginChangeCheck();
			this.databinding.dataContext = (DataBinding.DataContext)EditorGUILayout.EnumPopup(DataBindingWindow.GC_DataContext, (Enum)(object)this.databinding.dataContext);
			EditorGUI.EndChangeCheck();
			switch (this.databinding.dataContext)
			{
			case DataBinding.DataContext.Constant:
				this.DoConstant();
				break;
			case DataBinding.DataContext.GlobalProperty:
				this.DoGlobalProperty();
				break;
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal(plyEdGUI.Styles.BottomBar);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Accept", GUILayout.Width(80f)))
			{
				this.accepted = true;
			}
			GUILayout.Space(5f);
			if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
			{
				base.Close();
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		private void InitConstant()
		{
			if (DataBindingWindow.varEdLabels == null || DataBindingWindow.varEditors == null)
			{
				DataBindingWindow.varEditors = new List<plyVarEd>();
				plyVariablesEditor.LoadVarEds();
				foreach (plyVarEd value in plyVariablesEditor.editors.Values)
				{
					if (!((plyVarEdAttribute)value.nfo).UsesAdvancedEditor)
					{
						DataBindingWindow.varEditors.Add(value);
					}
				}
				DataBindingWindow.varEdLabels = new GUIContent[DataBindingWindow.varEditors.Count];
				for (int i = 0; i < DataBindingWindow.varEditors.Count; i++)
				{
					DataBindingWindow.varEdLabels[i] = new GUIContent(((plyVarEdAttribute)DataBindingWindow.varEditors[i].nfo).VarTypeName);
				}
			}
			this.varTypeIdx = -1;
			if (this.databinding.dataContext == DataBinding.DataContext.Constant && this.databinding.constant != null)
			{
				Type type = this.databinding.constant.ValueHandler.GetType();
				for (int j = 0; j < DataBindingWindow.varEditors.Count; j++)
				{
					plyVarEdAttribute plyVarEdAttribute = (plyVarEdAttribute)DataBindingWindow.varEditors[j].nfo;
					if (type == plyVarEdAttribute.TargetType)
					{
						this.currVarEd = DataBindingWindow.varEditors[j];
						this.varTypeIdx = j;
						break;
					}
				}
			}
			if (this.varTypeIdx == -1)
			{
				this.varTypeIdx = 0;
				this.currVarEd = DataBindingWindow.varEditors[0];
				this.databinding.constant = plyVar.Create(this.currVarEd.nfo.TargetType);
				this.databinding.constant.name = "constant";
			}
		}

		private void DoConstant()
		{
			EditorGUI.BeginChangeCheck();
			this.varTypeIdx = EditorGUILayout.Popup(DataBindingWindow.GC_ValueType, this.varTypeIdx, DataBindingWindow.varEdLabels);
			if (EditorGUI.EndChangeCheck())
			{
				this.currVarEd = DataBindingWindow.varEditors[this.varTypeIdx];
				Type _ = this.currVarEd.nfo.TargetType;
				this.databinding.constant = plyVar.Create(this.currVarEd.nfo.TargetType);
				this.databinding.constant.name = "constant";
			}
			if (this.currVarEd != null && this.databinding.constant != null)
			{
				Rect rect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
				rect.x = (float)(rect.x + EditorGUIUtility.labelWidth + 5.0);
				rect.width -= (float)(EditorGUIUtility.labelWidth + 15.0);
				this.currVarEd.DrawEditor(rect, false, this.databinding.constant, this.databinding.constant, 0);
			}
		}

		private void InitGlobalProperty()
		{
			if (DataBindingWindow.staticBindables == null || DataBindingWindow.staticBindableLabels == null)
			{
				DataBindingWindow.staticBindables = new List<BindableData>();
				Type typeFromHandle = typeof(Property);
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				for (int i = 0; i < assemblies.Length; i++)
				{
					Type[] exportedTypes = assemblies[i].GetExportedTypes();
					for (int j = 0; j < exportedTypes.Length; j++)
					{
						Type type = exportedTypes[j];
						PropertyInfo[] properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public);
						for (int k = 0; k < properties.Length; k++)
						{
							PropertyInfo propertyInfo = properties[k];
							if (typeFromHandle.IsAssignableFrom(propertyInfo.PropertyType))
							{
								DataBindingWindow.staticBindables.Add(new BindableData
								{
									visibleName = type.Name + "/" + propertyInfo.Name,
									mi = new BloxMemberInfo(propertyInfo, null)
								});
							}
						}
						FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
						for (int k = 0; k < fields.Length; k++)
						{
							FieldInfo fieldInfo = fields[k];
							if (typeFromHandle.IsAssignableFrom(fieldInfo.FieldType))
							{
								DataBindingWindow.staticBindables.Add(new BindableData
								{
									visibleName = type.Name + "/" + fieldInfo.Name,
									mi = new BloxMemberInfo(fieldInfo, null)
								});
							}
						}
					}
				}
				DataBindingWindow.staticBindableLabels = new GUIContent[DataBindingWindow.staticBindables.Count];
				for (int l = 0; l < DataBindingWindow.staticBindables.Count; l++)
				{
					DataBindingWindow.staticBindableLabels[l] = new GUIContent(DataBindingWindow.staticBindables[l].visibleName);
				}
			}
			string a = BloxMemberInfo.SimpleMemberPath(this.databinding.member).Replace(".", "/");
			int num = 0;
			while (true)
			{
				if (num < DataBindingWindow.staticBindables.Count)
				{
					if (!(a == DataBindingWindow.staticBindables[num].visibleName))
					{
						num++;
						continue;
					}
					break;
				}
				return;
			}
			this.staticBindIdx = num;
		}

		private void DoGlobalProperty()
		{
			EditorGUI.BeginChangeCheck();
			this.staticBindIdx = EditorGUILayout.Popup(DataBindingWindow.GC_DataSource, this.staticBindIdx, DataBindingWindow.staticBindableLabels);
			if (EditorGUI.EndChangeCheck())
			{
				this.databinding.member = DataBindingWindow.staticBindables[this.staticBindIdx].mi;
				this.databinding.property = (this.databinding.member.GetValue(null) as Property);
			}
		}

		private void DoObjectProperty()
		{
			GUILayout.Label("todo");
		}

		private void DoDataProvider()
		{
			GUILayout.Label("todo");
		}
	}
}
