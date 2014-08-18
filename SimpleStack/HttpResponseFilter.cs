using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleStack.Extensions;
using SimpleStack.Interfaces;

namespace SimpleStack
{
	/// <summary>
	/// Manage Content Serialization/Deserialization
	/// </summary>
	internal class HttpResponseFilter : IContentTypeFilter
	{
		private static readonly UTF8Encoding UTF8EncodingWithoutBom = new UTF8Encoding(false);

		public static HttpResponseFilter Instance = new HttpResponseFilter();
		
		public Dictionary<string, ResponseSerializerDelegate> ContentTypeResponseSerializers = new Dictionary<string, ResponseSerializerDelegate>();
		public Dictionary<string, StreamDeserializerDelegate> ContentTypeDeserializers = new Dictionary<string, StreamDeserializerDelegate>();
		public Dictionary<string, StreamSerializerDelegate> ContentTypeSerializers  = new Dictionary<string, StreamSerializerDelegate>();

		public HttpResponseFilter()
		{
			ContentTypeFormats = new Dictionary<string, List<string>>();
		}

		public void ClearCustomFilters()
		{
			ContentTypeFormats = new Dictionary<string, List<string>>();
			ContentTypeSerializers = new Dictionary<string, StreamSerializerDelegate>();
			ContentTypeDeserializers = new Dictionary<string, StreamDeserializerDelegate>();
		}

		public IDictionary<string, List<string>> ContentTypeFormats { get; set; }

		public void Register(IContentTypeSerializer serializer)
		{
			if(serializer == null)
				throw new ArgumentNullException("serializer");

			for (int i = 0; i < serializer.ContentTypes.Length; i++)
			{
				string[] parts = serializer.ContentTypes[i].Split('/');
				string format = parts[parts.Length - 1];
				if (!ContentTypeFormats.ContainsKey(format))
				{
					ContentTypeFormats.Add(format,new List<string>());
				}
				ContentTypeFormats[format].Add(serializer.ContentTypes[i]);

				ContentTypeSerializers[serializer.ContentTypes[i]] = serializer.GetStreamSerializer();
				ContentTypeDeserializers[serializer.ContentTypes[i]] = serializer.GetStreamDeserializer();
			}
		}

		//public void Register(string contentType,
		//					 StreamSerializerDelegate streamSerializer,
		//					 StreamDeserializerDelegate streamDeserializer)
		//{
		//	if (contentType.IsNullOrEmpty())
		//		throw new ArgumentNullException("contentType");

		//	string[] parts = contentType.Split('/');
		//	string format = parts[parts.Length - 1];
		//	ContentTypeFormats[format] = contentType;

		//	ContentTypeSerializers[contentType] = streamSerializer;
		//	ContentTypeDeserializers[contentType] = streamDeserializer;
		//}

		//public void Register(string contentType,
		//					 ResponseSerializerDelegate responseSerializer,
		//					 StreamDeserializerDelegate streamDeserializer)
		//{
		//	if (contentType.IsNullOrEmpty())
		//		throw new ArgumentNullException("contentType");

		//	string[] parts = contentType.Split('/');
		//	string format = parts[parts.Length - 1];
		//	ContentTypeFormats[format] = contentType;

		//	ContentTypeResponseSerializers[contentType] = responseSerializer;
		//	ContentTypeDeserializers[contentType] = streamDeserializer;
		//}

		public byte[] SerializeToBytes(IRequestContext requestContext, object response)
		{
			string contentType = requestContext.ResponseContentType;

			StreamSerializerDelegate responseStreamWriter;
			if (ContentTypeSerializers.TryGetValue(contentType, out responseStreamWriter) ||
			    ContentTypeSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseStreamWriter))
			{
				using (var ms = new MemoryStream())
				{
					responseStreamWriter(requestContext, response, ms);
					ms.Position = 0;
					return ms.ToArray();
				}
			}

			ResponseSerializerDelegate responseWriter;
			if (ContentTypeResponseSerializers.TryGetValue(contentType, out responseWriter) ||
			    ContentTypeResponseSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseWriter))
			{
				using (var ms = new MemoryStream())
				{
					var httpRes = new HttpResponseStreamWrapper(ms);
					responseWriter(requestContext, response, httpRes);
					ms.Position = 0;
					return ms.ToArray();
				}
			}

			//EndpointAttributes contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
			//switch (contentTypeAttr)
			//{
			//	case EndpointAttributes.Xml:
			//		return XmlSerializer.SerializeToString(response).ToUtf8Bytes();

			//	case EndpointAttributes.Json:
			//		return JsonDataContractSerializer.Instance.SerializeToString(response).ToUtf8Bytes();

			//	case EndpointAttributes.Jsv:
			//		return TypeSerializer.SerializeToString(response).ToUtf8Bytes();
			//}

