﻿using System;
using SimpleStack.Logging;
using SimpleStack.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace SimpleStack
{
	public class ServiceRoutes : IServiceRoutes
	{
		private static ILog log = Logger.CreateLog();

		public readonly List<RestPath> RestPaths = new List<RestPath>();

		public IServiceRoutes Add<TRequest>(string restPath)
		{
			if (HasExistingRoute(typeof(TRequest), restPath)) return this;

			RestPaths.Add(new RestPath(typeof(TRequest), restPath));
			return this;
		}

		public IServiceRoutes Add<TRequest>(string restPath, string verbs)
		{
			if (HasExistingRoute(typeof(TRequest), restPath)) return this;

			RestPaths.Add(new RestPath(typeof(TRequest), restPath, verbs));
			return this;
		}

		public IServiceRoutes Add(Type requestType, string restPath, string verbs)
		{
			if (HasExistingRoute(requestType, restPath)) return this;

			RestPaths.Add(new RestPath(requestType, restPath, verbs));
			return this;
		}

		public IServiceRoutes Add(Type requestType, string restPath, string verbs, string summary, string notes)
		{
			if (HasExistingRoute(requestType, restPath)) return this;

			RestPaths.Add(new RestPath(requestType, restPath, verbs, summary, notes));
			return this;
		}

		private bool HasExistingRoute(Type requestType, string restPath)
		{
			var existingRoute = RestPaths.FirstOrDefault(
				x => x.RequestType == requestType && x.Path == restPath);

			if (existingRoute != null)
			{
				var existingRouteMsg = string.Format("Existing Route for '{0}' at '{1}' already exists",requestType.Name, restPath);

				//if (!EndpointHostConfig.SkipRouteValidation) //wait till next deployment
				//    throw new Exception(existingRouteMsg);

				log.Warn(existingRouteMsg);
				return true;
			}

			return false;
		}
	}
}

