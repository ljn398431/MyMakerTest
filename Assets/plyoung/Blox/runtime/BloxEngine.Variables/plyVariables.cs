using System;
using System.Collections.Generic;
using UnityEngine;

namespace BloxEngine.Variables
{
	[Serializable]
	public class plyVariables
	{
		[SerializeField]
		public List<plyVar> varDefs = new List<plyVar>();

		[SerializeField]
		private int lastVarIdent;

		[NonSerialized]
		private Dictionary<string, plyVar> cache;

		[NonSerialized]
		private bool canReleaseStoredData = true;

		public static Dictionary<Type, Type> ValueTypeHandlers = new Dictionary<Type, Type>
		{
			{
				typeof(object[]),
				typeof(plyVar_Array)
			},
			{
				typeof(Array),
				typeof(plyVar_Array)
			},
			{
				typeof(List<object>),
				typeof(plyVar_List)
			},
			{
				typeof(bool),
				typeof(plyVar_Bool)
			},
			{
				typeof(int),
				typeof(plyVar_Int)
			},
			{
				typeof(float),
				typeof(plyVar_Float)
			},
			{
				typeof(string),
				typeof(plyVar_String)
			},
			{
				typeof(Color),
				typeof(plyVar_Color)
			},
			{
				typeof(Color32),
				typeof(plyVar_Color32)
			},
			{
				typeof(Rect),
				typeof(plyVar_Rect)
			},
			{
				typeof(Vector2),
				typeof(plyVar_Vector2)
			},
			{
				typeof(Vector3),
				typeof(plyVar_Vector3)
			},
			{
				typeof(Vector4),
				typeof(plyVar_Vector4)
			},
			{
				typeof(Quaternion),
				typeof(plyVar_Quaternion)
			},
			{
				typeof(GameObject),
				typeof(plyVar_GameObject)
			},
			{
				typeof(Component),
				typeof(plyVar_Component)
			},
			{
				typeof(UnityEngine.Object),
				typeof(plyVar_UnityObject)
			},
			{
				typeof(object),
				typeof(plyVar_SystemObject)
			}
		};

		[NonSerialized]
		public bool _isDirty;

		public plyVar FindVariable(string name)
		{
			this.BuildCache();
			plyVar result = null;
			if (this.cache.TryGetValue(name, out result))
			{
				return result;
			}
			return null;
		}

		public bool SetVarValue(string name, object value)
		{
			plyVar plyVar = this.FindVariable(name);
			if (plyVar != null)
			{
				plyVar.SetValue(value);
				return true;
			}
			return false;
		}

		public object GetVarValue(string name)
		{
			plyVar plyVar = this.FindVariable(name);
			if (plyVar == null)
			{
				return null;
			}
			return plyVar.GetValue();
		}

		public void Clear()
		{
			this.varDefs.Clear();
			Dictionary<string, plyVar> obj = this.cache;
			if (obj != null)
			{
				obj.Clear();
			}
		}

		public void AddWithValue(string name, object value)
		{
			this.BuildCache();
			plyVar plyVar = null;
			plyVar = ((value != null) ? this.AddOfValueType(name, value.GetType()) : this.AddOfplyVarType(name, typeof(plyVar_SystemObject)));
			if (plyVar != null)
			{
				plyVar.SetValue(value);
			}
		}

		public plyVar AddOfValueType(string name, Type t)
		{
			Type type = plyVariables.FindVariableTypeHandler(t);
			if (type == null)
			{
				Debug.LogError("There is no handler for Variable type: " + t);
				return null;
			}
			return this.AddOfplyVarType(name, type);
		}

		private plyVar AddOfplyVarType(string name, Type t)
		{
			plyVar plyVar = plyVar.Create(t);
			plyVar.name = name;
			this.varDefs.Add(plyVar);
			this.cache.Add(plyVar.name, plyVar);
			return plyVar;
		}

		public static Type FindVariableTypeHandler(Type t)
		{
			Type result = null;
			if (plyVariables.ValueTypeHandlers.TryGetValue(t, out result))
			{
				return result;
			}
			foreach (KeyValuePair<Type, Type> valueTypeHandler in plyVariables.ValueTypeHandlers)
			{
				if (valueTypeHandler.Key.IsAssignableFrom(t))
				{
					return valueTypeHandler.Value;
				}
			}
			return typeof(plyVar_SystemObject);
		}

		public void BuildCache()
		{
			if (this.cache == null && Application.isPlaying)
			{
				this.cache = new Dictionary<string, plyVar>();
				for (int i = 0; i < this.varDefs.Count; i++)
				{
					this.cache.Add(this.varDefs[i].name, this.varDefs[i]);
					if (!Application.isEditor && this.canReleaseStoredData)
					{
						this.varDefs[i].ReleaseStoredData();
					}
				}
			}
		}

		public plyVariables Copy()
		{
			plyVariables plyVariables = new plyVariables();
			this.CopyTo(plyVariables);
			return plyVariables;
		}

		public void CopyTo(plyVariables v)
		{
			if (Application.isPlaying)
			{
				Debug.LogError("This can't be done at runtime");
			}
			else
			{
				v.lastVarIdent = this.lastVarIdent;
				v.varDefs = new List<plyVar>();
				for (int i = 0; i < this.varDefs.Count; i++)
				{
					v.varDefs.Add(this.varDefs[i].Copy());
				}
			}
		}

		public int CreateVariableIdent()
		{
			this.lastVarIdent++;
			return this.lastVarIdent;
		}

		public void _SetDirty()
		{
			if (!Application.isPlaying)
			{
				this._isDirty = true;
			}
		}

		public void Deserialize(bool canReleaseStoredData)
		{
			this.canReleaseStoredData = canReleaseStoredData;
			for (int i = 0; i < this.varDefs.Count; i++)
			{
				this.varDefs[i].Deserialize();
			}
		}

		public void Serialize()
		{
			if (this._isDirty)
			{
				this._isDirty = false;
				for (int i = 0; i < this.varDefs.Count; i++)
				{
					this.varDefs[i].Serialize();
				}
			}
		}
	}
}
