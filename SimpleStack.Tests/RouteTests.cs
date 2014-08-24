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
	public class SomeStringClass : ISomeStringInterface
	{
		public static string TEST_STRING = "Test";

		public string GetSomeString()
		{
			return TEST_STRING;
		}
	}

	public class TestAppHost : AppHostBase
	{
		public TestAppHost()
			:base("Test app", Assembly.GetExecutingAssembly())
		{
		}

		public override void Configure(Funq.Container container)
		{
			ContentTypeFilters.Register(new JsonContentTypeSerializer());
			ContentTypeFilters.Register(new XmlContentTypeSerializer());

			container.Register<ISomeStringInterface>(x => new SomeStringClass());

		}
	}

	[TestFixture]
	public class RoutesTests
	{
		private TestAppHost _appHost;

		[TestFixtureSetUp]
		public void SetupAppHost()
		{
			_appHost = new TestAppHost();
			_appHost.Init();
		}
		
		[TestFixtureTearDown]
		public void DisposeAppHost()
		{
			_appHost.Dispose();
		}

		//https://github.com/ServiceStack/ServiceStack/wiki/Routing

		[Test]
		public void TestHelloServiceQueryString()
		{
			using (var ctx = new MockContext(new MockOwinEnv("GET", "/Hello", "Name=World")))
			{
				Assert.IsTrue(_appHost.ProcessRequest(ctx).Result);

				Assert.AreEqual(200, ctx.Response.StatusCode);
				Assert.AreEqual("Hello, World", ctx.GetResponseBodyAs<HelloResponse>().Result);
			}
		}

		[Test]
		public void TestHelloServiceWithMapping()
		{
			using (var ctx = new MockContext(new MockOwinEnv("GET", "/Hello/Bob")))
			{
				Assert.IsTrue(_appHost.ProcessRequest(ctx).Result);

				Assert.AreEqual(200, ctx.Response.StatusCode);

				Assert.AreEqual("Hello, Bob", ctx.GetResponseBodyAs<HelloResponse>().Result);
			}
		}

		[Test]
		public void TestThrowException()
		{
			using (var ctx = new MockContext(new MockOwinEnv("GET", "/throw-empty")))
			{
				Assert.IsTrue(_appHost.ProcessRequest(ctx).Result);

				Assert.AreEqual(500, ctx.Response.StatusCode);

				var response = ctx.GetResponseBodyAs<ErrorResponse>().ResponseStatus;
				Assert.NotNull(response.StackTrace);
				Assert.AreEqual(PafException.Message, response.Message);
				Assert.AreEqual(typeof(PafException).Name, response.ErrorCode);
				Assert.AreEqual(0,response.Errors.Count);
			}
		}

		[Test]
		public void TestThrowHttpErrorException()
		{
			using (var ctx = new MockContext(new MockOwinEnv("GET", "/throw-httperror")))
			{
				Assert.IsTrue(_appHost.ProcessRequest(ctx).Result);

				Assert.AreEqual(500, ctx.Response.StatusCode);

				var response = ctx.GetResponseBodyAs<ExceptionDetailResponse>().ResponseStatus;
				Assert.NotNull(response.StackTrace);
				Assert.AreEqual(PafException.Message, response.Message);
				Assert.AreEqual(typeof(PafException).Name, response.ErrorCode);
				Assert.AreEqual(0, response.Errors.Count);
			}
		}

		[Test]
		public void TestThrowExceptionWithDetails()
		{
			using (var ctx = new MockContext(new MockOwinEnv("GET", "/throw-detail")))
			{
				Assert.IsTrue(_appHost.ProcessRequest(ctx).Result);

				Assert.AreEqual(500, ctx.Response.StatusCode);
				Assert.AreNotEqual(0, ctx.Response.Body.Length);

				var response = ctx.GetResponseBodyAs<ExceptionDetailResponse>().ResponseStatus;

				Assert.NotNull(response.StackTrace);
				Assert.AreEqual(PafException.Message,response.Message);
				Assert.AreEqual(typeof(PafException).Name, response.ErrorCode);
				Assert.AreEqual(0, response.Errors.Count);
			}
		}

		[Test]
		public void TestThrowExceptionWithDetailsWithIHasResponseStatusInterface()
		{
			using (var ctx = new MockContext(new MockOwinEnv("GET", "/throw-detail-ihasresponsestatus")))
			{
				Assert.IsTrue(_appHost.ProcessRequest(ctx).Result);

				Assert.AreEqual(500, ctx.Response.StatusCode);
				Assert.AreNotEqual(0, ctx.Response.Body.Length);

				var response = ctx.GetResponseBodyAs<ExceptionDetailResponse>().ResponseStatus;

				Assert.NotNull(response.StackTrace);
				Assert.AreEqual(PafException.Message,response.Message);
				Assert.AreEqual(typeof(PafException).Name, response.ErrorCode);
				Assert.AreEqual(0, response.Errors.Count);
			}
		}

		[Test]
		public void TestNotFound()
		{
			using (var ctx = new MockContext(new MockOwinEnv("GET", "/unknownroute")))
			{
				Assert.IsFalse(_appHost.ProcessRequest(ctx).Result);
			}
		}

		[Test]
		public void TestCustomHttpResult()
		{
			using (var ctx = new MockContext(new MockOwinEnv("GET", "/conflict")))
			{
				Assert.IsTrue(_appHost.ProcessRequest(ctx).Result);

				Assert.AreEqual(409, ctx.Response.StatusCode);
			}
		}

		[Test]
		public void TestHelloSomeString()
		{
			using (var ctx = new MockContext(new MockOwinEnv("GET", "/hello-somestringinterface")))
			{
				Assert.IsTrue(_appHost.ProcessRequest(ctx).Result);

				Assert.AreEqual(200, ctx.Response.StatusCode);

				var response = ctx.GetResponseBodyAs<HelloResponse>();

				Assert.AreEqual(SomeStringClass.TEST_STRING,response.Result);
			}
		}
	}
}

