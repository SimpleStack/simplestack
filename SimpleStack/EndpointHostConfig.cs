using System;
using SimpleStack.Logging;
using System.Collections.Generic;
using SimpleStack.Enums;
using SimpleStack.Interfaces;
using System.Net;
using SimpleStack.Extensions;
using System.Reflection;
using System.Configuration;
using System.Xml.Linq;
using SimpleStack.Tools;
using System.Linq;
using System.IO;
using SimpleStack.Attributes;
using System.Runtime.Serialization;
using SimpleStack.Metadata;
using SimpleStack.Handlers;

namespace SimpleStack
{
	public class EndpointHostConfig
	{
		private static ILog log = Logger.CreateLog();

		public static bool SkipPathValidation = false;
		/// <summary>
		/// Use: \[Route\("[^\/]  regular expression to find violating routes in your sln
		/// </summary>
		public static bool SkipRouteValidation = false;

		public static string ServiceStackPath = null;

		private static EndpointHostConfig instance;
		public static EndpointHostConfig Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new EndpointHostConfig {
						MetadataTypesConfig = new MetadataTypesConfig(addDefaultXmlNamespace: "http://schemas.simplestack.org/types"),
						WsdlServiceNamespace = "http://schemas.simplestack.org/types",
						WsdlSoapActionNamespace = "http://schemas.simplestack.org/types",
						MetadataPageBodyHtml = @"<br />
                            <h3><a href=""https://github.com/ServiceStack/ServiceStack/wiki/Clients-overview"">Clients Overview</a></h3>",
						MetadataOperationPageBodyHtml = @"<br />
                            <h3><a href=""https://github.com/ServiceStack/ServiceStack/wiki/Clients-overview"">Clients Overview</a></h3>",
//						LogFactory = null,// new NullLogFactory(),
						EnableAccessRestrictions = true,
						WebHostPhysicalPath = "~".MapServerPath(),
						SimpleStackHandlerFactoryPath = ServiceStackPath,
						MetadataRedirectPath = null,
						DefaultContentType = null,
						AllowJsonpRequests = true,
						AllowNonHttpOnlyCookies = false,
						UseHttpsLinks = false,
						DebugMode = false,
						DefaultDocuments = new List<string> {
							"default.htm",
							"default.html",
							"default.cshtml",
							"default.md",
							"index.htm",
							"index.html",
							"default.aspx",
							"default.ashx",
						},
						GlobalResponseHeaders = new Dictionary<string, string> { { "X-Powered-By", Env.ServerUserAgent } },
						IgnoreFormatsInMetadata = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase),
						//AllowFileExtensions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
						//{
						//	"js", "css", "htm", "html", "shtm", "txt", "xml", "rss", "csv", 
						//	"jpg", "jpeg", "gif", "png", "bmp", "ico", "tif", "tiff", "svg", 
						//	"avi", "divx", "m3u", "mov", "mp3", "mpeg", "mpg", "qt", "vob", "wav", "wma", "wmv", 
						//	"flv", "xap", "xaml", "ogg", "mp4", "webm", 
						//},
						DebugAspNetHostEnvironment = Env.IsMono ? "FastCGI" : "IIS7",
						DebugHttpListenerHostEnvironment = Env.IsMono ? "XSP" : "WebServer20",
						EnableFeatures = Feature.All,
						WriteErrorsToResponse = true,
						ReturnsInnerException = true,
//						MarkdownOptions = new MarkdownOptions(),
//						MarkdownBaseType = typeof(MarkdownViewBase),
//						MarkdownGlobalHelpers = new Dictionary<string, Type>(),
						HtmlReplaceTokens = new Dictionary<string, string>(),
						AddMaxAgeForStaticMimeTypes = new Dictionary<string, TimeSpan> {
							{ "image/gif", TimeSpan.FromHours(1) },
							{ "image/png", TimeSpan.FromHours(1) },
							{ "image/jpeg", TimeSpan.FromHours(1) },
						},
						AppendUtf8CharsetOnContentTypes = new HashSet<string> { ContentType.Json, },
						RawHttpHandlers = new List<Func<IHttpRequest, ISimpleStackHttpHandler>>(),
						RouteNamingConventions = new List<RouteNamingConventionDelegate> {
							RouteNamingConvention.WithRequestDtoName,
							RouteNamingConvention.WithMatchingAttributes,
							RouteNamingConvention.WithMatchingPropertyNames
						},
						CustomHttpHandlers = new Dictionary<HttpStatusCode, ISimpleStackHttpHandler>(),
						GlobalHtmlErrorHttpHandler = null,
						MapExceptionToStatusCode = new Dictionary<Type, int>(),
						OnlySendSessionCookiesSecurely = false,
						RestrictAllCookiesToDomain = null,
						DefaultJsonpCacheExpiration = new TimeSpan(0, 20, 0),
						MetadataVisibility = EndpointAttributes.Any,
						Return204NoContentForEmptyResponse = true,
						AllowPartialResponses = true,
					};

