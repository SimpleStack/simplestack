using System;
using System.Net;
using SimpleStack.Attributes;
using SimpleStack.Interfaces;
using SimpleStack.Tools;

namespace SimpleStack.Tests
{
	public interface ISomeStringInterface
	{
		string GetSomeString();
	}

	public class PafException : Exception
	{
		public const string Message = "Paf";

		public PafException()
			: base(Message)
		{

		}
	}

	[Route("/hello","GET")]
	[Route("/hello/{Name}", "GET")]
	public class Hello
	{
		public string Name { get; set; }
	}

	[Route("/throw-empty", "GET")]
	public class ThrowExceptionRequest{}

	[Route("/throw-detail", "GET")]
	public class ThrowExceptionDetailRequest { }

	[Route("/throw-httperror", "GET")]
	public class ThrowHttpErrorException { }

	[Route("/throw-detail-ihasresponsestatus", "GET")]
	public class ThrowExceptionDetailRequestWithIHasResponseStatusInterface { }

	[Route("/conflict","GET")]
	public class ConflictHttpResponseRequest
	{
		
	}

	[Route("/hello-somestringinterface","GET")]
	public class HelloUsingSomeStringInterface
	{
		
	}

	public class HelloResponse
	{
		public string Result { get; set; }
	}

	public class EmptyResponse
	{}

	public class ExceptionDetailResponse
	{
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class ExceptionDetailResponseWithInterface : IHasResponseStatus
	{
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class TestService : Service
	{
		public ISomeStringInterface SomeStringInterface { get; set; }

		public HelloResponse Get(Hello request)
		{
			return new HelloResponse {Result = "Hello, " + request.Name};
		}
		public EmptyResponse Get(ThrowExceptionRequest request)
		{
			throw new PafException();
		}
		public ExceptionDetailResponse Get(ThrowExceptionDetailRequest request)
		{
			throw new PafException();
		}
		public ExceptionDetailResponseWithInterface Get(ThrowExceptionDetailRequestWithIHasResponseStatusInterface request)
		{
			throw new PafException();
		}
		public ExceptionDetailResponse Get(ThrowHttpErrorException request)
		{
			throw new HttpError(HttpStatusCode.InternalServerError,new PafException());
		}
		public object Get(ConflictHttpResponseRequest request)
		{
			return new HttpResult(HttpStatusCode.Conflict,"Conflict !");
		}
		public HelloResponse Get(HelloUsingSomeStringInterface request)
		{
			return new HelloResponse {Result = SomeStringInterface.GetSomeString()};
		}
	}
}
