using plyLib;
using System;
using UnityEngine.Events;

namespace BloxEngine.DataBinding
{
	public class ComparisonCheck : DataProvider
	{
		public enum ComparisonOpt : byte
		{
			Equal = 0,
			NotEqual = 1,
			SmallerThan = 2,
			GreaterThan = 3,
			SmallerThanOrEqual = 4,
			GreaterThanOrEqual = 5
		}

		public ComparisonOpt comparisonOpt;

		public DataBinding param1 = new DataBinding();

		public DataBinding param2 = new DataBinding();

		public UnityEvent onConditionSuccess;

		public bool triggerOnceOnValueChange = true;

		private bool valueIsFresh = true;

		private bool value;

		public new bool Value
		{
			get
			{
				if (base.UpdateValues())
				{
					this.valueIsFresh = true;
					IComparable comparable = (IComparable)this.param1.Value;
					if (comparable == null)
					{
						this.value = false;
					}
					else
					{
						object obj = (this.param2.Value == null) ? null : Convert.ChangeType(this.param2.Value, comparable.GetType());
						int num = comparable.CompareTo(obj);
						switch (this.comparisonOpt)
						{
						case ComparisonOpt.Equal:
							this.value = (num == 0);
							break;
						case ComparisonOpt.NotEqual:
							this.value = (num != 0);
							break;
						case ComparisonOpt.SmallerThan:
							this.value = (num < 0);
							break;
						case ComparisonOpt.GreaterThan:
							this.value = (num > 0);
							break;
						case ComparisonOpt.SmallerThanOrEqual:
							this.value = (num == 0 || num < 0);
							break;
						case ComparisonOpt.GreaterThanOrEqual:
							this.value = (num == 0 || num > 0);
							break;
						}
					}
				}
				return this.value;
			}
		}

		protected void Awake()
		{
			this.Deserialize();
			base.RegisterDataBinding(this.param1);
			base.RegisterDataBinding(this.param2);
			if (base._edManaged == plyManagedComponent.None && this.onConditionSuccess != null)
			{
				DataProviderUpdater.Instance.RegisterProvider(this);
			}
		}

		protected void Destroy()
		{
			if (base._edManaged == plyManagedComponent.None && this.onConditionSuccess != null)
			{
				DataProviderUpdater.Instance.RemoveProvider(this);
			}
		}

		public override void DoUpdate()
		{
			if (this.Value)
			{
				if (this.triggerOnceOnValueChange && !this.valueIsFresh)
					return;
				this.valueIsFresh = false;
				this.onConditionSuccess.Invoke();
			}
		}

		public override void Deserialize()
		{
			base.Deserialize();
			this.param1.Deserialize();
			this.param2.Deserialize();
		}
	}
}
