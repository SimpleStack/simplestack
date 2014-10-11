using System;
using SimpleStack.Interfaces;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace SimpleStack
{
	public class HttpResponseStreamWrapper : IHttpResponse
	{
		private static readonly UTF8Encoding UTF8EncodingWithoutBom = new UTF8Encoding(false);

		public HttpResponseStreamWrapper(IAppHost appHost, Stream stream)
		{
			if (appHost == null)
				throw new ArgumentNullException("appHost");
			if (stream == null)
				throw new ArgumentNullException("stream");

			AppHost = appHost;
			this.OutputStream = stream;
			this.Headers = new Dictionary<string, string>();
		}

		public Dictionary<string, string> Headers { get; set; }

		public object OriginalResponse
		{
			get { return null; }
		}

		public int StatusCode { set; get; }
		public string StatusDescription { set; get; }
		public string ContentType { get; set; }
		public bool KeepOpen { get; set; }

		public ICookies Cookies { get; set; }

		public void AddHeader(string name, string value)
		{
			this.Headers[name] = value;
		}

		public void Redirect(string url)
		{
			this.Headers[HttpHeaders.Location] = url;
		}

		public Stream OutputStream { get; private set; }

		public void Write(string text)
		{
			var bytes = UTF8EncodingWithoutBom.GetBytes(text);
			OutputStream.Write(bytes, 0, bytes.Length);
		}

		public void Close()
		{
			if (KeepOpen) return;
			ForceClose();
		}

		public void ForceClose()
		{
			if (IsClosed) return;

			OutputStream.Close();
			IsClosed = true;
		}

		public void End()
		{
			Close();
		}

		public void Flush()
		{
			OutputStream.Flush();
		}

		public bool IsClosed { get; private set; }

		public void SetContentLength(long contentLength) {}
		public IAppHost AppHost { get; private set; }
	}
}

