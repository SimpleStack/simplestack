using System;
using System.Threading.Tasks;
using SimpleStack.Enums;
using SimpleStack.Logging;
using SimpleStack.Interfaces;
using SimpleStack.Extensions;
using System.Runtime.Serialization;
using SimpleStack.Tools;

namespace SimpleStack.Handlers
{
	//TODO: vdaron - restore MINIROFILER
	public class RestHandler : EndpointHandlerBase
	{
		public RestHandler(IAppHost appHost)
			:base(appHost)
		{
			this.HandlerAttributes = EndpointAttributes.Reply;
		}

		private new static readonly ILog Log = Logger.CreateLog();

		public IRestPath FindMatchingRestPath(string httpMethod, string pathInfo)
		{
			var controller = AppHost.ServiceManager.ServiceController;

			return controller.GetRestPathForRequest(httpMethod, pathInfo);
		}

		public IRestPath GetRestPath(string httpMethod, string pathInfo)
		{
			if (this.RestPath == null)
			{
				this.RestPath = FindMatchingRestPath(httpMethod, pathInfo);
			}
			return this.RestPath;
		}

		public IRestPath RestPath { get; set; }

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			try
			{
				if (AppHost.ApplyPreRequestFilters(httpReq, httpRes)) 
					return;

				var restPath = GetRestPath(httpReq.HttpMethod, httpReq.PathInfo);
				if (restPath == null)
					throw new NotSupportedException("No RestPath found for: " + httpReq.HttpMethod + " " + httpReq.PathInfo);

				//operationName = restPath.RequestType.Name;

				var callback = httpReq.GetJsonpCallback();
				var doJsonp = AppHost.Config.AllowJsonpRequests
					&& !string.IsNullOrEmpty(callback);

				var responseContentType = httpReq.ResponseContentType;
				AppHost.Config.AssertContentType(responseContentType);

				var request = GetRequest(httpReq, restPath);
				if (AppHost.ApplyRequestFilters(httpReq, httpRes, request)) 
					return;

				var response = GetResponse(httpReq, httpRes, request);
				if (AppHost.ApplyResponseFilters(httpReq, httpRes, response)) 
					return;

				if (doJsonp && !(response is CompressedResult))
					httpRes.WriteToResponse(AppHost.Config, httpReq, response, (callback + "(").ToUtf8Bytes(), ")".ToUtf8Bytes());
				else
					httpRes.WriteToResponse(AppHost.Config, httpReq, response);
			}
			catch (Exception ex)
			{
				if (!AppHost.Config.WriteErrorsToResponse) throw;
				HandleException(httpReq, httpRes, operationName, ex);
			}
		}

		public override object GetResponse(IHttpRequest httpReq, IHttpResponse httpRes, object request)
		{
			var requestContentType = ContentType.GetEndpointAttributes(httpReq.ResponseContentType);

			return ExecuteService(request,
				HandlerAttributes | requestContentType | httpReq.GetAttributes(), httpReq, httpRes);
		}

		private object GetRequest(IHttpRequest httpReq, IRestPath restPath)
		{
			var requestType = restPath.RequestType;

			//using (Profiler.Current.Step("Deserialize Request"))
			{
				try
				{
					var requestDto = GetCustomRequestFromBinder(httpReq, requestType);
					if (requestDto != null) return requestDto;

					var requestParams = httpReq.GetRequestParams();
					requestDto = CreateContentTypeRequest(httpReq, requestType, httpReq.ContentType);

					return restPath.CreateRequest(httpReq.PathInfo, requestParams, requestDto);
				}
				catch (SerializationException e)
				{
					throw new RequestBindingException("Unable to bind request", e);
				}
				catch (ArgumentException e)
				{
					throw new RequestBindingException("Unable to bind request", e);
				}
			}
		}

		/// <summary>
		/// Used in Unit tests
		/// </summary>
		/// <returns></returns>
		public override object CreateRequest(IHttpRequest httpReq, string operationName)
		{
			if (this.RestPath == null)
				throw new ArgumentNullException("No RestPath found");

			return GetRequest(httpReq, this.RestPath);
		}
	}
}

