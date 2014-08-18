using NServiceKit.Text;
using SimpleStack;
using SimpleStack.Interfaces;

namespace SimpleStack.Serializers.NServicekit
{
	public class XmlContentTypeSerializer : IContentTypeSerializer
	{
		public string[] ContentTypes
		{
			get { return new[] {ContentType.XmlText, ContentType.Xml}; }
		}

		public StreamDeserializerDelegate GetStreamDeserializer()
		{
			return XmlSerializer.DeserializeFromStream;
		}

		public StreamSerializerDelegate GetStreamSerializer()
		{
			return (requestContext, response, outputStream) => XmlSerializer.SerializeToStream(response, outputStream);
		}
	}
}
