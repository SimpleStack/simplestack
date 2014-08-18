using System;

namespace SimpleStack.Interfaces
{
	public interface ITypeFactory
	{
		object CreateInstance(Type type);
	}
}

