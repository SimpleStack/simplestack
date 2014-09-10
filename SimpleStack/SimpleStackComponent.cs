using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using SimpleStack.Interfaces;


namespace SimpleStack
{
	using AppFunc = Func<IDictionary<string, object>, // Environment
	                     Task>; // Done

	public class SimpleStackComponent
	{
		private readonly AppFunc _next;
		private readonly AppHostBase _appHost;

		public SimpleStackComponent(AppFunc next, AppHostBase appHost)
		{
			_next = next;
			_appHost = appHost;
		}

		public async Task Invoke(IDictionary<string, object> environment)
		{
			Microsoft.Owin.OwinContext ctx = new Microsoft.Owin.OwinContext(environment);

			if (!await _appHost.ProcessRequest(ctx))
			{
				await _next.Invoke(environment);
			}
		}
	}
}

