using System;
using System.Collections.Generic;
using System.Net;
using SimpleStack.Enums;

namespace SimpleStack.Interfaces
{
	public interface IRequestContext : IDisposable
	{
		T Get<T>() where T : class;

		string IpAddress { get; }

		string GetHeader(string headerName);

		IDictionary<string, Cookie> Cookies { get; }

		EndpointAttributes EndpointAttributes { get; }

		IRequestAttributes RequestAttributes { get; }

		string ContentType { get; }

		string ResponseContentType { get; }

		string CompressionType { get; }

		string AbsoluteUri { get; }

		string PathInfo { get; }

		IFile[] Files { get; }
	}
}

