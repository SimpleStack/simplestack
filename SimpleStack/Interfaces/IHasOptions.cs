using System;
using System.Collections.Generic;

namespace SimpleStack.Interfaces
{
	public interface IHasOptions
	{
		IDictionary<string, string> Options { get; }
	}
}

