using System; 
using Funq;
using SimpleStack.Cache;
using SimpleStack.Interfaces;
using SimpleStack.Logging;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using SimpleStack.Enums;
using System.Linq;
using Microsoft.Owin;
using System.Text;
using SimpleStack.Extensions;
using System.Threading.Tasks;
using SimpleStack.Handlers;

namespace SimpleStack
{
	public abstract class AppHostBase
		: IFunqlet, IDisposable, IAppHost, IHasContainer
	{
		private static readonly ILog Log = Logger.CreateLog();

		private bool _initialized = false;

		private IContentTypeFilter _contentTypeFilter;
		private EndpointHostConfig _config;
		private SimpleStackHttpHandlerFactory _simpleStackHttpHandlerFactory;

		private List<Action<IHttpRequest, IHttpResponse>> _rawRequestFilters;
		private List<Action<IHttpRequest, IHttpResponse, object>> _requestFilter;
		private List<Action<IHttpRequest, IHttpResponse, object>> _responseFilters;
		private HandleUncaughtExceptionDelegate _exceptionHandler;
		private HandleServiceExceptionDelegate _serviceExceptionHandler;
		private List<HttpHandlerResolverDelegate> _catchAllHandlers;

		protected AppHostBase(string serviceName, params Assembly[] assembliesWithServices)
		{
			ServiceManager = CreateServiceManager(assembliesWithServices);
			_config = EndpointHostConfig.CreateNew();
			_config.DebugMode = GetType().Assembly.IsDebugBuild();

			_contentTypeFilter = new HttpResponseFilter();

			_rawRequestFilters = new List<Action<IHttpRequest, IHttpResponse>>();
			_requestFilter = new List<Action<IHttpRequest, IHttpResponse, object>>();
			_responseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			_catchAllHandlers = new List<HttpHandlerResolverDelegate>();
			_simpleStackHttpHandlerFactory = new SimpleStackHttpHandlerFactory(this);
			_exceptionHandler = (httpReq, httpRes, operationName, ex) =>
				{
					var errorMessage = String.Format("Error occured while Processing Request: {0}", ex.Message);
					var statusCode = ex.ToStatusCode();
					//httpRes.WriteToResponse always calls .Close in it's finally statement so 
					//if there is a problem writing to response, by now it will be closed
					if (!httpRes.IsClosed)
					{
						httpRes.WriteErrorToResponse(httpReq, httpReq.ResponseContentType, operationName, errorMessage, ex, statusCode);
					}
				};
		}

		protected virtual ServiceManager CreateServiceManager(params Assembly[] assembliesWithServices)
		{
			return new ServiceManager(this,assembliesWithServices);
			//Alternative way to inject Container + Service Resolver strategy
			//return new ServiceManager(new Container(),
			//    new ServiceController(() => assembliesWithServices.ToList().SelectMany(x => x.GetTypes())));
		}

		protected IServiceController ServiceController
		{
			get
			{
				return ServiceManager.ServiceController;
			}
		}

		public IServiceRoutes Routes
		{
			get { return ServiceManager.ServiceController.Routes; }
		}

		public Container Container
		{
			get
			{
				return ServiceManager.Container;
			}
		}

		public void Init()
		{
			if (_initialized)
			{
				throw new InvalidDataException("AppHostBase has already been initiliazed");
			}

			_initialized = true;

			ServiceManager.Init();
			Configure(ServiceManager.Container);
			ServiceManager.AfterInit();

			// From PredefinedRoutesFeature Plugin 
			CatchAllHandlers.Add(ProcessPredefinedRoutesRequest);

			//Ensure a CacheClient is register
			var registeredCacheClient = TryResolve<ICacheClient>();
			using (registeredCacheClient)
			{
				if (registeredCacheClient == null)
				{
					Container.Register<ICacheClient>(new MemoryCacheClient());
				}
			}

			//EndpointHost.AfterInit();
		}

		public abstract void Configure(Container container);

		public void SetConfig(EndpointHostConfig config)
		{
			if (config.ServiceName == null)
				config.ServiceName = _config.ServiceName;

			ServiceManager.ServiceController.EnableAccessRestrictions = config.EnableAccessRestrictions;

			_config = config;
		}

		public void RegisterAs<T, TAs>() where T : TAs
		{
			Container.RegisterAutoWiredAs<T, TAs>();
		}

		public virtual void Release(object instance)
		{
			try
			{
				var iocAdapterReleases = Container.Adapter as IRelease;
				if (iocAdapterReleases != null)
				{
					iocAdapterReleases.Release(instance);
				}
				else
				{
					var disposable = instance as IDisposable;
					if (disposable != null)
						disposable.Dispose();
				}
			}
			catch {/*ignore*/}
		}

		public virtual void OnEndRequest()
		{
			//TODO: vdaron check this
//			foreach (var item in HostContext.Instance.Items.Values)
//			{
//				Release(item);
//			}
//
//			HostContext.Instance.EndRequest();
		}

		public void Register<T>(T instance)
		{
			Container.Register(instance);
		}

		public T TryResolve<T>()
		{
			return Container.TryResolve<T>();
		}

		/// <summary>
		/// Resolves from IoC container a specified type instance.
		/// </summary>
		/// <typeparam name="T">Type to be resolved.</typeparam>
		/// <returns>Instance of <typeparamref name="T"/>.</returns>
		public T Resolve<T>()
		{
			if (!_initialized) 
				throw new InvalidOperationException("AppHostBase is not initialized.");
			return Container.Resolve<T>();
		}

		/// <summary>
		/// Resolves and auto-wires a ServiceStack Service
		/// </summary>
		/// <typeparam name="T">Type to be resolved.</typeparam>
		/// <returns>Instance of <typeparamref name="T"/>.</returns>
		public T ResolveService<T>(IRequestContext httpCtx) where T : class, IRequiresRequestContext
		{
			if (!_initialized) 
				throw new InvalidOperationException("AppHostBase is not initialized.");

			var service = Container.Resolve<T>();
			if (service == null) return null;
			service.RequestContext = httpCtx;
			return service;
		}

		public Dictionary<Type, Func<IHttpRequest, object>> RequestBinders
		{
			get { return ServiceManager.ServiceController.RequestTypeFactoryMap; }
		}

		public IContentTypeFilter ContentTypeFilters
		{
			get
			{
				return _contentTypeFilter;
			}
		}

		public List<Action<IHttpRequest, IHttpResponse>> PreRequestFilters
		{
			get { return _rawRequestFilters; }
		}

		public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters
		{
			get { return _requestFilter; }
		}

		public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters
		{
			get { return _responseFilters; }
		}

//		public List<IViewEngine> ViewEngines
//		{
//			get
//			{
//				return EndpointHost.ViewEngines;
//			}
//		}

		public HandleUncaughtExceptionDelegate ExceptionHandler
		{
			get { return _exceptionHandler; }
			set { _exceptionHandler = value; }
		}

		//TODO: vdaron => Ensure we use this
		public HandleServiceExceptionDelegate ServiceExceptionHandler
		{
			get { return _serviceExceptionHandler; }
			set { _serviceExceptionHandler = value; }
		}

		public List<HttpHandlerResolverDelegate> CatchAllHandlers
		{
			get { return _catchAllHandlers; }
		}

		public EndpointHostConfig Config
		{
			get { return _config; }
		}

		public ServiceManager ServiceManager { get; private set; }
		public ServiceMetadata Metadata { get { return ServiceManager.ServiceController.Metadata; } }

//		public List<IPlugin> Plugins
//		{
//			get { return EndpointHost.Plugins; }
//		}

//		public IVirtualPathProvider VirtualPathProvider
//		{
//			get { return EndpointHost.VirtualPathProvider; }
//			set { EndpointHost.VirtualPathProvider = value; }
//		}

		public virtual IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
		{
			//cached per service action
			return new ServiceRunner<TRequest>(this, actionContext);
		}

//		public virtual void LoadPlugin(params IPlugin[] plugins)
//		{
//			foreach (var plugin in plugins)
//			{
//				try
//				{
//					plugin.Register(this);
//				}
//				catch (Exception ex)
//				{
//					log.Warn("Error loading plugin " + plugin.GetType().Name, ex);
//				}
//			}
//		}

//		public void RegisterService(Type serviceType, params string[] atRestPaths)
//		{
//			var genericService = EndpointHost.Config.ServiceManager.RegisterService(serviceType);
//			if (genericService != null)
//			{
//				var requestType = genericService.GetGenericArguments()[0];
//				foreach (var atRestPath in atRestPaths)
//				{
//					this.Routes.Add(requestType, atRestPath, null);
//				}
//			}
//			else
//			{
//				var reqAttr = serviceType.GetCustomAttributes(true).OfType<DefaultRequestAttribute>().FirstOrDefault();
//				if (reqAttr != null)
//				{
//					foreach (var atRestPath in atRestPaths)
//					{
//						this.Routes.Add(reqAttr.RequestType, atRestPath, null);
//					}
//				}
//			}
//		}

		public async Task<bool> ProcessRequest(IOwinContext context)
		{
			try
			{
				return await InnerProcessRequest(context);
			}
			catch (Exception ex)
			{
				Log.ErrorFormat("Error this.ProcessRequest(context): [{0}]: {1}", ex.GetType().Name, ex.Message);

				try
				{
					ResponseStatus error = new ResponseStatus();
					error.ErrorCode = ex.GetType().Name;
					error.Message = ex.Message;
					error.StackTrace = ex.StackTrace;

					var response = new { ResponseStatus = error };

//					var sb = new StringBuilder();
//					sb.AppendLine("{");
//					sb.AppendLine("\"ResponseStatus\":{");
//					sb.AppendFormat(" \"ErrorCode\":{0},\n", ex.GetType().Name.EncodeJson());
//					sb.AppendFormat(" \"Message\":{0},\n", ex.Message.EncodeJson());
//					sb.AppendFormat(" \"StackTrace\":{0}\n", ex.StackTrace.EncodeJson());
//					sb.AppendLine("}");
//					sb.AppendLine("}");

					context.Response.StatusCode = 500;
					context.Response.ContentType = ContentType.Json;
					//var sbBytes = sb.ToString().ToUtf8Bytes();

					//TODO : Error mgmt
					//return context.Response.Body.WriteAsync(sbBytes, 0, sbBytes.Length);
					return true; 
				}
				catch (Exception errorEx)
				{
					Log.ErrorFormat("Error this.ProcessRequest(context)(Exception while writing error to the response): [{0}]: {1}", errorEx.GetType().Name, errorEx.Message);
					return false;
				}
			}
		}

		protected async Task<bool> InnerProcessRequest(IOwinContext context)
		{
			return await Task.Run(() =>
				{
					if (context.Request.Uri == null)
						return false;

					var operationName = context.Request.Uri.Segments[context.Request.Uri.Segments.Length - 1];

					var httpReq = new OwinRequestWrapper(operationName, context.Request, _config.DefaultContentType);
					var httpRes = new OwinResponseWrapper(context.Response);

					if (httpReq.PathInfo == null)
						return false;

					var simpleStackHttpHandler = _simpleStackHttpHandlerFactory.GetHandler(httpReq);
					if (simpleStackHttpHandler != null)
					{
						var restHandler = simpleStackHttpHandler as RestHandler;
						if (restHandler != null)
						{
							httpReq.OperationName = operationName = restHandler.RestPath.RequestType.Name;
						}
						simpleStackHttpHandler.ProcessRequest(httpReq, httpRes, operationName);
						httpRes.Close();
						return true;
					}

					return false;
				});
			//throw new NotImplementedException("Cannot execute handler: " + simpleStackHttpHandler + " at PathInfo: " + httpReq.PathInfo);
		}

		private ISimpleStackHttpHandler ProcessPredefinedRoutesRequest(string httpMethod, string pathInfo, string filePath)
		{
			var pathParts = pathInfo.TrimStart('/').Split('/');
			if (pathParts.Length == 0) 
				return null;

			if (pathParts.Length == 1)
			{
				//TODO: vdaron enable soap ??
				//if (pathController == "soap11")
				//	return new Soap11MessageSyncReplyHttpHandler();
				//if (pathController == "soap12")
				//	return new Soap12MessageSyncReplyHttpHandler();

				return null;
			}

			var pathController = string.Intern(pathParts[0].ToLower());

			var pathAction = string.Intern(pathParts[1].ToLower());
			var requestName = pathParts.Length > 2 ? pathParts[2] : null;
			var isReply = pathAction == "syncreply" || pathAction == "reply";
			var isOneWay = pathAction == "asynconeway" || pathAction == "oneway";

			List<string> contentTypes;
			if (_contentTypeFilter.ContentTypeFormats.TryGetValue(pathController, out contentTypes))
			{
				var contentType = contentTypes[0];

				if (isReply)
					return new GenericHandler(this, contentType, EndpointAttributes.Reply)
					{
						RequestName = requestName
					};
				if (isOneWay)
					return new GenericHandler(this, contentType, EndpointAttributes.OneWay)
					{
						RequestName = requestName
					};
			}

			return null;
		}

		public virtual void Dispose()
		{
			ServiceManager.Dispose();
		}
	}
}

