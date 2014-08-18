using System;
using System.Runtime.Serialization;

namespace SimpleStack
{
	/// <summary>
	/// Error information pertaining to a particular named field.
	/// Used for returning multiple field validation errors.s
	/// </summary>
	[DataContract]
	public class ResponseError
	{
		[DataMember(Order = 1)]
		public string ErrorCode { get; set; }

		[DataMember(Order = 2)]
		public string FieldName { get; set; }

		[DataMember(Order = 3)]
		public string Message { get; set; }
	}
}

