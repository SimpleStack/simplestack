using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace SimpleStack.Tests
{
	class MockContext : OwinContext, IDisposable
	{
		private readonly MockOwinEnv _env;

		public MockContext(MockOwinEnv env )
			:base(env)
		{
			_env = env;
		}

		public string GetResponseBodyAsText()
		{
			return _env.GetResponseBodyAsText();
		}

		public T GetResponseBodyAs<T>()
		{
			return _env.GetResponseBodyAs<T>();
		}

		public void Dispose()
		{
			_env.Dispose();
		}
	}
}
