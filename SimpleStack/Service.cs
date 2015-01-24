using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleStack.Cache;
using SimpleStack.Interfaces;

namespace SimpleStack
{
	/// <summary>
	/// Generic + Useful IService base class
	/// </summary>
	public class Service : IService, IRequiresRequestContext, IServiceBase, IDisposable
	{
		public IRequestContext RequestContext { get; set; }

		private IAppHost _appHost;

		public virtual IResolver GetResolver()
		{
			return _appHost;
		}

		public virtual IAppHost GetAppHost()
		{
			return _appHost;
		}

		public virtual void SetAppHost(IAppHost resolver)
		{
			_appHost = resolver;
		}

		public virtual T TryResolve<T>()
		{
			return GetResolver() == null ? default(T) : GetResolver().TryResolve<T>();
		}

		public virtual T ResolveService<T>()
		{
			var service = TryResolve<T>();
			var requiresContext = service as IRequiresRequestContext;
			if (requiresContext != null)
			{
				requiresContext.RequestContext = RequestContext;
			}
			return service;
		}

		private IHttpRequest request;
		protected virtual IHttpRequest Request
		{
			get { return request ?? (request = RequestContext != null ? RequestContext.Get<IHttpRequest>() : TryResolve<IHttpRequest>()); }
		}

		private IHttpResponse response;
		protected virtual IHttpResponse Response
		{
			get { return response ?? (response = RequestContext != null ? RequestContext.Get<IHttpResponse>() : TryResolve<IHttpResponse>()); }
		}

		private ICacheClient cache;
		public virtual ICacheClient Cache
		{
			get
			{
				return cache ??
					(cache = TryResolve<ICacheClient>())/* ??
					(cache = (TryResolve<IRedisClientsManager>() != null ? TryResolve<IRedisClientsManager>().GetCacheClient() : null))*/;
			}
		}

		//private IDbConnection db;
		//public virtual IDbConnection Db
		//{
		//	get { return db ?? (db = TryResolve<IDbConnectionFactory>().Open()); }
		//}

		//private IRedisClient redis;
		//public virtual IRedisClient Redis
		//{
		//	get { return redis ?? (redis = TryResolve<IRedisClientsManager>().GetClient()); }
		//}

		//private IMessageProducer messageProducer;
		//public virtual IMessageProducer MessageProducer
		//{
		//	get { return messageProducer ?? (messageProducer = TryResolve<IMessageFactory>().CreateMessageProducer()); }
		//}

		//private ISessionFactory sessionFactory;
		//public virtual ISessionFactory SessionFactory
		//{
		//	get { return sessionFactory ?? (sessionFactory = TryResolve<ISessionFactory>()) ?? new SessionFactory(Cache); }
		//}

		///// <summary>
		///// Dynamic Session Bag
		///// </summary>
		//private ISession session;
		//public virtual ISession Session
		//{
		//	get
		//	{
		//		return session ?? (session = TryResolve<ISession>() //Easier to mock
		//			?? SessionFactory.GetOrCreateSession(Request, Response));
		//	}
		//}

		/// <summary>
		/// Typed UserSession
		/// </summary>
		//private object userSession;
		//protected virtual TUserSession SessionAs<TUserSession>()
		//{
		//	if (userSession == null)
		//	{
		//		userSession = TryResolve<TUserSession>(); //Easier to mock
		//		if (userSession == null)
		//			userSession = Cache.SessionAs<TUserSession>(Request, Response);
		//	}
		//	return (TUserSession)userSession;
		//}

		//public virtual void PublishMessage<T>(T message)
		//{
		//	//TODO: Register In-Memory IMessageFactory by default
		//	if (MessageProducer == null)
		//		throw new NullReferenceException("No IMessageFactory was registered, cannot PublishMessage");

		//	MessageProducer.Publish(message);
		//}

		public virtual void Dispose()
		{
			//if (db != null)
			//	db.Dispose();
			//if (redis != null)
			//	redis.Dispose();
			//if (messageProducer != null)
			//	messageProducer.Dispose();
		}
	}
}
