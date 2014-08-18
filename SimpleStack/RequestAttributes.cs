﻿using System;
using SimpleStack.Interfaces;

namespace SimpleStack
{
	public class RequestAttributes : IRequestAttributes
	{
//		private readonly HttpContext httpContext;

		public RequestAttributes(IHttpRequest httpRequest)
		{
			this.acceptEncoding = httpRequest.Headers[HttpHeaders.AcceptEncoding];
			if (String.IsNullOrEmpty(acceptEncoding))
			{
				this.acceptEncoding = "none";
				return;
			}
			this.acceptEncoding = this.acceptEncoding.ToLower();
		}

//		public RequestAttributes(HttpContext httpContext)
//		{
//			this.httpContext = httpContext;
//		}

//		public static HttpWorkerRequest GetWorker(HttpContext context)
//		{
//			var provider = (IServiceProvider)context;
//			var worker = (HttpWorkerRequest)provider.GetService(typeof(HttpWorkerRequest));
//			return worker;
//		}

//		private HttpWorkerRequest httpWorkerRequest;
//		public HttpWorkerRequest HttpWorkerRequest
//		{
//			get
//			{
//				if (this.httpWorkerRequest == null)
//				{
//					this.httpWorkerRequest = GetWorker(this.httpContext);
//				}
//				return this.httpWorkerRequest;
//			}
//		}

		private string acceptEncoding;
		public string AcceptEncoding
		{
			get
			{
				//Allways false if not ASP.NET

//				if (acceptEncoding == null)
//				{
//					acceptEncoding = HttpWorkerRequest.GetKnownRequestHeader(HttpWorkerRequest.HeaderAcceptEncoding);
//					if (acceptEncoding != null) acceptEncoding = acceptEncoding.ToLower();
//				}
				return acceptEncoding;
			}
		}

		public bool AcceptsGzip
		{
			get
			{
				return AcceptEncoding != null && AcceptEncoding.Contains("gzip");
			}
		}

		public bool AcceptsDeflate
		{
			get
			{
				return AcceptEncoding != null && AcceptEncoding.Contains("deflate");
			}
		}

	}
}

