using System;
using UnityEngine;

namespace BloxEngine.Variables
{
	[Serializable]
	public class plyVar
	{
		[SerializeField]
		public int ident;

		[SerializeField]
		public string name;

		[SerializeField]
		public UnityEngine.Object[] objRefs;

		[SerializeField]
		private string storedData;

		[SerializeField]
		private string storedType;

		private plyVarValueHandler valueHandler;

		public plyVarValueHandler ValueHandler
		{
			get
			{
				return this.valueHandler;
			}
		}

		public virtual Type variableType
		{
			get
			{
				return this.valueHandler.variableType;
			}
		}

		public object GetValue()
		{
			return this.valueHandler.GetValue(this);
		}

		public void SetValue(object v)
		{
			this.valueHandler.SetValue(this, v);
		}

		public plyVar Copy()
		{
			plyVar plyVar = new plyVar();
			this.CopyTo(plyVar);
			return plyVar;
		}

		public void CopyTo(plyVar v)
		{
			this.Serialize();
			v.ident = this.ident;
			v.name = this.name;
			v.objRefs = this.objRefs;
			v.storedData = this.storedData;
			v.storedType = this.storedType;
			v.Deserialize();
		}

		public override string ToString()
		{
			return this.name;
		}

		public static plyVar Create(Type t)
		{
			return new plyVar
			{
				valueHandler = (plyVarValueHandler)Activator.CreateInstance(t),
				storedType = t.AssemblyQualifiedName
			};
		}

		public void ReleaseStoredData()
		{
			this.valueHandler.ReleaseStoredData();
			this.storedData = null;
			this.storedType = null;
		}

		public void Serialize()
		{
			if (this.valueHandler != null)
			{
				this.storedData = this.valueHandler.EncodeValues(this);
			}
		}

		public void Deserialize()
		{
			if (string.IsNullOrEmpty(this.storedType))
			{
				Debug.LogError("The variable's type is not set: " + this.name);
			}
			else
			{
				Type type = Type.GetType(this.storedType);
				if (type != null)
				{
					this.valueHandler = (plyVarValueHandler)Activator.CreateInstance(type);
					if (this.valueHandler != null)
					{
						this.valueHandler.DecodeValues(this, this.storedData);
					}
					else
					{
						Debug.LogError("Error encountered while trying to create plyVar: " + this.storedType);
					}
				}
				else
				{
					Debug.LogError("Error encountered while deserializing plyVar. Type not found: " + this.storedType);
				}
			}
		}
	}
}
