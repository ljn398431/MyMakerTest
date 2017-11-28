using System.Collections.Generic;
using UnityEngine;

namespace BloxEngine.DataBinding
{
	[AddComponentMenu("")]
	public class DataProviderUpdater : MonoBehaviour
	{
		private static DataProviderUpdater _instance;

		private List<DataProvider> dataproviders = new List<DataProvider>(5);

		public static DataProviderUpdater Instance
		{
			get
			{
				if ((Object)DataProviderUpdater._instance == (Object)null)
				{
					GameObject gameObject = new GameObject("[DataProviderUpdater]");
					DataProviderUpdater._instance = gameObject.AddComponent<DataProviderUpdater>();
					Object.DontDestroyOnLoad(gameObject);
				}
				return DataProviderUpdater._instance;
			}
		}

		public void RegisterProvider(DataProvider db)
		{
			if (!this.dataproviders.Contains(db))
			{
				this.dataproviders.Add(db);
			}
		}

		public void RemoveProvider(DataProvider db)
		{
			if (this.dataproviders.Contains(db))
			{
				this.dataproviders.Remove(db);
			}
		}

		protected void LateUpdate()
		{
			for (int i = 0; i < this.dataproviders.Count; i++)
			{
				this.dataproviders[i].DoUpdate();
			}
		}
	}
}
