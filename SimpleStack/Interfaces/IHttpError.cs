using System;

namespace SimpleStack.Interfaces
{
	public interface IHttpError : IHttpResult
	{
		string Message { get; }
		string ErrorCode { get; }
	}
}

