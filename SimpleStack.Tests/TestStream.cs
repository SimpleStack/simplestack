using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace SimpleStack.Tests
{
	class TestStream : MemoryStream
	{
		protected override void Dispose(bool disposing)
		{
			
		}

		public void Terminate()
		{
			base.Dispose(true);
		}
		
	}
}
