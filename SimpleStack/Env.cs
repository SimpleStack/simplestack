﻿using System;

namespace SimpleStack
{
	public static class Env
	{
		static Env()
		{
			var platform = (int)Environment.OSVersion.Platform;
			IsUnix = (platform == 4) || (platform == 6) || (platform == 128);

			IsMono = AssemblyUtils.FindType("Mono.Runtime") != null;

			IsMonoTouch = AssemblyUtils.FindType("MonoTouch.Foundation.NSObject") != null;

			SupportsExpressions = SupportsEmit = !IsMonoTouch;

			ServerUserAgent = "SimpleStack/" +
				ServiceStackVersion + " "
				+ Environment.OSVersion.Platform
				+ (IsMono ? "/Mono" : "/.NET")
				+ (IsMonoTouch ? " MonoTouch" : "");
		}

		public static decimal ServiceStackVersion = 3.935m;

		public static bool IsUnix { get; set; }

		public static bool IsMono { get; set; }

		public static bool IsMonoTouch { get; set; }

		public static bool SupportsExpressions { get; set; }

		public static bool SupportsEmit { get; set; }

		public static string ServerUserAgent { get; set; }
	}
}

