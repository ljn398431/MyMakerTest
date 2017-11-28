using System;
using System.Reflection;
using UnityEngine;

namespace BloxEngine
{
	public class BloxMemberInfo
	{
		private MemberInfo member;

		private ConstructorInfo ci;

		private MethodInfo mi;

		private PropertyInfo pi;

		private FieldInfo fi;

		public static readonly char MEMBER_ENCODE_SEPERATOR = '\u001f';

		public string Name
		{
			get;
			private set;
		}

		public MemberTypes MemberType
		{
			get;
			private set;
		}

		public Type ReflectedType
		{
			get;
			private set;
		}

		public Type DeclaringType
		{
			get;
			private set;
		}

		public Type ReturnType
		{
			get;
			private set;
		}

		public bool IsStatic
		{
			get;
			private set;
		}

		public bool CanSetValue
		{
			get;
			private set;
		}

		public bool IsFieldOrProperty
		{
			get;
			private set;
		}

		public bool IsValid
		{
			get
			{
				if (this.member != null && this.MemberType != 0 && this.ReflectedType != null)
				{
					return this.DeclaringType != null;
				}
				return false;
			}
		}

		public bool IsGenericMethod
		{
			get
			{
				if (this.ci != null)
				{
					return this.ci.IsGenericMethod;
				}
				if (this.mi != null)
				{
					return this.mi.IsGenericMethod;
				}
				return false;
			}
		}

		public bool IsSpecialName
		{
			get
			{
				if (this.ci != null)
				{
					return this.ci.IsSpecialName;
				}
				if (this.mi != null)
				{
					return this.mi.IsSpecialName;
				}
				if (this.pi != null)
				{
					return this.pi.IsSpecialName;
				}
				if (this.fi != null)
				{
					return this.fi.IsSpecialName;
				}
				return false;
			}
		}

		public bool HasIndexParameters
		{
			get
			{
				if (this.pi != null)
				{
					return this.pi.GetIndexParameters().Length != 0;
				}
				return false;
			}
		}

		public BloxMemberInfo(MemberInfo member, Type reflectedType = null)
		{
			if (member == null)
			{
				this.member = null;
			}
			else
			{
				this.member = member;
				this.MemberType = member.MemberType;
				this.ReflectedType = (reflectedType ?? member.ReflectedType);
				this.DeclaringType = member.DeclaringType;
				this.Name = member.Name;
				this.CanSetValue = false;
				this.IsFieldOrProperty = false;
				if (member.MemberType == MemberTypes.Property)
				{
					this.pi = (PropertyInfo)member;
					this.ReturnType = this.pi.PropertyType;
					MethodInfo getMethod = this.pi.GetGetMethod();
					MethodInfo setMethod = this.pi.GetSetMethod();
					this.IsStatic = ((getMethod != null && getMethod.IsStatic) || (setMethod != null && setMethod.IsStatic));
					this.CanSetValue = (setMethod != null && !this.IsStatic);
					this.IsFieldOrProperty = true;
				}
				else if (member.MemberType == MemberTypes.Field)
				{
					this.fi = (FieldInfo)member;
					this.ReturnType = this.fi.FieldType;
					this.IsStatic = this.fi.IsStatic;
					this.CanSetValue = (!this.fi.IsInitOnly && !this.fi.IsLiteral && !this.fi.IsStatic);
					this.IsFieldOrProperty = true;
				}
				else if (member.MemberType == MemberTypes.Constructor)
				{
					this.ci = (ConstructorInfo)member;
					this.ReturnType = this.DeclaringType;
					this.IsStatic = true;
				}
				else if (member.MemberType == MemberTypes.Method)
				{
					this.mi = (MethodInfo)member;
					this.ReturnType = this.mi.ReturnType;
					this.IsStatic = this.mi.IsStatic;
				}
			}
		}

		public ParameterInfo[] GetParameters()
		{
			if (this.mi != null)
			{
				return this.mi.GetParameters();
			}
			if (this.ci != null)
			{
				return this.ci.GetParameters();
			}
			return null;
		}

		public Type[] GetParameterTypes()
		{
			ParameterInfo[] parameters = this.GetParameters();
			if (((parameters != null) ? parameters.Length : 0) != 0)
			{
				Type[] array = new Type[parameters.Length];
				for (int i = 0; i < parameters.Length; i++)
				{
					array[i] = parameters[i].ParameterType;
				}
				return array;
			}
			return null;
		}

