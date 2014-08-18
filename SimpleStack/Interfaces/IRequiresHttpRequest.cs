using System;

namespace SimpleStack.Interfaces
{
	public interface IRequiresHttpRequest
	{
		IHttpRequest HttpRequest { get; set; }
	}
}

