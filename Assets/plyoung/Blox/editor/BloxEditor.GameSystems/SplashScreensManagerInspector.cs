using BloxEditor.Databinding;
using BloxEngine.DataBinding;
using BloxGameSystems;
using plyLib;
using plyLibEditor;
using System;
using UnityEditor;
using UnityEngine;

namespace BloxEditor.GameSystems
{
	[CustomEditor(typeof(SplashScreensManager))]
	public class SplashScreensManagerInspector : Editor
	{
		private static readonly GUIContent GC_Head = new GUIContent("Screens");

		private static readonly GUIContent GC_PlayerSkip = new GUIContent(" player can skip");

		private static readonly GUIContent GC_Seconds = new GUIContent(" seconds");

		private plyReorderableList screenList;

		private SplashScreensManager manager;

		private Component removeComponent;

		private SerializedProperty p_onSpashScreensDone;

		protected void OnEnable()
		{
			this.manager = (SplashScreensManager)base.target;
			this.p_onSpashScreensDone = base.serializedObject.FindProperty("onSpashScreensDone");
			for (int i = 0; i < this.manager.screens.Count; i++)
			{
				if ((UnityEngine.Object)this.manager.screens[i].watchVariable != (UnityEngine.Object)null)
				{
					this.manager.screens[i].watchVariable.OwnerCalledDeserialize(this.manager);
				}
			}
		}

		public override void OnInspectorGUI()
		{
			base.serializedObject.Update();
			this.manager = (SplashScreensManager)base.target;
			if (this.manager._edDeserialize)
			{
				for (int i = 0; i < this.manager.screens.Count; i++)
				{
					if ((UnityEngine.Object)this.manager.screens[i].watchVariable != (UnityEngine.Object)null && this.manager.screens[i].watchVariable._edDeserialize)
					{
						this.manager.screens[i].watchVariable.Deserialize();
					}
				}
				this.manager._edDeserialize = false;
				base.Repaint();
			}
			if (this.screenList == null)
			{
				this.screenList = new plyReorderableList(this.manager.screens, typeof(SplashScreensManager.SplashScreen), true, true, true, true, false, false, null, null);
				this.screenList.elementHeight = (float)(3.0 * (EditorGUIUtility.singleLineHeight + 2.0) + 6.0);
				this.screenList.drawHeaderCallback = this.DrawListHeader;
				this.screenList.drawElementCallback = this.DrawElement;
				this.screenList.onAddElement = this.OnAdd;
				this.screenList.onRemoveElement = this.OnRemove;
				this.screenList.onReorder = this.OnReorder;
			}
			EditorGUILayout.Space();
			this.screenList.DoLayoutList();
			if (GUI.changed)
			{
				EditorUtility.SetDirty(this.manager);
				GUI.changed = false;
			}
			if ((UnityEngine.Object)this.removeComponent != (UnityEngine.Object)null && Event.current.type == EventType.Repaint)
			{
				UnityEngine.Object.DestroyImmediate(this.removeComponent);
				this.removeComponent = null;
				GUIUtility.ExitGUI();
			}
		}

		private void DrawListHeader(Rect rect)
		{
			GUI.Label(rect, SplashScreensManagerInspector.GC_Head);
		}

		private void OnAdd()
		{
			this.manager.screens.Add(new SplashScreensManager.SplashScreen());
			EditorUtility.SetDirty(this.manager);
		}

		private void OnRemove()
		{
			this.RemoveComparisonComponent(this.screenList.index);
			this.manager.screens.RemoveAt(this.screenList.index);
			EditorUtility.SetDirty(this.manager);
		}

		private void OnReorder()
		{
			EditorUtility.SetDirty(this.manager);
		}

		private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			Rect rect2 = rect;
			rect2.y += 3f;
			rect2.height = EditorGUIUtility.singleLineHeight;
			this.manager.screens[index].target = (GameObject)EditorGUI.ObjectField(rect2, this.manager.screens[index].target, typeof(GameObject), true);
			rect2.y += (float)(rect2.height + 2.0);
			if (this.manager.screens[index].waitType == SplashScreensManager.SplashScreen.WaitType.Timeout)
			{
				this.RemoveComparisonComponent(index);
				rect2.width /= 2f;
				this.manager.screens[index].waitType = (SplashScreensManager.SplashScreen.WaitType)EditorGUI.EnumPopup(rect2, (Enum)(object)this.manager.screens[index].waitType);
				float x = GUI.skin.label.CalcSize(SplashScreensManagerInspector.GC_Seconds).x;
				rect2.x += (float)(rect2.width + 2.0);
				rect2.width -= (float)(2.0 + x);
				this.manager.screens[index].timeout = EditorGUI.FloatField(rect2, this.manager.screens[index].timeout);
				rect2.x += rect2.width;
				rect2.width = x;
				GUI.Label(rect2, SplashScreensManagerInspector.GC_Seconds);
			}
			else if (this.manager.screens[index].waitType == SplashScreensManager.SplashScreen.WaitType.WatchVariable)
			{
				if ((UnityEngine.Object)this.manager.screens[index].watchVariable == (UnityEngine.Object)null)
				{
					this.manager.screens[index].watchVariable = this.manager.gameObject.AddComponent<ComparisonCheck>();
					this.manager.screens[index].watchVariable.hideFlags = HideFlags.HideInInspector;
					this.manager.screens[index].watchVariable._edManaged = plyManagedComponent.Managed;
					this.manager.screens[index].watchVariable._edOwner = this.manager;
					EditorUtility.SetDirty(this.manager.screens[index].watchVariable);
					EditorUtility.SetDirty(this.manager);
				}
				rect2.width = 18f;
				this.manager.screens[index].waitType = (SplashScreensManager.SplashScreen.WaitType)EditorGUI.EnumPopup(rect2, (Enum)(object)this.manager.screens[index].waitType);
				rect2.x += 19f;
				rect2.width = (float)(rect.width - 19.0);
				DataProviderEd.editors[typeof(ComparisonCheck)].DrawEditor(rect2, this.manager.screens[index].watchVariable);
			}
			else if (this.manager.screens[index].waitType == SplashScreensManager.SplashScreen.WaitType.WaitScreenEndTrigger)
			{
				this.RemoveComparisonComponent(index);
				this.manager.screens[index].waitType = (SplashScreensManager.SplashScreen.WaitType)EditorGUI.EnumPopup(rect2, (Enum)(object)this.manager.screens[index].waitType);
			}
			rect2.x = rect.x;
			rect2.width = rect.width;
			rect2.y += (float)(rect2.height + 2.0);
			rect2.width = rect.width;
			this.manager.screens[index].playerCanSkip = EditorGUI.ToggleLeft(rect2, SplashScreensManagerInspector.GC_PlayerSkip, this.manager.screens[index].playerCanSkip);
		}

		private void RemoveComparisonComponent(int index)
		{
			if ((UnityEngine.Object)this.manager.screens[index].watchVariable != (UnityEngine.Object)null)
			{
				this.removeComponent = this.manager.screens[index].watchVariable;
				this.manager.screens[index].watchVariable._edManaged = plyManagedComponent.Remove;
				EditorUtility.SetDirty(this.manager.screens[index].watchVariable);
				this.manager.screens[index].watchVariable = null;
				EditorUtility.SetDirty(this.manager);
			}
		}
	}
}