		public object[] GetCustomAttributes(bool inherit)
		{
			if (this.member == null)
			{
				return new object[0];
			}
			return this.member.GetCustomAttributes(inherit);
		}

		public object Invoke(object context, object[] args)
		{
			try
			{
				if (this.mi != null)
				{
					return this.mi.Invoke(context, args);
				}
				if (this.ci != null)
				{
					return this.ci.Invoke(args);
				}
			}
			catch (ArgumentException)
			{
				throw new Exception("One or more parameters/field values could not be converted to the correct type(s) expected by this Block.");
			}
			return null;
		}

		public object GetValue(object context)
		{
			try
			{
				if (!this.IsStatic && context == null)
				{
					return null;
				}
				if (this.fi != null)
				{
					return this.fi.GetValue(context);
				}
				if (this.pi != null)
				{
					return this.pi.GetValue(context, null);
				}
			}
			catch (TargetException)
			{
				string[] obj = new string[5]
				{
					"The Context is invalid. Expected [",
					null,
					null,
					null,
					null
				};
				MemberInfo obj2 = this.member;
				obj[1] = ((obj2 != null) ? obj2.ReflectedType.Name : null);
				obj[2] = "] but context was set to [";
				obj[3] = ((context != null) ? context.GetType().Name : null);
				obj[4] = "]";
				throw new Exception(string.Concat(obj));
			}
			return null;
		}

