using System;
using System.IO;

namespace SimpleStack.Interfaces
{
	public interface IStreamWriter
	{
		void WriteTo(Stream responseStream);
	}
}

