using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using SimpleStack.Interfaces;
using SimpleStack.Logging;
using SimpleStack.Attributes;
using SimpleStack.Enums;
using SimpleStack.Extensions;
using SimpleStack.Serializers;
//using ServiceStack.Logging;
//using ServiceStack.Messaging;
//using ServiceStack.ServiceModel.Serialization;
//using ServiceStack.Text;
//using ServiceStack.WebHost.Endpoints;

namespace SimpleStack
{
	public delegate object ServiceExecFn(IRequestContext requestContext, object request);
	public delegate object InstanceExecFn(IRequestContext requestContext, object intance, object request);
	public delegate object ActionInvokerFn(object intance, object request);
	public delegate void VoidActionInvokerFn(object intance, object request);

	public class ServiceController : IServiceController
	{
		private readonly IAppHost _appHost;
		private static readonly ILog Log = Logger.CreateLog();
		private const string ResponseDtoSuffix = "Response";

		//private IResolver _resolver;
		readonly Dictionary<Type, ServiceExecFn> _requestExecMap = new Dictionary<Type, ServiceExecFn>();
		readonly Dictionary<Type, RestrictAttribute> _requestServiceAttrs = new Dictionary<Type, RestrictAttribute>();

		public ServiceController(IAppHost appHost, Func<IEnumerable<Type>> resolveServicesFn, ServiceMetadata metadata = null)
		{
			_appHost = appHost;
			Metadata = metadata ?? new ServiceMetadata(appHost);

			RequestTypeFactoryMap = new Dictionary<Type, Func<IHttpRequest, object>>();
			EnableAccessRestrictions = true;
			ResolveServicesFn = resolveServicesFn;
		}

		public bool EnableAccessRestrictions { get; set; }

		public ServiceMetadata Metadata { get; internal set; }

		public Dictionary<Type, Func<IHttpRequest, object>> RequestTypeFactoryMap { get; set; }

		public string DefaultOperationsNamespace { get; set; }

		public IServiceRoutes Routes { get { return Metadata.Routes; } }

		//public IResolver Resolver
		//{
		//	get { return _resolver ?? EndpointHost.AppHost; }
		//	set { _resolver = value; }
		//}

		public Func<IEnumerable<Type>> ResolveServicesFn { get; set; }

//		public void Register<TReq>(Func<IService<TReq>> invoker)
//		{
//			var requestType = typeof(TReq);
//			ServiceExecFn handlerFn = (requestContext, dto) => {
//				var service = invoker();
//
//				InjectRequestContext(service, requestContext);
//
//				return ServiceExec<TReq>.Execute(
//					service, (TReq)dto,
//					requestContext != null ? requestContext.EndpointAttributes : EndpointAttributes.None);
//			};
//
//			requestExecMap.Add(requestType, handlerFn);
//		}

		public void Register(ITypeFactory serviceFactoryFn)
		{
			foreach (var serviceType in ResolveServicesFn())
			{
				//TODO: vdaron obsolete call commented 
				//RegisterGService(serviceFactoryFn, serviceType);
				RegisterNService(serviceFactoryFn, serviceType);
			}
		}

//		[Obsolete("use obsolete api")]
//		public void RegisterGService(ITypeFactory serviceFactoryFn, Type serviceType)
//		{
//			if (serviceType.IsAbstract || serviceType.ContainsGenericParameters) return;

//			//IService<T>
//			foreach (var service in serviceType.GetInterfaces())
//			{
////TODO: vdaron : check this deprecated call
////				if (!service.IsGenericType
////					|| service.GetGenericTypeDefinition() != typeof(IService<>)
////				) continue;

//				var requestType = service.GetGenericArguments()[0];

//				RegisterGServiceExecutor(requestType, serviceType, serviceFactoryFn);

//				var responseTypeName = requestType.FullName + ResponseDtoSuffix;
//				var responseType = AssemblyUtils.FindType(responseTypeName);

//				RegisterCommon(serviceType, requestType, responseType);
//			}
//		}

		public void RegisterNService(ITypeFactory serviceFactoryFn, Type serviceType)
		{
			var processedReqs = new HashSet<Type>();

			if (typeof(IService).IsAssignableFrom(serviceType)
				&& !serviceType.IsAbstract && !serviceType.IsGenericTypeDefinition)
			{
				foreach (var mi in serviceType.GetActions())
				{
					var requestType = mi.GetParameters()[0].ParameterType;
					if (processedReqs.Contains(requestType)) continue;
					processedReqs.Add(requestType);

					RegisterNServiceExecutor(_appHost, requestType, serviceType, serviceFactoryFn);

					var returnMarker = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
					var responseType = returnMarker != null ?
						returnMarker.GetGenericArguments()[0]
						: mi.ReturnType != typeof(object) && mi.ReturnType != typeof(void) ?
						mi.ReturnType
						: AssemblyUtils.FindType(requestType.FullName + ResponseDtoSuffix);

					RegisterCommon(serviceType, requestType, responseType);
				}
			}
		}

