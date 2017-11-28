using BloxEngine;
using BloxEngine.DataBinding;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BloxGameSystems
{
	[ExcludeFromBlox(ExceptForSpecifiedMembers = true)]
	[DisallowMultipleComponent]
	[AddComponentMenu("Blox/GUI/Splash Screens Manager")]
	[HelpURL("http://www.plyoung.com/blox/splash-screens-manager.html")]
	public class SplashScreensManager : MonoBehaviour, IOwnDataProviders
	{
		[Serializable]
		[ExcludeFromBlox]
		public class SplashScreen
		{
			public enum WaitType
			{
				Timeout = 0,
				WatchVariable = 1,
				WaitScreenEndTrigger = 2
			}

			public GameObject target;

			public WaitType waitType;

			public float timeout = 3f;

			public bool playerCanSkip = true;

			public ComparisonCheck watchVariable;
		}

		public List<SplashScreen> screens = new List<SplashScreen>();

		public UnityEvent onSpashScreensDone;

		private int idx = -1;

		private float timer;

		public static SplashScreensManager Instance
		{
			get;
			private set;
		}

		public bool _edDeserialize
		{
			get;
			set;
		}

		protected void Awake()
		{
			SplashScreensManager.Instance = this;
			for (int i = 0; i < this.screens.Count; i++)
			{
				if ((UnityEngine.Object)this.screens[i].target != (UnityEngine.Object)null)
				{
					this.screens[i].target.SetActive(false);
				}
			}
		}

		protected void Start()
		{
			this.ShowNextScreen();
		}

		private void ShowNextScreen()
		{
			if (this.idx >= 0)
			{
				this.screens[this.idx].target.SetActive(false);
			}
			bool flag = false;
			while (this.idx < this.screens.Count - 1)
			{
				this.idx++;
				if ((UnityEngine.Object)this.screens[this.idx].target != (UnityEngine.Object)null)
				{
					flag = true;
					this.screens[this.idx].target.SetActive(true);
					this.timer = this.screens[this.idx].timeout;
					break;
				}
			}
			if (!flag)
			{
				base.enabled = false;
				this.idx = -1;
				this.onSpashScreensDone.Invoke();
			}
		}

		protected void LateUpdate()
		{
			if (this.idx >= 0)
			{
				if (this.screens[this.idx].playerCanSkip && Input.anyKeyDown)
				{
					this.ShowNextScreen();
				}
				else if (this.screens[this.idx].waitType == SplashScreen.WaitType.Timeout)
				{
					this.timer -= Time.deltaTime;
					if (this.timer <= 0.0)
					{
						this.ShowNextScreen();
					}
				}
				else if (this.screens[this.idx].waitType == SplashScreen.WaitType.WatchVariable && this.screens[this.idx].watchVariable.Value)
				{
					this.ShowNextScreen();
				}
			}
		}

		[IncludeInBlox]
		public static void TriggerScreenEnd()
		{
			if ((UnityEngine.Object)SplashScreensManager.Instance != (UnityEngine.Object)null)
			{
				if (SplashScreensManager.Instance.idx >= 0 && SplashScreensManager.Instance.screens[SplashScreensManager.Instance.idx].waitType == SplashScreen.WaitType.WaitScreenEndTrigger)
				{
					SplashScreensManager.Instance.ShowNextScreen();
				}
				else
				{
					Debug.LogWarning("The SplashScreensManager could not end a screen since there is no screen waiting for a ScreenEndTrigger.");
				}
			}
			else
			{
				Debug.LogError("The SplashScreensManager was not active when TriggerScreenEnd was called.");
			}
		}
	}
}
