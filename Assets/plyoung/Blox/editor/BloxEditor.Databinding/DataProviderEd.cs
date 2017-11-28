using BloxEngine;
using BloxEngine.DataBinding;
using BloxEngine.Variables;
using plyLibEditor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BloxEditor.Databinding
{
	public class DataProviderEd : plyCustomEd
	{
		public static Dictionary<Type, DataProviderEd> _editors;

		public static Dictionary<Type, DataProviderEd> editors
		{
			get
			{
				if (DataProviderEd._editors == null)
				{
					DataProviderEd._editors = plyCustomEd.CreateCustomEditorsDict<DataProviderEd>(typeof(plyCustomEdAttribute));
				}
				return DataProviderEd._editors;
			}
		}

		public virtual void DrawInspector(DataProvider target, SerializedObject serializedObject)
		{
		}

		public virtual void DrawEditor(Rect rect, DataProvider target)
		{
		}

		protected string GetDataBindingLabel(DataBinding binding)
		{
			if (binding.dataContext == DataBinding.DataContext.Constant)
			{
				plyVar constant = binding.constant;
				if (((constant != null) ? constant.GetValue() : null) != null)
				{
					return binding.constant.GetValue().ToString();
				}
				return "null";
			}
			if (binding.dataContext == DataBinding.DataContext.GlobalProperty)
			{
				if (binding.member == null)
				{
					return "null";
				}
				return BloxMemberInfo.SimpleMemberPath(binding.member);
			}
			return "-select-";
		}
	}
}
