using BloxEngine;
using HtmlAgilityPack;
using plyLibEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Internal;

namespace BloxEditor
{
	public class BloxSettingsWindow : EditorWindow
	{
		private class BottomButton
		{
			public GUIContent label;

			public Action callback;
		}

		[NonSerialized]
		private int activeEd;

		[NonSerialized]
		private int edMode;

		[NonSerialized]
		private GUIContent GC_Help = new GUIContent();

		[NonSerialized]
		private Vector2 scroll = Vector2.zero;

		[NonSerialized]
		private Vector2 helpScroll = Vector2.zero;

		[NonSerialized]
		private List<BottomButton> bottomButtons;

		[NonSerialized]
		private plyReorderableList list;

		[NonSerialized]
		private plyEdCoroutine scanner;

		[NonSerialized]
		private BloxBlockDef selectedDef;

		private static readonly GUIContent GC_Close = new GUIContent("Close");

		private static readonly GUIContent GC_CancelButton = new GUIContent("Cancel");

		private static readonly GUIContent GC_DoneButton = new GUIContent("Done");

		private static readonly GUIContent GC_RestoreDefaultButton = new GUIContent("Restore Default");

		private static readonly GUIContent GC_DefaultButton = new GUIContent("Reset to Default");

		private static readonly GUIContent GC_Blocks = new GUIContent(Ico._blox + " Blocks Setup");

		private static readonly GUIContent GC_BloxDoc = new GUIContent(Ico._book + " BloxDoc");

		private static readonly GUIContent GC_Runtime = new GUIContent("Runtime");

		private static readonly GUIContent GC_DeadLock = new GUIContent("Deadlock Detect", "This is used to detect when there is a deadlock in a loop or an event being called recursively. A deadlock is when a loop is unable to stop. This value should be high enough to allow normal execution of loops with a high loop count.");

		private static readonly GUIContent GC_Canvas = new GUIContent("Canvas");

		private static readonly GUIContent GC_BlockTheme = new GUIContent("Block Theme", "Theme determine what the Blocks look like");

		private static readonly GUIContent GC_CanvasColour = new GUIContent("Canvas Colour", "Background colour of the canvas");

		private static readonly GUIContent GC_FontSize = new GUIContent("Font size", "Font size used when drawing Blocks (default=13)");

		private static readonly GUIContent GC_BlocksList = new GUIContent("Blocks List");

		private static readonly GUIContent GC_BlocksListMode = new GUIContent("Blocks-List Mode", "How should the list of available Blocks be displayed in the Blox Editor?");

		private static readonly GUIContent GC_DelaySearch = new GUIContent("Delayed Search", "Should the Blocks List search as you type or wait until you press enter in the search field? Enabling this will help with performance on slower machines.");

		private static readonly GUIContent GC_DockBlockList = new GUIContent("Dock Blocks List", "Should the Blocks List be docked in the Blox Editor or be in a panel you can dock with other Unity window panels?");

		private static readonly GUIContent GC_Misc = new GUIContent("Misc");

		private static readonly GUIContent GC_ShowBloxIconHierarchy = new GUIContent("Hierarchy panel icon", "Should the Blox icon be shown next to objects in the hierarchy panel that has a BloxContainer component on them? You can disable this for better editor performance or if it interferes with other icons in the hierarchy panel.");

		private static readonly GUIContent GC_ShowBloxIconProject = new GUIContent("Project panel icon", "Should the Blox icon be shown next to objects in the project panel that has a BloxContainer component on them? You can disable this for better editor performance or if it interferes with other icons in the project panel.");

		private static Texture2D bloxIcon;

		private static GUIContent GC_Version = new GUIContent();

		private static readonly string S_BlockEdMsg1 = "Specify which namespaces should be scanned for types and members to be turned into Blocks.";

		private static readonly string S_BlockEdMsg2 = "Select which type members should be turned into Blocks. It is best to select only what you will actually be using since the Blox editor will perform better with fever Blocks present.";

		private static readonly GUIContent GC_ScanNamespaces = new GUIContent("Namespaces and Classes to Scan");

		private static readonly GUIContent GC_ScanButton = new GUIContent("Scan Namespaces");

		private static readonly GUIContent GC_SaveButton = new GUIContent("Save", "Save the changes");

		private static readonly GUIContent GC_PleaseWait = new GUIContent("scanning");

		private static readonly GUIContent GC_SelectDefaultButton = new GUIContent("Select Default", "Select some of the typically used Blocks");

		[NonSerialized]
		private plyEdTreeView<BloxBlockDef> treeView;

		private static readonly string S_BloxDocEdMsg1 = "Here you may add or remove Blox documentation sources. All paths are relative to the project's Assets folder.\n\n[Update Scraped Docs] can be used to update documentation for Unity type members. This is only needed if you added more UnityEngine related namespaces to the Blox scanner. Documentation for the UnityEngine, UI, Audio and SceneManagement namespaces are already included.";

		private static readonly GUIContent GC_DocSources = new GUIContent("BloxDoc sources");

		private static readonly GUIContent GC_ScanDocsButton = new GUIContent("Update Scraped Docs");

		private static readonly GUIContent GC_DocPleaseWait = new GUIContent("updating bloxdoc");

		private static readonly GUIContent GC_CompressDox = new GUIContent("Compress Scraped Docs");

		private string docsPath = "";

		private int scrapeCount;

		private bool compressScrapedDox = true;

		public static void Show_BloxSettingsWindow()
		{
			BloxSettingsWindow window = EditorWindow.GetWindow<BloxSettingsWindow>(true, "Blox Settings", true);
			window.minSize = new Vector2(430f, 400f);
			window.ShowUtility();
		}

		protected void OnEnable()
		{
			base.minSize = new Vector2(430f, 400f);
			if ((UnityEngine.Object)BloxSettingsWindow.bloxIcon == (UnityEngine.Object)null)
			{
				BloxSettingsWindow.bloxIcon = plyEdGUI.LoadTextureResource("BloxEditor.res.icons.blox.png", typeof(BloxSettingsWindow).Assembly, FilterMode.Point, TextureWrapMode.Clamp);
				BloxSettingsWindow.GC_Version.text = plyEdUtil.GetVersion(plyEdUtil.PackagesFullPath + "version-Blox.txt");
			}
		}

		protected void OnDestroy()
		{
			BloxEd.StopBloxDocFinder(false);
			if (this.scanner != null)
			{
				this.scanner.Stop();
			}
		}

		protected void OnFocus()
		{
		}