					if (instance.SimpleStackHandlerFactoryPath == null)
					{
						InferHttpHandlerPath();
					}
				}
				return instance;
			}
		}

		public EndpointHostConfig(string serviceName, ServiceManager serviceManager)
			: this()
		{
			this.ServiceName = serviceName;
			this.ServiceManager = serviceManager;
		}

		public EndpointHostConfig()
		{
			if (instance == null) return;

			//Get a copy of the singleton already partially configured
			this.MetadataTypesConfig = instance.MetadataTypesConfig;
			this.WsdlServiceNamespace = instance.WsdlServiceNamespace;
			this.WsdlSoapActionNamespace = instance.WsdlSoapActionNamespace;
			this.MetadataPageBodyHtml = instance.MetadataPageBodyHtml;
			this.MetadataOperationPageBodyHtml = instance.MetadataOperationPageBodyHtml;
			this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
			this.ServiceEndpointsMetadataConfig = instance.ServiceEndpointsMetadataConfig;
//			this.LogFactory = instance.LogFactory;
			this.EnableAccessRestrictions = instance.EnableAccessRestrictions;
			this.WebHostUrl = instance.WebHostUrl;
			this.WebHostPhysicalPath = instance.WebHostPhysicalPath;
			this.DefaultRedirectPath = instance.DefaultRedirectPath;
			this.MetadataRedirectPath = instance.MetadataRedirectPath;
			this.SimpleStackHandlerFactoryPath = instance.SimpleStackHandlerFactoryPath;
			this.DefaultContentType = instance.DefaultContentType;
			this.AllowJsonpRequests = instance.AllowJsonpRequests;
			this.DebugMode = instance.DebugMode;
			this.DefaultDocuments = instance.DefaultDocuments;
			this.GlobalResponseHeaders = instance.GlobalResponseHeaders;
			this.IgnoreFormatsInMetadata = instance.IgnoreFormatsInMetadata;
			//this.AllowFileExtensions = instance.AllowFileExtensions;
			this.EnableFeatures = instance.EnableFeatures;
			this.WriteErrorsToResponse = instance.WriteErrorsToResponse;
			this.ReturnsInnerException = instance.ReturnsInnerException;
//			this.MarkdownOptions = instance.MarkdownOptions;
//			this.MarkdownBaseType = instance.MarkdownBaseType;
//			this.MarkdownGlobalHelpers = instance.MarkdownGlobalHelpers;
			this.HtmlReplaceTokens = instance.HtmlReplaceTokens;
			this.AddMaxAgeForStaticMimeTypes = instance.AddMaxAgeForStaticMimeTypes;
			this.AppendUtf8CharsetOnContentTypes = instance.AppendUtf8CharsetOnContentTypes;
			this.RawHttpHandlers = instance.RawHttpHandlers;
			this.RouteNamingConventions = instance.RouteNamingConventions;
			this.CustomHttpHandlers = instance.CustomHttpHandlers;
			this.GlobalHtmlErrorHttpHandler = instance.GlobalHtmlErrorHttpHandler;
			this.MapExceptionToStatusCode = instance.MapExceptionToStatusCode;
			this.OnlySendSessionCookiesSecurely = instance.OnlySendSessionCookiesSecurely;
			this.RestrictAllCookiesToDomain = instance.RestrictAllCookiesToDomain;
			this.DefaultJsonpCacheExpiration = instance.DefaultJsonpCacheExpiration;
			this.MetadataVisibility = instance.MetadataVisibility;
			this.Return204NoContentForEmptyResponse = Return204NoContentForEmptyResponse;
			this.AllowNonHttpOnlyCookies = instance.AllowNonHttpOnlyCookies;
			this.AllowPartialResponses = instance.AllowPartialResponses;
		}

		public static string GetAppConfigPath()
		{
			if (EndpointHost.AppHost == null) return null;

			var configPath = "~/web.config".MapHostAbsolutePath();
			if (File.Exists(configPath))
				return configPath;

			configPath = "~/Web.config".MapHostAbsolutePath(); //*nix FS FTW!
			if (File.Exists(configPath))
				return configPath;

			var appHostDll = new FileInfo(EndpointHost.AppHost.GetType().Assembly.Location).Name;
			configPath = "~/{0}.config".Fmt(appHostDll).MapAbsolutePath();
			return File.Exists(configPath) ? configPath : null;
		}

		private static Configuration GetAppConfig()
		{
			Assembly entryAssembly;

			//Read the user-defined path in the Web.Config
//			if (EndpointHost.AppHost is AppHostBase)
//				return WebConfigurationManager.OpenWebConfiguration("~/");
//
			if ((entryAssembly = Assembly.GetEntryAssembly()) != null)
				return ConfigurationManager.OpenExeConfiguration(entryAssembly.Location);

			return null;
		}

		private static void InferHttpHandlerPath()
		{
			try
			{
				var config = GetAppConfig();
				if (config == null) return;

				//	SetPathsFromConfiguration(config, null);

				if (instance.MetadataRedirectPath == null)
				{
					foreach (ConfigurationLocation location in config.Locations)
					{
						//		SetPathsFromConfiguration(location.OpenConfiguration(), (location.Path ?? "").ToLower());

						if (instance.MetadataRedirectPath != null) { break; }
					}
				}

				if (!SkipPathValidation && instance.MetadataRedirectPath == null)
				{
					throw new ConfigurationErrorsException(
						"Unable to infer ServiceStack's <httpHandler.Path/> from the Web.Config\n"
						+ "Check with http://www.simplestack.org/ServiceStack.Hello/ to ensure you have configured ServiceStack properly.\n"
						+ "Otherwise you can explicitly set your httpHandler.Path by setting: EndpointHostConfig.ServiceStackPath");
				}
			}
			catch (Exception) { }
		}

