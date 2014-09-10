using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using SimpleStack.Extensions;

namespace SimpleStack.Tools
{
	internal class TypeAccessor
	{
		//internal ParseStringDelegate GetProperty;
		//internal SetPropertyDelegate SetProperty;
		//internal Type PropertyType;

		//public static Type ExtractType(ITypeSerializer Serializer, string strType)
		//{
		//	var typeAttrInObject = Serializer.TypeAttrInObject;

		//	if (strType != null
		//		&& strType.Length > typeAttrInObject.Length
		//		&& strType.Substring(0, typeAttrInObject.Length) == typeAttrInObject)
		//	{
		//		var propIndex = typeAttrInObject.Length;
		//		var typeName = Serializer.EatValue(strType, ref propIndex);
		//		var type = JsConfig.TypeFinder.Invoke(typeName);

		//		if (type == null)
		//			Tracer.Instance.WriteWarning("Could not find type: " + typeName);

		//		return type;
		//	}
		//	return null;
		//}

		//public static TypeAccessor Create(ITypeSerializer serializer, TypeConfig typeConfig, PropertyInfo propertyInfo)
		//{
		//	return new TypeAccessor
		//	{
		//		PropertyType = propertyInfo.PropertyType,
		//		GetProperty = serializer.GetParseFn(propertyInfo.PropertyType),
		//		SetProperty = GetSetPropertyMethod(typeConfig, propertyInfo),
		//	};
		//}

		private static SetPropertyDelegate GetSetPropertyMethod(TypeConfig typeConfig, PropertyInfo propertyInfo)
		{
			if (propertyInfo.ReflectedType() != propertyInfo.DeclaringType)
				propertyInfo = propertyInfo.DeclaringType.GetPropertyInfo(propertyInfo.Name);

			if (!propertyInfo.CanWrite && !typeConfig.EnableAnonymousFieldSetterses) return null;

			FieldInfo fieldInfo = null;
			if (!propertyInfo.CanWrite)
			{
				//TODO: What string comparison is used in SST?
				string fieldNameFormat = Env.IsMono ? "<{0}>" : "<{0}>i__Field";
				var fieldName = string.Format(fieldNameFormat, propertyInfo.Name);

				var fieldInfos = typeConfig.Type.GetWritableFields();
				foreach (var f in fieldInfos)
				{
					if (f.IsInitOnly && f.FieldType == propertyInfo.PropertyType && f.Name == fieldName)
					{
						fieldInfo = f;
						break;
					}
				}

				if (fieldInfo == null) return null;
			}

#if SILVERLIGHT || MONOTOUCH || XBOX
            if (propertyInfo.CanWrite)
            {
                var setMethodInfo = propertyInfo.SetMethod();
                return (instance, value) => setMethodInfo.Invoke(instance, new[] { value });
            }
            if (fieldInfo == null) return null;
            return (instance, value) => fieldInfo.SetValue(instance, value);
#else
			return propertyInfo.CanWrite
				? CreateIlPropertySetter(propertyInfo)
				: CreateIlFieldSetter(fieldInfo);
#endif
		}

#if !SILVERLIGHT && !MONOTOUCH && !XBOX

		private static SetPropertyDelegate CreateIlPropertySetter(PropertyInfo propertyInfo)
		{
			var propSetMethod = propertyInfo.GetSetMethod(true);
			if (propSetMethod == null)
				return null;

			var setter = CreateDynamicSetMethod(propertyInfo);

			var generator = setter.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
			generator.Emit(OpCodes.Ldarg_1);

			generator.Emit(propertyInfo.PropertyType.IsClass
				? OpCodes.Castclass
				: OpCodes.Unbox_Any,
				propertyInfo.PropertyType);

			generator.EmitCall(OpCodes.Callvirt, propSetMethod, (Type[])null);
			generator.Emit(OpCodes.Ret);

			return (SetPropertyDelegate)setter.CreateDelegate(typeof(SetPropertyDelegate));
		}

		private static SetPropertyDelegate CreateIlFieldSetter(FieldInfo fieldInfo)
		{
			var setter = CreateDynamicSetMethod(fieldInfo);

			var generator = setter.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, fieldInfo.DeclaringType);
			generator.Emit(OpCodes.Ldarg_1);

			generator.Emit(fieldInfo.FieldType.IsClass
				? OpCodes.Castclass
				: OpCodes.Unbox_Any,
				fieldInfo.FieldType);

			generator.Emit(OpCodes.Stfld, fieldInfo);
			generator.Emit(OpCodes.Ret);

			return (SetPropertyDelegate)setter.CreateDelegate(typeof(SetPropertyDelegate));
		}

		private static DynamicMethod CreateDynamicSetMethod(MemberInfo memberInfo)
		{
			var args = new[] { typeof(object), typeof(object) };
			var name = string.Format("_{0}{1}_", "Set", memberInfo.Name);
			var returnType = typeof(void);

			return !memberInfo.DeclaringType.IsInterface
				? new DynamicMethod(name, returnType, args, memberInfo.DeclaringType, true)
				: new DynamicMethod(name, returnType, args, memberInfo.Module, true);
		}
#endif

		internal static SetPropertyDelegate GetSetPropertyMethod(Type type, PropertyInfo propertyInfo)
		{
			if (!propertyInfo.CanWrite || propertyInfo.GetIndexParameters().Any()) return null;

#if SILVERLIGHT || MONOTOUCH || XBOX
            var setMethodInfo = propertyInfo.SetMethod();
            return (instance, value) => setMethodInfo.Invoke(instance, new[] { value });
#else
			return CreateIlPropertySetter(propertyInfo);
#endif
		}

		internal static SetPropertyDelegate GetSetFieldMethod(Type type, FieldInfo fieldInfo)
		{

#if SILVERLIGHT || MONOTOUCH || XBOX
            return (instance, value) => fieldInfo.SetValue(instance, value);
#else
			return CreateIlFieldSetter(fieldInfo);
#endif
		}


		//public static TypeAccessor Create(ITypeSerializer serializer, TypeConfig typeConfig, FieldInfo fieldInfo)
		//{
		//	return new TypeAccessor
		//	{
		//		PropertyType = fieldInfo.FieldType,
		//		GetProperty = serializer.GetParseFn(fieldInfo.FieldType),
		//		SetProperty = GetSetFieldMethod(typeConfig, fieldInfo),
		//	};

		//}

		private static SetPropertyDelegate GetSetFieldMethod(TypeConfig typeConfig, FieldInfo fieldInfo)
		{
			if (fieldInfo.ReflectedType() != fieldInfo.DeclaringType)
				fieldInfo = fieldInfo.DeclaringType.GetFieldInfo(fieldInfo.Name);

#if SILVERLIGHT || MONOTOUCH || XBOX
            return (instance, value) => fieldInfo.SetValue(instance, value);
#else
			return CreateIlFieldSetter(fieldInfo);
#endif
		}
	}
}
