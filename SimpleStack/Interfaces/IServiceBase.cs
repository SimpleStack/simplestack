using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleStack.Interfaces
{
	public interface IServiceBase : IResolver
	{
		IResolver GetResolver();

		/// <summary>
		/// Resolve an alternate Web Service from SimpleStack's IOC container.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		T ResolveService<T>();

		IRequestContext RequestContext { get; }
	}
}
