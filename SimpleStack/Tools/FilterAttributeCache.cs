using System;
using System.Collections.Generic;
using System.Threading;
using SimpleStack.Interfaces;

namespace SimpleStack.Tools
{
	public static class FilterAttributeCache
	{
		private static Dictionary<Type, IHasRequestFilter[]> _requestFilterAttributes
		= new Dictionary<Type, IHasRequestFilter[]>();

		private static Dictionary<Type, IHasResponseFilter[]> _responseFilterAttributes
		= new Dictionary<Type, IHasResponseFilter[]>();

		private static IHasRequestFilter[] ShallowCopy(this IHasRequestFilter[] filters)
		{
			var to = new IHasRequestFilter[filters.Length];
			for (int i = 0; i < filters.Length; i++)
			{
				to[i] = filters[i].Copy();
			}
			return to;
		}

		private static IHasResponseFilter[] ShallowCopy(this IHasResponseFilter[] filters)
		{
			var to = new IHasResponseFilter[filters.Length];
			for (int i = 0; i < filters.Length; i++)
			{
				to[i] = filters[i].Copy();
			}
			return to;
		}

		public static IHasRequestFilter[] GetRequestFilterAttributes(Type requestDtoType, ServiceMetadata metadata)
		{
			IHasRequestFilter[] attrs;
			if (_requestFilterAttributes.TryGetValue(requestDtoType, out attrs)) return attrs.ShallowCopy();

			var attributes = new List<IHasRequestFilter>(
				(IHasRequestFilter[])requestDtoType.GetCustomAttributes(typeof(IHasRequestFilter), true));

			var serviceType = metadata.GetServiceTypeByRequest(requestDtoType);
			attributes.AddRange(
				(IHasRequestFilter[])serviceType.GetCustomAttributes(typeof(IHasRequestFilter), true));

			attributes.Sort((x,y) => x.Priority - y.Priority);
			attrs = attributes.ToArray();

			Dictionary<Type, IHasRequestFilter[]> snapshot, newCache;
			do
			{
				snapshot = _requestFilterAttributes;
				newCache = new Dictionary<Type, IHasRequestFilter[]>(_requestFilterAttributes);
				newCache[requestDtoType] = attrs;

			} while (!ReferenceEquals(
				Interlocked.CompareExchange(ref _requestFilterAttributes, newCache, snapshot), snapshot));

			return attrs.ShallowCopy();
		}

		public static IHasResponseFilter[] GetResponseFilterAttributes(Type responseDtoType, ServiceMetadata metadata)
		{
			IHasResponseFilter[] attrs;
			if (_responseFilterAttributes.TryGetValue(responseDtoType, out attrs)) return attrs.ShallowCopy();

			var attributes = new List<IHasResponseFilter>(
				(IHasResponseFilter[])responseDtoType.GetCustomAttributes(typeof(IHasResponseFilter), true));

			var serviceType = metadata.GetServiceTypeByResponse(responseDtoType);
			if (serviceType != null)
			{
				attributes.AddRange(
					(IHasResponseFilter[])serviceType.GetCustomAttributes(typeof(IHasResponseFilter), true));
			}

			attributes.Sort((x, y) => x.Priority - y.Priority);
			attrs = attributes.ToArray();

			Dictionary<Type, IHasResponseFilter[]> snapshot, newCache;
			do
			{
				snapshot = _responseFilterAttributes;
				newCache = new Dictionary<Type, IHasResponseFilter[]>(_responseFilterAttributes);
				newCache[responseDtoType] = attrs;

			} while (!ReferenceEquals(
				Interlocked.CompareExchange(ref _responseFilterAttributes, newCache, snapshot), snapshot));

			return attrs.ShallowCopy();
		}
	}
}

