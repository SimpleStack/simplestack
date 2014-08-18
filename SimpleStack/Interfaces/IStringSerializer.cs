using System;

namespace SimpleStack.Interfaces
{
	public interface IStringSerializer
	{
		string Parse<TFrom>(TFrom from);
	}
}

