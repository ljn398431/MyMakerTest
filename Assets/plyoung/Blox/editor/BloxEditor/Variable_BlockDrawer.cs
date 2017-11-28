using BloxEngine;
using plyLibEditor;
using System;
using UnityEditor;
using UnityEngine;

namespace BloxEditor
{
	[BloxBlockDrawer(typeof(Variable_Block))]
	public class Variable_BlockDrawer : BloxBlockDrawer
	{
		private static readonly GUIContent GC_Set = new GUIContent("Set");

		private static readonly GUIContent GC_Eq = new GUIContent("=");

		private static readonly GUIContent GC_VarName = new GUIContent("");

		private static readonly GUIContent[] GC_Ico = new GUIContent[4]
		{
			new GUIContent(Ico._event_variable),
			new GUIContent(Ico._blox_variable),
			new GUIContent(Ico._object_variable),
			new GUIContent(Ico._global_variable)
		};

		private static readonly GUIContent GC_Type = new GUIContent("Type");

		private static readonly GUIContent GC_Name = new GUIContent("Name");

		public override void DrawHead(BloxEditorWindow ed, BloxBlockEd bdi)
		{
			Variable_Block variable_Block = (Variable_Block)bdi.b;
			Variable_BlockDrawer.GC_VarName.text = variable_Block.varName;
			if (bdi.owningBlock == null)
			{
				GUILayout.Label(Variable_BlockDrawer.GC_Set, BloxEdGUI.Styles.ActionLabel);
				GUILayout.Label(Variable_BlockDrawer.GC_Ico[(int)variable_Block.varType], BloxEdGUI.Styles.IconLabel);
				GUILayout.Label(Variable_BlockDrawer.GC_VarName, BloxEdGUI.Styles.ActionBoldLabel);
				GUILayout.Label(Variable_BlockDrawer.GC_Eq, BloxEdGUI.Styles.ActionLabel);
			}
			else
			{
				GUILayout.Label(Variable_BlockDrawer.GC_Ico[(int)variable_Block.varType], BloxEdGUI.Styles.IconLabel);
				GUILayout.Label(Variable_BlockDrawer.GC_VarName, BloxEdGUI.Styles.ActionLabel);
				if (bdi.b.paramBlocks[0] != null)
				{
					bdi.b.paramBlocks[0] = null;
					bdi.paramBlocks[0] = null;
					GUI.changed = true;
				}
				if (variable_Block.varType == plyVariablesType.Object)
				{
					ed.DrawBlockField(null, bdi, 1);
				}
				else if (bdi.b.paramBlocks[1] != null)
				{
					bdi.b.paramBlocks[1] = null;
					bdi.paramBlocks[1] = null;
					GUI.changed = true;
				}
			}
		}

		public override void DrawFields(BloxEditorWindow ed, BloxBlockEd bdi)
		{
			if (bdi.owningBlock == null)
			{
				Variable_Block obj = (Variable_Block)bdi.b;
				ed.DrawBlockField(null, bdi, 0);
				if (obj.varType == plyVariablesType.Object)
				{
					ed.DrawBlockField(null, bdi, 1);
				}
				else if (bdi.b.paramBlocks[1] != null)
				{
					bdi.b.paramBlocks[1] = null;
					bdi.paramBlocks[1] = null;
					GUI.changed = true;
				}
			}
		}

		public override void DrawProperties(BloxEditorWindow ed, BloxBlockEd bdi)
		{
			Variable_Block variable_Block = (Variable_Block)bdi.b;
			EditorGUIUtility.labelWidth = 75f;
			variable_Block.varType = (plyVariablesType)EditorGUILayout.EnumPopup(Variable_BlockDrawer.GC_Type, (Enum)(object)variable_Block.varType);
			variable_Block.varName = EditorGUILayout.TextField(Variable_BlockDrawer.GC_Name, variable_Block.varName);
		}
	}
}
