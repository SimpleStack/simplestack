using System.IO;
using System.Net;
using System.Text;
using Microsoft.Owin;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Collections.Generic;
using SimpleStack.Interfaces;
using SimpleStack.Serializers.NServicekit;

namespace SimpleStack.Tests
{
	[TestFixture]
	public class ConfigTests
	{
		private TestAppHost _appHost;

		[TestFixtureSetUp]
		public void SetupAppHost()
		{
			_appHost = new TestAppHost();
			_appHost.Init();
			_appHost.Config.SimpleStackHandlerFactoryPath = "/api";
		}
		
		[TestFixtureTearDown]
		public void DisposeAppHost()
		{
			_appHost.Dispose();
		}

		[Test]
		public void TestHelloServiceWithMappingAndBasePath()
		{
			
			using (var ctx = new MockContext(new MockOwinEnv("GET", "/api/Hello/Bob")))
			{
				Assert.IsTrue(_appHost.ProcessRequest(ctx).Result);

				Assert.AreEqual(200, ctx.Response.StatusCode);

				Assert.AreEqual("Hello, Bob", ctx.GetResponseBodyAs<HelloResponse>().Result);
			}
		}
	}
}

