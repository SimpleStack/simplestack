using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleStack.Interfaces
{
	public delegate object TextDeserializerDelegate(Type type, string dto);
	public delegate object StreamDeserializerDelegate(Type type, Stream fromStream);
	public delegate string TextSerializerDelegate(object dto);

	public delegate void StreamSerializerDelegate(IRequestContext requestContext, object dto, Stream outputStream);
	public delegate void ResponseSerializerDelegate(IRequestContext requestContext, object dto, IHttpResponse httpRes);

	public interface IContentTypeFilter : IContentTypeWriter, IContentTypeReader
	{
		IDictionary<string, List<string>> ContentTypeFormats { get; }

		//[Obsolete]
		//void Register(string contentType, StreamSerializerDelegate streamSerializer, StreamDeserializerDelegate streamDeserializer);
		//[Obsolete]
		//void Register(string contentType, ResponseSerializerDelegate responseSerializer, StreamDeserializerDelegate streamDeserializer);
		//[Obsolete]
		//void ClearCustomFilters();

		ResponseSerializerDelegate GetResponseSerializer(string contentType);

		StreamDeserializerDelegate GetStreamDeserializer(string contentType);


		//New
		void Register(IContentTypeSerializer serializer);
	}
}

