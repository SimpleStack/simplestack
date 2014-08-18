using System;
using System.Threading.Tasks;

namespace SimpleStack.Interfaces
{
	//TODO: rename to IHttpHander and refactor once migration is completed
	public interface ISimpleStackHttpHandler
	{
		void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName);
	}
}

