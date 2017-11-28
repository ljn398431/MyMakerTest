using BloxEngine.Variables;
using System;
using UnityEngine;

namespace BloxEngine.DataBinding
{
	[Serializable]
	public class DataBinding : ISerializationCallbackReceiver
	{
		public enum DataContext : byte
		{
			Invalid = 0,
			Constant = 1,
			GlobalProperty = 2
		}

		public DataContext dataContext;

		public string storedData;

		public plyVar constant;

		[NonSerialized]
		public Property property;

		[NonSerialized]
		public BloxMemberInfo member;

		private bool _isDirty;

		public object Value
		{
			get;
			private set;
		}

		private event Action onValueChanged;

		public DataBinding Copy()
		{
			DataBinding dataBinding = new DataBinding();
			this.CopyTo(dataBinding);
			return dataBinding;
		}

		public void CopyTo(DataBinding t)
		{
			t._SetDirty();
			t.dataContext = this.dataContext;
			t.constant = ((this.dataContext == DataContext.Constant) ? this.constant.Copy() : null);
			t.property = this.property;
			t.member = this.member;
		}

		public bool Initialize(Action onValueChangedCallback)
		{
			if (!Application.isEditor)
			{
				this.storedData = null;
			}
			if (this.dataContext == DataContext.Constant)
			{
				this.Value = this.constant.GetValue();
				if (!Application.isEditor)
				{
					this.constant = null;
				}
				return true;
			}
			if (this.dataContext == DataContext.GlobalProperty)
			{
				this.onValueChanged += onValueChangedCallback;
				this.property.onValueChanged += this.BoundValueChanged;
				return true;
			}
			return false;
		}

		public bool UpdateValue()
		{
			return false;
		}

		private void BoundValueChanged(object newValue)
		{
			this.Value = newValue;
			if (this.onValueChanged != null)
			{
				this.onValueChanged();
			}
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
			if (this.dataContext == DataContext.Constant)
			{
				this.constant.Deserialize();
			}
			else if (this.dataContext == DataContext.GlobalProperty)
			{
				this.member = BloxMemberInfo.DecodeMember(this.storedData);
				this.property = (this.member.GetValue(null) as Property);
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			if (this.dataContext == DataContext.Constant)
			{
				this.constant.Deserialize();
			}
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			if (this._isDirty)
			{
				this._isDirty = false;
				this.storedData = null;
				if (this.dataContext == DataContext.Constant)
				{
					this.constant.Serialize();
					this.member = null;
					this.property = null;
				}
				else if (this.dataContext == DataContext.GlobalProperty)
				{
					this.storedData = BloxMemberInfo.EncodeMember(this.member);
					this.constant = null;
				}
			}
		}
	}
}
