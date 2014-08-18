using System;
using Owin;
using SimpleStack.Interfaces;

namespace SimpleStack
{
	public static class AppBuilderExtensions
	{
		public static void UseSimpleStack<T>(
			this IAppBuilder app, T appHost) where T : IAppHost
		{
			app.Use<SimpleStackComponent>(appHost);
		}
	}
}

