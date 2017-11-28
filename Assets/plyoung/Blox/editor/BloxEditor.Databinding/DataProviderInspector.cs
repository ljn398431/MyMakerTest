using BloxEngine.DataBinding;
using plyLib;
using UnityEditor;
using UnityEngine;

namespace BloxEditor.Databinding
{
	[CustomEditor(typeof(DataProvider), true)]
	public class DataProviderInspector : Editor
	{
		private DataProvider dp;

		protected void OnEnable()
		{
			if (!this.CheckTarget())
			{
				if (this.dp._edManaged == plyManagedComponent.Managed)
				{
					if ((Object)this.dp._edOwner == (Object)null)
					{
						this.dp._edManaged = plyManagedComponent.Remove;
					}
					else if (this.dp.hideFlags != HideFlags.HideInInspector)
					{
						this.dp.hideFlags = HideFlags.HideInInspector;
						EditorUtility.SetDirty(this.dp);
					}
				}
				if (this.dp._edManaged != 0 && !this.dp._edDeserialize)
					return;
				this.dp.Deserialize();
			}
		}

		public override void OnInspectorGUI()
		{
			if (!this.CheckTarget())
			{
				if (this.dp._edDeserialize)
				{
					this.dp.Deserialize();
				}
				if (this.dp._edManaged == plyManagedComponent.None)
				{
					EditorGUILayout.Space();
					DataProviderEd.editors[this.dp.GetType()].DrawInspector(this.dp, base.serializedObject);
					EditorGUILayout.Space();
				}
			}
		}

		private bool CheckTarget()
		{
			if (base.target == (Object)null)
			{
				return true;
			}
			this.dp = (DataProvider)base.target;
			if ((Event.current == null || Event.current.type == EventType.Repaint) && this.dp._edManaged == plyManagedComponent.Remove)
			{
				Object.DestroyImmediate(this.dp);
				return true;
			}
			return false;
		}
	}
}