			throw new NotSupportedException("ContentType not supported: " + contentType);
		}

		public string SerializeToString(IRequestContext requestContext, object response)
		{
			string contentType = requestContext.ResponseContentType;

			StreamSerializerDelegate responseStreamWriter;
			if (ContentTypeSerializers.TryGetValue(contentType, out responseStreamWriter) ||
			    ContentTypeSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseStreamWriter))
			{
				using (var ms = new MemoryStream())
				{
					responseStreamWriter(requestContext, response, ms);

					ms.Position = 0;
					string result = new StreamReader(ms, UTF8EncodingWithoutBom).ReadToEnd();
					return result;
				}
			}

			ResponseSerializerDelegate responseWriter;
			if (ContentTypeResponseSerializers.TryGetValue(contentType, out responseWriter) ||
			    ContentTypeResponseSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseWriter))
			{
				using (var ms = new MemoryStream())
				{
					var httpRes = new HttpResponseStreamWrapper(ms)
						{
							KeepOpen = true, //Don't let view engines close the OutputStream
						};
					responseWriter(requestContext, response, httpRes);

					byte[] bytes = ms.ToArray();
					string result = bytes.FromUtf8Bytes();

					httpRes.ForceClose(); //Manually close the OutputStream

					return result;
				}
			}


			//EndpointAttributes contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
			//switch (contentTypeAttr)
			//{
			//	case EndpointAttributes.Xml:
			//		return XmlSerializer.SerializeToString(response);

			//	case EndpointAttributes.Json:
			//		return JsonDataContractSerializer.Instance.SerializeToString(response);

			//	case EndpointAttributes.Jsv:
			//		return TypeSerializer.SerializeToString(response);
			//}

			throw new NotSupportedException("ContentType not supported: " + contentType);
		}

		public void SerializeToStream(IRequestContext requestContext, object response, Stream responseStream)
		{
			string contentType = requestContext.ResponseContentType;
			ResponseSerializerDelegate serializer = GetResponseSerializer(contentType);
			if (serializer == null)
				throw new NotSupportedException("ContentType not supported: " + contentType);

			var httpRes = new HttpResponseStreamWrapper(responseStream);
			serializer(requestContext, response, httpRes);
		}

		public void SerializeToResponse(IRequestContext requestContext, object response, IHttpResponse httpResponse)
		{
			string contentType = requestContext.ResponseContentType;
			ResponseSerializerDelegate serializer = GetResponseSerializer(contentType);
			if (serializer == null)
				throw new NotSupportedException("ContentType not supported: " + contentType);

			serializer(requestContext, response, httpResponse);
		}

		public ResponseSerializerDelegate GetResponseSerializer(string contentType)
		{
			ResponseSerializerDelegate responseWriter;
			if (ContentTypeResponseSerializers.TryGetValue(contentType, out responseWriter) ||
			    ContentTypeResponseSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseWriter))
			{
				return responseWriter;
			}

			StreamSerializerDelegate serializer = GetStreamSerializer(contentType);
			if (serializer == null) return null;

			return (httpReq, dto, httpRes) => serializer(httpReq, dto, httpRes.OutputStream);
		}

		public object DeserializeFromString(string contentType, Type type, string request)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				byte[] content = Encoding.UTF8.GetBytes(request);
				ms.Write(content,0,content.Length);
				ms.Seek(0, SeekOrigin.Begin);

				return DeserializeFromStream(contentType, type, ms);
			}
		}

		public object DeserializeFromStream(string contentType, Type type, Stream fromStream)
		{
			StreamDeserializerDelegate deserializer = GetStreamDeserializer(contentType);
			if (deserializer == null)
				throw new NotSupportedException("ContentType not supported: " + contentType);

			return deserializer(type, fromStream);
		}

		public StreamDeserializerDelegate GetStreamDeserializer(string contentType)
		{
			StreamDeserializerDelegate streamReader;
			string realContentType = contentType.Split(';')[0].Trim();
			if (ContentTypeDeserializers.TryGetValue(realContentType, out streamReader))
			{
				return streamReader;
			}

			//EndpointAttributes contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
			//switch (contentTypeAttr)
			//{
			//	case EndpointAttributes.Xml:
			//		return XmlSerializer.DeserializeFromStream;

			//	case EndpointAttributes.Json:
			//		return JsonDataContractDeserializer.Instance.DeserializeFromStream;

			//	case EndpointAttributes.Jsv:
			//		return TypeSerializer.DeserializeFromStream;
			//}

			return null;
		}

		private StreamSerializerDelegate GetStreamSerializer(string contentType)
		{
			StreamSerializerDelegate responseWriter;
			if (ContentTypeSerializers.TryGetValue(contentType, out responseWriter) ||
			    ContentTypeSerializers.TryGetValue(ContentType.GetRealContentType(contentType), out responseWriter))
			{
				return responseWriter;
			}

			//EndpointAttributes contentTypeAttr = ContentType.GetEndpointAttributes(contentType);
			//switch (contentTypeAttr)
			//{
			//	case EndpointAttributes.Xml:
			//		return (r, o, s) => XmlSerializer.SerializeToStream(o, s);

			//	case EndpointAttributes.Json:
			//		return (r, o, s) => JsonDataContractSerializer.Instance.SerializeToStream(o, s);

			//	case EndpointAttributes.Jsv:
			//		return (r, o, s) => TypeSerializer.SerializeToStream(o, s);
			//}

			return null;
		}
	}
}