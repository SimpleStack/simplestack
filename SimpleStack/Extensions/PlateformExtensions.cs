using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace SimpleStack.Extensions
{
	public static class PlatformExtensions //Because WinRT is a POS
	{
		public static bool IsInterface(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().IsInterface;
			#else
			return type.IsInterface;
			#endif
		}

		public static bool IsArray(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().IsArray;
			#else
			return type.IsArray;
			#endif
		}

		public static bool IsValueType(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().IsValueType;
			#else
			return type.IsValueType;
			#endif
		}

		public static bool IsGeneric(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().IsGenericType;
			#else
			return type.IsGenericType;
			#endif
		}

		public static Type BaseType(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().BaseType;
			#else
			return type.BaseType;
			#endif
		}

		public static Type ReflectedType(this PropertyInfo pi)
		{
			#if NETFX_CORE
			return pi.PropertyType;
			#else
			return pi.ReflectedType;
			#endif
		}

		public static Type ReflectedType(this FieldInfo fi)
		{
			#if NETFX_CORE
			return fi.FieldType;
			#else
			return fi.ReflectedType;
			#endif
		}

		public static Type GenericTypeDefinition(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().GetGenericTypeDefinition();
			#else
			return type.GetGenericTypeDefinition();
			#endif
		}

		public static Type[] GetTypeInterfaces(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().ImplementedInterfaces.ToArray();
			#else
			return type.GetInterfaces();
			#endif
		}

		public static Type[] GetTypeGenericArguments(this Type type)
		{
			#if NETFX_CORE
			return type.GenericTypeArguments;
			#else
			return type.GetGenericArguments();
			#endif
		}

		public static ConstructorInfo GetEmptyConstructor(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().DeclaredConstructors.FirstOrDefault(c => c.GetParameters().Count() == 0);
			#else
			return type.GetConstructor(Type.EmptyTypes);
			#endif
		}

		internal static PropertyInfo[] GetTypesPublicProperties(this Type subType)
		{
			#if NETFX_CORE 
			return subType.GetRuntimeProperties().ToArray();
			#else
			return subType.GetProperties(
				BindingFlags.FlattenHierarchy |
				BindingFlags.Public |
				BindingFlags.Instance);
			#endif
		}

		public static PropertyInfo[] Properties(this Type type)
		{
			#if NETFX_CORE 
			return type.GetRuntimeProperties().ToArray();
			#else
			return type.GetProperties();
			#endif
		}

		public static FieldInfo[] GetAllFields(this Type type)
		{
			if (type.IsInterface())
			{
				return new FieldInfo[0];
			}

			#if NETFX_CORE
			return type.GetRuntimeFields().Where(p => !p.IsStatic).ToArray();
			#else
			return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.ToArray();
			#endif
		}

		public static FieldInfo[] GetPublicFields(this Type type)
		{
			if (type.IsInterface())
			{
				return new FieldInfo[0];
			}

			#if NETFX_CORE
			return type.GetRuntimeFields().Where(p => p.IsPublic && !p.IsStatic).ToArray();
			#else
			return type.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance)
				.ToArray();
			#endif
		}

		public static MemberInfo[] GetPublicMembers(this Type type)
		{

			#if NETFX_CORE
			var members = new List<MemberInfo>();
			members.AddRange(type.GetRuntimeFields().Where(p => p.IsPublic && !p.IsStatic));
			members.AddRange(type.GetPublicProperties());
			return members.ToArray();
			#else
			return type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
			#endif
		}

		public static MemberInfo[] GetAllPublicMembers(this Type type)
		{

			#if NETFX_CORE
			var members = new List<MemberInfo>();
			members.AddRange(type.GetRuntimeFields().Where(p => p.IsPublic && !p.IsStatic));
			members.AddRange(type.GetPublicProperties());
			return members.ToArray();
			#else
			return type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			#endif
		}

		public static bool HasAttribute<T>(this Type type, bool inherit = true) where T : Attribute
		{
			return type.CustomAttributes(inherit).Any(x => x.GetType() == typeof(T));
		}

		public static IEnumerable<T> AttributesOfType<T>(this Type type, bool inherit = true) where T : Attribute
		{
			#if NETFX_CORE
			return type.GetTypeInfo().GetCustomAttributes<T>(inherit);
			#else
			return type.GetCustomAttributes(inherit).OfType<T>();
			#endif
		}

		const string DataContract = "DataContractAttribute";
		public static bool IsDto(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().IsDefined(typeof(DataContractAttribute), false);
			#else
			return !Env.IsMono
				? type.IsDefined(typeof(DataContractAttribute), false)
					: type.GetCustomAttributes(true).Any(x => x.GetType().Name == DataContract);
			#endif
		}

		public static MethodInfo PropertyGetMethod(this PropertyInfo pi, bool nonPublic = false)
		{
			#if NETFX_CORE
			return pi.GetMethod;
			#else
			return pi.GetGetMethod(false);
			#endif
		}

		public static Type[] Interfaces(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().ImplementedInterfaces.ToArray();
			//return type.GetTypeInfo().ImplementedInterfaces
			//    .FirstOrDefault(x => !x.GetTypeInfo().ImplementedInterfaces
			//        .Any(y => y.GetTypeInfo().ImplementedInterfaces.Contains(y)));
			#else
			return type.GetInterfaces();
			#endif
		}

		public static PropertyInfo[] AllProperties(this Type type)
		{
			#if NETFX_CORE
			return type.GetRuntimeProperties().ToArray();
			#else
			return type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			#endif
		}

		public static object[] CustomAttributes(this PropertyInfo propertyInfo, bool inherit = true)
		{
			#if NETFX_CORE
			return propertyInfo.GetCustomAttributes(inherit).ToArray();
			#else
			return propertyInfo.GetCustomAttributes(inherit);
			#endif
		}

		public static object[] CustomAttributes(this PropertyInfo propertyInfo, Type attrType, bool inherit = true)
		{
			#if NETFX_CORE
			return propertyInfo.GetCustomAttributes(inherit).Where(x => x.GetType() == attrType).ToArray();
			#else
			return propertyInfo.GetCustomAttributes(attrType, inherit);
			#endif
		}

		public static object[] CustomAttributes(this FieldInfo fieldInfo, bool inherit = true)
		{
			#if NETFX_CORE
			return fieldInfo.GetCustomAttributes(inherit).ToArray();
			#else
			return fieldInfo.GetCustomAttributes(inherit);
			#endif
		}

		public static object[] CustomAttributes(this FieldInfo fieldInfo, Type attrType, bool inherit = true)
		{
			#if NETFX_CORE
			return fieldInfo.GetCustomAttributes(inherit).Where(x => x.GetType() == attrType).ToArray();
			#else
			return fieldInfo.GetCustomAttributes(attrType, inherit);
			#endif
		}

		public static object[] CustomAttributes(this Type type, bool inherit = true)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().GetCustomAttributes(inherit).ToArray();
			#else
			return type.GetCustomAttributes(inherit);
			#endif
		}

		public static object[] CustomAttributes(this Type type, Type attrType, bool inherit = true)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().GetCustomAttributes(inherit).Where(x => x.GetType() == attrType).ToArray();
			#else
			return type.GetCustomAttributes(attrType, inherit);
			#endif
		}

		public static TAttr FirstAttribute<TAttr>(this Type type, bool inherit = true) where TAttr : Attribute
		{
			#if NETFX_CORE
			return type.GetTypeInfo().GetCustomAttributes(typeof(TAttr), inherit)
			.FirstOrDefault() as TAttr;
			#else
			return type.GetCustomAttributes(typeof(TAttr), inherit)
				.FirstOrDefault() as TAttr;
			#endif
		}

		public static TAttribute FirstAttribute<TAttribute>(this PropertyInfo propertyInfo)
			where TAttribute : Attribute
		{
			return propertyInfo.FirstAttribute<TAttribute>(true);
		}

		public static TAttribute FirstAttribute<TAttribute>(this PropertyInfo propertyInfo, bool inherit)
			where TAttribute : Attribute
		{
			#if NETFX_CORE
			var attrs = propertyInfo.GetCustomAttributes<TAttribute>(inherit);
			return (TAttribute)(attrs.Count() > 0 ? attrs.ElementAt(0) : null);
			#else
			var attrs = propertyInfo.GetCustomAttributes(typeof(TAttribute), inherit);
			return (TAttribute)(attrs.Length > 0 ? attrs[0] : null);
			#endif
		}

		public static Type FirstGenericTypeDefinition(this Type type)
		{
			while (type != null)
			{
				if (type.HasGenericType())
					return type.GenericTypeDefinition();

				type = type.BaseType();
			}

			return null;
		}

		public static bool IsDynamic(this Assembly assembly)
		{
			#if MONOTOUCH || WINDOWS_PHONE || NETFX_CORE
			return false;
			#else
			try
			{
				var isDyanmic = assembly is System.Reflection.Emit.AssemblyBuilder
					|| string.IsNullOrEmpty(assembly.Location);
				return isDyanmic;
			}
			catch (NotSupportedException)
			{
				//Ignore assembly.Location not supported in a dynamic assembly.
				return true;
			}
			#endif
		}

		public static MethodInfo GetPublicStaticMethod(this Type type, string methodName, Type[] types = null)
		{
			#if NETFX_CORE
			return type.GetRuntimeMethod(methodName, types);
			#else
			return types == null
				? type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
					: type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, types, null);
			#endif
		}

		public static MethodInfo GetMethodInfo(this Type type, string methodName, Type[] types = null)
		{
			#if NETFX_CORE
			return type.GetRuntimeMethods().First(p => p.Name.Equals(methodName));
			#else
			return types == null
				? type.GetMethod(methodName)
					: type.GetMethod(methodName, types);
			#endif
		}

		public static object InvokeMethod(this Delegate fn, object instance, object[] parameters = null)
		{
			#if NETFX_CORE
			return fn.GetMethodInfo().Invoke(instance, parameters ?? new object[] { });
			#else
			return fn.Method.Invoke(instance, parameters ?? new object[] { });
			#endif
		}

		public static FieldInfo GetPublicStaticField(this Type type, string fieldName)
		{
			#if NETFX_CORE
			return type.GetRuntimeField(fieldName);
			#else
			return type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
			#endif
		}

		public static Delegate MakeDelegate(this MethodInfo mi, Type delegateType, bool throwOnBindFailure=true)
		{
			#if NETFX_CORE
			return mi.CreateDelegate(delegateType);
			#else
			return Delegate.CreateDelegate(delegateType, mi, throwOnBindFailure);
			#endif
		}

		public static Type[] GenericTypeArguments(this Type type)
		{
			#if NETFX_CORE
			return type.GenericTypeArguments;
			#else
			return type.GetGenericArguments();
			#endif
		}

		public static ConstructorInfo[] DeclaredConstructors(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().DeclaredConstructors.ToArray();
			#else
			return type.GetConstructors();
			#endif
		}

		public static bool AssignableFrom(this Type type, Type fromType)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().IsAssignableFrom(fromType.GetTypeInfo());
			#else
			return type.IsAssignableFrom(fromType);
			#endif
		}

		public static bool IsStandardClass(this Type type)
		{
			#if NETFX_CORE
			var typeInfo = type.GetTypeInfo();
			return typeInfo.IsClass && !typeInfo.IsAbstract && !typeInfo.IsInterface;
			#else
			return type.IsClass && !type.IsAbstract && !type.IsInterface;
			#endif
		}

		public static bool IsAbstract(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().IsAbstract;
			#else
			return type.IsAbstract;
			#endif
		}

		public static PropertyInfo GetPropertyInfo(this Type type, string propertyName)
		{
			#if NETFX_CORE
			return type.GetRuntimeProperty(propertyName);
			#else
			return type.GetProperty(propertyName);
			#endif
		}

		public static FieldInfo GetFieldInfo(this Type type, string fieldName)
		{
			#if NETFX_CORE
			return type.GetRuntimeField(fieldName);
			#else
			return type.GetField(fieldName);
			#endif
		}

		public static FieldInfo[] GetWritableFields(this Type type)
		{
			#if NETFX_CORE
			return type.GetRuntimeFields().Where(p => !p.IsPublic && !p.IsStatic).ToArray();
			#else
			return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField);
			#endif
		}

		public static MethodInfo SetMethod(this PropertyInfo pi, bool nonPublic = true)
		{
			#if NETFX_CORE
			return pi.SetMethod;
			#else
			return pi.GetSetMethod(nonPublic);
			#endif
		}

		public static MethodInfo GetMethodInfo(this PropertyInfo pi, bool nonPublic = true)
		{
			#if NETFX_CORE
			return pi.GetMethod;
			#else
			return pi.GetGetMethod(nonPublic);
			#endif
		}

		public static bool InstanceOfType(this Type type, object instance)
		{
			#if NETFX_CORE
			return type.IsInstanceOf(instance.GetType());
			#else
			return type.IsInstanceOfType(instance);
			#endif
		}

		public static bool IsClass(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().IsClass;
			#else
			return type.IsClass;
			#endif
		}

		public static bool IsEnum(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().IsEnum;
			#else
			return type.IsEnum;
			#endif
		}

		public static bool IsEnumFlags(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().IsEnum && type.FirstAttribute<FlagsAttribute>(false) != null;
			#else
			return type.IsEnum && type.FirstAttribute<FlagsAttribute>(false) != null;
			#endif
		}

		public static bool IsUnderlyingEnum(this Type type)
		{
			#if NETFX_CORE
			return type.GetTypeInfo().IsEnum;
			#else
			return type.IsEnum || type.UnderlyingSystemType.IsEnum;
			#endif
		}

		public static MethodInfo[] GetMethodInfos(this Type type)
		{
			#if NETFX_CORE
			return type.GetRuntimeMethods().ToArray();
			#else
			return type.GetMethods();
			#endif
		}

		public static PropertyInfo[] GetPropertyInfos(this Type type)
		{
			#if NETFX_CORE
			return type.GetRuntimeProperties().ToArray();
			#else
			return type.GetProperties();
			#endif
		}

		#if SILVERLIGHT || NETFX_CORE
		public static List<U> ConvertAll<T, U>(this List<T> list, Func<T, U> converter)
		{
		var result = new List<U>();
		foreach (var element in list)
		{
		result.Add(converter(element));
		}
		return result;
		}
		#endif


	}
}

