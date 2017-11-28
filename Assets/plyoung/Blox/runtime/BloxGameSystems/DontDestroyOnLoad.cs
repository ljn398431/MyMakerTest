using BloxEngine;
using UnityEngine;

namespace BloxGameSystems
{
	[ExcludeFromBlox]
	[AddComponentMenu("Blox/Helpers/Don't Destroy On Load")]
	public class DontDestroyOnLoad : BasicComponent
	{
		protected void Awake()
		{
			Object.DontDestroyOnLoad(base.gameObject);
			Object.Destroy(this);
		}
	}
}
