using System;

namespace SimpleStack.Interfaces
{
	public interface IStringDeserializer
	{
		To Parse<To>(string serializedText);
		object Parse(string serializedText, Type type);
	}
}

