using System;
using System.Reflection;
using System.Collections.Generic;

namespace SimpleStack.Extensions
{
	public static class NServiceExecExtensions
	{
		public static IEnumerable<MethodInfo> GetActions(this Type serviceType)
		{
			foreach (var mi in serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
			{
				if (mi.GetParameters().Length != 1)
					continue;

				var actionName = mi.Name.ToUpper();
				if (!HttpMethods.AllVerbs.Contains(actionName) && actionName != ActionContext.AnyAction)
					continue;

				yield return mi;
			}
		}
	}
}