		public void RegisterCommon(Type serviceType, Type requestType, Type responseType)
		{
			RegisterRestPaths(requestType);

			Metadata.Add(serviceType, requestType, responseType);

			if (typeof(IRequiresRequestStream).IsAssignableFrom(requestType))
			{
				RequestTypeFactoryMap[requestType] = httpReq => {
					var rawReq = (IRequiresRequestStream)requestType.CreateInstance();
					rawReq.RequestStream = httpReq.InputStream;
					return rawReq;
				};
			}

			Log.DebugFormat("Registering {0} service '{1}' with request '{2}'",
				(responseType != null ? "Reply" : "OneWay"), serviceType.Name, requestType.Name);
		}

		public readonly Dictionary<string, List<RestPath>> RestPathMap = new Dictionary<string, List<RestPath>>();

		public void RegisterRestPaths(Type requestType)
		{
			var attrs = TypeDescriptor.GetAttributes(requestType).OfType<RouteAttribute>();
			foreach (RouteAttribute attr in attrs)
			{
				var restPath = new RestPath(requestType, attr.Path, attr.Verbs, attr.Summary, attr.Notes);
				if (!restPath.IsValid)
					throw new NotSupportedException(string.Format(
						"RestPath '{0}' on Type '{1}' is not Valid", attr.Path, requestType.Name));

				RegisterRestPath(restPath);
			}
		}

		private static readonly char[] InvalidRouteChars = new[] {'?', '&'};

		public void RegisterRestPath(RestPath restPath)
		{
			if (!EndpointHostConfig.SkipRouteValidation)
			{
				if (!restPath.Path.StartsWith("/"))
					throw new ArgumentException("Route '{0}' on '{1}' must start with a '/'".Fmt(restPath.Path, restPath.RequestType.Name));
				if (restPath.Path.IndexOfAny(InvalidRouteChars) != -1)
					throw new ArgumentException("Route '{0}' on '{1}' contains invalid chars. " +
						"See https://github.com/ServiceStack/ServiceStack/wiki/Routing for info on valid routes.".Fmt(restPath.Path, restPath.RequestType.Name));
			}

			List<RestPath> pathsAtFirstMatch;
			if (!RestPathMap.TryGetValue(restPath.FirstMatchHashKey, out pathsAtFirstMatch))
			{
				pathsAtFirstMatch = new List<RestPath>();
				RestPathMap[restPath.FirstMatchHashKey] = pathsAtFirstMatch;
			}
			pathsAtFirstMatch.Add(restPath);
		}

		public void AfterInit()
		{
			//Register any routes configured on Metadata.Routes
			foreach (var restPath in this.Metadata.Routes.RestPaths)
			{
				RegisterRestPath(restPath);
			}

			//Sync the RestPaths collections
			Metadata.Routes.RestPaths.Clear();
			Metadata.Routes.RestPaths.AddRange(RestPathMap.Values.SelectMany(x => x));

			Metadata.AfterInit();
		}

		public IRestPath GetRestPathForRequest(string httpMethod, string pathInfo)
		{
			var matchUsingPathParts = RestPath.GetPathPartsForMatching(pathInfo);

			List<RestPath> firstMatches;

			var yieldedHashMatches = RestPath.GetFirstMatchHashKeys(matchUsingPathParts);
			foreach (var potentialHashMatch in yieldedHashMatches)
			{
				if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

				var bestScore = -1;
				foreach (var restPath in firstMatches)
				{
					var score = restPath.MatchScore(httpMethod, matchUsingPathParts);
					if (score > bestScore) bestScore = score;
				}
				if (bestScore > 0)
				{
					foreach (var restPath in firstMatches)
					{
						if (bestScore == restPath.MatchScore(httpMethod, matchUsingPathParts))
							return restPath;
					}
				}
			}

			var yieldedWildcardMatches = RestPath.GetFirstMatchWildCardHashKeys(matchUsingPathParts);
			foreach (var potentialHashMatch in yieldedWildcardMatches)
			{
				if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

				var bestScore = -1;
				foreach (var restPath in firstMatches)
				{
					var score = restPath.MatchScore(httpMethod, matchUsingPathParts);
					if (score > bestScore) bestScore = score;
				}
				if (bestScore > 0)
				{
					foreach (var restPath in firstMatches)
					{
						if (bestScore == restPath.MatchScore(httpMethod, matchUsingPathParts))
							return restPath;
					}
				}
			}

			return null;
		}

