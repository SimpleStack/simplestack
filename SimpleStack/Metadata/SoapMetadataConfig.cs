﻿using System;

namespace SimpleStack.Metadata
{
	public class SoapMetadataConfig : MetadataConfig
	{
		public SoapMetadataConfig(string format, string name, string syncReplyUri, string asyncOneWayUri, string defaultMetadataUri, string wsdlMetadataUri)
			: base(format, name, syncReplyUri, asyncOneWayUri, defaultMetadataUri)
		{
			WsdlMetadataUri = wsdlMetadataUri;
		}

		public string WsdlMetadataUri { get; set; }
	}
}

