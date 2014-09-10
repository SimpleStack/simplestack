using System;
using System.Threading.Tasks;
using SimpleStack.Interfaces;
using System.Net;
using SimpleStack.Extensions;

namespace SimpleStack.Handlers
{
	public class RedirectHttpHandler: ISimpleStackHttpHandler
	{
		private readonly IAppHost _appHost;
		public string RelativeUrl { get; set; }

		public string AbsoluteUrl { get; set; }

		public RedirectHttpHandler(IAppHost appHost)
		{
			_appHost = appHost;
		}

		/// <summary>
		/// Non ASP.NET requests
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <param name="operationName"></param>
		public void ProcessRequest(IHttpRequest request, IHttpResponse response, string operationName)
		{
			if (string.IsNullOrEmpty(RelativeUrl) && string.IsNullOrEmpty(AbsoluteUrl))
				throw new ArgumentNullException("RelativeUrl or AbsoluteUrl");

			if (!string.IsNullOrEmpty(AbsoluteUrl))
			{
				response.StatusCode = (int)HttpStatusCode.Redirect;
				response.AddHeader(HttpHeaders.Location, this.AbsoluteUrl);
			}
			else
			{
				var absoluteUrl = GetApplicationUrl(request);
				if (!string.IsNullOrEmpty(RelativeUrl))
				{
					if (this.RelativeUrl.StartsWith("/"))
						absoluteUrl = absoluteUrl.CombineWith(this.RelativeUrl);
					else if (this.RelativeUrl.StartsWith("~/"))
						absoluteUrl = absoluteUrl.CombineWith(this.RelativeUrl.Replace("~/", ""));
					else
						absoluteUrl = request.AbsoluteUri.CombineWith(this.RelativeUrl);
				}
				response.StatusCode = (int)HttpStatusCode.Redirect;
				response.AddHeader(HttpHeaders.Location, absoluteUrl);
			}

			response.EndHttpRequest(skipClose:true);
		}

		private string GetApplicationUrl(IHttpRequest httpReq)
		{
			var url = new Uri(httpReq.AbsoluteUri);
			var baseUrl = url.Scheme + "://" + url.Host;
			if (url.Port != 80)
				baseUrl += ":" + url.Port;
			var appUrl = baseUrl.CombineWith(_appHost.Config.SimpleStackHandlerFactoryPath);
			return appUrl;
		}

		/// <summary>
		/// ASP.NET requests
		/// </summary>
		/// <param name="context"></param>
//		public void ProcessRequest(HttpContext context)
//		{
//			var request = context.Request;
//			var response = context.Response;
//
//			if (string.IsNullOrEmpty(RelativeUrl) && string.IsNullOrEmpty(AbsoluteUrl))
//				throw new ArgumentNullException("RelativeUrl or AbsoluteUrl");
//
//			if (!string.IsNullOrEmpty(AbsoluteUrl))
//			{
//				response.StatusCode = (int)HttpStatusCode.Redirect;
//				response.AddHeader(HttpHeaders.Location, this.AbsoluteUrl);
//			}
//			else 
//			{
//				var absoluteUrl = request.GetApplicationUrl();
//				if (!string.IsNullOrEmpty(RelativeUrl))
//				{
//					if (this.RelativeUrl.StartsWith("/"))
//						absoluteUrl = absoluteUrl.CombineWith(this.RelativeUrl);
//					else if (this.RelativeUrl.StartsWith("~/"))
//						absoluteUrl = absoluteUrl.CombineWith(this.RelativeUrl.Replace("~/", ""));
//					else
//						absoluteUrl = request.Url.AbsoluteUri.CombineWith(this.RelativeUrl);
//				}
//				response.StatusCode = (int)HttpStatusCode.Redirect;
//				response.AddHeader(HttpHeaders.Location, absoluteUrl);
//			}
//
//			response.EndHttpRequest(closeOutputStream:true);
//		}

		public bool IsReusable
		{
			get { return false; }
		}
	}
}