		public void SetValue(object context, object value)
		{
			try
			{
				if (this.IsStatic || context != null)
				{
					if (this.fi != null)
					{
						this.fi.SetValue(context, value);
					}
					if (this.pi != null)
					{
						this.pi.SetValue(context, value, null);
					}
				}
			}
			catch (ArgumentException)
			{
				throw new Exception("One or more parameters/field values could not be converted to the correct type(s) expected by this Block.");
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		public static BloxMemberInfo DecodeMember(string data)
		{
			if (string.IsNullOrEmpty(data))
			{
				return null;
			}
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
			BloxMemberInfo result = null;
			string[] array = data.Split(BloxMemberInfo.MEMBER_ENCODE_SEPERATOR);
			if (array.Length >= 3)
			{
				Type type = Type.GetType(array[1]);
				if (type == null)
				{
					return null;
				}
				if (array[0] == "F")
				{
					MemberInfo[] array2 = type.FindMembers(MemberTypes.Field, bindingAttr, BloxMemberInfo.FilterMembersByName, array[2]);
					if (array2.Length == 0)
					{
						Debug.LogError("Could not find field [" + array[2] + "] in " + type);
						return null;
					}
					if (array2.Length > 1)
					{
						Debug.LogWarning("More than one field with same name: " + array[2]);
					}
					result = new BloxMemberInfo(array2[0], type);
				}
				else if (array[0] == "P")
				{
					MemberInfo[] array3 = type.FindMembers(MemberTypes.Property, bindingAttr, BloxMemberInfo.FilterMembersByName, array[2]);
					if (array3.Length == 0)
					{
						Debug.LogError("Could not find property [" + array[2] + "] in " + type);
						return null;
					}
					if (array3.Length > 1)
					{
						Debug.LogWarning("More than one property with same name: " + array[2]);
					}
					result = new BloxMemberInfo(array3[0], type);
				}
				else if (array[0] == "C")
				{
					if (string.IsNullOrEmpty(array[2]))
					{
						return new BloxMemberInfo(type, type);
					}
					ConstructorInfo[] array4 = (ConstructorInfo[])type.FindMembers(MemberTypes.Constructor, bindingAttr, BloxMemberInfo.FilterMembersByName, array[2]);
					if (array4.Length == 0)
					{
						Debug.LogError("Could not find constructor [" + array[2] + "] in " + type);
						return null;
					}
					if (array4.Length == 1)
					{
						result = new BloxMemberInfo(array4[0], type);
					}
					else
					{
						Type[] array5 = new Type[0];
						if (array.Length > 3)
						{
							array5 = new Type[array.Length - 3];
							for (int i = 0; i < array.Length - 3; i++)
							{
								array5[i] = Type.GetType(array[i + 3]);
								if (array5[i] == null)
								{
									Debug.LogError("Could not get parameter type [" + array[i + 3] + "] for " + array[1] + "." + array[2]);
									return null;
								}
							}
						}
						for (int j = 0; j < array4.Length; j++)
						{
							ParameterInfo[] parameters = array4[j].GetParameters();
							if (array5.Length == parameters.Length)
							{
								if (parameters.Length == 0)
								{
									result = new BloxMemberInfo(array4[j], type);
									break;
								}
								bool flag = true;
								int num = 0;
								while (num < parameters.Length)
								{
									if (array5[num] == parameters[num].ParameterType)
									{
										num++;
										continue;
									}
									flag = false;
									break;
								}
								if (flag)
								{
									result = new BloxMemberInfo(array4[j], type);
									break;
								}
							}
						}
					}
				}
				else if (array[0] == "M")
				{
					MethodInfo[] array6 = (MethodInfo[])type.FindMembers(MemberTypes.Method, bindingAttr, BloxMemberInfo.FilterMembersByName, array[2]);
					if (array6.Length == 0)
					{
						Debug.LogError("Could not find method [" + array[2] + "] in " + type);
						return null;
					}
					if (array6.Length == 1)
					{
						result = new BloxMemberInfo(array6[0], type);
					}
					else
					{
						Type[] array7 = new Type[0];
						if (array.Length > 3)
						{
							array7 = new Type[array.Length - 3];
							for (int k = 0; k < array.Length - 3; k++)
							{
								array7[k] = Type.GetType(array[k + 3]);
								if (array7[k] == null)
								{
									Debug.Log(data);
									Debug.LogError("Could not get parameter type [" + array[k + 3] + "] for " + array[1] + " ." + array[2]);
									return null;
								}
							}
						}
						for (int l = 0; l < array6.Length; l++)
						{
							ParameterInfo[] parameters2 = array6[l].GetParameters();
							if (array7.Length == parameters2.Length)
							{
								if (parameters2.Length == 0)
								{
									result = new BloxMemberInfo(array6[l], type);
									break;
								}
								bool flag2 = true;
								int num2 = 0;
								while (num2 < parameters2.Length)
								{
									if (array7[num2] == parameters2[num2].ParameterType)
									{
										num2++;
										continue;
									}
									flag2 = false;
									break;
								}
								if (flag2)
								{
									result = new BloxMemberInfo(array6[l], type);
									break;
								}
							}
						}
					}
				}
				else
				{
					Debug.LogError("[BloxUtil.Create] Unexpected flag: " + array[0]);
				}
			}
			else
			{
				Debug.LogError("[BloxUtil.Create] Invalid data provided");
			}
			return result;
		}

		public static string EncodeMember(BloxMemberInfo member)
		{
			if (member == null)
			{
				return "";
			}
			string text = "";
			if (member.MemberType == MemberTypes.Field)
			{
				text = "F";
			}
			else if (member.MemberType == MemberTypes.Property)
			{
				text = "P";
			}
			else if (member.MemberType == MemberTypes.Constructor)
			{
				text = "C";
			}
			else if (member.MemberType == MemberTypes.Method)
			{
				text = "M";
			}
			string[] obj = new string[5]
			{
				text,
				null,
				null,
				null,
				null
			};
			char mEMBER_ENCODE_SEPERATOR = BloxMemberInfo.MEMBER_ENCODE_SEPERATOR;
			obj[1] = mEMBER_ENCODE_SEPERATOR.ToString();
			obj[2] = member.ReflectedType.AssemblyQualifiedName;
			mEMBER_ENCODE_SEPERATOR = BloxMemberInfo.MEMBER_ENCODE_SEPERATOR;
			obj[3] = mEMBER_ENCODE_SEPERATOR.ToString();
			obj[4] = member.Name;
			text = string.Concat(obj);
			ParameterInfo[] parameters = member.GetParameters();
			if (((parameters != null) ? parameters.Length : 0) != 0)
			{
				for (int i = 0; i < parameters.Length; i++)
				{
					string str = text;
					mEMBER_ENCODE_SEPERATOR = BloxMemberInfo.MEMBER_ENCODE_SEPERATOR;
					text = str + mEMBER_ENCODE_SEPERATOR.ToString() + parameters[i].ParameterType.AssemblyQualifiedName;
				}
			}
			return text;
		}

		public static string SimpleMemberPath(BloxMemberInfo member)
		{
			if (member == null)
			{
				return "";
			}
			return member.ReflectedType.Name + "." + member.Name;
		}

		private static bool FilterMembersByName(MemberInfo mi, object obj)
		{
			return mi.Name == obj.ToString();
		}

		public static object GetDefaultValue(Type t)
		{
			if (t != null && t != typeof(void))
			{
				if (!(t.IsValueType | t.IsEnum))
				{
					if (t != typeof(string))
					{
						return null;
					}
					return string.Empty;
				}
				return Activator.CreateInstance(t);
			}
			return null;
		}
	}
}
