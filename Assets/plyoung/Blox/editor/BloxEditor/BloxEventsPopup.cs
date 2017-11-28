using plyLibEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BloxEditor
{
	public class BloxEventsPopup : PopupWindowContent
	{
		public class StyleDefs
		{
			public GUIStyle DocsHead;

			public StyleDefs()
			{
				this.DocsHead = new GUIStyle(GUI.skin.label)
				{
					fontSize = 12,
					alignment = TextAnchor.MiddleLeft,
					padding = new RectOffset(6, 6, 5, 0)
				};
				this.DocsHead.normal.textColor = this.DocsHead.normal.textColor - new Color(0f, 0f, 0f, 0.3f);
			}
		}

		private static readonly GUIContent GC_LoadingEvents = new GUIContent("loading events");

		private static readonly GUIContent GC_SelectEvent = new GUIContent("Click on an Event to add it");

		private static StyleDefs _styles;

		private const float LeftWidth = 200f;

		private const float RightWidth = 450f;

		private const float Height = 300f;

		private static readonly Vector2 Size = new Vector2(650f, 300f);

		private plyEdCoroutine loader;

		private plyEdTreeView<BloxEventDef> treeView;

		private bool treeLoading = true;

		private Vector2 scroll;

		public static StyleDefs Styles
		{
			get
			{
				return BloxEventsPopup._styles ?? (BloxEventsPopup._styles = new StyleDefs());
			}
		}

		public override void OnOpen()
		{
			base.OnOpen();
			if (this.loader == null)
			{
				this.treeLoading = true;
				this.loader = plyEdCoroutine.Start(this.LoadEventDefs(), true);
			}
			else if (this.treeView != null)
			{
				this.treeView.Reset();
			}
		}

		public override void OnClose()
		{
			base.OnClose();
		}

		public override Vector2 GetWindowSize()
		{
			return BloxEventsPopup.Size;
		}

		public override void OnGUI(Rect rect)
		{
			if (this.treeLoading)
			{
				EditorGUILayout.Space();
				plyEdGUI.DrawSpinner(BloxEventsPopup.GC_LoadingEvents, true, true);
				base.editorWindow.Repaint();
				this.loader.DoUpdate();
				if (this.treeView != null && Event.current.type == EventType.Repaint)
				{
					this.treeLoading = false;
				}
			}
			else
			{
				if (this.treeView.Initialize())
				{
					this.treeView.onItemSelected = this.OnTreeItemSelected;
					this.treeView.canMark = false;
					this.treeView.drawMode = plyEdTreeViewDrawMode.List;
					this.treeView.drawBackground = true;
					this.treeView.itemHeight = 20f;
					this.treeView.itemStyle.fontSize = 12;
				}
				EditorGUILayout.BeginHorizontal();
				this.treeView.editorWindow = base.editorWindow;
				this.treeView.DrawLayout(200f);
				EditorGUILayout.BeginVertical(GUILayout.Width(450f));
				EditorGUILayout.BeginHorizontal(plyEdGUI.Styles.TopBar);
				if (this.treeView.selected != null && !this.treeView.selected.hasChildren)
				{
					GUILayout.Label(this.treeView.selected.data.name, BloxEventsPopup.Styles.DocsHead);
				}
				else
				{
					GUILayout.Label(GUIContent.none, BloxEventsPopup.Styles.DocsHead);
				}
				EditorGUILayout.EndHorizontal();
				this.scroll = EditorGUILayout.BeginScrollView(this.scroll);
				if (this.treeView.selected != null && !this.treeView.selected.hasChildren)
				{
					BloxEd.Instance.DrawBloxDoc(this.treeView.selected.data, true, base.editorWindow);
				}
				else
				{
					GUILayout.Label(BloxEventsPopup.GC_SelectEvent, plyEdGUI.Styles.WordWrappedLabel_RT);
				}
				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}
		}

		private void OnTreeItemSelected(plyEdTreeItem<BloxEventDef> item)
		{
			if (item.data != null)
			{
				BloxEditorWindow.Instance.AddEvent(item.data);
				base.editorWindow.Close();
			}
		}

		private IEnumerator LoadEventDefs()
		{
			BloxEd.Instance.LoadEventDefs();
			while (BloxEd.Instance.EventDefsLoading)
			{
				yield return (object)null;
			}
			plyEdTreeItem<BloxEventDef> treeRoot = new plyEdTreeItem<BloxEventDef>
			{
				children = new List<plyEdTreeItem<BloxEventDef>>()
			};
			int count = 0;
			int countBeforeYield = 20;
			List<BloxEventDef>.Enumerator enumerator = BloxEd.Instance.eventDefs.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					BloxEventDef current = enumerator.Current;
					string[] array = current.ident.Split('/');
					plyEdTreeItem<BloxEventDef> plyEdTreeItem = treeRoot;
					for (int i = 0; i < array.Length - 1; i++)
					{
						string text = array[i];
						bool flag = false;
						if (plyEdTreeItem.children == null)
						{
							plyEdTreeItem.children = new List<plyEdTreeItem<BloxEventDef>>();
						}
						foreach (plyEdTreeItem<BloxEventDef> child in plyEdTreeItem.children)
						{
							if (child.label == text)
							{
								flag = true;
								plyEdTreeItem = child;
								break;
							}
						}
						if (!flag)
						{
							plyEdTreeItem<BloxEventDef> plyEdTreeItem2 = new plyEdTreeItem<BloxEventDef>
							{
								label = text
							};
							plyEdTreeItem.AddChild(plyEdTreeItem2);
							plyEdTreeItem = plyEdTreeItem2;
							if (plyEdTreeItem2.parent == treeRoot)
							{
								plyEdTreeItem2.icon = BloxEdGUI.Instance.folderIcon;
							}
						}
					}
					if (plyEdTreeItem.children == null)
					{
						plyEdTreeItem.children = new List<plyEdTreeItem<BloxEventDef>>();
					}
					plyEdTreeItem.children.Add(new plyEdTreeItem<BloxEventDef>
					{
						icon = current.icon,
						label = array[array.Length - 1],
						data = current
					});
					count++;
					if (count >= countBeforeYield)
					{
						count = 0;
						yield return (object)null;
					}
				}
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			enumerator = default(List<BloxEventDef>.Enumerator);
			this.treeView = new plyEdTreeView<BloxEventDef>(null, treeRoot, BloxEdGUI.Instance.folderIcon, "Events");
		}
	}
}
