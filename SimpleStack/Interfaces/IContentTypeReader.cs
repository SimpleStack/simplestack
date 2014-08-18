using System;
using System.IO;

namespace SimpleStack.Interfaces
{
	public interface IContentTypeReader
	{
		object DeserializeFromString(string contentType, Type type, string request);

		object DeserializeFromStream(string contentType, Type type, Stream requestStream);
	}


}