		internal class TypeFactoryWrapper : ITypeFactory
		{
			private readonly Func<Type, object> typeCreator;

			public TypeFactoryWrapper(Func<Type, object> typeCreator)
			{
				this.typeCreator = typeCreator;
			}

			public object CreateInstance(Type type)
			{
				return typeCreator(type);
			}
		}

		//[Obsolete("obsolete ?")]
		//public void Register(Type requestType, Type serviceType)
		//{
		//	var handlerFactoryFn = Expression.Lambda<Func<Type, object>>
		//	(
		//		Expression.New(serviceType),
		//		Expression.Parameter(typeof(Type), "serviceType")
		//	).Compile();

		//	RegisterGServiceExecutor(requestType, serviceType, new TypeFactoryWrapper(handlerFactoryFn));
		//}
		//[Obsolete("obsolete ?")]
		//public void Register(Type requestType, Type serviceType, Func<Type, object> handlerFactoryFn)
		//{
		//	RegisterGServiceExecutor(requestType, serviceType, new TypeFactoryWrapper(handlerFactoryFn));
		//}

		//[Obsolete("use obsolete api")]
		//public void RegisterGServiceExecutor(Type requestType, Type serviceType, ITypeFactory serviceFactoryFn)
		//{
		//	var typeFactoryFn = CallServiceExecuteGeneric(requestType, serviceType);

		//	ServiceExecFn handlerFn = (requestContext, dto) => {
		//		var service = serviceFactoryFn.CreateInstance(serviceType);

		//		var endpointAttrs = requestContext != null
		//			? requestContext.EndpointAttributes
		//			: EndpointAttributes.None;

		//		ServiceExecFn serviceExec = (reqCtx, req) =>
		//			typeFactoryFn(req, service, endpointAttrs);

		//		return ManagedServiceExec(serviceExec, service, requestContext, dto);
		//	};

		//	AddToRequestExecMap(requestType, serviceType, handlerFn);
		//}

		public void RegisterNServiceExecutor(IAppHost appHost, Type requestType, Type serviceType, ITypeFactory serviceFactoryFn)
		{
			var serviceExecDef = typeof(NServiceRequestExec<,>).MakeGenericType(serviceType, requestType);
			var iserviceExec = (INServiceExec)serviceExecDef.CreateInstance();

			iserviceExec.CreateServiceRunner(appHost);

		    ServiceExecFn handlerFn = (requestContext, dto) =>
		    {
		        var service = serviceFactoryFn.CreateInstance(serviceType);
		        var iserv = service as IServiceBase;
		        if (iserv != null)
		        {
		            iserv.SetAppHost(appHost);
		        }
		        ServiceExecFn serviceExec = (reqCtx, req) => iserviceExec.Execute(reqCtx, service, req);
		        return ManagedServiceExec(serviceExec, service, requestContext, dto);
		    };

			AddToRequestExecMap(requestType, serviceType, handlerFn);
		}

		private void AddToRequestExecMap(Type requestType, Type serviceType, ServiceExecFn handlerFn)
		{
			if (_requestExecMap.ContainsKey(requestType))
			{
				throw new AmbiguousMatchException(
					string.Format(
						"Could not register Request '{0}' with service '{1}' as it has already been assigned to another service.\n"
						+ "Each Request DTO can only be handled by 1 service.",
						requestType.FullName, serviceType.FullName));
			}

			_requestExecMap.Add(requestType, handlerFn);

			var serviceAttrs = requestType.GetCustomAttributes(typeof(RestrictAttribute), false);
			if (serviceAttrs.Length > 0)
			{
				_requestServiceAttrs.Add(requestType, (RestrictAttribute)serviceAttrs[0]);
			}
		}

		private object ManagedServiceExec(
			ServiceExecFn serviceExec,
			object service, IRequestContext requestContext, object dto)
		{
			try
			{
				InjectRequestContext(service, requestContext);

				try
				{
					//Executes the service and returns the result
					var response = serviceExec(requestContext, dto);
					return response;
				}
				finally
				{
					//Gets disposed by AppHost or ContainerAdapter if set
					_appHost.Release(service);
				}
			}
			catch (TargetInvocationException tex)
			{
				//Mono invokes using reflection
				throw tex.InnerException ?? tex;
			}
		}

