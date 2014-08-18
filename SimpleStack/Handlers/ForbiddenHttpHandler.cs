﻿using System;
using System.Threading.Tasks;
using SimpleStack.Interfaces;
using System.Collections.Generic;
using SimpleStack.Extensions;
using SimpleStack.Tools;

namespace SimpleStack.Handlers
{
	public class ForbiddenHttpHandler : ISimpleStackHttpHandler
	{
		public bool? IsIntegratedPipeline { get; set; }
		public string WebHostPhysicalPath { get; set; }
		public List<string> WebHostRootFileNames { get; set; }
		public string ApplicationBaseUrl { get; set; }
		public string DefaultRootFileName { get; set; }
		public string DefaultHandler { get; set; }

		public void ProcessRequest(IHttpRequest request, IHttpResponse response, string operationName)
		{
			response.ContentType = "text/plain";
			response.StatusCode = 403;

			response.EndHttpRequest(skipClose: true, afterBody: r => {
				r.Write("Forbidden\n\n");

				r.Write("\nRequest.HttpMethod: " + request.HttpMethod);
				r.Write("\nRequest.PathInfo: " + request.PathInfo);
				r.Write("\nRequest.QueryString: " + request.QueryString);
				r.Write("\nRequest.RawUrl: " + request.RawUrl);

				if (IsIntegratedPipeline.HasValue)
					r.Write("\nApp.IsIntegratedPipeline: " + IsIntegratedPipeline);
				if (!WebHostPhysicalPath.IsNullOrEmpty())
					r.Write("\nApp.WebHostPhysicalPath: " + WebHostPhysicalPath);
				if (!WebHostRootFileNames.IsEmpty())
					r.Write("\nApp.WebHostRootFileNames: " + TypeSerializer.SerializeToString(WebHostRootFileNames));
				if (!ApplicationBaseUrl.IsNullOrEmpty())
					r.Write("\nApp.ApplicationBaseUrl: " + ApplicationBaseUrl);
				if (!DefaultRootFileName.IsNullOrEmpty())
					r.Write("\nApp.DefaultRootFileName: " + DefaultRootFileName);
				if (!DefaultHandler.IsNullOrEmpty())
					r.Write("\nApp.DefaultHandler: " + DefaultHandler);
				if (!SimpleStackHttpHandlerFactory.DebugLastHandlerArgs.IsNullOrEmpty())
					r.Write("\nApp.DebugLastHandlerArgs: " + SimpleStackHttpHandlerFactory.DebugLastHandlerArgs);
			});
		}

//		public void ProcessRequest(HttpContext context)
//		{
//			var request = context.Request;
//			var response = context.Response;
//
//			response.ContentType = "text/plain";
//			response.StatusCode = 403;
//
//			response.EndHttpRequest(skipClose:true, afterBody: r=> {
//				r.Write("Forbidden\n\n");
//
//				r.Write("\nRequest.HttpMethod: " + request.HttpMethod);
//				r.Write("\nRequest.PathInfo: " + request.PathInfo);
//				r.Write("\nRequest.QueryString: " + request.QueryString);
//				r.Write("\nRequest.RawUrl: " + request.RawUrl);
//
//				if (IsIntegratedPipeline.HasValue)
//					r.Write("\nApp.IsIntegratedPipeline: " + IsIntegratedPipeline);
//				if (!WebHostPhysicalPath.IsNullOrEmpty())
//					r.Write("\nApp.WebHostPhysicalPath: " + WebHostPhysicalPath);
//				if (!WebHostRootFileNames.IsEmpty())
//					r.Write("\nApp.WebHostRootFileNames: " + TypeSerializer.SerializeToString(WebHostRootFileNames));
//				if (!ApplicationBaseUrl.IsNullOrEmpty())
//					r.Write("\nApp.ApplicationBaseUrl: " + ApplicationBaseUrl);
//				if (!DefaultRootFileName.IsNullOrEmpty())
//					r.Write("\nApp.DefaultRootFileName: " + DefaultRootFileName);
//			});
//		}

		public bool IsReusable
		{
			get { return true; }
		}
	}
}

