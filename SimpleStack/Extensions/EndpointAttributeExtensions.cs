using System;
using SimpleStack.Enums;
using SimpleStack.Handlers;
using SimpleStack.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace SimpleStack.Extensions
{

	public static class EndpointAttributesExtensions
	{
		public static bool IsLocalhost(this EndpointAttributes attrs)
		{
			return (EndpointAttributes.Localhost & attrs) == EndpointAttributes.Localhost;
		}

		public static bool IsLocalSubnet(this EndpointAttributes attrs)
		{
			return (EndpointAttributes.LocalSubnet & attrs) == EndpointAttributes.LocalSubnet;
		}

		public static bool IsExternal(this EndpointAttributes attrs)
		{
			return (EndpointAttributes.External & attrs) == EndpointAttributes.External;
		}

		public static Format ToFormat(this string format)
		{
			try
			{
				return (Format)Enum.Parse(typeof(Format), format.ToUpper().Replace("X-", ""), true);
			}
			catch (Exception)
			{
				return Format.Other;
			}
		}

		public static string FromFormat(this Format format)
		{
			var formatStr = format.ToString().ToLower();
			if (format == Format.ProtoBuf || format == Format.MsgPack)
				return "x-" + formatStr;
			return formatStr;
		}

		public static Format ToFormat(this Feature feature)
		{
			switch (feature)
			{
			case Feature.Xml:
				return Format.Xml;
			case Feature.Json:
				return Format.Json;
			case Feature.Jsv:
				return Format.Jsv;
			case Feature.Csv:
				return Format.Csv;
			case Feature.Html:
				return Format.Html;
			case Feature.MsgPack:
				return Format.MsgPack;
			case Feature.ProtoBuf:
				return Format.ProtoBuf;
			case Feature.Soap11:
				return Format.Soap11;
			case Feature.Soap12:
				return Format.Soap12;
			}
			return Format.Other;
		}

		public static Feature ToFeature(this Format format)
		{
			switch (format)
			{
			case Format.Xml:
				return Feature.Xml;
			case Format.Json:
				return Feature.Json;
			case Format.Jsv:
				return Feature.Jsv;
			case Format.Csv:
				return Feature.Csv;
			case Format.Html:
				return Feature.Html;
			case Format.MsgPack:
				return Feature.MsgPack;
			case Format.ProtoBuf:
				return Feature.ProtoBuf;
			case Format.Soap11:
				return Feature.Soap11;
			case Format.Soap12:
				return Feature.Soap12;
			}
			return Feature.CustomFormat;
		}

		public static Feature ToSoapFeature(this EndpointAttributes attributes)
		{
			if ((EndpointAttributes.Soap11 & attributes) == EndpointAttributes.Soap11)
				return Feature.Soap11;
			if ((EndpointAttributes.Soap12 & attributes) == EndpointAttributes.Soap12)
				return Feature.Soap12;            
			return Feature.None;
		}

		public static EndpointAttributes GetAttributes(IPAddress ipAddress)
		{
			if (IPAddress.IsLoopback(ipAddress))
				return EndpointAttributes.Localhost;

			return IsInLocalSubnet(ipAddress)
				? EndpointAttributes.LocalSubnet
					: EndpointAttributes.External;
		}

		public static EndpointAttributes GetAttributes(this IHttpRequest request)
		{
			if (EndpointHost.DebugMode
			    && request.QueryString != null) { //Mock<IHttpRequest>
				var simulate = request.QueryString ["simulate"];
				if (simulate != null) {
					return ToEndpointAttributes(simulate.Split (','));
				}
			}

			var portRestrictions = EndpointAttributes.None;

			portRestrictions |= HttpMethods.GetEndpointAttribute (request.HttpMethod);
			portRestrictions |= request.IsSecureConnection ? EndpointAttributes.Secure : EndpointAttributes.InSecure;

			if (request.UserHostAddress != null) {
				var isIpv4Address = request.UserHostAddress.IndexOf ('.') != -1
				                    && request.UserHostAddress.IndexOf ("::", StringComparison.InvariantCulture) == -1;

				string ipAddressNumber = null;
				if (isIpv4Address) {
					ipAddressNumber = request.UserHostAddress.SplitOnFirst (":") [0];
				} else {
					if (request.UserHostAddress.Contains ("]:")) {
						ipAddressNumber = request.UserHostAddress.SplitOnLast (":") [0];
					} else {
						ipAddressNumber = request.UserHostAddress.LastIndexOf ("%", StringComparison.InvariantCulture) > 0 ?
							request.UserHostAddress.SplitOnLast (":") [0] :
							request.UserHostAddress;
					}
				}

				try {
					ipAddressNumber = ipAddressNumber.SplitOnFirst (',') [0];
					var ipAddress = ipAddressNumber.StartsWith ("::1")
						? IPAddress.IPv6Loopback
						: IPAddress.Parse (ipAddressNumber);
					portRestrictions |= GetAttributes (ipAddress);
				} catch (Exception ex) {
					throw new ArgumentException ("Could not parse Ipv{0} Address: {1} / {2}"
						.Fmt ((isIpv4Address ? 4 : 6), request.UserHostAddress, ipAddressNumber), ex);
				}
			}

			return portRestrictions;
		}

		public static EndpointAttributes ToEndpointAttributes(string[] attrNames)
		{
			var attrs = EndpointAttributes.None;
			foreach (var simulatedAttr in attrNames)
			{
				var attr = (EndpointAttributes)Enum.Parse(typeof(EndpointAttributes), simulatedAttr, true);
				attrs |= attr;
			}
			return attrs;
		}

		public static bool Has<T>(this Enum @enum, T value)
		{
			var enumType = Enum.GetUnderlyingType(@enum.GetType());
			if (enumType == typeof(int))
				return (((int)(object)@enum & (int)(object)value) == (int)(object)value);
			if (enumType == typeof(long))
				return (((long)(object)@enum & (long)(object)value) == (long)(object)value);
			if (enumType == typeof(byte))
				return (((byte)(object)@enum & (byte)(object)value) == (byte)(object)value);

			throw new NotSupportedException("Enums of type {0}".Fmt(enumType.Name));
		}

		public static bool IsInLocalSubnet(IPAddress ipAddress)
		{
			var ipAddressBytes = ipAddress.GetAddressBytes();
			switch (ipAddress.AddressFamily)
			{
			case AddressFamily.InterNetwork:
				foreach (var localIpv4AddressAndMask in EndpointHandlerBase.NetworkInterfaceIpv4Addresses)
				{
					if (ipAddressBytes.IsInSameIpv4Subnet(localIpv4AddressAndMask.Key, localIpv4AddressAndMask.Value))
					{
						return true;
					}
				}
				break;

			case AddressFamily.InterNetworkV6:
				foreach (var localIpv6Address in EndpointHandlerBase.NetworkInterfaceIpv6Addresses)
				{
					if (ipAddressBytes.IsInSameIpv6Subnet(localIpv6Address))
					{
						return true;
					}
				}
				break;
			}

			return false;
		}
	}
}

