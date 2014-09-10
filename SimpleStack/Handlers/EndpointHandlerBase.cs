using System;
using System.Collections.Generic;
using SimpleStack.Logging;
using SimpleStack.Extensions;
using SimpleStack.Interfaces;
using SimpleStack.Enums;
using SimpleStack.Serializers;
using System.Runtime.Serialization;
using System.Net;
using System.ServiceModel;

namespace SimpleStack.Handlers
{
	public abstract class EndpointHandlerBase : ISimpleStackHttpHandler//, IHttpHandler
	{
		private readonly IAppHost _appHost;
		internal static readonly ILog Log = Logger.CreateLog ();
		internal static readonly Dictionary<byte[], byte[]> NetworkInterfaceIpv4Addresses = new Dictionary<byte[], byte[]> ();
		internal static readonly byte[][] NetworkInterfaceIpv6Addresses = new byte[0][];

		public string RequestName { get; set; }

		protected IAppHost AppHost
		{
			get { return _appHost; }
		}

		static EndpointHandlerBase ()
		{
			try {
				IPAddressExtensions.GetAllNetworkInterfaceIpv4Addresses ().ForEach((x, y) => NetworkInterfaceIpv4Addresses [x.GetAddressBytes ()] = y.GetAddressBytes ());

				NetworkInterfaceIpv6Addresses = IPAddressExtensions.GetAllNetworkInterfaceIpv6Addresses ().ConvertAll (x => x.GetAddressBytes ()).ToArray ();
			} catch (Exception ex) {
				Log.Warn ("Failed to retrieve IP Addresses, some security restriction features may not work: " + ex.Message, ex);
			}
		}

		protected EndpointHandlerBase(IAppHost appHost)
		{
			_appHost = appHost;
		}

		public EndpointAttributes HandlerAttributes { get; set; }

		public bool IsReusable {
			get { return false; }
		}

		public abstract object CreateRequest (IHttpRequest request, string operationName);

		public abstract object GetResponse (IHttpRequest httpReq, IHttpResponse httpRes, object request);

		public abstract void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName);

		public object DeserializeHttpRequest (Type operationType, IHttpRequest httpReq, string contentType)
		{
			var httpMethod = httpReq.HttpMethod;
			var queryString = httpReq.QueryString;

			if (httpMethod == HttpMethods.Get || httpMethod == HttpMethods.Delete || httpMethod == HttpMethods.Options) {
				try {
					return KeyValueDataContractDeserializer.Instance.Parse (queryString, operationType);
				} catch (Exception ex) {
					var msg = "Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}'.\nError: '{2}'"
							.Fmt (operationType, queryString, ex);
					throw new SerializationException (msg);
				}
			}

			var isFormData = httpReq.HasAnyOfContentTypes(ContentType.FormUrlEncoded, ContentType.MultiPartFormData);
			if (isFormData) {
				try {
					return KeyValueDataContractDeserializer.Instance.Parse (httpReq.FormData, operationType);
				} catch (Exception ex) {
					throw new SerializationException ("Error deserializing FormData: " + httpReq.FormData, ex);
				}
			}

			var request = CreateContentTypeRequest (httpReq, operationType, contentType);
			return request;
		}

		protected object CreateContentTypeRequest (IHttpRequest httpReq, Type requestType, string contentType)
		{
			try {
				if (!string.IsNullOrEmpty (contentType) && httpReq.ContentLength > 0) {
					var deserializer = _appHost.ContentTypeFilters.GetStreamDeserializer (contentType);
					if (deserializer != null) {
						return deserializer (requestType, httpReq.InputStream);
					}
				}
			} catch (Exception ex) {
				var msg = "Could not deserialize '{0}' request using {1}'\nError: {2}"
						.Fmt (contentType, requestType, ex);
				throw new SerializationException (msg);
			}
			return requestType.CreateInstance (); //Return an empty DTO, even for empty request bodies
		}

		protected object GetCustomRequestFromBinder (IHttpRequest httpReq, Type requestType)
		{
			Func<IHttpRequest, object> requestFactoryFn;
			_appHost.ServiceManager.ServiceController.RequestTypeFactoryMap.TryGetValue(requestType, out requestFactoryFn);

