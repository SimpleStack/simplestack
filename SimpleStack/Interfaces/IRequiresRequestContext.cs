using System;
using Microsoft.Owin;

namespace SimpleStack.Interfaces
{
	/// <summary>
	/// Implement on services that need access to the RequestContext
	/// </summary>
	public interface IRequiresRequestContext
	{
		IRequestContext RequestContext { get; set; }
	}
}

