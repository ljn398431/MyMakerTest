using System;

namespace BloxEngine.DataBinding
{
	public class Property
	{
		protected object val;

		public object Value
		{
			get
			{
				return this.val;
			}
			set
			{
				if (!object.Equals(this.val, value))
				{
					this.val = value;
					this.ValueChanged();
				}
			}
		}

		public event Action<object> onValueChanged;

		public Property()
		{
		}

		public Property(object value)
		{
			this.Value = value;
		}

		protected void ValueChanged()
		{
			if (this.onValueChanged != null)
			{
				this.onValueChanged(this.val);
			}
		}
	}
	public class Property<T> : Property
	{
		public new T Value
		{
			get
			{
				object value = base.Value;
				if (value != null)
				{
					return (T)value;
				}
				return default(T);
			}
			set
			{
				base.Value = value;
			}
		}

		public Property()
		{
		}

		public Property(T value) : base(value)
		{
			this.Value = value;
		}
	}
}
