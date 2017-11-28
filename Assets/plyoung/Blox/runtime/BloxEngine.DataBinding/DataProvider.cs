using plyLib;
using System.Collections.Generic;
using UnityEngine;

namespace BloxEngine.DataBinding
{
	[AddComponentMenu("")]
	[HelpURL("http://www.plyoung.com/blox/databinding.html")]
	public class DataProvider : MonoBehaviour, ISerializationCallbackReceiver
	{
		public plyManagedComponent _edManaged;

		public MonoBehaviour _edOwner;

		private List<DataBinding> databinds = new List<DataBinding>();

		private IOwnDataProviders owner;

		private bool valuesChanged = true;

		private bool someBindsCantCallback;

		public object Value
		{
			get
			{
				return null;
			}
		}

		public bool _edDeserialize
		{
			get;
			private set;
		}

		public void RegisterDataBinding(DataBinding bind)
		{
			this.valuesChanged = true;
			this.databinds.Add(bind);
			if (!bind.Initialize(this.BoundValueChanged))
			{
				this.someBindsCantCallback = true;
			}
		}

		public virtual void DoUpdate()
		{
		}

		protected bool UpdateValues()
		{
			bool result = this.valuesChanged;
			if (this.someBindsCantCallback)
			{
				for (int i = 0; i < this.databinds.Count; i++)
				{
					if (this.databinds[i].UpdateValue())
					{
						result = true;
					}
				}
			}
			else
			{
				this.valuesChanged = false;
			}
			return result;
		}

		private void BoundValueChanged()
		{
			this.valuesChanged = true;
		}

		public override string ToString()
		{
			if (this.Value != null)
			{
				return this.Value.ToString();
			}
			return "null";
		}

		public void OwnerCalledDeserialize(IOwnDataProviders owner)
		{
			this.owner = owner;
			this.Deserialize();
		}

		public virtual void Deserialize()
		{
			this._edDeserialize = false;
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			this._edDeserialize = true;
			if (this.owner != null)
			{
				this.owner._edDeserialize = true;
			}
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
		}
	}
}
