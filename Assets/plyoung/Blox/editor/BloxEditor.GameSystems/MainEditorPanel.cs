using BloxGameSystems;
using plyLibEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BloxEditor.GameSystems
{
	[InitializeOnLoad]
	public class MainEditorPanel : MainEdChild
	{
		public class StyleDefs
		{
			public GUIStyle ButtonMid_LeftText;

			public StyleDefs()
			{
				this.ButtonMid_LeftText = new GUIStyle(GUI.skin.FindStyle("ButtonMid"))
				{
					alignment = TextAnchor.MiddleLeft
				};
				this.ButtonMid_LeftText.padding.top++;
			}
		}

		private static readonly GUIContent[] menuLabels;

		private static StyleDefs _styles;

		private static int menuIdx;

		private Vector2[] scroll = new Vector2[2]
		{
			Vector2.zero,
			Vector2.zero
		};

		private Bootstrap bootstrap;

		private BGSSettings settings;

		private bool canTryLoadBootstrap;

		private static readonly GUIContent GC_GameScenes;

		private static readonly GUIContent GC_AddScene;

		private static readonly GUIContent GC_AutoloadBootstrap;

		private static readonly GUIContent GC_RenameScene;

		private static readonly GUIContent GC_RemoveScene;

		private static readonly GUIContent GC_FixError;

		private static readonly string STR_BootstrapError1;

		private static readonly string STR_BootstrapError2;

		private static GUIContent GC_StartupOrder;

		private static GUIContent GC_AutoLoad_OFF;

		private static GUIContent GC_AutoLoad_ON;

		private static readonly GUIContent GC_GlobalVars;

		private static readonly GUIContent GC_GlobalVarsMsg;

		private static readonly GUIContent GC_ShowGlobalVars;

		private static readonly GUIContent GC_Settings;

		private static readonly GUIContent GC_AutoLoadBootstrap;

		private static readonly GUIContent GC_InstallDocs;

		private static readonly GUIContent GC_UseDocs;

		private static readonly GUIContent GC_OnlineDocs;

		private static GUIContent GC_OfflineDocs;

		public static StyleDefs Styles
		{
			get
			{
				return MainEditorPanel._styles ?? (MainEditorPanel._styles = new StyleDefs());
			}
		}

		public override int order
		{
			get
			{
				return 0;
			}
		}

		public override string label
		{
			get
			{
				return Ico._settings + " Main";
			}
		}

		static MainEditorPanel()
		{
			MainEditorPanel.menuLabels = new GUIContent[4]
			{
				new GUIContent("Startup & Scenes"),
				new GUIContent("Global Variables"),
				null,
				new GUIContent("Settings")
			};
			MainEditorPanel.menuIdx = 0;
			MainEditorPanel.GC_GameScenes = new GUIContent(Ico._ellipsis_v + " Scenes");
			MainEditorPanel.GC_AddScene = new GUIContent(Ico._add_c, "Add scene");
			MainEditorPanel.GC_AutoloadBootstrap = new GUIContent(Ico._star, "The bootstrap scene should always load first");
			MainEditorPanel.GC_RenameScene = new GUIContent(Ico._rename, "Rename scene");
			MainEditorPanel.GC_RemoveScene = new GUIContent(Ico._remove, "Remove scene");
			MainEditorPanel.GC_FixError = new GUIContent("Fix it now");
			MainEditorPanel.STR_BootstrapError1 = "The bootstrap data could not be loaded.";
			MainEditorPanel.STR_BootstrapError2 = "The bootstrap scene must be present and first in the list of scenes.";
			MainEditorPanel.GC_StartupOrder = new GUIContent(" ", "Toggle startup scene and order of loading");
			MainEditorPanel.GC_AutoLoad_OFF = new GUIContent(" ", "Toggle scene for auto-loading");
			MainEditorPanel.GC_AutoLoad_ON = new GUIContent(Ico._star_o, "Toggle scene for auto-loading");
			MainEditorPanel.GC_GlobalVars = new GUIContent(Ico._ellipsis_v + " Global Variables");
			MainEditorPanel.GC_GlobalVarsMsg = new GUIContent("The Global Variables should now be available for editing in the Inspector.");
			MainEditorPanel.GC_ShowGlobalVars = new GUIContent("Show Global Variables");
			MainEditorPanel.GC_Settings = new GUIContent(Ico._ellipsis_v + " Settings");
			MainEditorPanel.GC_AutoLoadBootstrap = new GUIContent("Auto-load Bootstrap", "Auto-load Bootstrap when the Unity Play Button is used? This should be on if you are using the Blox Game Systems.");
			MainEditorPanel.GC_InstallDocs = new GUIContent("Install Offline Documentation", "The offline documentation will be installed to the 'Documentations/Blox' folder under the root of your project's path.");
			MainEditorPanel.GC_UseDocs = new GUIContent("Use ...");
			MainEditorPanel.GC_OnlineDocs = new GUIContent("ONLINE: " + BloxEdGlobal.URL_DOCS);
			MainEditorPanel.GC_OfflineDocs = null;
			MainEditorWindow.AddChildEditor(new MainEditorPanel());
		}

		public override void OnFocus()
		{
			if ((Object)this.bootstrap == (Object)null)
			{
				this.bootstrap = plyEdUtil.LoadPrefab<Bootstrap>(BloxEdGlobal.BootstrapFabPath);
			}
			if ((Object)this.settings == (Object)null)
			{
				plyEdUtil.CheckPath(BloxEdGlobal.ResourcesRoot);
				this.settings = plyEdUtil.LoadOrCreateAsset<BGSSettings>(BloxEdGlobal.ResourcesRoot + "BGSSettings.asset", true);
			}
		}

		public override void OnGUI()
		{
			if ((Object)this.bootstrap == (Object)null)
			{
				if (this.canTryLoadBootstrap && Event.current.type == EventType.Repaint)
				{
					this.canTryLoadBootstrap = false;
					this.bootstrap = plyEdUtil.LoadPrefab<Bootstrap>(BloxEdGlobal.BootstrapFabPath);
					GUIUtility.ExitGUI();
				}
				else if (plyEdGUI.MessageButton(MainEditorPanel.GC_FixError, MainEditorPanel.STR_BootstrapError1, MessageType.Error))
				{
					this.canTryLoadBootstrap = true;
					BloxEdGlobal.CheckBootstrap();
					GUIUtility.ExitGUI();
				}
			}
			else
			{
				EditorGUILayout.BeginHorizontal();
				if (plyEdGUI.Menu(ref MainEditorPanel.menuIdx, ref this.scroll[0], MainEditorPanel.menuLabels, new GUILayoutOption[1]
				{
					GUILayout.Width(150f)
				}))
				{
					switch (MainEditorPanel.menuIdx)
					{
					case 0:
						this.SelectedStartupSettings();
						break;
					case 1:
						this.SelectGlobalVars();
						break;
					case 3:
						this.SelectedSettings();
						break;
					}
				}
				EditorGUILayout.BeginVertical();
				this.scroll[1] = EditorGUILayout.BeginScrollView(this.scroll[1]);
				EditorGUILayout.Space();
				switch (MainEditorPanel.menuIdx)
				{
				case 0:
					this.DrawStartupSettings();
					break;
				case 1:
					this.DrawGlobalVars();
					break;
				case 3:
					this.DrawSettings();
					break;
				}
				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}
		}

		private void SelectedStartupSettings()
		{
		}

		private void DrawStartupSettings()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(MainEditorPanel.GC_GameScenes, plyEdGUI.Styles.HeadLabel);
			EditorGUILayout.Space();
			plyEdHelpManager.Button("blox", "scenes");
			EditorGUILayout.Space();
			if (GUILayout.Button(MainEditorPanel.GC_AddScene, plyEdGUI.Styles.BigButtonFlat))
			{
				string text = EditorUtility.OpenFilePanel("Select Scene", plyEdUtil.ProjectFullPath + "Assets/", "unity");
				if (!string.IsNullOrEmpty(text))
				{
					text = plyEdUtil.ProjectRelativePath(text);
					plyEdUtil.AddSceneToBuildSettings(text, false);
				}
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(10f);
			if (EditorBuildSettings.scenes.Length == 0)
			{
				if (plyEdGUI.MessageButton(MainEditorPanel.GC_FixError, MainEditorPanel.STR_BootstrapError2, MessageType.Error))
				{
					BloxEdGlobal.CheckBootstrap();
					GUIUtility.ExitGUI();
				}
			}
			else
			{
				int num = -1;
				int num2 = -1;
				int num3 = -1;
				int num4 = -1;
				for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
				{
					if (i == 0 && (EditorBuildSettings.scenes[i].path != BloxEdGlobal.BootstrapScenePath || !EditorBuildSettings.scenes[i].enabled))
					{
						if (!plyEdGUI.MessageButton(MainEditorPanel.GC_FixError, MainEditorPanel.STR_BootstrapError2, MessageType.Error))
							break;
						this.canTryLoadBootstrap = true;
						BloxEdGlobal.CheckBootstrap();
						GUIUtility.ExitGUI();
						return;
					}
					if (EditorBuildSettings.scenes[i].enabled)
					{
						GUI.enabled = (i != 0);
						EditorGUILayout.BeginHorizontal();
						string text2 = EditorBuildSettings.scenes[i].path.Substring(7, EditorBuildSettings.scenes[i].path.Length - 13);
						if (i == 0)
						{
							GUILayout.Label(MainEditorPanel.GC_AutoloadBootstrap, plyEdGUI.Styles.ButtonLeft, GUILayout.Width(25f));
							GUILayout.Label(MainEditorPanel.GC_AutoloadBootstrap, plyEdGUI.Styles.ButtonMid, GUILayout.Width(25f));
						}
						else
						{
							int num5 = this.bootstrap.startupScenes.IndexOf(i);
							MainEditorPanel.GC_StartupOrder.text = ((num5 >= 0) ? (num5 + 1).ToString() : " ");
							if (GUILayout.Button(MainEditorPanel.GC_StartupOrder, plyEdGUI.Styles.ButtonLeft, GUILayout.Width(25f)))
							{
								num = i;
							}
							if (GUILayout.Button(this.bootstrap.autoloadScenes.Contains(i) ? MainEditorPanel.GC_AutoLoad_ON : MainEditorPanel.GC_AutoLoad_OFF, plyEdGUI.Styles.ButtonMid, GUILayout.Width(25f)))
							{
								num2 = i;
							}
						}
						if (GUILayout.Button(text2, MainEditorPanel.Styles.ButtonMid_LeftText, GUILayout.Width(375f)) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
						{
							EditorSceneManager.OpenScene(EditorBuildSettings.scenes[i].path, OpenSceneMode.Single);
							if (SceneView.sceneViews.Count > 0)
							{
								((SceneView)SceneView.sceneViews[0]).Focus();
							}
						}
						if (GUILayout.Button(MainEditorPanel.GC_RenameScene, plyEdGUI.Styles.ButtonMid, GUILayout.Width(30f)))
						{
							num4 = i;
						}
						if (GUILayout.Button(MainEditorPanel.GC_RemoveScene, plyEdGUI.Styles.ButtonRight, GUILayout.Width(30f)))
						{
							num3 = i;
						}
						EditorGUILayout.EndHorizontal();
						GUI.enabled = true;
					}
				}
				if (num3 >= 1)
				{
					plyEdUtil.RemoveSceneFromBuildSettings(num3);
				}
				if (num4 >= 1)
				{
					plyTextInputWiz.ShowWiz("Rename Scene", "Enter unique name for scene", plyEdUtil.SceneNameFromPath(EditorBuildSettings.scenes[num4].path), this.OnRenameScene, new object[1]
					{
						num4
					}, 250f);
				}
				if (num >= 1)
				{
					if (this.bootstrap.startupScenes.Contains(num))
					{
						this.bootstrap.startupScenes.Remove(num);
					}
					else
					{
						this.bootstrap.startupScenes.Add(num);
						if (this.bootstrap.autoloadScenes.Contains(num))
						{
							this.bootstrap.autoloadScenes.Remove(num);
						}
					}
					EditorUtility.SetDirty(this.bootstrap);
				}
				if (num2 >= 1)
				{
					if (this.bootstrap.autoloadScenes.Contains(num2))
					{
						this.bootstrap.autoloadScenes.Remove(num2);
					}
					else
					{
						this.bootstrap.autoloadScenes.Add(num2);
						if (this.bootstrap.startupScenes.Contains(num2))
						{
							this.bootstrap.startupScenes.Remove(num2);
						}
					}
					EditorUtility.SetDirty(this.bootstrap);
				}
			}
		}

		private void OnRenameScene(plyTextInputWiz wiz)
		{
			int num = (int)wiz.args[0];
			string text = wiz.text;
			wiz.Close();
			if (num != -1 && !string.IsNullOrEmpty(text))
			{
				string text2 = AssetDatabase.RenameAsset(EditorBuildSettings.scenes[num].path, text);
				if (!string.IsNullOrEmpty(text2))
				{
					EditorUtility.DisplayDialog("Error", text2, "Close");
				}
				base.editorWindow.Repaint();
			}
		}

		private void SelectGlobalVars()
		{
			GameObject bloxGlobalPrefab = BloxEd.BloxGlobalPrefab;
			if ((Object)bloxGlobalPrefab != (Object)null)
			{
				Selection.activeGameObject = bloxGlobalPrefab;
			}
			else
			{
				EditorUtility.DisplayDialog("Global Variables", "The BloxGlobal could not be found or created.", "OK");
			}
		}

		private void DrawGlobalVars()
		{
			GUILayout.Label(MainEditorPanel.GC_GlobalVars, plyEdGUI.Styles.HeadLabel);
			EditorGUILayout.BeginVertical(GUILayout.Width(MainEditorWindow.PanelContentWidth));
			GUILayout.Label(MainEditorPanel.GC_GlobalVarsMsg);
			EditorGUILayout.Space();
			if (GUILayout.Button(MainEditorPanel.GC_ShowGlobalVars, GUILayout.Width(200f)))
			{
				this.SelectGlobalVars();
			}
			EditorGUILayout.EndVertical();
		}

		private void SelectedSettings()
		{
		}

		private void DrawSettings()
		{
			GUILayout.Label(MainEditorPanel.GC_Settings, plyEdGUI.Styles.HeadLabel);
			this.settings.autoLoadBootstrapOnUnityPlay = EditorGUILayout.Toggle(MainEditorPanel.GC_AutoLoadBootstrap, this.settings.autoLoadBootstrapOnUnityPlay);
			EditorGUILayout.Space();
			plyEdGUI.DrawLine();
			EditorGUILayout.Space();
			if (GUILayout.Button(MainEditorPanel.GC_InstallDocs, GUILayout.Width(200f)))
			{
				plyEdHelpManager.InstallOfflineDocs(new string[1]
				{
					"blox"
				});
			}
			EditorGUILayout.Space();
			GUILayout.Label(MainEditorPanel.GC_UseDocs);
			if (MainEditorPanel.GC_OfflineDocs == null)
			{
				MainEditorPanel.GC_OfflineDocs = new GUIContent("OFFLINE: " + plyEdUtil.ProjectDocumentationFolder);
			}
			if (plyEdGUI.ToggleButton(!plyEdHelpManager.UseOfflineDocs, MainEditorPanel.GC_OnlineDocs, GUI.skin.toggle))
			{
				plyEdHelpManager.UseOfflineDocs = false;
			}
			if (plyEdGUI.ToggleButton(plyEdHelpManager.UseOfflineDocs, MainEditorPanel.GC_OfflineDocs, GUI.skin.toggle))
			{
				plyEdHelpManager.UseOfflineDocs = true;
			}
		}
	}
}
