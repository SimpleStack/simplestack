using System;
using Funq;
using SimpleStack.Interfaces;
using SimpleStack.Logging;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using SimpleStack.Enums;
using System.Linq;
using Microsoft.Owin;
using System.Text;
using SimpleStack.Extensions;

namespace SimpleStack
{
	class OwinResponseWrapper : IHttpResponse
	{
		private IOwinResponse _response;
		private bool _isClosed = false;

		public OwinResponseWrapper(IAppHost appHost, IOwinResponse response)
		{
			if (appHost == null)
				throw new ArgumentNullException("appHost");
			if (response == null)
				throw new ArgumentNullException("response");

			AppHost = appHost;

			this._response = response;
		}

		#region IHttpResponse implementation

		public void AddHeader (string name, string value)
		{
			_response.Headers.Add(name, new string[]{value});
		}

		public void Redirect (string url)
		{
			_response.Redirect (url);
		}

		public void Write (string text)
		{
			_response.Write (text);
		}

		public void Close ()
		{
			_isClosed = true;
		}

		public void End ()
		{
			_isClosed = true;
		}

		public void Flush ()
		{
		
		}

		public void SetContentLength (long contentLength)
		{
			_response.ContentLength = contentLength;
		}

		public IAppHost AppHost { get; private set; }

		public object OriginalResponse {
			get {
				return _response;
			}
		}

		public int StatusCode {
			get {
				return _response.StatusCode;
			}
			set {
				_response.StatusCode = value;
			}
		}

		//TODO
		public string StatusDescription {
			get {
				return StatusCode == 200 ? "Ok" : "Error";
			}
			set {

			}
		}

		public string ContentType {
			get {
				return _response.ContentType;
			}
			set {
				_response.ContentType = value;
			}
		}

		public ICookies Cookies {
			get {
				return new CookiesCollections(_response.Cookies);
			}
		}

		public Stream OutputStream {
			get {
				return _response.Body;
			}
		}

		public bool IsClosed {
			get {
				return _isClosed;
			}
		}

		#endregion

		class CookiesCollections : ICookies
		{
			ResponseCookieCollection cookies;

			public CookiesCollections(ResponseCookieCollection cookies)
			{
				if(cookies == null)
					throw new ArgumentNullException("cookies");


				this.cookies = cookies;
			}

			#region ICookies implementation

			public void DeleteCookie(string cookieName)
			{
				cookies.Delete(cookieName);
			}

			public void AddCookie(System.Net.Cookie cookie)
			{
				CookieOptions options = new CookieOptions();
				options.Domain = cookie.Domain;
				options.Expires = cookie.Expires;
				options.HttpOnly = cookie.HttpOnly;
				options.Path = cookie.Path;
				options.Secure = cookie.Secure;

				cookies.Append(cookie.Name, cookie.Value,options);
			}

			public void AddPermanentCookie(string cookieName, string cookieValue, bool? secureOnly = default(bool?))
			{
				CookieOptions options = new CookieOptions();
				options.Expires = DateTime.MaxValue;
				options.Secure = secureOnly.HasValue ? secureOnly.Value : false;;

				cookies.Append(cookieName, cookieValue,options);
			}

			public void AddSessionCookie(string cookieName, string cookieValue, bool? secureOnly = default(bool?))
			{
				CookieOptions options = new CookieOptions();
				options.Expires = null;
				options.Secure = secureOnly.HasValue ? secureOnly.Value : false;

				cookies.Append(cookieName, cookieValue,options);
			}

			#endregion


		}
	}

}

