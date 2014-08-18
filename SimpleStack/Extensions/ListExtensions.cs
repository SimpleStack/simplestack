using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SimpleStack.Extensions
{
	public static class ListExtensions
	{
		public static bool IsNullOrEmpty<T>(this List<T> list)
		{
			if (list != null)
				return list.Count == 0;
			else
				return true;
		}

		public static IEnumerable<TFrom> SafeWhere<TFrom>(this List<TFrom> list, Func<TFrom, bool> predicate)
		{
			return Enumerable.Where<TFrom>((IEnumerable<TFrom>) list, predicate);
		}

		public static int NullableCount<T>(this List<T> list)
		{
			if (list != null)
				return list.Count;
			else
				return 0;
		}

		public static void AddIfNotExists<T>(this List<T> list, T item)
		{
			if (list.Contains(item))
				return;
			list.Add(item);
		}
	}
}

