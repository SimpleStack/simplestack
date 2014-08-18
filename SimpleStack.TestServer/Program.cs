using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using SimpleStack;
using SimpleStack.Serializers.NServicekit;
using SimpleStack.Swagger;

namespace SimpleStack.TestServer
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			using (WebApp.Start<Startup>("http://localhost:12345"))
			{
				Console.ReadLine();
			}
		}

		public class TestAppHost : AppHostBase
		{
			public TestAppHost()
				: base("Test app", Assembly.GetExecutingAssembly(),typeof(ApiAllowableValuesAttribute).Assembly)
			{
				SwaggerApiService.UseCamelCaseModelPropertyNames = true;
				SwaggerApiService.UseLowercaseUnderscoreModelPropertyNames = true;
				SwaggerApiService.DisableAutoDtoInBodyParam = false;
			}

			public override void Configure(Funq.Container container)
			{
				ContentTypeFilters.Register(new JsonContentTypeSerializer());
				ContentTypeFilters.Register(new XmlContentTypeSerializer());
			}
		}

		public class Startup
		{
			public void Configuration(IAppBuilder app)
			{
				app.UseCors(CorsOptions.AllowAll);

				var host = new TestAppHost();
				host.Init();

				app.UseSimpleStack(host);
#if DEBUG
				app.UseErrorPage();
#endif
				app.UseWelcomePage("/");
			}
		}
	}
}
