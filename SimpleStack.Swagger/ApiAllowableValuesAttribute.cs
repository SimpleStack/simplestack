using System;
using System.Net;

namespace SimpleStack.Swagger
{
	public interface IApiResponseDescription
	{
		/// <summary>
		/// The status code of a response
		/// </summary>
		int StatusCode { get; }

		/// <summary>
		/// The description of a response status code
		/// </summary>
		string Description { get; }
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class ApiResponseAttribute : Attribute, IApiResponseDescription
	{
		public int StatusCode { get; set; }

		public string Description { get; set; }

		public ApiResponseAttribute(HttpStatusCode statusCode, string description)
		{
			StatusCode = (int)statusCode;
			Description = description;
		}

		public ApiResponseAttribute(int statusCode, string description)
		{
			StatusCode = statusCode;
			Description = description;
		}
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class ApiAllowableValuesAttribute : Attribute
	{
		public ApiAllowableValuesAttribute(string name)
		{
			Name = name;
		}
		public ApiAllowableValuesAttribute(string name, int min, int max)
			: this(name)
		{
			Type = "RANGE";
			Min = min;
			Max = max;
		}

		public ApiAllowableValuesAttribute(string name, params string[] values)
			: this(name)
		{
			Type = "LIST";
			Values = values;
		}

		public ApiAllowableValuesAttribute(string name, Type enumType)
			: this(name)
		{
			if (enumType.IsEnum)
			{
				Type = "LIST";
				Values = Enum.GetNames(enumType);
			}
		}

		public ApiAllowableValuesAttribute(string name, Func<string[]> listAction)
			: this(name)
		{
			if (listAction != null)
			{
				Type = "LIST";
				Values = listAction();
			}
		}
		/// <summary>
		/// Gets or sets parameter name with which allowable values will be associated.
		/// </summary>
		public string Name { get; set; }

		public string Type { get; set; }

		public int? Min { get; set; }

		public int? Max { get; set; }

		public String[] Values { get; set; }

		//TODO: should be implemented according to:
		//https://github.com/wordnik/swagger-core/wiki/datatypes
	}
}
