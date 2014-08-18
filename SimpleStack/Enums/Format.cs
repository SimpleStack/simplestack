using System;

namespace SimpleStack.Enums
{
	public enum Format : long
	{
		Soap11 = 1 << 15,
		Soap12 = 1 << 16,
		Xml = 1 << 17,
		Json = 1 << 18,
		Jsv = 1 << 19,
		ProtoBuf = 1 << 20,
		Csv = 1 << 21,
		Html = 1 << 22,
		Yaml = 1 << 23,
		MsgPack = 1 << 24,
		Other = 1 << 25,
	}
}

