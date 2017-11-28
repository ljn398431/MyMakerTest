using BloxEngine.Variables;
using System;
using UnityEngine;

namespace BloxEngine
{
	[Serializable]
	public class Blox : ScriptableObject, ISerializationCallbackReceiver
	{
		public string ident;

		public string screenName;

		public BloxEvent[] events = new BloxEvent[0];

		public plyVariables variables;

		public bool scriptDirty = true;

		[NonSerialized]
		public bool bloxLoaded;

		[NonSerialized]
		public Type scriptType;

		private bool _isDirty;

		protected void OnEnable()
		{
			this.bloxLoaded = false;
            Debug.Log("Blox Enable");
			try
			{
				this.scriptType = Types.GetType("BloxGenerated." + this.ident, "Assembly-CSharp, Version=0.0.0.0, Culture=neutral");
			}
			catch (Exception)
			{
			}
			if (this.scriptDirty)
			{
				this.Deserialize();
			}
			else
			{
				this.variables.BuildCache();
			}
		}

		public void CopyTo(Blox def)
		{
			def.scriptDirty = true;
			def.variables = this.variables.Copy();
			def.events = new BloxEvent[this.events.Length];
			for (int i = 0; i < this.events.Length; i++)
			{
				def.events[i] = this.events[i].Copy();
			}
		}

		public override string ToString()
		{
			return this.screenName;
		}

		public void _SetDirty()
		{
			if (!Application.isPlaying)
			{
				this._isDirty = true;
			}
		}

		public void Deserialize()
		{
			this.bloxLoaded = true;
			plyVariables obj = this.variables;
			if (obj != null)
			{
				obj.BuildCache();
			}
			for (int i = 0; i < this.events.Length; i++)
			{
				this.events[i].Deserialize(this);
			}
		}

		public void Serialize()
		{
			this.variables.Serialize();
			if (this._isDirty)
			{
				this._isDirty = false;
				for (int i = 0; i < this.events.Length; i++)
				{
					this.events[i].Serialize();
				}
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
            Debug.Log("Blox 反序列化");
            this.variables.Deserialize(false);
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
            Debug.Log("Blox 序列化");
			this.Serialize();
		}
	}
}