		protected void OnGUI()
		{
			switch (this.activeEd)
			{
			case 0:
				this.DoGeneralSettings();
				break;
			case 1:
				this.DoBlocksSettings();
				break;
			case 2:
				this.DoBloxDocSettings();
				break;
			}
			if (!string.IsNullOrEmpty(this.GC_Help.text) || this.selectedDef != null)
			{
				EditorGUILayout.BeginVertical(plyEdGUI.Styles.BottomBar, GUILayout.Height(100f));
				this.helpScroll = EditorGUILayout.BeginScrollView(this.helpScroll);
				if (this.selectedDef != null)
				{
					BloxEd.Instance.DrawBloxDoc(this.selectedDef, false, this);
				}
				else
				{
					GUILayout.Label(this.GC_Help, plyEdGUI.Styles.WordWrappedLabel);
				}
				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.BeginHorizontal(plyEdGUI.Styles.BottomBar);
			EditorGUILayout.Space();
			if (this.bottomButtons != null)
			{
				int num = 0;
				while (num < this.bottomButtons.Count)
				{
					if (!GUILayout.Button(this.bottomButtons[num].label, GUILayout.MinWidth(60f)) || this.bottomButtons[num].callback == null)
					{
						num++;
						continue;
					}
					this.bottomButtons[num].callback();
					GUIUtility.ExitGUI();
					break;
				}
			}
			else
			{
				GUI.DrawTexture(GUILayoutUtility.GetRect(16f, 16f), BloxSettingsWindow.bloxIcon);
				GUILayout.Label(BloxSettingsWindow.GC_Version);
			}
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(BloxSettingsWindow.GC_Close, GUILayout.MinWidth(60f)))
			{
				base.Close();
			}
			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();
			if (Event.current.type == EventType.Repaint)
			{
				BloxEd.Instance.DoUpdate();
				if (this.scanner != null)
				{
					this.scanner.DoUpdate();
				}
			}
		}

		private void DoGeneralSettings()
		{
			EditorGUILayout.Space();
			this.scroll = EditorGUILayout.BeginScrollView(this.scroll);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(BloxSettingsWindow.GC_Blocks, plyEdGUI.Styles.BigButton))
			{
				this.EnterBlocksEdMode();
			}
			if (GUILayout.Button(BloxSettingsWindow.GC_BloxDoc, plyEdGUI.Styles.BigButton))
			{
				this.EnterBloxDocEdMode();
			}
			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(20f);
			GUILayout.Label(BloxSettingsWindow.GC_Runtime);
			EditorGUI.indentLevel++;
			EditorGUI.BeginChangeCheck();
			BloxEd.BloxGlobalObj.deadlockDetect = EditorGUILayout.IntField(BloxSettingsWindow.GC_DeadLock, BloxEd.BloxGlobalObj.deadlockDetect);
			if (EditorGUI.EndChangeCheck())
			{
				EditorUtility.SetDirty(BloxEd.BloxGlobalObj);
			}
			EditorGUI.indentLevel--;
			GUILayout.Label(BloxSettingsWindow.GC_Canvas);
			EditorGUI.indentLevel++;
			EditorGUI.BeginChangeCheck();
			BloxEdGlobal.BlockTheme = EditorGUILayout.Popup(BloxSettingsWindow.GC_BlockTheme, BloxEdGlobal.BlockTheme, BloxEdGUI.BlockThemeNames);
			if (EditorGUI.EndChangeCheck())
			{
				EditorPrefs.SetInt("Blox.BlockTheme", BloxEdGlobal.BlockTheme);
				BloxEdGUI.UpdateBlockTheme();
			}
			EditorGUI.BeginChangeCheck();
			BloxEdGlobal.CanvasColour = EditorGUILayout.ColorField(BloxSettingsWindow.GC_CanvasColour, BloxEdGlobal.CanvasColour);
			if (EditorGUI.EndChangeCheck())
			{
				plyEdUtil.EdPrefs_SetColor("Blox.CanvasColour", BloxEdGlobal.CanvasColour);
			}
			BloxEdGUI.Styles.FontSize = EditorGUILayout.IntField(BloxSettingsWindow.GC_FontSize, BloxEdGUI.Styles.FontSize);
			EditorGUI.indentLevel--;
			GUILayout.Label(BloxSettingsWindow.GC_BlocksList);
			EditorGUI.indentLevel++;
			EditorGUI.BeginChangeCheck();
			BloxEdGlobal.BlocksListMode = (plyEdTreeViewDrawMode)EditorGUILayout.EnumPopup(BloxSettingsWindow.GC_BlocksListMode, (Enum)(object)BloxEdGlobal.BlocksListMode);
			if (EditorGUI.EndChangeCheck())
			{
				EditorPrefs.SetInt("Blox.BlocksListMode", (int)BloxEdGlobal.BlocksListMode);
				BloxBlocksList.OnSettingsChanged();
			}
			EditorGUI.BeginChangeCheck();
			BloxEdGlobal.DelayedSearch = EditorGUILayout.Toggle(BloxSettingsWindow.GC_DelaySearch, BloxEdGlobal.DelayedSearch);
			if (EditorGUI.EndChangeCheck())
			{
				EditorPrefs.SetBool("Blox.DelayedSearch", BloxEdGlobal.DelayedSearch);
			}
			EditorGUI.BeginChangeCheck();
			BloxEdGlobal.BlocksListDocked = EditorGUILayout.Toggle(BloxSettingsWindow.GC_DockBlockList, BloxEdGlobal.BlocksListDocked);
			if (EditorGUI.EndChangeCheck())
			{
				if (BloxEdGlobal.BlocksListDocked)
				{
					BloxBlocksWindow.Close_BloxBlocksWindow();
				}
				else
				{
					BloxBlocksWindow.Show_BloxBlocksWindow();
				}
				BloxEditorWindow instance = BloxEditorWindow.Instance;
				if ((object)instance != null)
				{
					instance.Repaint();
				}
			}
			EditorGUI.indentLevel--;
			GUILayout.Label(BloxSettingsWindow.GC_Misc);
			EditorGUI.indentLevel++;
			EditorGUI.BeginChangeCheck();
			BloxEdGlobal.ShowBloxIconInHierarchyPanel = EditorGUILayout.Toggle(BloxSettingsWindow.GC_ShowBloxIconHierarchy, BloxEdGlobal.ShowBloxIconInHierarchyPanel);
			if (EditorGUI.EndChangeCheck())
			{
				EditorPrefs.SetBool("Blox.ShowBloxIconInHierarchyPanel", BloxEdGlobal.ShowBloxIconInHierarchyPanel);
			}
			EditorGUI.BeginChangeCheck();
			BloxEdGlobal.ShowBloxIconInProjectPanel = EditorGUILayout.Toggle(BloxSettingsWindow.GC_ShowBloxIconProject, BloxEdGlobal.ShowBloxIconInProjectPanel);
			if (EditorGUI.EndChangeCheck())
			{
				EditorPrefs.SetBool("Blox.ShowBloxIconInProjectPanel", BloxEdGlobal.ShowBloxIconInProjectPanel);
			}
			EditorGUI.indentLevel--;
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndScrollView();
		}

