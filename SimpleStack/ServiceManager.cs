using System;
using SimpleStack.Logging;
using System.Reflection;
using System.Collections.Generic;
using Funq;

namespace SimpleStack
{
	public class ServiceManager : IDisposable
	{
		private static readonly ILog Log = Logger.CreateLog ();

		private ContainerResolveCache _typeFactory;

		public Container Container { get; private set; }
		public ServiceController ServiceController { get; private set; }
		public ServiceMetadata Metadata { get; internal set; }

		//public ServiceOperations ServiceOperations { get; set; }
		//public ServiceOperations AllServiceOperations { get; set; }

		public ServiceManager(params Assembly[] assembliesWithServices)
		{
			if (assembliesWithServices == null || assembliesWithServices.Length == 0)
				throw new ArgumentException(
					"No Assemblies provided in your AppHost's base constructor.\n"
					+ "To register your services, please provide the assemblies where your web services are defined.");

			Container = new Container { DefaultOwner = Owner.External };
			Metadata = new ServiceMetadata();
			ServiceController = new ServiceController(() => GetAssemblyTypes(assembliesWithServices), Metadata);
		}

		//public ServiceManager(bool autoInitialize, params Assembly[] assembliesWithServices)
		//	: this(assembliesWithServices)
		//{
		//	if (autoInitialize)
		//	{
		//		Init();
		//	}
		//}

		//public ServiceManager(Container container, params Assembly[] assembliesWithServices)
		//	: this(assembliesWithServices)
		//{
		//	Container = container ?? new Container();
		//}

		/// <summary>
		/// Inject alternative container and strategy for resolving Service Types
		/// </summary>
		public ServiceManager(Container container, ServiceController serviceController)
		{
			if (serviceController == null)
				throw new ArgumentNullException("serviceController");

			Container = container ?? new Container();
			Metadata = serviceController.Metadata; //always share the same metadata
			ServiceController = serviceController;
		}

		public void Init()
		{
			_typeFactory = new ContainerResolveCache(Container);

			ServiceController.Register(_typeFactory);

			Container.RegisterAutoWiredTypes(Metadata.ServiceTypes);
		}

		public void AfterInit()
		{
			ServiceController.AfterInit();
		}


		private static IEnumerable<Type> GetAssemblyTypes(IEnumerable<Assembly> assembliesWithServices)
		{
			var results = new List<Type>();
			string assemblyName = null;
			string typeName = null;

			try
			{
				foreach (var assembly in assembliesWithServices)
				{
					assemblyName = assembly.FullName;
					foreach (var type in assembly.GetTypes())
					{
						typeName = type.Name;
						results.Add(type);
					}
				}
				return results;
			}
			catch (Exception ex)
			{
				var msg = string.Format("Failed loading types, last assembly '{0}', type: '{1}'", assemblyName, typeName);
				Log.Error(msg, ex);
				throw new Exception(msg, ex);
			}
		}

		//[Obsolete("Old API")]
		//public void RegisterService<T>()
		//{
//			if (!typeof(T).IsGenericType
//				|| typeof(T).GetGenericTypeDefinition() != typeof(IService<>))
//				throw new ArgumentException("Type {0} is not a Web Service that inherits IService<>".Fmt(typeof(T).FullName));
//
//			this.ServiceController.RegisterGService(typeFactory, typeof(T));
//			this.Container.RegisterAutoWired<T>();
		//}
	
		//[Obsolete("Old API")]
		//public Type RegisterService(Type serviceType)
		//{
		//	return null;
//			var genericServiceType = serviceType.GetTypeWithGenericTypeDefinitionOf(typeof(IService<>));
//			try
//			{
//				if (genericServiceType != null)
//				{
//					this.ServiceController.RegisterGService(typeFactory, serviceType);
//					this.Container.RegisterAutoWiredType(serviceType);
//					return genericServiceType;
//				}
//
//				var isNService = typeof(IService).IsAssignableFrom(serviceType);
//				if (isNService)
//				{
//					this.ServiceController.RegisterNService(typeFactory, serviceType);
//					this.Container.RegisterAutoWiredType(serviceType);
//					return null;
//				}
//
//				throw new ArgumentException("Type {0} is not a Web Service that inherits IService<> or IService".Fmt(serviceType.FullName));
//			}
//			catch (Exception ex)
//			{
//				Log.Error(ex);
//				return genericServiceType;
//			}
//		}

		//public object Execute(object dto)
		//{
		//	return this.ServiceController.Execute(dto, null);
		//}

		public void Dispose()
		{
			if (Container != null)
			{
				Container.Dispose();
			}
		}


	}

}

