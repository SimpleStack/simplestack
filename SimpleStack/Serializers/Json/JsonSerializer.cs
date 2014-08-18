using System;
using System.IO;

namespace SimpleStack.Serializers.Json
{
	public class JsonSerializer
	{
		public static object DeserializeFromString(string value, Type type)
		{
			throw new NotImplementedException();
		}

		public static T DeserializeFromString<T>(string value)
		{
			throw new NotImplementedException();
		}

		public static T DeserializeFromStream<T>(Stream stream)
		{
			throw new NotImplementedException();
		}

		public static object DeserializeFromStream(Type type, Stream stream)
		{
			throw new NotImplementedException();
		}

		public static string SerializeToString<T>(T value)
		{
			throw new NotImplementedException();
		}

		public static void SerializeToStream<T>(T value, Stream stream)
		{
			throw new NotImplementedException();
		}
	}
}

