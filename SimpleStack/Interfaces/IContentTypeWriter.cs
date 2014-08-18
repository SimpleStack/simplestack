using System;
using System.IO;

namespace SimpleStack.Interfaces
{
	public interface IContentTypeWriter
	{
		byte[] SerializeToBytes(IRequestContext requestContext, object response);

		string SerializeToString(IRequestContext requestContext, object response);

		void SerializeToStream(IRequestContext requestContext, object response, Stream toStream);

		void SerializeToResponse(IRequestContext requestContext, object response, IHttpResponse httpRes);
	}
}

