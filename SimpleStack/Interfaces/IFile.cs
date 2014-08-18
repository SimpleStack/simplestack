using System;
using System.IO;

namespace SimpleStack.Interfaces
{
	public interface IFile
	{
		string FileName { get; }
		long ContentLength { get; }
		string ContentType { get; }
		Stream InputStream { get; }
	}
}

