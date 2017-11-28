using BloxEngine;
using UnityEngine;

namespace BloxGameSystems
{
	[ExcludeFromBlox]
	[AddComponentMenu("Blox/Helpers/Disable On Awake")]
	public class DisableOnAwake : BasicComponent
	{
		protected void Awake()
		{
			base.gameObject.SetActive(false);
			Object.Destroy(this);
		}
	}
}
