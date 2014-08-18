using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleStack.Interfaces;
using SimpleStack.Serializers.NServicekit;

namespace SimpleStack.Tests
{
	class MockOwinEnv : Dictionary<string, object>, IDisposable
	{
		private readonly string _verb;
		private readonly string _path;
		private readonly string _queryString;
		private readonly string _accept;
		private readonly string _body;
		private readonly string _contentType;

		private readonly TestStream _outputStream = new TestStream();

		public MockOwinEnv(string verb, 
		                   string path, 
		                   string queryString = null,
		                   string accept = ContentType.Json,
		                   string body = null, 
		                   string contentType = ContentType.Json)
		{
			_verb = verb;
			_path = path;
			_queryString = queryString;
			_accept = accept;
			_body = body;
			_contentType = contentType;
			Stream bodyStream = _body != null ? new MemoryStream() : Stream.Null;

			var headers = new Dictionary<string, string[]>
				{
					{"Accept", new[] {_accept}},
					{"Accept-Encoding", new[] {"gzip", "deflate"}},
					{"Accept-Language", new[] {"fr,fr-fr;q=0.8,en-us;q=0.5,en;q=0.3"}},
					{"Host", new[] {"hostname"}},
					{"User-Agent", new[] {"Mozilla/5.0 (X11; Linux x86_64; rv:31.0) Gecko/20100101 Firefox/31.0"}}
				};

			if (_body != null)
			{
				headers.Add("Content-Type", new[] {_contentType});
				byte[] bs = Encoding.UTF8.GetBytes(_body);
				bodyStream.Write(bs, 0, bs.Length);
				bodyStream.Seek(0, SeekOrigin.Begin);
			}

			Add("owin.RequestBody", bodyStream);
			Add("owin.RequestHeaders", headers);
			Add("owin.RequestMethod", _verb);
			Add("owin.RequestPath", _path);
			Add("owin.RequestPathBase", String.Empty);
			Add("owin.RequestProtocol", "HTTP/1.1");
			Add("owin.RequestQueryString", _queryString);
			Add("owin.RequestScheme", "http");
			Add("owin.ResponseBody", _outputStream);
			Add("owin.ResponseHeaders", new Dictionary<string, string[]>());
		}

		public string GetResponseBodyAsText()
		{
			_outputStream.Seek(0, SeekOrigin.Begin);

			byte[] resultBytes = new byte[_outputStream.Length];
			_outputStream.Read(resultBytes, 0, resultBytes.Length);

			return Encoding.UTF8.GetString(resultBytes);
		}

		public T GetResponseBodyAs<T>()
		{
			_outputStream.Seek(0, SeekOrigin.Begin);

			IContentTypeSerializer serializer;
			switch (GetResponseContentType())
			{
				case ContentType.JsonText:
				case ContentType.Json:
					serializer = new JsonContentTypeSerializer();
					break;
				case ContentType.Xml:
				case ContentType.XmlText:
					serializer = new XmlContentTypeSerializer();
					break;
				default:
					throw new Exception("Invalid ContentType");
			}

			return (T) serializer.GetStreamDeserializer()(typeof (T), _outputStream);
		}

		private string GetResponseContentType()
		{
			Dictionary<string, string[]> headers = (Dictionary<string, string[]>) base["owin.ResponseHeaders"];
			string contentType = headers[HttpHeaders.ContentType][0];

			if (contentType.StartsWith(ContentType.Json) ||
			    contentType.StartsWith(ContentType.JsonText))
			{
				return ContentType.Json;
			}

			if (contentType.StartsWith(ContentType.Xml) ||
			    contentType.StartsWith(ContentType.XmlText))
			{
				return ContentType.Xml;
			}
			return ContentType.PlainText;
		}

		public void Dispose()
		{
			_outputStream.Terminate();
		}
	}
}