		private void SetHelpText(string s)
		{
			this.selectedDef = null;
			this.GC_Help.text = s;
			this.helpScroll = Vector2.zero;
		}

		private void EnterBlocksEdMode()
		{
			if (this.scanner != null)
			{
				this.scanner.Stop();
			}
			this.list = new plyReorderableList(BloxEd.Instance.namespaces, typeof(string), true, true, true, true, true, false, null, null);
			this.list.elementHeight = (float)(EditorGUIUtility.singleLineHeight + 4.0);
			this.list.drawHeaderCallback = this.NamespacesList_Header;
			this.list.drawElementCallback = this.NamespacesList_Element;
			this.list.onAddElement = this.NamespacesList_Add;
			this.list.onRemoveElement = this.NamespacesList_Remove;
			this.activeEd = 1;
			this.edMode = 1;
			this.scroll = Vector2.zero;
			this.SetHelpText(BloxSettingsWindow.S_BlockEdMsg1);
			this.bottomButtons = new List<BottomButton>
			{
				new BottomButton
				{
					label = BloxSettingsWindow.GC_ScanButton,
					callback = this.GoNamespaceScanning
				},
				new BottomButton
				{
					label = BloxSettingsWindow.GC_RestoreDefaultButton,
					callback = BloxEd.Instance.RestoreDefaultNamespaces
				},
				new BottomButton
				{
					label = BloxSettingsWindow.GC_CancelButton,
					callback = this.CloseBlocksEdMode
				}
			};
		}

		private void CloseBlocksEdMode()
		{
			BloxEd.StopBloxDocFinder(false);
			if (this.scanner != null)
			{
				this.scanner.Stop();
			}
			this.activeEd = 0;
			this.edMode = 0;
			this.SetHelpText(null);
			this.bottomButtons = null;
		}

		private void GoNamespaceScanning()
		{
			this.edMode = 2;
			this.scroll = Vector2.zero;
			this.SetHelpText(null);
			this.scanner = plyEdCoroutine.Start(this.NamespaceScanner(), true);
			this.bottomButtons = new List<BottomButton>
			{
				new BottomButton
				{
					label = BloxSettingsWindow.GC_CancelButton,
					callback = this.CloseBlocksEdMode
				}
			};
		}

		private void GoTypesSelection()
		{
			this.edMode = 3;
			this.scroll = Vector2.zero;
			this.SetHelpText(BloxSettingsWindow.S_BlockEdMsg2);
			this.bottomButtons = new List<BottomButton>
			{
				new BottomButton
				{
					label = BloxSettingsWindow.GC_SelectDefaultButton,
					callback = this.SelectDefaultBlocks
				},
				new BottomButton
				{
					label = BloxSettingsWindow.GC_SaveButton,
					callback = this.SaveBlocksSelection
				},
				new BottomButton
				{
					label = BloxSettingsWindow.GC_DoneButton,
					callback = this.CloseBlocksEdMode
				}
			};
		}

		private void DoBlocksSettings()
		{
			if (this.edMode == 1)
			{
				this.scroll = EditorGUILayout.BeginScrollView(this.scroll);
				EditorGUILayout.Space();
				this.list.DoLayoutList();
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndScrollView();
			}
			else if (this.edMode == 2)
			{
				GUILayout.Space(20f);
				plyEdGUI.DrawSpinner(BloxSettingsWindow.GC_PleaseWait, true, true);
				GUILayout.FlexibleSpace();
				base.Repaint();
			}
			else if (this.edMode == 3)
			{
				EditorGUILayout.BeginHorizontal(plyEdGUI.Styles.TopBar);
				this.treeView.SearchString = plyEdGUI.SearchField(this.treeView.SearchString, BloxEdGlobal.DelayedSearch);
				EditorGUILayout.EndHorizontal();
				this.treeView.DrawLayout();
			}
			if (GUI.changed)
			{
				GUI.changed = false;
				EditorUtility.SetDirty(BloxEd.Instance);
			}
		}

		private void NamespacesList_Header(Rect rect)
		{
			GUI.Label(rect, BloxSettingsWindow.GC_ScanNamespaces);
		}

		private void NamespacesList_Element(Rect rect, int index, bool isActive, bool isFocused)
		{
			rect.y += 1f;
			rect.height = EditorGUIUtility.singleLineHeight;
			BloxEd.Instance.namespaces[index] = GUI.TextField(rect, BloxEd.Instance.namespaces[index]);
		}

		private void NamespacesList_Add()
		{
			BloxEd.Instance.namespaces.Add("");
			EditorUtility.SetDirty(BloxEd.Instance);
		}

		private void NamespacesList_Remove()
		{
			if (this.list.index >= 0 && this.list.index < BloxEd.Instance.namespaces.Count)
			{
				BloxEd.Instance.namespaces.RemoveAt(this.list.index);
				EditorUtility.SetDirty(BloxEd.Instance);
			}
		}

		private void SelectDefaultBlocks()
		{
			EditorUtility.DisplayProgressBar("Blox Scanner", "Selecting default Blocks", 0f);
			this.treeView.SetMarkedAll(false);
			plyEdTreeItem<BloxBlockDef> plyEdTreeItem = this.treeView.FindItemByPath("BloxGameSystems");
			if (plyEdTreeItem != null)
			{
				plyEdTreeItem.SetMarkedAndChildren(true, true);
			}
			try
			{
				string path = plyEdUtil.PackagesRelativePath + "Blox/editor/res/default_blocks.txt";
				if (plyEdUtil.RelativeFileExist(BloxEdGlobal.DataRoot + "default_blocks.txt"))
				{
					path = BloxEdGlobal.DataRoot + "default_blocks.txt";
				}
				string[] array = File.ReadAllLines(path);
				if (array.Length != 0)
				{
					float num = 0f;
					float num2 = (float)(1.0 / (float)array.Length);
					for (int i = 0; i < array.Length; i++)
					{
						EditorUtility.DisplayProgressBar("Blox Scanner", "Selecting default Blocks", num += num2);
						if (!string.IsNullOrEmpty(array[i]))
						{
							array[i] = array[i].Trim();
							if (array[i][0] != '#')
							{
								plyEdTreeItem<BloxBlockDef> plyEdTreeItem2 = this.treeView.FindItemByPath(array[i]);
								if (plyEdTreeItem2 != null)
								{
									plyEdTreeItem2.SetMarkedAndChildren(true, true);
								}
								else
								{
									Debug.LogWarning("Could not find [" + array[i] + "] while trying to select default Blocks.");
								}
							}
						}
					}
				}
			}
			catch
			{
			}
			EditorUtility.ClearProgressBar();
		}

