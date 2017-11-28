using BloxEngine;
using BloxEngine.Variables;
using plyLibEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BloxEditor
{
	[Serializable]
	public class BloxEd : ScriptableObject
	{
		public List<string> namespaces;

		public List<string> docsources;

		public bool includeDeclaredOnly;

		private static BloxEd _instance;

		private const int EntriesPorcessCount = 50;

		private plyEdCoroutine evnloader;

		private plyEdCoroutine defloader;

		private plyEdCoroutine docloader;

		private plyEdCoroutine docfinder;

		private static GameObject _bloxGlobalPrefab;

		private static BloxGlobal _bloxGlobalObj;

		private static GlobalVariables _globalVarsObj;

		[NonSerialized]
		public List<BloxEventDef> eventDefs = new List<BloxEventDef>();

		[NonSerialized]
		public List<string> eventMethodNames = new List<string>();

		[NonSerialized]
		public Dictionary<string, BloxBlockDef> blockDefs = new Dictionary<string, BloxBlockDef>();

		private static List<string> defaultNamespaces = new List<string>
		{
			"UnityEngine",
			"UnityEngine.UI",
			"UnityEngine.Audio",
			"UnityEngine.SceneManagement",
			"System.String",
			"BloxGameSystems",
			"*"
		};

		private static List<string> defaultDocSources = new List<string>
		{
			plyEdUtil.PackagesRelativePath + "Blox/editor/res/events.txt",
			plyEdUtil.PackagesRelativePath + "Blox/editor/res/blocks.txt",
			BloxEdGlobal.DocsPath + "scraped_b.bin",
			BloxEdGlobal.DocsPath + "scraped_b.txt",
			plyEdUtil.PackagesRelativePath + "Blox/editor/res/unity_b.bin"
		};

		private static readonly GUIContent GC_Loading = new GUIContent("loading ...");

		[NonSerialized]
		private BloxDocEntries docs;

		[NonSerialized]
		private int loadDocs = 1;

		[NonSerialized]
		private int findDoc_step;

		[NonSerialized]
		private BloxEdDef currLookupDef;

		[NonSerialized]
		private List<BloxEdDef> docLookupList = new List<BloxEdDef>();

		public static BloxEd Instance
		{
			get
			{
				if ((UnityEngine.Object)BloxEd._instance == (UnityEngine.Object)null)
				{
					BloxEd.Create();
				}
				return BloxEd._instance;
			}
		}

		public static GameObject BloxGlobalPrefab
		{
			get
			{
				if ((UnityEngine.Object)BloxEd._bloxGlobalPrefab == (UnityEngine.Object)null)
				{
					BloxEd.LoadBloxGlobal();
				}
				return BloxEd._bloxGlobalPrefab;
			}
		}

		public static BloxGlobal BloxGlobalObj
		{
			get
			{
				if ((UnityEngine.Object)BloxEd._bloxGlobalObj == (UnityEngine.Object)null)
				{
					BloxEd.LoadBloxGlobal();
				}
				return BloxEd._bloxGlobalObj;
			}
		}

		public static GlobalVariables GlobalVarsObj
		{
			get
			{
				if ((UnityEngine.Object)BloxEd._globalVarsObj == (UnityEngine.Object)null)
				{
					BloxEd.LoadBloxGlobal();
				}
				return BloxEd._globalVarsObj;
			}
		}

		public bool EventDefsLoading
		{
			get;
			private set;
		}

		public bool BlockDefsLoading
		{
			get;
			private set;
		}

		public void DoUpdate()
		{
			if (this.evnloader != null)
			{
				this.evnloader.DoUpdate();
			}
			if (this.defloader != null)
			{
				this.defloader.DoUpdate();
			}
			if (this.docloader != null)
			{
				this.docloader.DoUpdate();
			}
			if (this.docfinder != null)
			{
				this.docfinder.DoUpdate();
			}
		}

		private static BloxEd Create()
		{
			plyEdUtil.CheckPath(BloxEdGlobal.MiscPath);
			BloxEd._instance = plyEdUtil.LoadAsset<BloxEd>(BloxEdGlobal.MiscPath + "bloxsettings.asset");
			if ((UnityEngine.Object)BloxEd._instance == (UnityEngine.Object)null)
			{
				BloxEd._instance = plyEdUtil.LoadOrCreateAsset<BloxEd>(BloxEdGlobal.MiscPath + "bloxsettings.asset", false);
				BloxEd._instance.namespaces = new List<string>();
				BloxEd._instance.namespaces.AddRange(BloxEd.defaultNamespaces);
				BloxEd._instance.docsources = new List<string>();
				BloxEd._instance.docsources.AddRange(BloxEd.defaultDocSources);
				EditorUtility.SetDirty(BloxEd._instance);
				AssetDatabase.SaveAssets();
			}
			BloxEd._instance.EventDefsLoading = false;
			BloxEd._instance.BlockDefsLoading = false;
			BloxEd._instance.ReloadBloxDoc();
			return BloxEd._instance;
		}

		public static void LoadBloxGlobal()
		{
			if (!((UnityEngine.Object)BloxEd._bloxGlobalPrefab != (UnityEngine.Object)null))
			{
				plyEdUtil.CheckPath(BloxEdGlobal.DataRoot);
				BloxEd._bloxGlobalPrefab = plyEdUtil.LoadOrCreatePrefab<BloxGlobal>("BloxGlobal", BloxEdGlobal.BloxGlobalFabPath);
				BloxEd._bloxGlobalObj = BloxEd._bloxGlobalPrefab.GetComponent<BloxGlobal>();
				BloxEd._globalVarsObj = BloxEd._bloxGlobalPrefab.GetComponent<GlobalVariables>();
				if ((UnityEngine.Object)BloxEd._bloxGlobalObj == (UnityEngine.Object)null)
				{
					BloxEd._bloxGlobalPrefab.AddComponent<BloxGlobal>();
					EditorUtility.SetDirty(BloxEd._bloxGlobalPrefab);
				}
				if ((UnityEngine.Object)BloxEd._globalVarsObj == (UnityEngine.Object)null)
				{
					BloxEd._bloxGlobalPrefab.AddComponent<GlobalVariables>();
					EditorUtility.SetDirty(BloxEd._bloxGlobalPrefab);
				}
				bool flag = BloxEd._bloxGlobalObj.CleanupBloxDefsListAndBuildCache();
				try
				{
					string[] files = Directory.GetFiles(plyEdUtil.ProjectFullPath + BloxEdGlobal.DefsPath);
					for (int i = 0; i < files.Length; i++)
					{
						Blox blox = plyEdUtil.LoadAsset<Blox>(plyEdUtil.ProjectRelativePath(files[i]));
						if ((UnityEngine.Object)blox != (UnityEngine.Object)null && (UnityEngine.Object)BloxEd._bloxGlobalObj.FindBloxDef(blox.ident) == (UnityEngine.Object)null)
						{
							BloxEd._bloxGlobalObj.bloxDefs.Add(blox);
							flag = true;
						}
					}
				}
				catch
				{
				}
				BloxEd.SortBloxDefList();
				if (flag)
				{
					EditorUtility.SetDirty(BloxEd._bloxGlobalObj);
				}
			}
		}

		public static Blox CreateNewBloxDef()
		{
			plyEdUtil.CheckPath(BloxEdGlobal.DefsPath);
			string text = plyEdUtil.GenerateUniqueFileGUID("_", BloxEdGlobal.DefsPath, ".asset");
			Blox blox = plyEdUtil.LoadOrCreateAsset<Blox>(BloxEdGlobal.DefsPath + text + ".asset", false);
			blox.ident = text;
			blox.screenName = text;
			EditorUtility.SetDirty(blox);
			AssetDatabase.SaveAssets();
			BloxEd.BloxGlobalObj.bloxDefs.Add(blox);
			BloxEd.SortBloxDefList();
			EditorUtility.SetDirty(BloxEd.BloxGlobalObj);
			return blox;
		}

		public static void DeleteBloxDef(Blox def)
		{
			int num = 0;
			while (num < BloxEd.BloxGlobalObj.bloxDefs.Count)
			{
				if (!(BloxEd.BloxGlobalObj.bloxDefs[num].ident == def.ident))
				{
					num++;
					continue;
				}
				BloxEd.BloxGlobalObj.bloxDefs.RemoveAt(num);
				break;
			}
			EditorUtility.SetDirty(BloxEd.BloxGlobalObj);
			string assetPath = AssetDatabase.GetAssetPath(def);
			if (!string.IsNullOrEmpty(assetPath))
			{
				AssetDatabase.DeleteAsset(assetPath);
			}
		}

		public static void SortBloxDefList()
		{
			BloxEd.BloxGlobalObj.bloxDefs.Sort((Blox a, Blox b) => a.screenName.CompareTo(b.screenName));
		}

		public void LoadEventDefs()
		{
			if (!this.EventDefsLoading && this.eventDefs.Count == 0)
			{
				this.EventDefsLoading = true;
				plyEdCoroutine obj = this.evnloader;
				if (obj != null)
				{
					obj.Stop();
				}
				this.evnloader = plyEdCoroutine.Start(this.EventDefsLoader(), true);
			}
		}

		public void UnloadEventDefs()
		{
			plyEdCoroutine obj = this.evnloader;
			if (obj != null)
			{
				obj.Stop();
			}
			this.EventDefsLoading = false;
			this.eventDefs.Clear();
		}

		public BloxEventDef FindEventDef(BloxEvent ev)
		{
			if (ev == null)
			{
				return null;
			}
			for (int i = 0; i < this.eventDefs.Count; i++)
			{
				if (this.eventDefs[i].ident.Equals(ev.ident))
				{
					return this.eventDefs[i];
				}
			}
			return null;
		}

		private IEnumerator EventDefsLoader()
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			List<Type> foundEventHandlers = new List<Type>();
			for (int j = 0; j < assemblies.Length; j++)
			{
				Type[] exportedTypes = assemblies[j].GetExportedTypes();
				for (int k = 0; k < exportedTypes.Length; k++)
				{
					if (exportedTypes[k].IsClass && typeof(BloxEventHandler).IsAssignableFrom(exportedTypes[k]) && exportedTypes[k].Name != "BloxEventHandler")
					{
						foundEventHandlers.Add(exportedTypes[k]);
					}
				}
			}
			yield return (object)null;
			int count = 0;
			for (int i = 0; i < foundEventHandlers.Count; i++)
			{
				MethodInfo[] methods;
				MethodInfo[] array = methods = foundEventHandlers[i].GetMethods();
				for (int l = 0; l < methods.Length; l++)
				{
					MethodInfo methodInfo = methods[l];
					object[] customAttributes = methodInfo.GetCustomAttributes(typeof(BloxEventAttribute), true);
					if (customAttributes.Length != 0)
					{
						BloxEventAttribute bloxEventAttribute = (BloxEventAttribute)customAttributes[0];
						string name = bloxEventAttribute.Ident.Contains("/") ? bloxEventAttribute.Ident.Substring(bloxEventAttribute.Ident.LastIndexOf('/') + 1) : bloxEventAttribute.Ident;
						BloxEventDef bloxEventDef = new BloxEventDef
						{
							icon = BloxEdGUI.Instance.unityIcon,
							name = name,
							ident = bloxEventAttribute.Ident,
							order = bloxEventAttribute.Order,
							methodNfo = methodInfo,
							yieldAllowed = bloxEventAttribute.YieldAllowed
						};
						if (!string.IsNullOrEmpty(bloxEventAttribute.IconName) && BloxEdGUI.Instance.namedIcons.ContainsKey(bloxEventAttribute.IconName))
						{
							bloxEventDef.icon = BloxEdGUI.Instance.namedIcons[bloxEventAttribute.IconName];
						}
						this.eventDefs.Add(bloxEventDef);
						if (bloxEventDef.methodNfo.Name != "Custom")
						{
							this.eventMethodNames.Add(bloxEventDef.methodNfo.Name);
						}
						ParameterInfo[] parameters = methodInfo.GetParameters();
						if (parameters.Length != 0)
						{
							bloxEventDef.pars = new BloxEventDef.Param[parameters.Length];
							for (int m = 0; m < parameters.Length; m++)
							{
								bloxEventDef.pars[m] = new BloxEventDef.Param
								{
									name = parameters[m].Name,
									type = parameters[m].ParameterType,
									typeName = BloxEd.PrettyTypeName(parameters[m].ParameterType, false)
								};
							}
						}
					}
					count++;
					if (count >= 50)
					{
						count = 0;
						yield return (object)null;
					}
				}
			}
			this.eventDefs.Sort((BloxEventDef a, BloxEventDef b) => a.CompareTo(b));
			this.EventDefsLoading = false;
		}

		public void LoadBlockDefs(bool forced)
		{
			if (!forced)
			{
				if (this.blockDefs.Count != 0)
					return;
				if (this.BlockDefsLoading)
					return;
			}
			this.blockDefs.Clear();
			this.BlockDefsLoading = true;
			plyEdCoroutine obj = this.defloader;
			if (obj != null)
			{
				obj.Stop();
			}
			this.defloader = plyEdCoroutine.Start(this.BlockDefsLoader(), true);
		}

		public BloxBlockDef FindBlockDef(BloxBlock b)
		{
			if (b == null)
			{
				return null;
			}
			BloxBlockDef bloxBlockDef = null;
			if (this.blockDefs.TryGetValue(b.ident, out bloxBlockDef))
			{
				return bloxBlockDef;
			}
			if (this.BlockDefsLoading)
			{
				return null;
			}
			if (BloxBlocksList.HasInstance && BloxBlocksList.Instance.IsBuildingList)
			{
				return null;
			}
			bloxBlockDef = this.ForceLoadDefFor(b);
			if (bloxBlockDef == null)
			{
				bloxBlockDef = new BloxBlockDef
				{
					blockType = BloxBlockType.Unknown
				};
			}
			return bloxBlockDef;
		}

		public BloxBlockDef FindBlockDef(string ident)
		{
			BloxBlockDef result = null;
			if (this.blockDefs.TryGetValue(ident, out result))
			{
				return result;
			}
			return null;
		}

		public BloxBlockDef FindValueBlockDef(Type t)
		{
			if (t == null)
			{
				return null;
			}
			string valueBlockIdent = BloxEd.GetValueBlockIdent(t);
			BloxBlockDef result = null;
			if (this.blockDefs.TryGetValue(valueBlockIdent, out result))
			{
				return result;
			}
			return null;
		}

		private BloxBlockDef ForceLoadDefFor(BloxBlock b)
		{
			if (b.mi == null)
			{
				return null;
			}
			List<Type> usedValueTypes = new List<Type>();
			BloxBlockDef bloxBlockDef = BloxEd.CreateMemberBlock(b.mi, usedValueTypes);
			if (bloxBlockDef == null)
			{
				return null;
			}
			if (this.blockDefs.ContainsKey(bloxBlockDef.ident))
			{
				return this.blockDefs[bloxBlockDef.ident];
			}
			this.blockDefs.Add(bloxBlockDef.ident, bloxBlockDef);
			return bloxBlockDef;
		}

		private IEnumerator BlockDefsLoader()
		{
			List<Type> usedValueTypes = new List<Type>(10)
			{
				typeof(bool),
				typeof(int),
				typeof(float),
				typeof(string),
				typeof(Vector2),
				typeof(Vector3),
				typeof(Rect),
				typeof(Color)
			};
			new List<Type>();
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			List<Type> foundBlockTypes = new List<Type>();
			Dictionary<Type, Type> editorTypes = new Dictionary<Type, Type>();
			for (int k = 0; k < assemblies.Length; k++)
			{
				Type[] exportedTypes = assemblies[k].GetExportedTypes();
				for (int l = 0; l < exportedTypes.Length; l++)
				{
					if (exportedTypes[l].IsClass && typeof(BloxBlock).IsAssignableFrom(exportedTypes[l]) && exportedTypes[l].Name != "BloxBlock")
					{
						foundBlockTypes.Add(exportedTypes[l]);
					}
					if (exportedTypes[l].IsClass && typeof(BloxBlockDrawer).IsAssignableFrom(exportedTypes[l]) && exportedTypes[l].Name != "BloxBlockDrawer")
					{
						object[] customAttributes = exportedTypes[l].GetCustomAttributes(typeof(BloxBlockDrawerAttribute), false);
						if (customAttributes.Length != 0)
						{
							object[] array = customAttributes;
							for (int m = 0; m < array.Length; m++)
							{
								BloxBlockDrawerAttribute bloxBlockDrawerAttribute = (BloxBlockDrawerAttribute)array[m];
								if (bloxBlockDrawerAttribute.TargetType != null && !editorTypes.ContainsKey(bloxBlockDrawerAttribute.TargetType))
								{
									editorTypes.Add(bloxBlockDrawerAttribute.TargetType, exportedTypes[l]);
								}
							}
						}
					}
				}
			}
			yield return (object)null;
			int count2 = 0;
			for (int i = 0; i < foundBlockTypes.Count; i++)
			{
				object[] customAttributes2 = foundBlockTypes[i].GetCustomAttributes(typeof(BloxBlockAttribute), true);
				if (customAttributes2.Length != 0)
				{
					BloxBlockAttribute bloxBlockAttribute = (BloxBlockAttribute)customAttributes2[0];
					string text = bloxBlockAttribute.Ident.Contains("/") ? bloxBlockAttribute.Ident.Substring(bloxBlockAttribute.Ident.LastIndexOf('/') + 1) : bloxBlockAttribute.Ident;
					BloxBlockDef bloxBlockDef = new BloxBlockDef
					{
						ident = bloxBlockAttribute.Ident,
						blockType = bloxBlockAttribute.BlockType,
						order = bloxBlockAttribute.Order,
						name = text,
						shortName = text,
						icon = BloxEdGUI.Instance.bloxIcon,
						blockSystemType = foundBlockTypes[i],
						att = bloxBlockAttribute,
						returnType = bloxBlockAttribute.ReturnType,
						returnName = BloxEd.PrettyTypeName(bloxBlockAttribute.ReturnType, true),
						overrideRenderFields = bloxBlockAttribute.OverrideRenderFields,
						contextType = bloxBlockAttribute.ContextType,
						contextName = BloxEd.PrettyTypeName(bloxBlockAttribute.ContextType, false),
						isYieldBlock = bloxBlockAttribute.IsYieldBlock
					};
					if (!string.IsNullOrEmpty(bloxBlockAttribute.IconName) && BloxEdGUI.Instance.namedIcons.ContainsKey(bloxBlockAttribute.IconName))
					{
						bloxBlockDef.icon = BloxEdGUI.Instance.namedIcons[bloxBlockAttribute.IconName];
						bloxBlockDef.showIconInCanvas = false;
					}
					if (editorTypes.ContainsKey(foundBlockTypes[i]))
					{
						bloxBlockDef.drawer = (BloxBlockDrawer)Activator.CreateInstance(editorTypes[foundBlockTypes[i]]);
					}
					if (((bloxBlockAttribute.ParamTypes != null) ? bloxBlockAttribute.ParamTypes.Length : 0) != 0)
					{
						if (bloxBlockAttribute.ParamTypes.Length != bloxBlockAttribute.ParamNames.Length)
						{
							Debug.LogError("ParamNames.Length != ParamTypes.Length for: " + foundBlockTypes[i].Name);
						}
						else
						{
							bool flag = bloxBlockAttribute.ParamEmptyVal != null;
							if (flag && bloxBlockAttribute.ParamEmptyVal.Length != bloxBlockAttribute.ParamTypes.Length)
							{
								flag = false;
								Debug.LogError("ParamEmptyVal.Length != ParamTypes.Length for: " + foundBlockTypes[i].Name);
							}
							bloxBlockDef.paramDefs = new BloxBlockDef.Param[bloxBlockAttribute.ParamTypes.Length];
							for (int n = 0; n < bloxBlockAttribute.ParamTypes.Length; n++)
							{
								bloxBlockDef.paramDefs[n] = new BloxBlockDef.Param();
								bloxBlockDef.paramDefs[n].name = bloxBlockAttribute.ParamNames[n];
								bloxBlockDef.paramDefs[n].type = bloxBlockAttribute.ParamTypes[n];
								bloxBlockDef.paramDefs[n].emptyVal = (flag ? bloxBlockAttribute.ParamEmptyVal[n] : null);
								if (bloxBlockDef.paramDefs[n].type != null && bloxBlockDef.paramDefs[n].type.IsByRef)
								{
									bloxBlockDef.paramDefs[n].type = bloxBlockDef.paramDefs[n].type.GetElementType();
									bloxBlockDef.paramDefs[n].isRefType = true;
									bloxBlockDef.paramDefs[n].emptyVal = "-variable-required-";
								}
								if (string.IsNullOrEmpty(bloxBlockAttribute.ParamNames[n]))
								{
									bloxBlockDef.paramDefs[n].showName = false;
								}
								else if (bloxBlockAttribute.ParamNames[n][0] == '!')
								{
									bloxBlockDef.paramDefs[n].name = ((bloxBlockAttribute.ParamNames[n].Length > 1) ? bloxBlockDef.paramDefs[n].name.Substring(1) : "");
									bloxBlockDef.paramDefs[n].showName = false;
								}
							}
						}
					}
					this.blockDefs.Add(bloxBlockDef.ident, bloxBlockDef);
				}
				count2++;
				if (count2 >= 50)
				{
					count2 = 0;
					yield return (object)null;
				}
			}
			yield return (object)null;
			List<string> lines = plyEdUtil.ReadCompressedLines(plyEdUtil.ProjectFullPath + BloxEdGlobal.MiscPath + "blocks.bin");
			if (lines.Count > 0)
			{
				yield return (object)null;
				count2 = 0;
				List<int> removeLines = new List<int>();
				for (int j = 0; j < lines.Count; j++)
				{
					string text2 = lines[j];
					if (!string.IsNullOrEmpty(text2))
					{
						BloxMemberInfo bloxMemberInfo = BloxMemberInfo.DecodeMember(text2);
						if (bloxMemberInfo != null)
						{
							BloxBlockDef bloxBlockDef2 = BloxEd.CreateMemberBlock(bloxMemberInfo, usedValueTypes);
							if (bloxBlockDef2 != null && !this.blockDefs.ContainsKey(bloxBlockDef2.ident))
							{
								this.blockDefs.Add(bloxBlockDef2.ident, bloxBlockDef2);
							}
						}
						else
						{
							removeLines.Add(j);
						}
						count2++;
						if (count2 >= 50)
						{
							count2 = 0;
							yield return (object)null;
						}
					}
				}
				if (removeLines.Count > 0)
				{
					Debug.LogWarning("Missing entries removed from Block Definitions since they are no longer present in any assembly.");
					for (int num = lines.Count - 1; num >= 0; num--)
					{
						if (removeLines.Contains(num))
						{
							lines.RemoveAt(num);
						}
					}
					plyEdUtil.WriteCompressedLines(plyEdUtil.ProjectFullPath + BloxEdGlobal.MiscPath + "blocks.bin", lines);
				}
			}
			count2 = 0;
			List<Type>.Enumerator enumerator = usedValueTypes.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					BloxBlockDef bloxBlockDef3 = BloxEd.CreateValueBlock(enumerator.Current);
					if (bloxBlockDef3 != null && !this.blockDefs.ContainsKey(bloxBlockDef3.ident))
					{
						this.blockDefs.Add(bloxBlockDef3.ident, bloxBlockDef3);
					}
					count2++;
					if (count2 >= 50)
					{
						count2 = 0;
						yield return (object)null;
					}
				}
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			enumerator = default(List<Type>.Enumerator);
			this.BlockDefsLoading = false;
		}

		private static void IncludeThisValueType(Type t, List<Type> usedValueTypes)
		{
			if (t != null && t != typeof(void) && !usedValueTypes.Contains(t))
			{
				Type type = t;
				if (type.IsArray)
				{
					type = type.GetElementType();
				}
				else if (type.IsGenericType)
				{
					if (type.GetGenericTypeDefinition() != typeof(List<>))
						return;
					type = type.GetGenericArguments()[0];
				}
				if (!type.IsValueType && type != typeof(string) && !typeof(UnityEngine.Object).IsAssignableFrom(type))
					return;
				if (!type.IsEnum && !BloxPropsPanel.SupportedEditTypes.Contains(type))
					return;
				usedValueTypes.Add(t);
			}
		}

		public static BloxBlockDef CreateValueBlock(Type t)
		{
			BloxBlockDef bloxBlockDef = new BloxBlockDef();
			bloxBlockDef.blockType = BloxBlockType.Value;
			bloxBlockDef.order = 0;
			bloxBlockDef.returnType = t;
			bloxBlockDef.returnName = BloxEd.PrettyTypeName(t, true);
			bloxBlockDef.blockSystemType = typeof(BloxBlock);
			bloxBlockDef.att = null;
			bloxBlockDef.ident = BloxEd.GetValueBlockIdent(t);
			if (bloxBlockDef.ident == null)
			{
				return null;
			}
			return bloxBlockDef;
		}

		public static BloxBlockDef CreateMemberBlock(BloxMemberInfo mi, List<Type> usedValueTypes)
		{
			string name = mi.Name;
			int num = 0;
			ParameterInfo[] array = null;
			BloxBlockType blockType = BloxBlockType.Action;
			if (mi.MemberType == MemberTypes.Constructor)
			{
				num = 1;
				array = mi.GetParameters();
				name = "new " + mi.ReflectedType.Name + BloxEd.ParametersNameSection(array);
				goto IL_011c;
			}
			if (mi.MemberType == MemberTypes.Method)
			{
				num = 2;
				array = mi.GetParameters();
				name += BloxEd.ParametersNameSection(array);
				if (mi.ReturnType != null && mi.ReturnType != typeof(void))
				{
					name = name + ": " + BloxEd.PrettyTypeName(mi.ReturnType, true);
					BloxEd.IncludeThisValueType(mi.ReturnType, usedValueTypes);
				}
				goto IL_011c;
			}
			if (mi.MemberType == MemberTypes.Field)
			{
				name = name + ": " + BloxEd.PrettyTypeName(mi.ReturnType, true);
				BloxEd.IncludeThisValueType(mi.ReturnType, usedValueTypes);
				if (!mi.CanSetValue)
				{
					blockType = BloxBlockType.Value;
				}
				goto IL_011c;
			}
			if (mi.MemberType == MemberTypes.Property)
			{
				name = name + ": " + BloxEd.PrettyTypeName(mi.ReturnType, true);
				BloxEd.IncludeThisValueType(mi.ReturnType, usedValueTypes);
				if (!mi.CanSetValue)
				{
					blockType = BloxBlockType.Value;
				}
				goto IL_011c;
			}
			blockType = BloxBlockType.Unknown;
			return null;
			IL_011c:
			bool showIconInCanvas = true;
			Texture2D texture2D;
			if (mi.DeclaringType != mi.ReflectedType && mi.MemberType != MemberTypes.Field && mi.MemberType != MemberTypes.Property)
			{
				num += 10;
				showIconInCanvas = false;
				texture2D = BloxEdGUI.Instance.bloxFadedIcon;
			}
			else
			{
				texture2D = AssetPreview.GetMiniTypeThumbnail(mi.ReflectedType);
				if ((UnityEngine.Object)texture2D == (UnityEngine.Object)null || texture2D.name == "DefaultAsset Icon")
				{
					texture2D = ((!mi.ReflectedType.FullName.StartsWith("UnityEngine")) ? BloxEdGUI.Instance.bloxIcon : BloxEdGUI.Instance.unityIcon);
					showIconInCanvas = false;
				}
			}
			BloxBlockDef bloxBlockDef = new BloxBlockDef();
			bloxBlockDef.blockType = blockType;
			bloxBlockDef.order = 10000 + num;
			bloxBlockDef.name = name;
			bloxBlockDef.shortName = BloxEd.PrettyMemberName(mi);
			bloxBlockDef.icon = texture2D;
			bloxBlockDef.showIconInCanvas = showIconInCanvas;
			bloxBlockDef.ident = mi.ReflectedType.FullName.Replace('.', '/') + "/" + name;
			bloxBlockDef.ident = bloxBlockDef.ident.Replace('+', '.');
			if (string.IsNullOrEmpty(mi.ReflectedType.Namespace))
			{
				bloxBlockDef.ident = "Scripts/" + bloxBlockDef.ident;
			}
			bloxBlockDef.returnType = mi.ReturnType;
			bloxBlockDef.returnName = BloxEd.PrettyTypeName(bloxBlockDef.returnType, true);
			bloxBlockDef.contextType = ((mi.IsStatic || mi.MemberType == MemberTypes.Constructor) ? null : mi.ReflectedType);
			bloxBlockDef.contextName = BloxEd.PrettyTypeName(bloxBlockDef.contextType, false);
			bloxBlockDef.mi = mi;
			bloxBlockDef.blockSystemType = typeof(BloxBlock);
			bloxBlockDef.att = null;
			if (array != null)
			{
				bloxBlockDef.paramDefs = new BloxBlockDef.Param[array.Length];
				for (int i = 0; i < array.Length; i++)
				{
					Type type;
					Type type2 = type = array[i].ParameterType;
					if (type.IsByRef)
					{
						type = type.GetElementType();
					}
					bloxBlockDef.paramDefs[i] = new BloxBlockDef.Param
					{
						showName = true,
						name = array[i].Name,
						type = type
					};
					if (type2.IsByRef)
					{
						bloxBlockDef.paramDefs[i].isRefType = true;
						bloxBlockDef.paramDefs[i].emptyVal = "-variable-required-";
					}
					else if (type2 == typeof(string))
					{
						bloxBlockDef.paramDefs[i].emptyVal = "-empty-";
					}
					BloxEd.IncludeThisValueType(type, usedValueTypes);
				}
			}
			else if (mi.CanSetValue)
			{
				BloxEd.IncludeThisValueType(mi.ReturnType, usedValueTypes);
				bloxBlockDef.paramDefs = new BloxBlockDef.Param[1]
				{
					new BloxBlockDef.Param
					{
						name = mi.Name,
						showName = false,
						type = mi.ReturnType
					}
				};
				if (mi.ReturnType == typeof(string))
				{
					bloxBlockDef.paramDefs[0].emptyVal = "-empty-";
				}
			}
			return bloxBlockDef;
		}

		public static string PrettyTypeName(Type t, bool isReturn)
		{
			if (t != null && t != typeof(void))
			{
				if (t.IsByRef)
				{
					t = t.GetElementType();
				}
				string text = t.FullName;
				try
				{
					if (t.IsArray)
					{
						t = t.GetElementType();
						text = t.FullName;
						if (text != null)
						{
							if (t == typeof(int))
							{
								text = "Integer";
							}
							else if (t == typeof(float))
							{
								text = "Float";
							}
							else if (t == typeof(UnityEngine.Object))
							{
								text = "UnityObject";
							}
							else if (t == typeof(object))
							{
								text = (isReturn ? "SystemObject" : "Any");
							}
							else if (text.Contains("."))
							{
								text = text.Substring(text.LastIndexOf('.') + 1);
							}
						}
						text = "Array<" + text + ">";
					}
					else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
					{
						t = t.GetGenericArguments()[0];
						text = t.FullName;
						if (text != null)
						{
							if (t == typeof(int))
							{
								text = "Integer";
							}
							else if (t == typeof(float))
							{
								text = "Float";
							}
							else if (t == typeof(UnityEngine.Object))
							{
								text = "UnityObject";
							}
							else if (t == typeof(object))
							{
								text = (isReturn ? "SystemObject" : "Any");
							}
							else if (text.Contains("."))
							{
								text = text.Substring(text.LastIndexOf('.') + 1);
							}
						}
						text = "List<" + text + ">";
					}
					else
					{
						if (t.IsByRef)
						{
							t = t.GetElementType();
						}
						if (t == typeof(int))
						{
							return "Integer";
						}
						if (t == typeof(float))
						{
							return "Float";
						}
						if (t == typeof(UnityEngine.Object))
						{
							return "UnityObject";
						}
						if (t == typeof(object))
						{
							return isReturn ? "SystemObject" : "Any";
						}
						if (text.Contains("."))
						{
							text = text.Substring(text.LastIndexOf('.') + 1);
						}
					}
				}
				catch (Exception exception)
				{
					Debug.LogError("Error with type: t=[" + t + "] asm=[" + t.AssemblyQualifiedName + "] ns=[" + t.Namespace + "] nm=[" + t.AssemblyQualifiedName + "]");
					Debug.LogException(exception);
				}
				return text.Replace('+', '.');
			}
			return "None";
		}

		public static string PrettyMemberName(BloxMemberInfo mi)
		{
			string text = "";
			if (mi.MemberType == MemberTypes.Constructor)
			{
				text = mi.ReflectedType.FullName;
				if (text.Contains("."))
				{
					text = text.Substring(text.LastIndexOf('.') + 1);
				}
				return "new " + text.Replace('+', '.');
			}
			if (mi.IsStatic)
			{
				text = mi.DeclaringType.FullName;
			}
			if (text.Length > 0)
			{
				if (text.Contains("."))
				{
					text = text.Substring(text.LastIndexOf('.') + 1);
				}
				text = text.Replace('+', '.');
				return text + "." + mi.Name;
			}
			return mi.Name;
		}

		public static string ParametersNameSection(ParameterInfo[] pars)
		{
			string str = "(";
			for (int i = 0; i < pars.Length; i++)
			{
				Type parameterType = pars[i].ParameterType;
				str = ((!parameterType.IsByRef) ? (str + BloxEd.PrettyTypeName(parameterType, false)) : (str + "Variable<" + BloxEd.PrettyTypeName(parameterType, false) + ">"));
				str = str + " " + pars[i].Name;
				if (i < pars.Length - 1)
				{
					str += ", ";
				}
			}
			return str + ")";
		}

		public static GUIStyle[] GetBlockStyle(BloxBlockType bt, string customStyleName)
		{
			switch (bt)
			{
			case BloxBlockType.Value:
				return BloxEdGUI.Styles.Value;
			case BloxBlockType.Container:
				return BloxEdGUI.Styles.Container;
			default:
				if (!string.IsNullOrEmpty(customStyleName))
				{
					GUIStyle[] result = null;
					if (BloxEdGUI.Styles.Action.TryGetValue(customStyleName, out result))
					{
						return result;
					}
				}
				return BloxEdGUI.Styles.Action["default"];
			}
		}

		private static string GetValueBlockIdent(Type t)
		{
			if (t.IsArray)
			{
				Type elementType = t.GetElementType();
				return "Values/Array/Array <" + BloxEd.PrettyTypeName(elementType, false) + ">";
			}
			if (t.IsGenericType)
			{
				if (t.GetGenericTypeDefinition() == typeof(List<>))
				{
					Type t2 = t.GetGenericArguments()[0];
					return "Values/List/List <" + BloxEd.PrettyTypeName(t2, false) + ">";
				}
				return null;
			}
			if (t == typeof(bool))
			{
				return "Values/Boolean";
			}
			if (t == typeof(int))
			{
				return "Values/Integer";
			}
			if (t == typeof(float))
			{
				return "Values/Float";
			}
			if (t == typeof(string))
			{
				return "Values/String";
			}
			if (t == typeof(Vector2))
			{
				return "Values/Vector2";
			}
			if (t == typeof(Vector3))
			{
				return "Values/Vector3";
			}
			if (t == typeof(Rect))
			{
				return "Values/Rect";
			}
			if (t == typeof(Color))
			{
				return "Values/Color";
			}
			if (t == typeof(Component))
			{
				return "Values/UnityEngine/Component";
			}
			if (t == typeof(GameObject))
			{
				return "Values/UnityEngine/GameObject";
			}
			if (t == typeof(UnityEngine.Object))
			{
				return "Values/UnityEngine/UnityObject";
			}
			if (t == typeof(BloxContainer))
			{
				return "Values/Blox";
			}
			if (string.IsNullOrEmpty(t.Namespace))
			{
				return "Values/Scripts/" + t.FullName.Replace('.', '/').Replace("+", ".");
			}
			return "Values/" + t.FullName.Replace('.', '/').Replace("+", ".");
		}

		public static void UpdateLinkFile()
		{
			int num = EditorUtility.DisplayDialogComplex("Update Link File", "Should existing Link file be deleted before updating?\n\nA link file is needed if you enable stripping in a build.", "Yes", "No", "Cancel");
			if (num != 2)
			{
				bool delOldFile = num == 0;
				float progress = 0f;
				float num2 = 1f;
				EditorUtility.DisplayProgressBar("Please wait", "Updating Link File ...", progress);
				List<Type> list = new List<Type>();
				progress = 0f;
				num2 = (float)(1.0 / (float)BloxEd.BloxGlobalObj.bloxDefs.Count);
				for (int i = 0; i < BloxEd.BloxGlobalObj.bloxDefs.Count; i++)
				{
					Blox blox = BloxEd.BloxGlobalObj.bloxDefs[i];
					progress += num2;
					EditorUtility.DisplayProgressBar("Please wait", "Updating Link File ...", progress);
					BloxEvent[] events = blox.events;
					for (int j = 0; j < events.Length; j++)
					{
						BloxEd.GetUsedTypes(events[j].firstBlock, list);
					}
				}
				list.Sort((Type a, Type b) => a.Assembly.ToString().CompareTo(b.Assembly.ToString()));
				plyEdUtil.UpdateLinkFileData(delOldFile, list);
				EditorUtility.ClearProgressBar();
				AssetDatabase.Refresh();
			}
		}

		private static void GetUsedTypes(BloxBlock b, List<Type> res)
		{
			if (b != null)
			{
				if (!res.Contains(b.GetType()))
				{
					res.Add(b.GetType());
				}
				if (b.mi != null && b.mi.IsValid && !res.Contains(b.mi.ReflectedType))
				{
					res.Add(b.mi.ReflectedType);
				}
				if (b.paramBlocks != null)
				{
					for (int i = 0; i < b.paramBlocks.Length; i++)
					{
						BloxEd.GetUsedTypes(b.paramBlocks[i], res);
					}
				}
				BloxEd.GetUsedTypes(b.contextBlock, res);
				BloxEd.GetUsedTypes(b.firstChild, res);
				BloxEd.GetUsedTypes(b.next, res);
			}
		}

		public void RestoreDefaultNamespaces()
		{
			bool flag = false;
			foreach (string defaultNamespace in BloxEd.defaultNamespaces)
			{
				bool flag2 = false;
				int num = 0;
				while (num < this.namespaces.Count)
				{
					if (!(this.namespaces[num] == defaultNamespace))
					{
						num++;
						continue;
					}
					flag2 = true;
					break;
				}
				if (!flag2)
				{
					this.namespaces.Add(defaultNamespace);
					flag = true;
				}
			}
			if (flag)
			{
				EditorUtility.SetDirty(this);
			}
		}

		public void RestoreDefaultDocSources()
		{
			this.docsources.Clear();
			this.docsources.AddRange(BloxEd.defaultDocSources);
			EditorUtility.SetDirty(this);
		}

		public void ReloadBloxDoc()
		{
			this.loadDocs = 1;
		}

		public void DrawBloxDoc(BloxEdDef def, bool fullDoc, EditorWindow ed)
		{
			if (def != null && def.ident != null)
			{
				if (def != this.currLookupDef && def.bloxdoc == null)
				{
					if (this.docLookupList.Contains(def))
					{
						this.docLookupList.Remove(def);
					}
					this.docLookupList.Add(def);
					this.FindBloxDoc();
				}
				if (def.bloxdoc == null)
				{
					plyEdGUI.DrawSpinner(BloxEd.GC_Loading, false, false);
					ed.Repaint();
				}
				else
				{
					if (fullDoc)
					{
						if (!def.bloxdoc.inited)
						{
							def.bloxdoc.inited = true;
							BloxBlockDef bloxBlockDef = def as BloxBlockDef;
							if (bloxBlockDef != null)
							{
								if (bloxBlockDef.returnType != null && bloxBlockDef.returnType != typeof(void))
								{
									def.bloxdoc.returns = bloxBlockDef.returnName;
									if (bloxBlockDef.mi != null && bloxBlockDef.mi.CanSetValue && bloxBlockDef.mi.MemberType != MemberTypes.Method && bloxBlockDef.mi.MemberType != MemberTypes.Constructor)
									{
										def.bloxdoc.getonly = false;
									}
								}
								if (bloxBlockDef.contextType != null && bloxBlockDef.contextType != typeof(void))
								{
									def.bloxdoc.context = bloxBlockDef.contextName;
								}
							}
						}
						if (!string.IsNullOrEmpty(def.bloxdoc.context))
						{
							GUILayout.Label("Context: " + def.bloxdoc.context, BloxEdGUI.Styles.DocReturn);
						}
						if (!string.IsNullOrEmpty(def.bloxdoc.returns))
						{
							if (def.bloxdoc.getonly)
							{
								GUILayout.Label("Returns: " + def.bloxdoc.returns, BloxEdGUI.Styles.DocReturn);
							}
							else
							{
								GUILayout.Label("Get/Set: " + def.bloxdoc.returns, BloxEdGUI.Styles.DocReturn);
							}
						}
						for (int i = 0; i < def.bloxdoc.parameters.Length; i++)
						{
							GUILayout.Label("<b>" + def.bloxdoc.parameters[i].name + " (" + def.bloxdoc.parameters[i].type + ")</b> " + def.bloxdoc.parameters[i].doc, BloxEdGUI.Styles.DocParam);
						}
					}
					GUILayout.Label(def.bloxdoc.doc, plyEdGUI.Styles.WordWrappedLabel_RT);
				}
				if (def.bloxdoc == null && Event.current.type == EventType.Repaint)
				{
					this.DoUpdate();
				}
			}
		}

		public static BloxDocEntry CreateEmptyDoc(BloxEdDef def)
		{
			BloxDocEntry bloxDocEntry = new BloxDocEntry();
			bloxDocEntry.ident = def.ident;
			bloxDocEntry.url = "";
			bloxDocEntry.doc = "";
			bloxDocEntry.inited = true;
			bloxDocEntry.getonly = true;
			bloxDocEntry.returns = null;
			bloxDocEntry.context = null;
			BloxBlockDef bloxBlockDef = def as BloxBlockDef;
			if (bloxBlockDef != null)
			{
				if (bloxBlockDef.mi != null && bloxBlockDef.mi.ReflectedType.Namespace != null && bloxBlockDef.mi.ReflectedType.Namespace.StartsWith("System"))
				{
					bloxDocEntry.url = "http://msdn.microsoft.com/en-us/library/" + bloxBlockDef.mi.ReflectedType.ToString().ToLower() + "." + bloxBlockDef.mi.Name.ToLower();
				}
				if (bloxBlockDef.mi != null && bloxBlockDef.mi.CanSetValue && bloxBlockDef.mi.MemberType != MemberTypes.Method && bloxBlockDef.mi.MemberType != MemberTypes.Constructor)
				{
					bloxDocEntry.getonly = false;
				}
				if (bloxBlockDef.returnType != null && bloxBlockDef.returnType != typeof(void))
				{
					bloxDocEntry.returns = bloxBlockDef.returnName;
				}
				if (bloxBlockDef.contextType != null && bloxBlockDef.contextType != typeof(void))
				{
					bloxDocEntry.context = bloxBlockDef.contextName;
				}
				List<BloxDocEntry.Parameter> list = new List<BloxDocEntry.Parameter>();
				bloxDocEntry.parameters = new BloxDocEntry.Parameter[bloxBlockDef.paramDefs.Length];
				for (int i = 0; i < bloxBlockDef.paramDefs.Length; i++)
				{
					if (bloxBlockDef.paramDefs[i].type != null)
					{
						BloxDocEntry.Parameter parameter = new BloxDocEntry.Parameter
						{
							name = bloxBlockDef.paramDefs[i].name,
							type = BloxEd.PrettyTypeName(bloxBlockDef.paramDefs[i].type, false),
							doc = ""
						};
						if (bloxBlockDef.paramDefs[i].isRefType)
						{
							parameter.type = "Variable<" + parameter.type + ">";
						}
						list.Add(parameter);
					}
				}
				bloxDocEntry.parameters = list.ToArray();
			}
			return bloxDocEntry;
		}

		private void FindBloxDoc()
		{
			if (this.docfinder == null)
			{
				this.docfinder = plyEdCoroutine.Start(this.DocFinder(), true);
			}
			this.docfinder.UnPause();
		}

		public static void StopBloxDocFinder(bool pauseOnly = false)
		{
			if ((UnityEngine.Object)BloxEd._instance != (UnityEngine.Object)null && BloxEd._instance.docfinder != null)
			{
				if (pauseOnly)
				{
					BloxEd._instance.docfinder.Pause();
				}
				else
				{
					BloxEd._instance.docfinder.Stop();
					BloxEd._instance.docfinder = null;
				}
			}
		}

		private void DoReloadBloxDoc()
		{
			this.loadDocs = 2;
			plyEdCoroutine obj = this.docloader;
			if (obj != null)
			{
				obj.Stop();
			}
			this.docloader = plyEdCoroutine.Start(this.DocsLoader(), true);
		}

		private IEnumerator DocFinder()
		{
			while (true)
			{
				IL_0026:
				this.findDoc_step = 2;
				if (this.docLookupList.Count > 0)
				{
					this.currLookupDef = this.docLookupList[this.docLookupList.Count - 1];
					this.docLookupList.RemoveAt(this.docLookupList.Count - 1);
				}
				if (this.loadDocs == 1 || this.docs == null || this.docs.entries.Count == 0)
				{
					this.DoReloadBloxDoc();
					while (this.loadDocs != 3)
					{
						yield return (object)null;
					}
				}
				if (this.findDoc_step == 2 && this.currLookupDef != null)
				{
					int count = 0;
					for (int i = 0; i < this.docs.entries.Count; i++)
					{
						if (this.findDoc_step == 1)
							goto IL_0026;
						if (this.currLookupDef == null)
							break;
						if (this.currLookupDef.identHash == this.docs.entries[i].identHash && this.currLookupDef.ident == this.docs.entries[i].ident)
						{
							this.currLookupDef.bloxdoc = this.docs.entries[i];
							break;
						}
						count++;
						if (count >= 50)
						{
							count = 0;
							yield return (object)null;
						}
					}
				}
				if (this.currLookupDef != null && this.currLookupDef.bloxdoc == null)
				{
					this.currLookupDef.bloxdoc = BloxEd.CreateEmptyDoc(this.currLookupDef);
				}
				this.currLookupDef = null;
				this.findDoc_step = 0;
				if (this.docLookupList.Count == 0)
				{
					this.docfinder.Pause();
				}
				while (true)
				{
					if (this.docLookupList.Count > 0)
						break;
					yield return (object)null;
				}
			}
		}

		private IEnumerator DocsLoader()
		{
			this.docs = new BloxDocEntries();
			List<string>.Enumerator enumerator = this.docsources.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					string current = enumerator.Current;
					string text = plyEdUtil.ProjectFullPath + current;
					if (File.Exists(text))
					{
						string text2 = "";
						text2 = ((!text.EndsWith(".bin")) ? File.ReadAllText(text) : plyEdUtil.ReadCompressedString(text));
						text2 = text2.Trim();
						if (text2.Length != 0)
						{
							BloxDocEntries bd;
							try
							{
								bd = JsonUtility.FromJson<BloxDocEntries>(text2);
							}
							catch (Exception ex)
							{
								Debug.LogErrorFormat("Error: {0} While loading: {1}\n", ex.Message, text);
								continue;
							}
							if (bd != null && bd.entries != null && bd.entries.Count > 0)
							{
								int count = 0;
								for (int bdIdx = 0; bdIdx < bd.entries.Count; bdIdx++)
								{
									count++;
									if (count >= 50)
									{
										count = 0;
										yield return (object)null;
									}
									this.docs.entries.Add(bd.entries[bdIdx]);
									if (this.findDoc_step == 2 && this.currLookupDef != null && this.currLookupDef.identHash == bd.entries[bdIdx].identHash && this.currLookupDef.ident == bd.entries[bdIdx].ident)
									{
										this.currLookupDef.bloxdoc = bd.entries[bdIdx];
										this.currLookupDef = null;
										this.findDoc_step = 0;
									}
								}
							}
							goto IL_0286;
						}
						continue;
					}
					goto IL_0286;
					IL_0286:;
				}
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			enumerator = default(List<string>.Enumerator);
			this.loadDocs = 3;
			this.docloader = null;
		}
	}
}
