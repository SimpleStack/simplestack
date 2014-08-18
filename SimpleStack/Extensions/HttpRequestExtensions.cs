using System;
using SimpleStack.Interfaces;
using System.Net;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SimpleStack.Tools;

namespace SimpleStack.Extensions
{
	public static class HttpRequestExtensions
	{
		public static Dictionary<string, string> GetRequestParams(this IHttpRequest request)
		{
			var map = new Dictionary<string, string>();

			foreach (var name in request.QueryString.AllKeys)
			{
				if (name == null) continue; //thank you ASP.NET
				map[name] = request.QueryString[name];
			}

			if ((request.HttpMethod == HttpMethods.Post || request.HttpMethod == HttpMethods.Put)
				&& request.FormData != null)
			{
				foreach (var name in request.FormData.AllKeys)
				{
					if (name == null) continue; //thank you ASP.NET
					map[name] = request.FormData[name];
				}
			}

			return map;
		}

		public static bool IsContentType(this IHttpRequest request, string contentType)
		{
			return request.ContentType.StartsWith (contentType, StringComparison.InvariantCultureIgnoreCase);
		}

		public static bool HasAnyOfContentTypes(this IHttpRequest request, params string[] contentTypes)
		{
			if (contentTypes == null || request.ContentType == null)
				return false;
			foreach (var contentType in contentTypes) {
				if (IsContentType (request, contentType))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Gets string value from Items[name] then Cookies[name] if exists.
		/// Useful when *first* setting the users response cookie in the request filter.
		/// To access the value for this initial request you need to set it in Items[].
		/// </summary>
		/// <returns>string value or null if it doesn't exist</returns>
		public static string GetItemOrCookie(this IHttpRequest httpReq, string name)
		{
			object value;
			if (httpReq.Items.TryGetValue(name, out value)) return value.ToString();

			Cookie cookie;
			if (httpReq.Cookies.TryGetValue(name, out cookie)) return cookie.Value;

			return null;
		}

		/// <summary>
		/// Gets request paramater string value by looking in the following order:
		/// - QueryString[name]
		/// - FormData[name]
		/// - Cookies[name]
		/// - Items[name]
		/// </summary>
		/// <returns>string value or null if it doesn't exist</returns>
		public static string GetParam(this IHttpRequest httpReq, string name)
		{
			string value;
			if ((value = httpReq.Headers[HttpHeaders.XParamOverridePrefix + name]) != null) return value;
			if ((value = httpReq.QueryString[name]) != null) return value;
			if ((value = httpReq.FormData[name]) != null) return value;

			//IIS will assign null to params without a name: .../?some_value can be retrieved as req.Params[null]
			//TryGetValue is not happy with null dictionary keys, so we should bail out here
			if (string.IsNullOrEmpty(name)) return null;

			Cookie cookie;
			if (httpReq.Cookies.TryGetValue(name, out cookie)) return cookie.Value;

			object oValue;
			if (httpReq.Items.TryGetValue(name, out oValue)) return oValue.ToString();

			return null;
		}

		public static string GetParentAbsolutePath(this IHttpRequest httpReq)
		{
			return httpReq.GetAbsolutePath().ToParentPath();
		}

		public static string GetAbsolutePath(this IHttpRequest httpReq)
		{
			var resolvedPathInfo = httpReq.PathInfo;

			var pos = httpReq.RawUrl.IndexOf(resolvedPathInfo, StringComparison.InvariantCultureIgnoreCase);
			if (pos == -1)
				throw new ArgumentException(
					String.Format("PathInfo '{0}' is not in Url '{1}'", resolvedPathInfo, httpReq.RawUrl));

			return httpReq.RawUrl.Substring(0, pos + resolvedPathInfo.Length);
		}

		public static string GetParentPathUrl(this IHttpRequest httpReq)
		{
			return httpReq.GetPathUrl().ToParentPath();
		}

		public static string GetPathUrl(this IHttpRequest httpReq)
		{
			var resolvedPathInfo = httpReq.PathInfo;

			var pos = resolvedPathInfo == String.Empty
				? httpReq.AbsoluteUri.Length
				: httpReq.AbsoluteUri.IndexOf(resolvedPathInfo, StringComparison.InvariantCultureIgnoreCase);

			if (pos == -1)
				throw new ArgumentException(
					String.Format("PathInfo '{0}' is not in Url '{1}'", resolvedPathInfo, httpReq.RawUrl));

			return httpReq.AbsoluteUri.Substring(0, pos + resolvedPathInfo.Length);
		}

		public static string GetUrlHostName(this IHttpRequest httpReq)
		{
//			var aspNetReq = httpReq as HttpRequestWrapper;
//			if (aspNetReq != null)
//			{
//				return aspNetReq.UrlHostName;
//			}
			var uri = httpReq.AbsoluteUri;

			var pos = uri.IndexOf("://") + "://".Length;
			var partialUrl = uri.Substring(pos);
			var endPos = partialUrl.IndexOf('/');
			if (endPos == -1) endPos = partialUrl.Length;
			var hostName = partialUrl.Substring(0, endPos).Split(':')[0];
			return hostName;
		}

		public static string GetPhysicalPath(this IHttpRequest httpReq)
		{
//			var aspNetReq = httpReq as HttpRequestWrapper;
//			var res = aspNetReq != null 
//				? aspNetReq.Request.PhysicalPath 
//				: EndpointHostConfig.Instance.WebHostPhysicalPath.CombineWith(httpReq.PathInfo);
//
//			return res;
			return EndpointHostConfig.Instance.WebHostPhysicalPath.CombineWith(httpReq.PathInfo);
		}

//		public static string GetApplicationUrl(this HttpRequest httpReq)
//		{
//			var appPath = httpReq.ApplicationPath;
//			var baseUrl = httpReq.Url.Scheme + "://" + httpReq.Url.Host;
//			if (httpReq.Url.Port != 80) baseUrl += ":" + httpReq.Url.Port;
//			var appUrl = baseUrl.CombineWith(appPath);
//			return appUrl;
//		}

		public static string GetApplicationUrl(this IHttpRequest httpReq)
		{
			var url = new Uri(httpReq.AbsoluteUri);
			var baseUrl = url.Scheme + "://" + url.Host;
			if (url.Port != 80) baseUrl += ":" + url.Port;
			var appUrl = baseUrl.CombineWith(EndpointHost.Config.ServiceStackHandlerFactoryPath);
			return appUrl;
		}

		public static string GetHttpMethodOverride(this IHttpRequest httpReq)
		{
			var httpMethod = httpReq.HttpMethod;

			if (httpMethod != HttpMethods.Post)
				return httpMethod;			

			var overrideHttpMethod = 
				httpReq.Headers[HttpHeaders.XHttpMethodOverride].ToNullIfEmpty()
				?? httpReq.FormData[HttpHeaders.XHttpMethodOverride].ToNullIfEmpty()
				?? httpReq.QueryString[HttpHeaders.XHttpMethodOverride].ToNullIfEmpty();

			if (overrideHttpMethod != null)
			{
				if (overrideHttpMethod != HttpMethods.Get && overrideHttpMethod != HttpMethods.Post)
					httpMethod = overrideHttpMethod;
			}

			return httpMethod;
		}

		public static string GetFormatModifier(this IHttpRequest httpReq)
		{
			var format = httpReq.QueryString["format"];
			if (format == null) return null;
			var parts = format.SplitOnFirst('.');
			return parts.Length > 1 ? parts[1] : null;
		}

		public static bool HasNotModifiedSince(this IHttpRequest httpReq, DateTime? dateTime)
		{
			if (!dateTime.HasValue) return false;
			var strHeader = httpReq.Headers[HttpHeaders.IfModifiedSince];
			try
			{
				if (strHeader != null)
				{
					var dateIfModifiedSince = DateTime.ParseExact(strHeader, "r", null);
					var utcFromDate = dateTime.Value.ToUniversalTime();
					//strip ms
					utcFromDate = new DateTime(
						utcFromDate.Ticks - (utcFromDate.Ticks % TimeSpan.TicksPerSecond),
						utcFromDate.Kind
					);

					return utcFromDate <= dateIfModifiedSince;
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		public static bool DidReturn304NotModified(this IHttpRequest httpReq, DateTime? dateTime, IHttpResponse httpRes)
		{
			if (httpReq.HasNotModifiedSince(dateTime))
			{
				httpRes.StatusCode = (int) HttpStatusCode.NotModified;
				return true;
			}
			return false;
		}

		public static string GetJsonpCallback(this IHttpRequest httpReq)
		{
			return httpReq == null ? null : httpReq.QueryString["callback"];
		}

		public static Dictionary<string, string> CookiesAsDictionary(this IHttpRequest httpReq)
		{
			var map = new Dictionary<string, string>();
//			var aspNet = httpReq.OriginalRequest as HttpRequest;
//			if (aspNet != null)
//			{
//				foreach (var name in aspNet.Cookies.AllKeys)
//				{
//					var cookie = aspNet.Cookies[name];
//					if (cookie == null) continue;
//					map[name] = cookie.Value;
//				}
//			}
//			else
//			{
//				var httpListener = httpReq.OriginalRequest as HttpListenerRequest;
//				if (httpListener != null)
//				{
//					for (var i = 0; i < httpListener.Cookies.Count; i++)
//					{
//						var cookie = httpListener.Cookies[i];
//						if (cookie == null || cookie.Name == null) continue;
//						map[cookie.Name] = cookie.Value;
//					}
//				}
//			}

			foreach(var cookie in httpReq.Cookies) {
				if (cookie.Value == null) continue;
				map.Add(cookie.Key, cookie.Value.Value);
			}

			return map;
		}

		public static int ToStatusCode(this Exception ex)
		{
			int errorStatus;
			if (EndpointHost.Config != null && EndpointHost.Config.MapExceptionToStatusCode.TryGetValue(ex.GetType(), out errorStatus))
			{
				return errorStatus;
			}

			//if (ex is HttpError) return ((HttpError)ex).Status;
			if (ex is NotImplementedException || ex is NotSupportedException) return (int)HttpStatusCode.MethodNotAllowed;
			if (ex is ArgumentException || ex is SerializationException) return (int)HttpStatusCode.BadRequest;
			if (ex is UnauthorizedAccessException) return (int) HttpStatusCode.Forbidden;
			return (int)HttpStatusCode.InternalServerError;
		}

		public static string ToErrorCode(this Exception ex)
		{
			if (ex is HttpError)
				return ((HttpError)ex).ErrorCode;
			return ex.GetType().Name;
		}

		public static string GetResponseContentType(this IHttpRequest httpReq)
		{
			var specifiedContentType = GetQueryStringContentType(httpReq);

			if (!String.IsNullOrEmpty(specifiedContentType))
				return specifiedContentType;

			var acceptContentTypes = httpReq.AcceptTypes;

			//By default use same output format as the request (if any)
			var defaultContentType = httpReq.ContentType;

			//Check if request is a form submission
			if (httpReq.HasAnyOfContentTypes(ContentType.FormUrlEncoded, ContentType.MultiPartFormData))
			{
				defaultContentType = EndpointHost.Config.DefaultContentType;
			}

			var availableContentTypes = EndpointHost.ContentTypeFilter.ContentTypeFormats.Values;

			var acceptsAnything = false;

			if (acceptContentTypes != null)
			{
				foreach (var contentType in acceptContentTypes)
				{
					acceptsAnything = acceptsAnything || contentType == "*/*";

					foreach (var customContentTypeValues in availableContentTypes)
					{
						foreach (var customContentType in customContentTypeValues)
						{
							if (contentType.StartsWith(customContentType))
								return customContentType;
						}
					}
				}

				if (acceptsAnything && !String.IsNullOrEmpty(defaultContentType))
					return defaultContentType;
			}

			//We could also send a '406 Not Acceptable', but this is allowed also
			return defaultContentType;
		}

		public static string GetQueryStringContentType(this IHttpRequest httpReq)
		{
			var callback = httpReq.QueryString["callback"];
			if (!String.IsNullOrEmpty(callback))
				return ContentType.Json;

			var format = httpReq.QueryString["format"];
			if (format == null)
			{
				const int formatMaxLength = 4;
				var pi = httpReq.PathInfo;
				if (pi == null || pi.Length <= formatMaxLength)
					return null;
				if (pi[0] == '/')
					pi = pi.Substring(1);

				format = pi.SplitOnFirst('/')[0];
				if (format.Length > formatMaxLength)
					return null;
			}

			format = format.SplitOnFirst('.')[0].ToLower();
			if (format.Contains("json"))
				return ContentType.Json;
			if (format.Contains("xml"))
				return ContentType.Xml;
			if (format.Contains("jsv"))
				return ContentType.Jsv;

			List<string> contentType;

			EndpointHost.ContentTypeFilter.ContentTypeFormats.TryGetValue(format, out contentType);

			return contentType != null ? contentType[0] : null;
		}

		//public static string[] PreferredContentTypes = new[] {
		//	ContentType.Html,
		//	ContentType.Json,
		//	ContentType.Xml,
		//	ContentType.Jsv
		//};

	}
}
