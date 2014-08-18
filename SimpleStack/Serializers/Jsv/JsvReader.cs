using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using SimpleStack.Logging;
using System.Collections.Generic;
using SimpleStack.Extensions;
using System.Runtime.Serialization;
using SimpleStack.Tools;
using SimpleStack.Config;

namespace SimpleStack.Serializers.Jsv
{
	public class JsvReader
	{
		private static Dictionary<Type, ParseFactoryDelegate> ParseFnCache = new Dictionary<Type, ParseFactoryDelegate>();

		public static ParseStringDelegate GetParseFn(Type type)
		{
			ParseFactoryDelegate parseFactoryFn;

			if (ParseFnCache.TryGetValue(type, out parseFactoryFn) && parseFactoryFn != null) 
				return parseFactoryFn();

			var genericType = typeof(JsvReader<>).MakeGenericType(type);
			var mi = genericType.GetPublicStaticMethod("GetParseFn");
			parseFactoryFn = (ParseFactoryDelegate)mi.MakeDelegate(typeof(ParseFactoryDelegate));

			Dictionary<Type, ParseFactoryDelegate> snapshot, newCache;
			do
			{
				snapshot = ParseFnCache;
				newCache = new Dictionary<Type, ParseFactoryDelegate>(ParseFnCache);
				newCache[type] = parseFactoryFn;

			} while (!ReferenceEquals(
				Interlocked.CompareExchange(ref ParseFnCache, newCache, snapshot), snapshot));

			return parseFactoryFn();
		}
	}

	public class JsvReader<T>
	{
		public static ParseStringDelegate GetParseFn()
		{
			if (typeof(T) == typeof(string))
			{
				return x => x;
			}
			if (typeof(T).IsEnum())
			{
				return x => Enum.Parse(typeof(T), x, true);
			}
			//Note the generic typeof(T) is faster than using var type = typeof(T)
			if (typeof(T) == typeof(bool))
				return value => value.Length == 1 ? value == "1" : bool.Parse(value);
			if (typeof(T) == typeof(byte))
				return value => byte.Parse(value, CultureInfo.InvariantCulture);
			if (typeof(T) == typeof(sbyte))
				return value => sbyte.Parse(value, CultureInfo.InvariantCulture);
			if (typeof(T) == typeof(short))
				return value => short.Parse(value, CultureInfo.InvariantCulture);
			if (typeof(T) == typeof(int))
				return value => int.Parse(value, CultureInfo.InvariantCulture);
			if (typeof(T) == typeof(long))
				return value => long.Parse(value, CultureInfo.InvariantCulture);
			if (typeof(T) == typeof(float))
				return value => float.Parse(value, CultureInfo.InvariantCulture);
			if (typeof(T) == typeof(double))
				return value => double.Parse(value, CultureInfo.InvariantCulture);
			if (typeof(T) == typeof(decimal))
				return value => decimal.Parse(value, CultureInfo.InvariantCulture);

			if (typeof(T) == typeof(Guid))
				return value => new Guid(value);
			if (typeof(T) == typeof(DateTime?))
				return value => DateTimeSerializer.ParseShortestNullableXsdDateTime(value);
			if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
				return value => DateTimeSerializer.ParseShortestXsdDateTime(value);
			if (typeof(T) == typeof(DateTimeOffset?))
				return value => DateTimeSerializer.ParseDateTimeOffsetNullable(value);
			if (typeof(T) == typeof(DateTimeOffset) || typeof(T) == typeof(DateTimeOffset?))
				return value => DateTimeSerializer.ParseDateTimeOffset(value);
			if (typeof(T) == typeof(TimeSpan))
				return value => DateTimeSerializer.ParseTimeSpan(value);
			if (typeof(T) == typeof(TimeSpan?))
				return value => DateTimeSerializer.ParseNullableTimeSpan(value);
			//#if !MONOTOUCH && !SILVERLIGHT && !XBOX && !ANDROID
			//			if (typeof(T) == typeof(System.Data.Linq.Binary))
			//				return value => new System.Data.Linq.Binary(Convert.FromBase64String(value));
			//#endif
			if (typeof(T) == typeof(char))
			{
				char cValue;
				return value => char.TryParse(value, out cValue) ? cValue : '\0';
			}
			if (typeof(T) == typeof(ushort))
				return value => ushort.Parse(value);
			if (typeof(T) == typeof(uint))
				return value => uint.Parse(value);
			if (typeof(T) == typeof(ulong))
				return value => ulong.Parse(value);

			if (typeof(T) == typeof(bool?))
				return value => string.IsNullOrEmpty(value) ? (bool?)null : value.Length == 1 ? value == "1" : bool.Parse(value);
			if (typeof(T) == typeof(byte?))
				return value => string.IsNullOrEmpty(value) ? (byte?)null : byte.Parse(value);
			if (typeof(T) == typeof(sbyte?))
				return value => string.IsNullOrEmpty(value) ? (sbyte?)null : sbyte.Parse(value);
			if (typeof(T) == typeof(short?))
				return value => string.IsNullOrEmpty(value) ? (short?)null : short.Parse(value);
			if (typeof(T) == typeof(int?))
				return value => string.IsNullOrEmpty(value) ? (int?)null : int.Parse(value);
			if (typeof(T) == typeof(long?))
				return value => string.IsNullOrEmpty(value) ? (long?)null : long.Parse(value);
			if (typeof(T) == typeof(float?))
				return value => string.IsNullOrEmpty(value) ? (float?)null : float.Parse(value, CultureInfo.InvariantCulture);
			if (typeof(T) == typeof(double?))
				return value => string.IsNullOrEmpty(value) ? (double?)null : double.Parse(value, CultureInfo.InvariantCulture);
			if (typeof(T) == typeof(decimal?))
				return value => string.IsNullOrEmpty(value) ? (decimal?)null : decimal.Parse(value, CultureInfo.InvariantCulture);

			if (typeof(T) == typeof(TimeSpan?))
				return value => string.IsNullOrEmpty(value) ? (TimeSpan?)null : TimeSpan.Parse(value);
			if (typeof(T) == typeof(Guid?))
				return value => string.IsNullOrEmpty(value) ? (Guid?)null : new Guid(value);
			if (typeof(T) == typeof(ushort?))
				return value => string.IsNullOrEmpty(value) ? (ushort?)null : ushort.Parse(value);
			if (typeof(T) == typeof(uint?))
				return value => string.IsNullOrEmpty(value) ? (uint?)null : uint.Parse(value);
			if (typeof(T) == typeof(ulong?))
				return value => string.IsNullOrEmpty(value) ? (ulong?)null : ulong.Parse(value);

			if (typeof(T) == typeof(char?))
			{
				char cValue;
				return value => string.IsNullOrEmpty(value) ? (char?)null : char.TryParse(value, out cValue) ? cValue : '\0';
			}

			return null;
		}

		public static object Parse(string value)
		{
			throw new NotImplementedException();
		}
	}

}

