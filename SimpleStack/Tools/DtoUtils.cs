using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SimpleStack.Extensions;
using SimpleStack.Logging;
using SimpleStack.Interfaces;

namespace SimpleStack.Tools
{
	public static class DtoUtils
	{
		private static ILog Log = Logger.CreateLog();

		/// <summary>
		/// Naming convention for the ResponseStatus property name on the response DTO
		/// </summary>
		public const string ResponseStatusPropertyName = "ResponseStatus";

		public static ResponseStatus ToResponseStatus(this Exception exception)
		{
			//var responseStatusConverter = exception as IResponseStatusConvertible;
			//if (responseStatusConverter != null)
			//{
			//	return responseStatusConverter.ToResponseStatus();
			//}

			var httpError = exception as IHttpError;
			return httpError != null
				? CreateResponseStatus(httpError.ErrorCode, httpError.Message)
				: CreateResponseStatus(exception.GetType().Name, exception.Message);
		}

		//public static ResponseStatus ToResponseStatus(this ValidationError validationException)
		//{
		//	return ResponseStatusUtils.CreateResponseStatus(validationException.ErrorCode, validationException.Message, validationException.Violations);
		//}

		//public static ResponseStatus ToResponseStatus(this ValidationErrorResult validationResult)
		//{
		//	return validationResult.IsValid
		//		? CreateSuccessResponse(validationResult.SuccessMessage)
		//		: ResponseStatusUtils.CreateResponseStatus(validationResult.ErrorCode, validationResult.ErrorMessage, validationResult.Errors);
		//}

		public static ResponseStatus CreateSuccessResponse(string message)
		{
			return new ResponseStatus { Message = message };
		}

		public static ResponseStatus CreateResponseStatus(string errorCode)
		{
			var errorMessage = errorCode.SplitCamelCase();
			return CreateResponseStatus(errorCode, errorMessage, null);
		}

		public static ResponseStatus CreateResponseStatus(string errorCode, string errorMessage)
		{
			return CreateResponseStatus(errorCode, errorMessage, null);
		}

		/// <summary>
		/// Creates the error response from the values provided.
		/// 
		/// If the errorCode is empty it will use the first validation error code, 
		/// if there is none it will throw an error.
		/// </summary>
		/// <param name="errorCode">The error code.</param>
		/// <param name="errorMessage">The error message.</param>
		/// <param name="validationErrors">The validation errors.</param>
		/// <returns></returns>
		public static ResponseStatus CreateResponseStatus(string errorCode, string errorMessage, /*IEnumerable<ValidationErrorField>*/object validationErrors)
		{
			var to = new ResponseStatus
			{
				ErrorCode = errorCode,
				Message = errorMessage,
				Errors = new List<ResponseError>(),
			};
			if (validationErrors != null)
			{
				//foreach (var validationError in validationErrors)
				//{
				//	var error = new ResponseError
				//	{
				//		ErrorCode = validationError.ErrorCode,
				//		FieldName = validationError.FieldName,
				//		Message = validationError.ErrorMessage,
				//	};
				//	to.Errors.Add(error);

				//	if (string.IsNullOrEmpty(to.ErrorCode))
				//	{
				//		to.ErrorCode = validationError.ErrorCode;
				//	}
				//	if (string.IsNullOrEmpty(to.Message))
				//	{
				//		to.Message = validationError.ErrorMessage;
				//	}
				//}
			}
			if (string.IsNullOrEmpty(errorCode))
			{
				if (string.IsNullOrEmpty(to.ErrorCode))
				{
					throw new ArgumentException("Cannot create a valid error response with a en empty errorCode and an empty validationError list");
				}
			}
			return to;
		}


		public static object CreateErrorResponse(string errorCode, string errorMessage, /*IEnumerable<ValidationErrorField>*/ object validationErrors)
		{
			var responseStatus = new ResponseStatus();// ResponseStatusUtils.CreateResponseStatus(errorCode, errorMessage, validationErrors);
			var responseDto = CreateResponseDto(null, responseStatus);
			return new HttpError(responseDto, HttpStatusCode.BadRequest, errorCode, errorMessage);
		}

		public static object CreateErrorResponse(object request, /*ValidationErrorResult*/object validationError)
		{
			//var responseStatus = validationError.ToResponseStatus();

			//var errorResponse = CreateErrorResponse(
			//	request,
			//	new ValidationError(validationError),
			//	responseStatus);

			return new ResponseStatus();// errorResponse;
		}

		public static object CreateErrorResponse(object request, Exception ex, ResponseStatus responseStatus)
		{
			var responseDto = CreateResponseDto(request, responseStatus);

			var httpError = ex as IHttpError;
			if (httpError != null)
			{
				if (responseDto != null)
					httpError.Response = responseDto;

				return httpError;
			}

			var errorCode = ex.GetType().Name;
			var errorMsg = ex.Message;
			if (responseStatus != null)
			{
				errorCode = responseStatus.ErrorCode ?? errorCode;
				errorMsg = responseStatus.Message ?? errorMsg;
			}

			return new HttpError(responseDto, ex.ToStatusCode(), errorCode, errorMsg);
		}
		public static Type GetErrorResponseDtoType(object request)
		{
			if (request == null)
				return typeof(ErrorResponse);

			//If a conventionally-named Response type exists use that regardless if it has ResponseStatus or not
			var responseDtoType = AssemblyUtils.FindType(GetResponseDtoName(request));
			if (responseDtoType == null)
			{
				var genericDef = request.GetType().GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
				if (genericDef != null)
				{

					var returnDtoType = genericDef.GenericTypeArguments()[0];
					var hasResponseStatus = returnDtoType is IHasResponseStatus
						|| returnDtoType.GetPropertyInfo("ResponseStatus") != null;

					//Only use the specified Return type if it has a ResponseStatus property
					if (hasResponseStatus)
					{
						responseDtoType = returnDtoType;
					}
				}
			}

			return responseDtoType ?? typeof(ErrorResponse);
		}

