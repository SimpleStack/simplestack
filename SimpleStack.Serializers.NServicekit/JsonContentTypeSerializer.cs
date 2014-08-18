using NServiceKit.Text;
using SimpleStack.Interfaces;

namespace SimpleStack.Serializers.NServicekit
{
	public class JsonContentTypeSerializer : IContentTypeSerializer
	{
		public string[] ContentTypes
		{
			get { return new[] {ContentType.JsonText, ContentType.Json}; }
		}

		public StreamDeserializerDelegate GetStreamDeserializer()
		{
			return JsonSerializer.DeserializeFromStream;
		}

		public StreamSerializerDelegate GetStreamSerializer()
		{
			return (requestContext, response, outputStream) => JsonSerializer.SerializeToStream(response, outputStream);
		}
	}
}
