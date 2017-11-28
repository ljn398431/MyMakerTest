using UnityEngine;

namespace BloxEngine.Variables
{
	[DisallowMultipleComponent]
	[AddComponentMenu("")]
	[HelpURL("http://www.plyoung.com/blox/variables.html")]
	public class plyVariablesBehaviour : MonoBehaviour, ISerializationCallbackReceiver
	{
		public plyVariables variables;

		public plyVar FindVariable(string name)
		{
			return this.variables.FindVariable(name);
		}

		public void SetVarValue(string name, object value)
		{
			if (!this.variables.SetVarValue(name, value))
			{
				Debug.LogError("The variables does not exist: " + name, base.gameObject);
			}
		}

		public object GetVarValue(string name)
		{
			return this.variables.GetVarValue(name);
		}

		public GameObject GetGameObjectVarValue(string name)
		{
			plyVar plyVar = this.variables.FindVariable(name);
			if (plyVar == null)
			{
				return null;
			}
			plyVar_Component plyVar_Component = plyVar.ValueHandler as plyVar_Component;
			if (plyVar_Component != null)
			{
				return plyVar_Component.GetGameObject();
			}
			return (GameObject)plyVar.GetValue();
		}

		public T GetComponentVarValue<T>(string name) where T : Component
		{
			plyVar plyVar = this.variables.FindVariable(name);
			if (plyVar == null)
			{
				return null;
			}
			plyVar_Component plyVar_Component = plyVar.ValueHandler as plyVar_Component;
			if (plyVar_Component != null)
			{
				return plyVar_Component.GetComponent<T>();
			}
			plyVar_GameObject plyVar_GameObject = plyVar.ValueHandler as plyVar_GameObject;
			if (plyVar_GameObject != null)
			{
				return plyVar_GameObject.GetComponent<T>();
			}
			return (T)plyVar.GetValue();
		}

		protected virtual void Awake()
		{
			this.variables.BuildCache();
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			this.variables.Deserialize(true);
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			this.variables.Serialize();
		}
	}
}
