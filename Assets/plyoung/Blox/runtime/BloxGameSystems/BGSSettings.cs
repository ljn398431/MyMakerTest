using BloxEngine;
using UnityEngine;

namespace BloxGameSystems
{
	[ExcludeFromBlox]
	public class BGSSettings : ScriptableObject
	{
		[HideInInspector]
		public bool autoLoadBootstrapOnUnityPlay = true;
	}
}
