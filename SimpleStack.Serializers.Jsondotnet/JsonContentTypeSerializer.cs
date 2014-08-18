using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SimpleStack.Interfaces;

namespace SimpleStack.Serializers.Jsondotnet
{
	public class JsonContentTypeSerializer : IContentTypeSerializer
	{
		public JsonContentTypeSerializer()
		{
			SerializerSettings = new JsonSerializerSettings();
			Encoding = Encoding.UTF8;
		}

		public Encoding Encoding { get; private set; }

		public JsonSerializerSettings SerializerSettings { get; private set; }

		public string[] ContentTypes
		{
			get { return new[] { ContentType.JsonText, ContentType.Json }; }
		}

		public StreamDeserializerDelegate GetStreamDeserializer()
		{
			//TODO: check request ContentType for correct encoding
			return (type, fromStream) =>
				{
					byte[] content = new byte[fromStream.Length];
					fromStream.Read(content, 0, content.Length);
					string stringValue = Encoding.GetString(content);
					return JsonConvert.DeserializeObject(stringValue, type, SerializerSettings);
				};
		}

		public StreamSerializerDelegate GetStreamSerializer()
		{
			//TODO: check request ContentType for correct encoding
			return (requestContext, response, outputStream) =>
				{
					string value = JsonConvert.SerializeObject(response, SerializerSettings);
					byte[] tmp = Encoding.GetBytes(value);
					outputStream.Write(tmp,0,tmp.Length);
				};
		}
	}
}
