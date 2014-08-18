using System;

namespace SimpleStack.Enums
{
	[Flags]
	public enum EndpointAttributes : long
	{
		None = 0,

		Any = AnyNetworkAccessType | AnySecurityMode | AnyHttpMethod | AnyCallStyle | AnyFormat,
		AnyNetworkAccessType = External | Localhost | LocalSubnet,
		AnySecurityMode = Secure | InSecure,
		AnyHttpMethod = HttpHead | HttpGet | HttpPost | HttpPut | HttpDelete | HttpOther,
		AnyCallStyle = OneWay | Reply,
		AnyFormat = Soap11 | Soap12 | Xml | Json | Jsv | Html | ProtoBuf | Csv | MsgPack | Yaml | FormatOther,
		AnyEndpoint = Http | MessageQueue | Tcp | EndpointOther,
		InternalNetworkAccess = Localhost | LocalSubnet,

		//Whether it came from an Internal or External address
		Localhost = 1 << 0,
		LocalSubnet = 1 << 1,
		External = 1 << 2,

		//Called over a secure or insecure channel
		Secure = 1 << 3,
		InSecure = 1 << 4,

		//HTTP request type
		HttpHead = 1 << 5,
		HttpGet = 1 << 6,
		HttpPost = 1 << 7,
		HttpPut = 1 << 8,
		HttpDelete = 1 << 9,
		HttpPatch = 1 << 10,
		HttpOptions = 1 << 11,
		HttpOther = 1 << 12,

		//Call Styles
		OneWay = 1 << 13,
		Reply = 1 << 14,

		//Different formats
		Soap11 = 1 << 15,
		Soap12 = 1 << 16,
		//POX
		Xml = 1 << 17,
		//Javascript
		Json = 1 << 18,
		//Jsv i.e. TypeSerializer
		Jsv = 1 << 19,
		//e.g. protobuf-net
		ProtoBuf = 1 << 20,
		//e.g. text/csv
		Csv = 1 << 21,
		Html = 1 << 22,
		Yaml = 1 << 23,
		MsgPack = 1 << 24,
		FormatOther = 1 << 25,

		//Different endpoints
		Http = 1 << 26,
		MessageQueue = 1 << 27,
		Tcp = 1 << 28,
		EndpointOther = 1 << 29,
	}
}