			return requestFactoryFn != null ? requestFactoryFn (httpReq) : null;
		}

//			protected static bool DefaultHandledRequest(HttpListenerContext context)
//			{
//				return false;
//			}
//
//			protected static bool DefaultHandledRequest(HttpContext context)
//			{
//				return false;
//			}

//		public virtual void ProcessRequest (Dictionary<string,string> context)
//		{
//			var operationName = this.RequestName ?? context.Request.GetOperationName ();
//
//			if (string.IsNullOrEmpty (operationName))
//				return;
//
//			//if (DefaultHandledRequest(context)) return;
//
//			ProcessRequest (
//				new HttpRequestWrapper (operationName, context.Request),
//				new HttpResponseWrapper (context.Response),
//				operationName);
//		}
//
//		public virtual void ProcessRequest(HttpListenerContext context)
//		{
//			var operationName = this.RequestName ?? context.Request.GetOperationName();
//
//			if (string.IsNullOrEmpty(operationName)) return;
//
//			if (DefaultHandledRequest(context)) return;
//
//			ProcessRequest(
//				new HttpListenerRequestWrapper(operationName, context.Request),
//				new HttpListenerResponseWrapper(context.Response),
//				operationName);
//		}

		//public ServiceManager ServiceManager { get; set; }

		public Type GetOperationType (string operationName)
		{
			return _appHost.Metadata.GetOperationType(operationName);
		}

		protected object ExecuteService(object request,
		                                EndpointAttributes endpointAttributes,
		                                IHttpRequest httpReq,
		                                IHttpResponse httpRes)
		{
			return _appHost.ServiceManager.ServiceController.Execute(request,
			                                                         new HttpRequestContext(httpReq, httpRes, request,
			                                                                                endpointAttributes));
		}

		public EndpointAttributes GetEndpointAttributes (OperationContext operationContext)
		{
			if (!_appHost.Config.EnableAccessRestrictions)
				return default(EndpointAttributes);

			var portRestrictions = default(EndpointAttributes);
			var ipAddress = GetIpAddress (operationContext);

			portRestrictions |= EndpointAttributesExtensions.GetAttributes (ipAddress);

			//TODO: work out if the request was over a secure channel
			//portRestrictions |= request.IsSecureConnection ? PortRestriction.Secure : PortRestriction.InSecure;

			return portRestrictions;
		}

		public static IPAddress GetIpAddress (OperationContext context)
		{
			#if !MONO
			var prop = context.IncomingMessageProperties;
			if (context.IncomingMessageProperties.ContainsKey (System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name)) {
				var endpoint = prop [System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name]
						as System.ServiceModel.Channels.RemoteEndpointMessageProperty;
				if (endpoint != null) {
					return IPAddress.Parse (endpoint.Address);
				}
			}
			#endif
			return null;
		}

		protected static void AssertOperationExists (string operationName, Type type)
		{
			if (type == null) {
				throw new NotImplementedException (
					string.Format ("The operation '{0}' does not exist for this service", operationName));
			}
		}

		protected void HandleException (IHttpRequest httpReq, IHttpResponse httpRes, string operationName, Exception ex)
		{
			var errorMessage = string.Format ("Error occured while Processing Request: {0}", ex.Message);
			Log.Error (errorMessage, ex);

			try {
				_appHost.ExceptionHandler (httpReq, httpRes, operationName, ex);
			} catch (Exception writeErrorEx) {
				//Exception in writing to response should not hide the original exception
				Log.Info ("Failed to write error to response: {0}", writeErrorEx);
				//rethrow the original exception
				throw ex;
			} finally {
				httpRes.EndServiceStackRequest(skipHeaders: true);
			}
		}

		protected bool AssertAccess (IHttpRequest httpReq, IHttpResponse httpRes, Feature feature, string operationName)
		{
			if (operationName == null)
				throw new ArgumentNullException ("operationName");

			if (_appHost.Config.EnableFeatures != Feature.All) {
				if (!_appHost.Config.HasFeature(feature))
				{
					_appHost.Config.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Feature Not Available");
					return false;
				}
			}

			var format = feature.ToFormat ();
			if (!_appHost.Metadata.CanAccess(httpReq, format, operationName))
			{
				_appHost.Config.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Service Not Available");
				return false;
			}
			return true;
		}
	}
}