		private void OnTreeItemSelected(plyEdTreeItem<BloxBlockDef> item)
		{
			if (item != null && item.data != null)
			{
				this.SetHelpText(null);
				this.selectedDef = item.data;
			}
			else
			{
				this.SetHelpText(BloxSettingsWindow.S_BlockEdMsg2);
			}
			base.Repaint();
		}

		private IEnumerator NamespaceScanner()
		{
			int countBeforeYield = 200;
			List<BloxBlockDef> defs = new List<BloxBlockDef>(10);
			Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
			int count3 = 0;
			List<Type> foundTypes = new List<Type>();
			List<string>.Enumerator enumerator = BloxEd.Instance.namespaces.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					string ns = enumerator.Current;
					if (!string.IsNullOrEmpty(ns))
					{
						if (ns.StartsWith("BloxGenerated") || ns.StartsWith("BloxEngine") || ns.StartsWith("BloxEditor") || ns.StartsWith("plyLib") || ns.StartsWith("plyLibEditor") || ns.StartsWith("UnityEditor"))
						{
							Debug.LogWarning("Unity Editor, Blox, and plyLib namespaces should not be included. Skipping: " + ns);
						}
						else
						{
							foundTypes.Clear();
							if (ns == "*")
							{
                                Debug.Log("*****************Find Type* ");
                                Assembly[] array = asms;
								for (int j = 0; j < array.Length; j++)
								{
									Assembly assembly = array[j];
									if (assembly.FullName.Contains("Assembly-CSharp"))
									{
										Type[] exportedTypes;
										Type[] array2 = exportedTypes = assembly.GetExportedTypes();
										for (int k = 0; k < exportedTypes.Length; k++)
										{
											Type type = exportedTypes[k];
											if (string.IsNullOrEmpty(type.Namespace))
											{
												foundTypes.Add(type);
											}
											count3++;
											if (count3 >= countBeforeYield)
											{
												count3 = 0;
												yield return (object)null;
											}
										}
									}
								}
							}
							else
							{
								Type type2 = Type.GetType(ns);
                                Debug.Log("Find Type" + type2+"ns : "+ ns);
								if (type2 != null)
								{
									foundTypes.Add(type2);
								}
								Assembly[] array = asms;
								for (int j = 0; j < array.Length; j++)
								{
									Type[] exportedTypes;
									Type[] array3 = exportedTypes = array[j].GetExportedTypes();
									for (int k = 0; k < exportedTypes.Length; k++)
									{
										Type type3 = exportedTypes[k];
										if (type3.Namespace == ns)
										{
											foundTypes.Add(type3);
										}
										count3++;
										if (count3 >= countBeforeYield)
										{
											count3 = 0;
											yield return (object)null;
										}
									}
								}
							}
							List<Type>.Enumerator enumerator2 = foundTypes.GetEnumerator();
							try
							{
								while (enumerator2.MoveNext())
								{
									Type current = enumerator2.Current;
									if (!current.IsEnum && !current.IsAbstract && !current.IsSpecialName && (current.IsClass || current.IsValueType) && !current.IsSubclassOf(typeof(Attribute)) && current != typeof(Attribute) && !current.IsSubclassOf(typeof(Delegate)) && current != typeof(Delegate) && !current.IsSubclassOf(typeof(Exception)) && current != typeof(Exception))
									{
										bool inclOnlySpecifiedMembers = false;
										object[] customAttributes = current.GetCustomAttributes(typeof(ExcludeFromBloxAttribute), true);
										if (customAttributes.Length != 0)
										{
											if (!((ExcludeFromBloxAttribute)customAttributes[0]).ExceptForSpecifiedMembers)
											{
												continue;
											}
											inclOnlySpecifiedMembers = true;
										}
										Texture2D texture2D = AssetPreview.GetMiniTypeThumbnail(current);
										if ((UnityEngine.Object)texture2D == (UnityEngine.Object)null || texture2D.name == "DefaultAsset Icon")
										{
											texture2D = ((!current.FullName.StartsWith("UnityEngine")) ? BloxEdGUI.Instance.bloxIcon : BloxEdGUI.Instance.unityIcon);
										}
										BindingFlags bindingFlags = BloxBlockData.BindFlags;
										if (BloxEd.Instance.includeDeclaredOnly)
										{
											bindingFlags = (BindingFlags)((int)bindingFlags | 2);
										}
										MemberInfo[] members = current.GetMembers(bindingFlags);
										for (int l = 0; l < members.Length; l++)
										{
											MemberInfo member = members[l];
											this.ProcessMember(defs, new BloxMemberInfo(member, current), inclOnlySpecifiedMembers, texture2D);
										}
										if (!typeof(Component).IsAssignableFrom(current) && !typeof(ScriptableObject).IsAssignableFrom(current) && current.GetConstructor(Type.EmptyTypes) == null)
										{
											this.ProcessMember(defs, new BloxMemberInfo(current, current), inclOnlySpecifiedMembers, texture2D);
										}
										count3++;
										if (count3 >= countBeforeYield)
										{
											count3 = 0;
											yield return (object)null;
										}
									}
								}
							}
							finally
							{
								((IDisposable)enumerator2).Dispose();
							}
							enumerator2 = default(List<Type>.Enumerator);
						}
					}
				}
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			enumerator = default(List<string>.Enumerator);
			plyEdTreeItem<BloxBlockDef> treeRoot = new plyEdTreeItem<BloxBlockDef>
			{
				children = new List<plyEdTreeItem<BloxBlockDef>>()
			};
			count3 = 0;
			List<BloxBlockDef>.Enumerator enumerator3 = defs.GetEnumerator();
			try
			{
				while (enumerator3.MoveNext())
				{
					BloxBlockDef current2 = enumerator3.Current;
					string[] array4 = current2.ident.Split('/');
					if (string.IsNullOrEmpty(current2.name))
					{
						current2.name = array4[array4.Length - 1];
					}
					if ((UnityEngine.Object)current2.icon == (UnityEngine.Object)null)
					{
						if (current2.ident.StartsWith("UnityEngine"))
						{
							current2.icon = BloxEdGUI.Instance.unityIcon;
						}
						else
						{
							current2.icon = BloxEdGUI.Instance.bloxIcon;
						}
					}
					plyEdTreeItem<BloxBlockDef> plyEdTreeItem = treeRoot;
					plyEdTreeItem<BloxBlockDef> plyEdTreeItem2;
					for (int m = 0; m < array4.Length - 1; m++)
					{
						string text = array4[m];
						bool flag = false;
						if (plyEdTreeItem.children == null)
						{
							plyEdTreeItem.children = new List<plyEdTreeItem<BloxBlockDef>>();
						}
						foreach (plyEdTreeItem<BloxBlockDef> child in plyEdTreeItem.children)
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
							plyEdTreeItem<BloxBlockDef> obj = plyEdTreeItem;
							plyEdTreeItem<BloxBlockDef> obj2 = new plyEdTreeItem<BloxBlockDef>
							{
								label = text,
								searchLabel = text
							};
							plyEdTreeItem2 = obj2;
							obj.AddChild(obj2);
							if (plyEdTreeItem2.searchLabel.Contains("("))
							{
								plyEdTreeItem2.searchLabel = plyEdTreeItem2.searchLabel.Substring(0, plyEdTreeItem2.searchLabel.LastIndexOf('('));
							}
							else if (plyEdTreeItem2.searchLabel.Contains(":"))
							{
								plyEdTreeItem2.searchLabel = plyEdTreeItem2.searchLabel.Substring(0, plyEdTreeItem2.searchLabel.LastIndexOf(':'));
							}
							plyEdTreeItem = plyEdTreeItem2;
							if (plyEdTreeItem2.parent == treeRoot)
							{
								plyEdTreeItem2.icon = BloxEdGUI.Instance.folderIcon;
							}
						}
					}
					if (plyEdTreeItem.children == null)
					{
						plyEdTreeItem.children = new List<plyEdTreeItem<BloxBlockDef>>();
					}
					plyEdTreeItem.children.Add(plyEdTreeItem2 = new plyEdTreeItem<BloxBlockDef>
					{
						icon = current2.icon,
						label = current2.name,
						searchLabel = current2.name,
						data = current2
					});
					if (plyEdTreeItem2.searchLabel.Contains("("))
					{
						plyEdTreeItem2.searchLabel = plyEdTreeItem2.searchLabel.Substring(0, plyEdTreeItem2.searchLabel.LastIndexOf('('));
					}
					else if (plyEdTreeItem2.searchLabel.Contains(":"))
					{
						plyEdTreeItem2.searchLabel = plyEdTreeItem2.searchLabel.Substring(0, plyEdTreeItem2.searchLabel.LastIndexOf(':'));
					}
					Texture2D texture2D2 = current2.icon;
					if ((UnityEngine.Object)texture2D2 == (UnityEngine.Object)BloxEdGUI.Instance.bloxFadedIcon)
					{
						texture2D2 = AssetPreview.GetMiniTypeThumbnail(current2.returnType);
						if ((UnityEngine.Object)texture2D2 == (UnityEngine.Object)null || texture2D2.name == "DefaultAsset Icon")
						{
							texture2D2 = ((!current2.returnType.FullName.StartsWith("UnityEngine")) ? BloxEdGUI.Instance.bloxIcon : BloxEdGUI.Instance.unityIcon);
						}
					}
					plyEdTreeItem.SetIconRecusriveUp(texture2D2);
					count3++;
					if (count3 >= countBeforeYield)
					{
						count3 = 0;
						yield return (object)null;
					}
				}
			}
			finally
			{
				((IDisposable)enumerator3).Dispose();
			}
			enumerator3 = default(List<BloxBlockDef>.Enumerator);
			for (int i = 0; i < treeRoot.children.Count; i++)
			{
				this.SortChildren(treeRoot.children[i]);
				yield return (object)null;
			}
			this.treeView = new plyEdTreeView<BloxBlockDef>(this, treeRoot, null, "");
			this.treeView.Initialize();
			this.treeView.onItemSelected = this.OnTreeItemSelected;
			BloxEd.Instance.LoadBlockDefs(false);
			while (BloxEd.Instance.BlockDefsLoading)
			{
				yield return (object)null;
			}
			count3 = 0;
			Dictionary<string, BloxBlockDef>.ValueCollection.Enumerator enumerator5 = BloxEd.Instance.blockDefs.Values.GetEnumerator();
			try
			{
				while (enumerator5.MoveNext())
				{
					BloxBlockDef current4 = enumerator5.Current;
					plyEdTreeItem<BloxBlockDef> plyEdTreeItem3 = this.treeView.FindItemByPath(current4.ident);
					if (plyEdTreeItem3 != null)
					{
						plyEdTreeItem3.SetMarked(true, true);
					}
					count3++;
					if (count3 >= countBeforeYield)
					{
						count3 = 0;
						yield return (object)null;
					}
				}
			}
			finally
			{
				((IDisposable)enumerator5).Dispose();
			}
			enumerator5 = default(Dictionary<string, BloxBlockDef>.ValueCollection.Enumerator);
			this.GoTypesSelection();
		}

		private void SortChildren(plyEdTreeItem<BloxBlockDef> item)
		{
			if (((item.children != null) ? item.children.Count : 0) != 0)
			{
				item.children.Sort(delegate(plyEdTreeItem<BloxBlockDef> a, plyEdTreeItem<BloxBlockDef> b)
				{
					bool flag = a.data == null || (a.children != null && a.children.Count > 0);
					bool flag2 = b.data == null || (b.children != null && b.children.Count > 0);
					if (flag & flag2)
					{
						return a.label.CompareTo(b.label);
					}
					if (flag)
					{
						return 1;
					}
					if (flag2)
					{
						return -1;
					}
					if (a.data.order == b.data.order)
					{
						return a.label.CompareTo(b.label);
					}
					return a.data.order.CompareTo(b.data.order);
				});
				for (int i = 0; i < item.children.Count; i++)
				{
					this.SortChildren(item.children[i]);
				}
			}
		}

		private void ProcessMember(List<BloxBlockDef> defs, BloxMemberInfo m, bool inclOnlySpecifiedMembers, Texture2D icon)
		{
			bool flag = inclOnlySpecifiedMembers;
			object[] customAttributes = m.GetCustomAttributes(true);
			for (int i = 0; i < customAttributes.Length; i++)
			{
				Type type = customAttributes[i].GetType();
				if (type == typeof(IncludeInBloxAttribute))
				{
					flag = false;
				}
				else
				{
					if (type == typeof(ExcludeFromBloxAttribute))
					{
						flag = true;
						break;
					}
					if (type == typeof(BloxEventAttribute))
					{
						flag = true;
						break;
					}
					if (type == typeof(ObsoleteAttribute))
					{
						flag = true;
						break;
					}
					if (type == typeof(ExcludeFromDocsAttribute))
					{
						flag = true;
						break;
					}
				}
			}
			int num;
			string name;
			if (!flag)
			{
				name = m.Name;
				num = 0;
				ParameterInfo[] array = null;
				if (m.MemberType == MemberTypes.Constructor)
				{
					num = 1;
					array = m.GetParameters();
					if (!m.IsGenericMethod && !typeof(Component).IsAssignableFrom(m.ReflectedType) && !typeof(ScriptableObject).IsAssignableFrom(m.ReflectedType) && this.IsSupportedParams(array))
					{
						name = "new " + m.ReflectedType.Name + BloxEd.ParametersNameSection(array);
						goto IL_0216;
					}
				}
				else if (m.MemberType == MemberTypes.Method)
				{
					num = 2;
					array = m.GetParameters();
					if (!m.IsSpecialName && !m.IsGenericMethod && this.IsSupportedType(m.ReturnType) && this.IsSupportedParams(array))
					{
						name += BloxEd.ParametersNameSection(array);
						if (m.ReturnType != null && m.ReturnType != typeof(void))
						{
							name = name + ": " + BloxEd.PrettyTypeName(m.ReturnType, true);
						}
						goto IL_0216;
					}
				}
				else if (m.MemberType == MemberTypes.Field)
				{
					if (!m.IsSpecialName && this.IsSupportedType(m.ReturnType))
					{
						name = name + ": " + BloxEd.PrettyTypeName(m.ReturnType, true);
						goto IL_0216;
					}
				}
				else if (m.MemberType == MemberTypes.Property && !m.IsSpecialName && this.IsSupportedType(m.ReturnType) && !m.HasIndexParameters)
				{
					name = name + ": " + BloxEd.PrettyTypeName(m.ReturnType, true);
					goto IL_0216;
				}
			}
			return;
			IL_0216:
			if (m.DeclaringType != m.ReflectedType && m.MemberType != MemberTypes.Field && m.MemberType != MemberTypes.Property)
			{
				num += 10;
				icon = BloxEdGUI.Instance.bloxFadedIcon;
			}
			BloxBlockDef bloxBlockDef = new BloxBlockDef();
			defs.Add(bloxBlockDef);
			bloxBlockDef.order = num;
			bloxBlockDef.name = name;
			bloxBlockDef.icon = icon;
			bloxBlockDef.returnType = m.ReflectedType;
			bloxBlockDef.mi = m;
			bloxBlockDef.ident = m.ReflectedType.FullName.Replace('.', '/') + "/" + name;
			bloxBlockDef.ident = bloxBlockDef.ident.Replace('+', '.');
			if (string.IsNullOrEmpty(m.ReflectedType.Namespace))
			{
				bloxBlockDef.ident = "Scripts/" + bloxBlockDef.ident;
			}
		}

		private bool IsSupportedParams(ParameterInfo[] pars)
		{
			for (int i = 0; i < pars.Length; i++)
			{
				if (!this.IsSupportedType(pars[i].ParameterType))
				{
					return false;
				}
			}
			return true;
		}

		private bool IsSupportedType(Type t)
		{
			if (t != null && t != typeof(void))
			{
				if (t.IsArray)
				{
					t = t.GetElementType();
				}
				if (t.IsGenericType)
				{
					if (t.GetGenericTypeDefinition() != typeof(List<>))
					{
						return false;
					}
					t = t.GetGenericArguments()[0];
				}
				if (!t.IsArray && !t.IsGenericType)
				{
					if (!string.IsNullOrEmpty(t.Name) && !string.IsNullOrEmpty(t.FullName))
					{
						if (typeof(Exception).IsAssignableFrom(t))
						{
							return false;
						}
						if (t.IsPointer)
						{
							return false;
						}
						return true;
					}
					return false;
				}
				return false;
			}
			return true;
		}

		private void SaveBlocksSelection()
		{
			List<string> list = new List<string>();
			foreach (plyEdTreeItem<BloxBlockDef> item in this.treeView.GetMarked())
			{
				if (item.data != null && !item.hasChildren)
				{
					string text = "";
					text = BloxMemberInfo.EncodeMember(item.data.mi);
					list.Add(text);
				}
			}
			plyEdUtil.CheckPath(BloxEdGlobal.MiscPath);
			if (plyEdUtil.WriteCompressedLines(plyEdUtil.ProjectFullPath + BloxEdGlobal.MiscPath + "blocks.bin", list))
			{
				EditorUtility.DisplayDialog("Blox Scanner", "The Blocks selection has been saved.", "OK");
				BloxBlocksList.ReloadBlockDefs();
			}
			else
			{
				EditorUtility.DisplayDialog("Blox Scanner", "ERROR: Failed to save the Blocks selection.", "OK");
			}
		}

		private void EnterBloxDocEdMode()
		{
			this.list = new plyReorderableList(BloxEd.Instance.docsources, typeof(string), true, true, true, true, true, false, null, null);
			this.list.elementHeight = (float)(EditorGUIUtility.singleLineHeight + 4.0);
			this.list.drawHeaderCallback = this.DocSourcesList_Header;
			this.list.drawElementCallback = this.DocSourcesList_Element;
			this.list.onAddElement = this.DocSourcesList_Add;
			this.list.onRemoveElement = this.DocSourcesList_Remove;
			this.activeEd = 2;
			this.edMode = 1;
			this.scroll = Vector2.zero;
			this.SetHelpText(BloxSettingsWindow.S_BloxDocEdMsg1);
			this.bottomButtons = new List<BottomButton>
			{
				new BottomButton
				{
					label = BloxSettingsWindow.GC_ScanDocsButton,
					callback = this.GoScrapeDocs
				},
				new BottomButton
				{
					label = BloxSettingsWindow.GC_RestoreDefaultButton,
					callback = BloxEd.Instance.RestoreDefaultDocSources
				},
				new BottomButton
				{
					label = BloxSettingsWindow.GC_DoneButton,
					callback = this.StopScannerAndAndCloseBloxDocEdMode
				}
			};
		}

		private void StopScannerAndAndCloseBloxDocEdMode()
		{
			if (this.scanner != null)
			{
				this.scanner.Stop();
			}
			this.ReloadDocsAndCloseBloxDocEdMode();
		}

		private void ReloadDocsAndCloseBloxDocEdMode()
		{
			BloxEd.Instance.ReloadBloxDoc();
			this.activeEd = 0;
			this.edMode = 0;
			this.SetHelpText(null);
			this.bottomButtons = null;
		}

		private void GoScrapeDocs()
		{
			switch (EditorUtility.DisplayDialogComplex("BloxDoc", "Do you want to use online or local documentation as source? Using local documentation, normally installed with Unity, will be much faster.", "Local", "Online", "Cancel"))
			{
			case 0:
				this.docsPath = EditorApplication.applicationContentsPath + "/Documentation/en/ScriptReference/";
				this.docsPath = this.docsPath.Replace('\\', '/');
				this.DocsConfirm();
				break;
			case 1:
				this.docsPath = "http://docs.unity3d.com/ScriptReference/";
				this.StartScrapeDocs();
				break;
			}
		}

		private void DocsConfirm()
		{
			switch (EditorUtility.DisplayDialogComplex("BloxDoc", string.Format("Path to Unity scripting reference documentation: \n\n{0}\n\nContinue with this path or use [Change] to change it now.", this.docsPath), "Continue", "Change", "Cancel"))
			{
			case 0:
				this.StartScrapeDocs();
				break;
			case 1:
				this.docsPath = EditorUtility.OpenFolderPanel("Unity Scripting reference root", this.docsPath, "");
				this.docsPath = this.docsPath.Replace('\\', '/');
				this.DocsConfirm();
				break;
			}
		}

		private void StartScrapeDocs()
		{
			if (!this.docsPath.EndsWith("/"))
			{
				this.docsPath += "/";
			}
			if (!this.docsPath.StartsWith("http://") && !File.Exists(this.docsPath + "MonoBehaviour.Awake.html") && EditorUtility.DisplayDialog("BloxDoc", "The chosen path is not valid. You must choose a path to where the Unity Scripting Reference files are located.", "OK"))
				return;
			this.edMode = 2;
			this.scroll = Vector2.zero;
			this.SetHelpText(" ");
			this.scanner = plyEdCoroutine.Start(this.DocScraper(), true);
			this.bottomButtons = new List<BottomButton>
			{
				new BottomButton
				{
					label = BloxSettingsWindow.GC_CancelButton,
					callback = this.CloseBlocksEdMode
				}
			};
		}

		private void DoBloxDocSettings()
		{
			if (this.edMode == 1)
			{
				this.scroll = EditorGUILayout.BeginScrollView(this.scroll);
				EditorGUILayout.Space();
				this.list.DoLayoutList();
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndScrollView();
				EditorGUILayout.BeginVertical(plyEdGUI.Styles.BottomBar);
				EditorGUIUtility.labelWidth = 150f;
				this.compressScrapedDox = EditorGUILayout.Toggle(BloxSettingsWindow.GC_CompressDox, this.compressScrapedDox);
				EditorGUILayout.EndVertical();
			}
			else if (this.edMode == 2)
			{
				GUILayout.Space(20f);
				if (this.scrapeCount == 0)
				{
					BloxSettingsWindow.GC_DocPleaseWait.text = "updating bloxdoc";
				}
				else
				{
					BloxSettingsWindow.GC_DocPleaseWait.text = "updating bloxdoc: " + this.scrapeCount.ToString();
				}
				plyEdGUI.DrawSpinner(BloxSettingsWindow.GC_DocPleaseWait, true, true);
				GUILayout.FlexibleSpace();
				base.Repaint();
			}
			if (GUI.changed)
			{
				GUI.changed = false;
				EditorUtility.SetDirty(BloxEd.Instance);
			}
		}

		private void DocSourcesList_Header(Rect rect)
		{
			GUI.Label(rect, BloxSettingsWindow.GC_DocSources);
		}

		private void DocSourcesList_Element(Rect rect, int index, bool isActive, bool isFocused)
		{
			rect.y += 1f;
			rect.height = EditorGUIUtility.singleLineHeight;
			BloxEd.Instance.docsources[index] = GUI.TextField(rect, BloxEd.Instance.docsources[index]);
		}

		private void DocSourcesList_Add()
		{
			BloxEd.Instance.docsources.Add("");
			EditorUtility.SetDirty(BloxEd.Instance);
		}

		private void DocSourcesList_Remove()
		{
			if (this.list.index >= 0 && this.list.index < BloxEd.Instance.docsources.Count)
			{
				BloxEd.Instance.docsources.RemoveAt(this.list.index);
				EditorUtility.SetDirty(BloxEd.Instance);
			}
		}

		private IEnumerator DocScraper()
		{
			plyEdUtil.CheckPath(BloxEdGlobal.DocsPath);
			BloxEd.Instance.LoadEventDefs();
			while (BloxEd.Instance.EventDefsLoading)
			{
				yield return (object)null;
			}
			BloxEd.Instance.LoadBlockDefs(true);
			while (BloxEd.Instance.BlockDefsLoading)
			{
				yield return (object)null;
			}
			try
			{
				File.WriteAllText(plyEdUtil.ProjectFullPath + BloxEdGlobal.DocsPath + "scraped_b.txt", "");
				File.WriteAllText(plyEdUtil.ProjectFullPath + BloxEdGlobal.DocsPath + "scraped_b.bin", "");
			}
			catch
			{
			}
			int countBeforeYield = 50;
			int count = 0;
			this.scrapeCount = BloxEd.Instance.blockDefs.Count;
			BloxDocEntries dox = new BloxDocEntries();
			Dictionary<string, BloxBlockDef>.ValueCollection.Enumerator enumerator = BloxEd.Instance.blockDefs.Values.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					BloxBlockDef current = enumerator.Current;
					this.scrapeCount--;
					Type type = (current.mi == null) ? current.returnType : current.mi.DeclaringType;
					if (type != null && type.Namespace != null && type.Namespace.StartsWith("UnityEngine"))
					{
						current.bloxdoc = BloxEd.CreateEmptyDoc(current);
						current.bloxdoc.url = this.GetUnity3DHelpURL(type, current.mi);
						if (!string.IsNullOrEmpty(current.bloxdoc.url) && this.ScrapeUnity3D(current.bloxdoc))
						{
							dox.entries.Add(current.bloxdoc);
						}
						count++;
						if (count >= countBeforeYield)
						{
							count = 0;
							yield return (object)null;
						}
					}
				}
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			enumerator = default(Dictionary<string, BloxBlockDef>.ValueCollection.Enumerator);
			try
			{
				string text = JsonUtility.ToJson(dox, true);
				if (this.compressScrapedDox)
				{
					plyEdUtil.WriteCompressedString(plyEdUtil.ProjectFullPath + BloxEdGlobal.DocsPath + "scraped_b.bin", text);
				}
				else
				{
					File.WriteAllText(plyEdUtil.ProjectFullPath + BloxEdGlobal.DocsPath + "scraped_b.txt", text);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			EditorUtility.DisplayDialog("BloxDoc", "Documentation updated", "OK");
			this.ReloadDocsAndCloseBloxDocEdMode();
		}

		private string GetUnity3DHelpURL(Type t, BloxMemberInfo mi)
		{
			if (t.IsArray)
			{
				t = t.GetElementType();
			}
			else if (t.IsGenericType)
			{
				if (t.GetGenericTypeDefinition() != typeof(List<>))
				{
					return "";
				}
				t = t.GetGenericArguments()[0];
			}
			string str = "http://docs.unity3d.com/ScriptReference/";
			if (t.Namespace.Contains("."))
			{
				str = str + t.Namespace.Substring(t.Namespace.IndexOf('.') + 1) + ".";
			}
			str += t.Name;
			if (mi != null)
			{
				str = ((mi.MemberType != MemberTypes.Constructor) ? (str + ((mi.MemberType == MemberTypes.Method) ? "." : "-") + mi.Name) : (str + "-ctor"));
			}
			return str + ".html";
		}

		private bool ScrapeUnity3D(BloxDocEntry doc)
		{
			string text = doc.url;
			if (this.docsPath != "http://docs.unity3d.com/ScriptReference/")
			{
				text = text.Replace("http://docs.unity3d.com/ScriptReference/", this.docsPath);
			}
			try
			{
				string text2 = null;
				text2 = ((!text.StartsWith("http")) ? File.ReadAllText(text) : new WebClient().DownloadString(text));
				if (string.IsNullOrEmpty(text2))
				{
					return false;
				}
				HtmlDocument htmlDocument = new HtmlDocument();
				htmlDocument.LoadHtml(text2);
				this.ScrapeUnity3DDescription(htmlDocument, doc);
				this.ScrapeUnity3DParameters(htmlDocument, doc);
				return true;
			}
			catch (FileNotFoundException)
			{
				return false;
			}
			catch (WebException)
			{
				return false;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				return false;
			}
		}

		private void ScrapeUnity3DDescription(HtmlDocument html, BloxDocEntry doc)
		{
			StringBuilder stringBuilder = new StringBuilder();
			HtmlNode htmlNode = html.DocumentNode.SelectSingleNode("//div[@class=\"subsection\"]/h2[.=\"Description\"]");
			if (htmlNode != null)
			{
				foreach (HtmlNode item in (IEnumerable<HtmlNode>)htmlNode.ParentNode.ChildNodes)
				{
					if (!(item.Name == "h2"))
					{
						string text = item.InnerHtml.Trim();
						if (text.Length > 0)
						{
							stringBuilder.Append(this.ReplaceTagsUnity3DDotCom(text)).Append("\n\n");
						}
					}
				}
				HtmlNode nextSibling = htmlNode.ParentNode.NextSibling;
				while (nextSibling != null)
				{
					if (!(nextSibling.Name == "div"))
					{
						nextSibling = nextSibling.NextSibling;
						continue;
					}
					if (!nextSibling.InnerHtml.Contains("<h2>") && !nextSibling.InnerHtml.Contains("class=\"signature\""))
					{
						stringBuilder.Append(this.ReplaceTagsUnity3DDotCom(nextSibling.InnerHtml));
					}
					break;
				}
				doc.doc = stringBuilder.ToString();
			}
		}

		private void ScrapeUnity3DParameters(HtmlDocument html, BloxDocEntry doc)
		{
			if (doc.parameters.Length != 0)
			{
				HtmlNodeCollection htmlNodeCollection = html.DocumentNode.SelectNodes("//div[@class=\"subsection\"]/table/tr");
				if (htmlNodeCollection != null)
				{
					foreach (HtmlNode item in (IEnumerable<HtmlNode>)htmlNodeCollection)
					{
						HtmlNode htmlNode = item.SelectSingleNode("td[@class=\"name lbl\"]");
						HtmlNode htmlNode2 = item.SelectSingleNode("td[@class=\"desc\"]");
						if (htmlNode != null && htmlNode2 != null)
						{
							string innerText = htmlNode.InnerText;
							if (!string.IsNullOrEmpty(innerText))
							{
								BloxDocEntry.Parameter[] parameters = doc.parameters;
								for (int i = 0; i < parameters.Length; i++)
								{
									BloxDocEntry.Parameter parameter = parameters[i];
									if (innerText.ToLower().Equals(parameter.name.ToLower()))
									{
										parameter.doc = htmlNode2.InnerText;
									}
								}
							}
						}
					}
				}
			}
		}

		private string ReplaceTagsUnity3DDotCom(string s)
		{
			s = s.Replace("\n\r", " ").Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Replace("\t", "");
			while (true)
			{
				int num = s.IndexOf("<pre");
				if (num >= 0)
				{
					int num2 = s.IndexOf("</pre>", num + 1);
					s = s.Remove(num, num2 - num + 6);
				}
				if (num < 0)
					break;
			}
			while (true)
			{
				int num = s.IndexOf("<code class=\"varname\">");
				if (num >= 0)
				{
					int num3 = s.IndexOf("</code>", num + 22);
					s = s.Substring(0, num) + "<b>" + s.Substring(num + 22, num3 - num - 22) + "</b>" + s.Substring(num3 + 7);
				}
				if (num < 0)
					break;
			}
			while (true)
			{
				int num = s.IndexOf("<code class=\"mono\">");
				if (num >= 0)
				{
					int num4 = s.IndexOf("</code>", num + 19);
					s = s.Substring(0, num) + "<i>" + s.Substring(num + 19, num4 - num - 19) + "</i>" + s.Substring(num4 + 7);
				}
				if (num < 0)
					break;
			}
			while (true)
			{
				int num = s.IndexOf("<a ");
				if (num >= 0)
				{
					int num5 = s.IndexOf(">", num + 1);
					s = s.Remove(num, num5 - num + 1);
				}
				if (num < 0)
					break;
			}
			s = s.Replace("</a>", "");
			while (true)
			{
				int num = s.IndexOf("<p class=\"basic\"><img");
				if (num >= 0)
				{
					int num6 = s.IndexOf(">", num + 21);
					s = s.Remove(num, num6 - num + 1);
				}
				if (num < 0)
					break;
			}
			while (true)
			{
				int num = s.IndexOf("<img");
				if (num >= 0)
				{
					int num7 = s.IndexOf(">", num + 1);
					s = s.Remove(num, num7 - num + 1);
				}
				if (num < 0)
					break;
			}
			while (true)
			{
				int num = s.IndexOf("<em>");
				if (num >= 0)
				{
					int num8 = s.IndexOf("</em>", num + 4);
					s = s.Remove(num, num8 - num + 5);
				}
				if (num < 0)
					break;
			}
			while (true)
			{
				int num = s.IndexOf("<p");
				if (num >= 0)
				{
					int num9 = s.IndexOf(">", num + 1);
					s = s.Substring(0, num) + "\n\n" + s.Substring(num9 + 1);
				}
				if (num < 0)
					break;
			}
			s = s.Replace("<strong>", "<b>");
			s = s.Replace("</strong>", "</b>");
			s = s.Replace("</p>", "");
			s = s.Replace("<br>", "");
			return s.Trim();
		}
	}
}