		private static void InjectRequestContext(object service, IRequestContext requestContext)
		{
			if (requestContext == null) return;

			var serviceRequiresContext = service as IRequiresRequestContext;
			if (serviceRequiresContext != null)
			{
				serviceRequiresContext.RequestContext = requestContext;
			}

			var servicesRequiresHttpRequest = service as IRequiresHttpRequest;
			if (servicesRequiresHttpRequest != null)
				servicesRequiresHttpRequest.HttpRequest = requestContext.Get<IHttpRequest>();
		}

		[Obsolete("use obsolete api ?")]
		private static Func<object, object, EndpointAttributes, object> CallServiceExecuteGeneric(Type requestType, Type serviceType)
		{
			var mi = GServiceExec.GetExecMethodInfo(serviceType, requestType);

			try
			{
				var requestDtoParam = Expression.Parameter(typeof(object), "requestDto");
				var requestDtoStrong = Expression.Convert(requestDtoParam, requestType);

				var serviceParam = Expression.Parameter(typeof(object), "serviceObj");
				var serviceStrong = Expression.Convert(serviceParam, serviceType);

				var attrsParam = Expression.Parameter(typeof(EndpointAttributes), "attrs");

				Expression callExecute = Expression.Call(
					mi, new Expression[] { serviceStrong, requestDtoStrong, attrsParam });

				var executeFunc = Expression.Lambda<Func<object, object, EndpointAttributes, object>>
				(callExecute, requestDtoParam, serviceParam, attrsParam).Compile();

				return executeFunc;

			}
			catch (Exception)
			{
				//problems with MONO, using reflection for fallback
				return (request, service, attrs) => mi.Invoke(null, new[] { service, request, attrs });
			}
		}

		//Execute MQ
//		public object ExecuteMessage<T>(IMessage<T> mqMessage)
//		{
//			return Execute(mqMessage.Body, new MqRequestContext(this.Resolver, mqMessage));
//		}
//
//		//Execute MQ with requestContext
//		public object ExecuteMessage<T>(IMessage<T> dto, IRequestContext requestContext)
//		{
//			return Execute(dto.Body, requestContext);
//		}

		public object Execute(object request)
		{
			return Execute(request, null);
		}

		//Execute HTTP
		public object Execute(object request, IRequestContext requestContext)
		{
			var requestType = request.GetType();

			if (EnableAccessRestrictions)
			{
				AssertServiceRestrictions(requestType,
					requestContext != null ? requestContext.EndpointAttributes : EndpointAttributes.None);
			}

			var handlerFn = GetService(requestType);
			return handlerFn(requestContext, request);
		}

		public ServiceExecFn GetService(Type requestType)
		{
			ServiceExecFn handlerFn;
			if (!_requestExecMap.TryGetValue(requestType, out handlerFn))
			{
				throw new NotImplementedException(string.Format("Unable to resolve service '{0}'", requestType.Name));
			}

			return handlerFn;
		}

		public object ExecuteText(string requestXml, Type requestType, IRequestContext requestContext)
		{
			var request = DataContractDeserializer.Instance.Parse(requestXml, requestType);
			var response = Execute(request, requestContext);
			var responseXml = DataContractSerializer.Instance.Parse(response);
			return responseXml;
		}

		public void AssertServiceRestrictions(Type requestType, EndpointAttributes actualAttributes)
		{
			if (!_appHost.Config.EnableAccessRestrictions)
				return;

			RestrictAttribute restrictAttr;
			var hasNoAccessRestrictions = !_requestServiceAttrs.TryGetValue(requestType, out restrictAttr)
				|| restrictAttr.HasNoAccessRestrictions;

			if (hasNoAccessRestrictions)
			{
				return;
			}

			var failedScenarios = new StringBuilder();
			foreach (var requiredScenario in restrictAttr.AccessibleToAny)
			{
				var allServiceRestrictionsMet = (requiredScenario & actualAttributes) == actualAttributes;
				if (allServiceRestrictionsMet)
				{
					return;
				}

				var passed = requiredScenario & actualAttributes;
				var failed = requiredScenario & ~(passed);

				failedScenarios.AppendFormat("\n -[{0}]", failed);
			}

			var internalDebugMsg = (EndpointAttributes.InternalNetworkAccess & actualAttributes) != 0
				? "\n Unauthorized call was made from: " + actualAttributes
				: "";

			throw new UnauthorizedAccessException(
				string.Format("Could not execute service '{0}', The following restrictions were not met: '{1}'" + internalDebugMsg,
					requestType.Name, failedScenarios));
		}
	}

}