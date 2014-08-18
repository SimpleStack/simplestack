using System;
using System.Collections.Generic;

namespace SimpleStack.Interfaces
{
	public interface IRestPath
	{
		Type RequestType { get; }

		object CreateRequest(string pathInfo, Dictionary<string, string> queryStringAndFormData, object fromInstance);
	}
}

