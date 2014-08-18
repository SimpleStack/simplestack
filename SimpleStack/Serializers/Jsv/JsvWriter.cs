using System;
using SimpleStack.Tools;
using System.IO;

namespace SimpleStack.Serializers.Jsv
{
	internal static class JsvWriter
	{

		public static WriteObjectDelegate GetWriteFn(Type type)
		{
			throw new NotImplementedException();
		}

		public static void WriteLateBoundObject(TextWriter writer, object value)
		{
			throw new NotImplementedException();
		}

		public static WriteObjectDelegate GetValueTypeToStringMethod(Type type)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Implement the serializer using a more static approach
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal static class JsvWriter<T>
	{
		private static readonly WriteObjectDelegate CacheFn;

		public static WriteObjectDelegate WriteFn()
		{
			throw new NotImplementedException();
		}

		static JsvWriter()
		{

		}

		public static void WriteObject(TextWriter writer, object value)
		{
			throw new NotImplementedException();
		}

	}
}

