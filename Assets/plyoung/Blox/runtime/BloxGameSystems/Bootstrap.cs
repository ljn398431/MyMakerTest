using BloxEngine;
using BloxEngine.DataBinding;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace BloxGameSystems
{
	[ExcludeFromBlox]
	[DisallowMultipleComponent]
	[AddComponentMenu("")]
	[HelpURL("http://www.plyoung.com/blox/scenes.html")]
	public class Bootstrap : MonoBehaviour
	{
		[HideInInspector]
		public GameObject bloxGlobalPrefab;

		[HideInInspector]
		public List<int> startupScenes = new List<int>();

		[HideInInspector]
		public List<int> autoloadScenes = new List<int>();

		public UnityEvent onBootstrapDone;

		public static Property<bool> IsDone = new Property<bool>(false);

		private static bool StartedViaUnityPlayButton = false;

		private List<AsyncOperation> loadingScenes = new List<AsyncOperation>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void RunOnGameStart()
		{
			if (Application.isEditor && (Object)Object.FindObjectOfType<Bootstrap>() == (Object)null)
			{
				BGSSettings bGSSettings = Resources.Load<BGSSettings>("Blox/BGSSettings");
				if ((Object)bGSSettings != (Object)null && !bGSSettings.autoLoadBootstrapOnUnityPlay)
					return;
				Bootstrap.StartedViaUnityPlayButton = true;
				SceneManager.LoadScene("00-bootstrap", LoadSceneMode.Additive);
			}
		}

		protected void Awake()
		{
			Object.DontDestroyOnLoad(base.gameObject);
			base.gameObject.name = "Bootstrap";
			BloxGlobal.Create(this.bloxGlobalPrefab);
			if (!Bootstrap.StartedViaUnityPlayButton)
			{
				if (this.startupScenes.Count > 0 && this.startupScenes[0] > 0 && this.startupScenes[0] < SceneManager.sceneCountInBuildSettings)
				{
					SceneManager.LoadScene(this.startupScenes[0], LoadSceneMode.Single);
				}
				else if (SceneManager.sceneCountInBuildSettings > 1)
				{
					SceneManager.LoadScene(1, LoadSceneMode.Single);
				}
				else
				{
					Debug.LogError("There are no scenes for the Bootstrap to load. You need to add at least one game scene to the Build Settings List or via the Blox Game Systems Window.");
				}
			}
			else
			{
				for (int i = 0; i < this.autoloadScenes.Count; i++)
				{
					int num = this.autoloadScenes[i];
					if (num > 0 && num < SceneManager.sceneCountInBuildSettings)
					{
						SceneManager.LoadScene(num, LoadSceneMode.Additive);
					}
				}
			}
		}

		protected void Start()
		{
			if (!Bootstrap.StartedViaUnityPlayButton)
			{
				if (this.startupScenes.Count > 1)
				{
					for (int i = 1; i < this.startupScenes.Count; i++)
					{
						int num = this.startupScenes[i];
						if (num > 0 && num < SceneManager.sceneCountInBuildSettings)
						{
							AsyncOperation item = SceneManager.LoadSceneAsync(num, LoadSceneMode.Additive);
							this.loadingScenes.Add(item);
						}
					}
				}
				for (int j = 0; j < this.autoloadScenes.Count; j++)
				{
					int num2 = this.autoloadScenes[j];
					if (num2 > 0 && num2 < SceneManager.sceneCountInBuildSettings)
					{
						AsyncOperation item2 = SceneManager.LoadSceneAsync(num2, LoadSceneMode.Additive);
						this.loadingScenes.Add(item2);
					}
				}
			}
		}

		protected void LateUpdate()
		{
			if (this.loadingScenes.Count > 0)
			{
				bool flag = true;
				int num = 0;
				while (num < this.loadingScenes.Count)
				{
					if (this.loadingScenes[num].isDone)
					{
						num++;
						continue;
					}
					flag = false;
					break;
				}
				if (flag)
				{
					this.DoneLoading();
				}
			}
			else
			{
				this.DoneLoading();
			}
		}

		private void DoneLoading()
		{
			Bootstrap.IsDone.Value = true;
			if (this.onBootstrapDone != null)
			{
				this.onBootstrapDone.Invoke();
			}
			Object.Destroy(base.gameObject);
		}
	}
}