		/// <summary>
		/// Naming convention for the request's Response DTO
		/// </summary>
		public const string ResponseDtoSuffix = "Response";

		public static string GetResponseDtoName(object request)
		{
			var requestType = request.GetType();
			return requestType != typeof(object)
				? requestType.FullName + ResponseDtoSuffix
				: request.GetType().FullName + ResponseDtoSuffix;
		}

		/// <summary>
		/// Create an instance of the service response dto type and inject it with the supplied responseStatus
		/// </summary>
		/// <param name="request"></param>
		/// <param name="responseStatus"></param>
		/// <returns></returns>
		public static object CreateResponseDto(object request, ResponseStatus responseStatus)
		{
			// Predict the Response message type name
			var responseDtoType = GetErrorResponseDtoType(request);
			var responseDto = responseDtoType.CreateInstance();
			if (responseDto == null)
				return null;

			// For faster serialization of exceptions, services should implement IHasResponseStatus
			var hasResponseStatus = responseDto as IHasResponseStatus;
			if (hasResponseStatus != null)
			{
				hasResponseStatus.ResponseStatus = responseStatus;
			}
			else
			{
				var responseStatusProperty = responseDtoType.GetProperty(ResponseStatusPropertyName);
				if (responseStatusProperty != null)
				{
					// Set the ResponseStatus
					ReflectionUtils.SetProperty(responseDto, responseStatusProperty, responseStatus);
				}
			}

			// Return an Error DTO with the exception populated
			return responseDto;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="appHost"></param>
		/// <param name="request"></param>
		/// <param name="ex"></param>
		/// <returns></returns>
		public static object HandleException(IAppHost appHost, object request, Exception ex)
		{
			if (appHost.Config.ReturnsInnerException && ex.InnerException != null && !(ex is IHttpError))
			{
				ex = ex.InnerException;
			}

			var responseStatus = ex.ToResponseStatus();

			if (appHost.Config.DebugMode)
			{
				// View stack trace in tests and on the client
				responseStatus.StackTrace = GetRequestErrorBody(request) + "\n" + ex;
			}

			Log.Error("ServiceBase<TRequest>::Service Exception", ex);

			//if (iocResolver != null)
			//	LogErrorInRedisIfExists(iocResolver.TryResolve<IRedisClientsManager>(), request.GetType().Name, responseStatus);

			var errorResponse = CreateErrorResponse(request, ex, responseStatus);

			return errorResponse;
		}

		/// <summary>
		/// Service error logs are kept in 'urn:ServiceErrors:{ServiceName}'
		/// </summary>
		public const string UrnServiceErrorType = "ServiceErrors";

		/// <summary>
		/// Combined service error logs are maintained in 'urn:ServiceErrors:All'
		/// </summary>
		public const string CombinedServiceLogId = "All";

		//public static void LogErrorInRedisIfExists(
		//	IRedisClientsManager redisManager, string operationName, ResponseStatus responseStatus)
		//{
		//	//If Redis is configured, maintain rolling service error logs in Redis (an in-memory datastore)
		//	if (redisManager == null) return;
		//	try
		//	{
		//		//Get a thread-safe redis client from the client manager pool
		//		using (var client = redisManager.GetClient())
		//		{
		//			//Get a client with a native interface for storing 'ResponseStatus' objects
		//			var redis = client.GetTypedClient<ResponseStatus>();

		//			//Store the errors in predictable Redis-named lists i.e. 
		//			//'urn:ServiceErrors:{ServiceName}' and 'urn:ServiceErrors:All' 
		//			var redisSeriviceErrorList = redis.Lists[UrnId.Create(UrnServiceErrorType, operationName)];
		//			var redisCombinedErrorList = redis.Lists[UrnId.Create(UrnServiceErrorType, CombinedServiceLogId)];

		//			//Append the error at the start of the service-specific and combined error logs.
		//			redisSeriviceErrorList.Prepend(responseStatus);
		//			redisCombinedErrorList.Prepend(responseStatus);

		//			//Clip old error logs from the managed logs
		//			const int rollingErrorCount = 1000;
		//			redisSeriviceErrorList.Trim(0, rollingErrorCount);
		//			redisCombinedErrorList.Trim(0, rollingErrorCount);
		//		}
		//	}
		//	catch (Exception suppressRedisException)
		//	{
		//		Log.Error("Could not append exception to redis service error logs", suppressRedisException);
		//	}
		//}

		/// <summary>
		/// Override to provide additional/less context about the Service Exception. 
		/// By default the request is serialized and appended to the ResponseStatus StackTrace.
		/// </summary>
		public static string GetRequestErrorBody(object request)
		{
			var requestString = "";
			try
			{
				requestString = TypeSerializer.SerializeToString(request);
			}
			catch /*(Exception ignoreSerializationException)*/
			{
				//Serializing request successfully is not critical and only provides added error info
			}

			return String.Format("[{0}: {1}]:\n[REQUEST: {2}]", (request ?? new object()).GetType().Name, DateTime.UtcNow, requestString);
		}
	}
}