//		private static void SetPathsFromConfiguration(System.Configuration.Configuration config, string locationPath)
//		{
//			if (config == null)
//				return;
//
//			//standard config
//			var handlersSection = config.GetSection("system.web/httpHandlers") as HttpHandlersSection;
//			if (handlersSection != null)
//			{
//				for (var i = 0; i < handlersSection.Handlers.Count; i++)
//				{
//					var httpHandler = handlersSection.Handlers[i];
//					if (!httpHandler.Type.StartsWith("ServiceStack"))
//						continue;
//
//					SetPaths(httpHandler.Path, locationPath);
//					break;
//				}
//			}
//
//			//IIS7+ integrated mode system.webServer/handlers
//			var pathsNotSet = instance.MetadataRedirectPath == null;
//			if (pathsNotSet)
//			{
//				var webServerSection = config.GetSection("system.webServer");
//				if (webServerSection != null)
//				{
//					var rawXml = webServerSection.SectionInformation.GetRawXml();
//					if (!string.IsNullOrEmpty(rawXml))
//					{
//						SetPaths(ExtractHandlerPathFromWebServerConfigurationXml(rawXml), locationPath);
//					}
//				}
//
//				//In some MVC Hosts auto-inferencing doesn't work, in these cases assume the most likely default of "/api" path
//				pathsNotSet = instance.MetadataRedirectPath == null;
//				if (pathsNotSet)
//				{
//					var isMvcHost = Type.GetType("System.Web.Mvc.Controller") != null;
//					if (isMvcHost)
//					{
//						SetPaths("api", null);
//					}
//				}
//			}
//		}
//
		private static void SetPaths(string handlerPath, string locationPath)
		{
			if (handlerPath == null) return;

			if (locationPath == null)
			{
				handlerPath = handlerPath.Replace("*", String.Empty);
			}

			instance.SimpleStackHandlerFactoryPath = locationPath ?? (string.IsNullOrEmpty(handlerPath) ? null : handlerPath);

			instance.MetadataRedirectPath = PathUtils.CombinePaths(
				null != locationPath ? instance.SimpleStackHandlerFactoryPath : handlerPath, "metadata");
		}

		private static string ExtractHandlerPathFromWebServerConfigurationXml(string rawXml)
		{
			return XDocument.Parse(rawXml).Root.Element("handlers")
				.Descendants("add")
				.Where(handler => EnsureHandlerTypeAttribute(handler).StartsWith("ServiceStack"))
				.Select(handler => handler.Attribute("path").Value)
				.FirstOrDefault();
		}
		private static string EnsureHandlerTypeAttribute(XElement handler)
		{
			if (handler.Attribute("type") != null && !string.IsNullOrEmpty(handler.Attribute("type").Value))
			{
				return handler.Attribute("type").Value;
			}
			return string.Empty;
		}

		public ServiceManager ServiceManager { get; internal set; }
		public ServiceMetadata Metadata { get { return ServiceManager.Metadata; } }
		public IServiceController ServiceController { get { return ServiceManager.ServiceController; } }

		public MetadataTypesConfig MetadataTypesConfig { get; set; }
		public string WsdlServiceNamespace { get; set; }
		public string WsdlSoapActionNamespace { get; set; }

		private EndpointAttributes metadataVisibility;
		public EndpointAttributes MetadataVisibility
		{
			get { return metadataVisibility; }
			set { metadataVisibility = value.ToAllowedFlagsSet(); }
		}

		public string MetadataPageBodyHtml { get; set; }
		public string MetadataOperationPageBodyHtml { get; set; }

		public string ServiceName { get; set; }
		public string DefaultContentType { get; set; }
		public bool AllowJsonpRequests { get; set; }
		public bool DebugMode { get; set; }
		public bool DebugOnlyReturnRequestInfo { get; set; }
		public string DebugAspNetHostEnvironment { get; set; }
		public string DebugHttpListenerHostEnvironment { get; set; }
		public List<string> DefaultDocuments { get; private set; }

		public HashSet<string> IgnoreFormatsInMetadata { get; set; }

		public HashSet<string> AllowFileExtensions { get; set; }

		public string WebHostUrl { get; set; }
		public string WebHostPhysicalPath { get; set; }
		public string SimpleStackHandlerFactoryPath { get; set; }
		public string DefaultRedirectPath { get; set; }
		public string MetadataRedirectPath { get; set; }

		public ServiceEndpointsMetadataConfig ServiceEndpointsMetadataConfig { get; set; }
		public bool EnableAccessRestrictions { get; set; }
		public bool UseBclJsonSerializers { get; set; }
		public Dictionary<string, string> GlobalResponseHeaders { get; set; }
		public Feature EnableFeatures { get; set; }
		public bool ReturnsInnerException { get; set; }
		public bool WriteErrorsToResponse { get; set; }

		public Dictionary<string, string> HtmlReplaceTokens { get; set; }

		public HashSet<string> AppendUtf8CharsetOnContentTypes { get; set; }

		public Dictionary<string, TimeSpan> AddMaxAgeForStaticMimeTypes { get; set; }

		public List<Func<IHttpRequest, ISimpleStackHttpHandler>> RawHttpHandlers { get; set; }

		public List<RouteNamingConventionDelegate> RouteNamingConventions { get; set; }

		public Dictionary<HttpStatusCode, ISimpleStackHttpHandler> CustomHttpHandlers { get; set; }
		public ISimpleStackHttpHandler GlobalHtmlErrorHttpHandler { get; set; }
		public Dictionary<Type, int> MapExceptionToStatusCode { get; set; }

		public bool OnlySendSessionCookiesSecurely { get; set; }
		public string RestrictAllCookiesToDomain { get; set; }

		public TimeSpan DefaultJsonpCacheExpiration { get; set; }
		public bool Return204NoContentForEmptyResponse { get; set; }
		public bool AllowPartialResponses { get; set; }

		public bool AllowNonHttpOnlyCookies { get; set; }

		public bool UseHttpsLinks { get; set; }

		private string defaultOperationNamespace;
		public string DefaultOperationNamespace
		{
			get
			{
				if (this.defaultOperationNamespace == null)
				{
					this.defaultOperationNamespace = GetDefaultNamespace();
				}
				return this.defaultOperationNamespace;
			}
			set
			{
				this.defaultOperationNamespace = value;
			}
		}

		private string GetDefaultNamespace()
		{
			if (!String.IsNullOrEmpty(this.defaultOperationNamespace)
				|| this.ServiceController == null) return null;

			foreach (var operationType in this.Metadata.RequestTypes)
			{
				var attrs = operationType.GetCustomAttributes(
					typeof(DataContractAttribute), false);

				if (attrs.Length <= 0) continue;

				var attr = (DataContractAttribute)attrs[0];

				if (String.IsNullOrEmpty(attr.Namespace)) continue;

				return attr.Namespace;
			}

			return null;
		}

		public bool HasFeature(Feature feature)
		{
			return (feature & EndpointHost.Config.EnableFeatures) == feature;
		}

		public void AssertFeatures(Feature usesFeatures)
		{
			if (EndpointHost.Config.EnableFeatures == Feature.All) 
				return;

			if (!HasFeature(usesFeatures))
			{
				throw new UnauthorizedAccessException(
					String.Format("'{0}' Features have been disabled by your administrator", usesFeatures));
			}
		}

		public UnauthorizedAccessException UnauthorizedAccess(EndpointAttributes requestAttrs)
		{
			return new UnauthorizedAccessException(
				String.Format("Request with '{0}' is not allowed", requestAttrs));
		}

		public void AssertContentType(string contentType)
		{
			if (EndpointHost.Config.EnableFeatures == Feature.All) return;

			var contentTypeFeature = ContentType.ToFeature(contentType);
			AssertFeatures(contentTypeFeature);
		}

		public MetadataPagesConfig MetadataPagesConfig
		{
			get
			{
				return new MetadataPagesConfig(
					Metadata,
					ServiceEndpointsMetadataConfig,
					IgnoreFormatsInMetadata,
					EndpointHost.ContentTypeFilter.ContentTypeFormats.Keys.ToList());
			}
		}

		public bool HasAccessToMetadata(IHttpRequest httpReq, IHttpResponse httpRes)
		{
			if (!HasFeature(Feature.Metadata))
			{
				EndpointHost.Config.HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Metadata Not Available");
				return false;
			}

			if (MetadataVisibility != EndpointAttributes.Any)
			{
				var actualAttributes = httpReq.GetAttributes();
				if ((actualAttributes & MetadataVisibility) != MetadataVisibility)
				{
					HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Metadata Not Visible");
					return false;
				}
			}
			return true;
		}

		public void HandleErrorResponse(IHttpRequest httpReq, IHttpResponse httpRes, HttpStatusCode errorStatus, string errorStatusDescription=null)
		{
			if (httpRes.IsClosed) return;

			httpRes.StatusDescription = errorStatusDescription;

			var handler = GetHandlerForErrorStatus(errorStatus);

			handler.ProcessRequest(httpReq, httpRes, httpReq.OperationName);
		}

		public ISimpleStackHttpHandler GetHandlerForErrorStatus(HttpStatusCode errorStatus)
		{
			var httpHandler = GetCustomErrorHandler(errorStatus);

			switch (errorStatus)
			{
			case HttpStatusCode.Forbidden:
				return httpHandler ?? new ForbiddenHttpHandler();
			case HttpStatusCode.NotFound:
				return httpHandler ?? new NotFoundHttpHandler();
			}

			if (CustomHttpHandlers != null)
			{
				CustomHttpHandlers.TryGetValue(HttpStatusCode.NotFound, out httpHandler);
			}

			return httpHandler ?? new NotFoundHttpHandler();
		}

		public ISimpleStackHttpHandler GetCustomErrorHandler(int errorStatusCode)
		{
			try
			{
				return GetCustomErrorHandler((HttpStatusCode) errorStatusCode);
			}
			catch
			{
				return null;
			}
		}

		public ISimpleStackHttpHandler GetCustomErrorHandler(HttpStatusCode errorStatus)
		{
			ISimpleStackHttpHandler httpHandler = null;
			if (CustomHttpHandlers != null)
			{
				CustomHttpHandlers.TryGetValue(errorStatus, out httpHandler);
			}
			return httpHandler ?? GlobalHtmlErrorHttpHandler;
		}
	}
}

