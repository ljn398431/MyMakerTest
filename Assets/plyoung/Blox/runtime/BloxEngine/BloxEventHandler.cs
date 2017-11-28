using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloxEngine
{
	[AddComponentMenu("")]
	[HelpURL("http://www.plyoung.com/blox/eventhandler.html")]
	public class BloxEventHandler : MonoBehaviour
	{
		[NonSerialized]
		public BloxContainer container;

		public virtual void AddEvent(BloxEvent ev)
		{
		}

		protected void RunEvents(List<BloxEvent> events, params BloxEventArg[] args)
		{
			if (events.Count != 0)
			{
				for (int i = 0; i < events.Count; i++)
				{
					this.RunEvent(events[i], args);
				}
			}
		}

		private void RunEvent(BloxEvent ev, params BloxEventArg[] args)
		{
			ev.GetReadyToRun(args);
			if (ev.canYield)
			{
				BloxGlobal.Instance.StartCoroutine(ev.RunYield(this.container));
			}
			else
			{
				ev.Run(this.container);
			}
		}
	}
}
