using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Microsoft.Owin;
using SimpleStack.Extensions;
using SimpleStack.Interfaces;
using SimpleStack.Tools;

namespace SimpleStack
{
	public class OwinRequestWrapper : IHttpRequest
	{
		private static readonly string physicalFilePath;
		private readonly IOwinRequest request;
		private IFile[] _files;
		private MemoryStream bufferedStream;
		protected bool checked_cookies;
		protected bool checked_form;
		protected bool checked_query_string;
		private Dictionary<string, Cookie> cookies;
		private HttpFileCollection files;
		private WebROCollection form;
		private string httpMethod;
		private Dictionary<string, object> items;
		private string pathInfo;
		private NameValueCollection queryString;
		private string remoteIp;
		private string responseContentType;
		protected bool validate_cookies;
		protected bool validate_form;
		protected bool validate_query_string;

		static OwinRequestWrapper()
		{
			physicalFilePath = "~".MapAbsolutePath();
		}

		public OwinRequestWrapper(
			string operationName,
			IOwinRequest request,
			string defaultContentType)
		{
			OperationName = operationName;
			DefaultContentType = defaultContentType;
			this.request = request;
		}

		public IOwinRequest Request
		{
			get { return request; }
		}

		public string DefaultContentType { get; set; }

		public Encoding ContentEncoding
		{
			get { return Encoding.GetEncoding(request.Headers[HttpHeaders.ContentEncoding] ?? "UTF-8"); }
		}

		public NameValueCollection Form
		{
			get
			{
				if (form == null)
				{
					form = new WebROCollection();
					files = new HttpFileCollection();

					if (IsContentType("multipart/form-data", true))
						LoadMultiPart();
					else if (
						IsContentType("application/x-www-form-urlencoded", true))
						LoadWwwForm();

					form.Protect();
				}

#if NET_4_0
				if (validateRequestNewMode && !checked_form) {
				// Setting this before calling the validator prevents
				// possible endless recursion
				checked_form = true;
				ValidateNameValueCollection ("Form", query_string_nvc, RequestValidationSource.Form);
				} else
				#endif
				if (validate_form && !checked_form)
				{
					checked_form = true;
					ValidateNameValueCollection("Form", form);
				}

				return form;
			}
		}

		public object OriginalRequest
		{
			get { return request; }
		}

		//public OwinRequestWrapper(IOwinRequest request)
		//	: this(null, request) {}

		public string OperationName { get; set; }

		public string GetRawBody()
		{
			if (bufferedStream != null)
			{
				return bufferedStream.ToArray().FromUtf8Bytes();
			}

			using (var reader = new StreamReader(InputStream))
			{
				return reader.ReadToEnd();
			}
		}

		public string RawUrl
		{
			get { return request.Uri.AbsoluteUri; }
		}

		public string AbsoluteUri
		{
			get { return request.Uri.AbsoluteUri.TrimEnd('/'); }
		}

		public string UserHostAddress
		{
			get
			{
				//TODO: vdaron - fix this
				return null; // request.Host.Value; 
			}
		}

		public string XForwardedFor
		{
			get
			{
				return string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedFor])
					       ? null
					       : request.Headers[HttpHeaders.XForwardedFor];
			}
		}

		public string XRealIp
		{
			get { return string.IsNullOrEmpty(request.Headers[HttpHeaders.XRealIp]) ? null : request.Headers[HttpHeaders.XRealIp]; }
		}

		public string RemoteIp
		{
			get
			{
				return remoteIp ??
				       (remoteIp = XForwardedFor ??
				                   (XRealIp ??
				                    ((request.RemoteIpAddress != null) ? request.RemoteIpAddress : null)));
			}
		}

		public bool IsSecureConnection
		{
			get { return request.Scheme == "https"; }
		}

		public string[] AcceptTypes
		{
			get { return request.Accept != null ? request.Accept.Split(new[] {','}) : null; }
		}

		public Dictionary<string, object> Items
		{
			get { return items ?? (items = new Dictionary<string, object>()); }
		}

		public string ResponseContentType
		{
			get { return responseContentType ?? (responseContentType = this.GetResponseContentType(DefaultContentType)); }
			set { responseContentType = value; }
		}

		public string PathInfo
		{
			get
			{
				if (String.IsNullOrEmpty(pathInfo))
				{
					pathInfo = request.Path.HasValue ? request.Path.Value : String.Empty;

					if (!String.IsNullOrEmpty(EndpointHostConfig.Instance.SimpleStackHandlerFactoryPath))
					{
						if (pathInfo.StartsWith(EndpointHostConfig.Instance.SimpleStackHandlerFactoryPath))
						{
							pathInfo = pathInfo.Substring(EndpointHostConfig.Instance.SimpleStackHandlerFactoryPath.Length);
						}
						else
						{
							//The request is not under the AbsolutePath
							return null;
						}
					}
				}
				return pathInfo;
			}
		}

		public IDictionary<string, Cookie> Cookies
		{
			get
			{
				if (cookies == null)
				{
					cookies = new Dictionary<string, Cookie>();
					foreach (var c in request.Cookies)
					{
						cookies.Add(c.Key, new Cookie(c.Key, c.Value));
					}
				}

				return cookies;
			}
		}

		public string UserAgent
		{
			get { return string.IsNullOrEmpty(request.Headers[HttpHeaders.UserAgent]) ? null : request.Headers[HttpHeaders.UserAgent]; }
		}

		public NameValueCollection Headers
		{
			get
			{
				var result = new NameValueCollection();

				foreach (var h in request.Headers)
				{
					if (h.Value.Length > 0)
						result.Add(h.Key, h.Value[0]);
				}

				return result;
			}
		}

		public NameValueCollection QueryString
		{
			get { return queryString ?? (queryString = HttpUtility.ParseQueryString(request.Uri.Query)); }
		}

		public NameValueCollection FormData
		{
			get { return Form; }
		}

		public bool IsLocal
		{
			get
			{
				IPAddress address;
				if (IPAddress.TryParse(request.RemoteIpAddress, out address))
				{
					return IPAddress.IsLoopback(address);
				}
				return false;
			}
		}

		public string HttpMethod
		{
			get
			{
				return httpMethod
				       ?? (httpMethod = Param(HttpHeaders.XHttpMethodOverride)
				                        ?? request.Method);
			}
		}

		public string ContentType
		{
			get { return request.ContentType; }
		}

		public bool UseBufferedStream
		{
			get { return bufferedStream != null; }
			set
			{
				bufferedStream = value
					                 ? bufferedStream ?? new MemoryStream(request.Body.ReadFully())
					                 : null;
			}
		}

		public Stream InputStream
		{
			get { return bufferedStream ?? request.Body; }
		}

		public long ContentLength
		{
			get { return request.Body.Length; }
		}

		public string ApplicationFilePath
		{
			get { return physicalFilePath; }
		}

		public IFile[] Files
		{
			get
			{
				if (_files == null)
				{
					if (files == null)
						return _files = new IFile[0];

					_files = new IFile[files.Count];
					for (int i = 0; i < files.Count; i++)
					{
						HttpPostedFile reqFile = files[i];

						_files[i] = new HttpFile
							{
								ContentType = reqFile.ContentType,
								ContentLength = reqFile.ContentLength,
								FileName = reqFile.FileName,
								InputStream = reqFile.InputStream,
							};
					}
				}
				return _files;
			}
		}

		public string Param(string name)
		{
			return Headers[name]
			       ?? QueryString[name]
			       ?? FormData[name];
		}

		private static Stream GetSubStream(Stream stream)
		{
			if (stream is MemoryStream)
			{
				var other = (MemoryStream) stream;
				try
				{
					return new MemoryStream(other.GetBuffer(), 0, (int) other.Length, false, true);
				}
				catch (UnauthorizedAccessException)
				{
					return new MemoryStream(other.ToArray(), 0, (int) other.Length, false, true);
				}
			}

			return stream;
		}

		private static string GetPathInfo(string fullPath, string mode, string appPath)
		{
			string pathInfo = ResolvePathInfoFromMappedPath(fullPath, mode);
			if (!String.IsNullOrEmpty(pathInfo)) return pathInfo;

			//Wildcard mode relies on this to find work out the handlerPath
			pathInfo = ResolvePathInfoFromMappedPath(fullPath, appPath);
			if (!String.IsNullOrEmpty(pathInfo)) return pathInfo;

			return fullPath;
		}

		private static string ResolvePathInfoFromMappedPath(string fullPath, string mappedPathRoot)
		{
			if (mappedPathRoot == null) return null;

			var sbPathInfo = new StringBuilder();
			string[] fullPathParts = fullPath.Split('/');
			string[] mappedPathRootParts = mappedPathRoot.Split('/');
			int fullPathIndexOffset = mappedPathRootParts.Length - 1;
			bool pathRootFound = false;

			for (int fullPathIndex = 0; fullPathIndex < fullPathParts.Length; fullPathIndex++)
			{
				if (pathRootFound)
				{
					sbPathInfo.Append("/" + fullPathParts[fullPathIndex]);
				}
				else if (fullPathIndex - fullPathIndexOffset >= 0)
				{
					pathRootFound = true;
					for (int mappedPathRootIndex = 0; mappedPathRootIndex < mappedPathRootParts.Length; mappedPathRootIndex++)
					{
						if (
							!String.Equals(fullPathParts[fullPathIndex - fullPathIndexOffset + mappedPathRootIndex],
							               mappedPathRootParts[mappedPathRootIndex], StringComparison.InvariantCultureIgnoreCase))
						{
							pathRootFound = false;
							break;
						}
					}
				}
			}
			if (!pathRootFound) return null;

			string path = sbPathInfo.ToString();
			return path.Length > 1 ? path.TrimEnd('/') : "/";
		}

		private static void EndSubStream(Stream stream)
		{
		}

		public static string GetHandlerPathIfAny(string listenerUrl)
		{
			if (listenerUrl == null) return null;
			int pos = listenerUrl.IndexOf("://", StringComparison.InvariantCultureIgnoreCase);
			if (pos == -1) return null;
			string startHostUrl = listenerUrl.Substring(pos + "://".Length);
			int endPos = startHostUrl.IndexOf('/');
			if (endPos == -1) return null;
			string endHostUrl = startHostUrl.Substring(endPos + 1);
			return String.IsNullOrEmpty(endHostUrl) ? null : endHostUrl.TrimEnd('/');
		}

		public static string NormalizePathInfo(string pathInfo, string handlerPath)
		{
			if (handlerPath != null && pathInfo.TrimStart('/').StartsWith(
				handlerPath, StringComparison.InvariantCultureIgnoreCase))
			{
				return pathInfo.TrimStart('/').Substring(handlerPath.Length);
			}

			return pathInfo;
		}

		internal static string GetParameter(string header, string attr)
		{
			int ap = header.IndexOf(attr);
			if (ap == -1)
				return null;

			ap += attr.Length;
			if (ap >= header.Length)
				return null;

			char ending = header[ap];
			if (ending != '"')
				ending = ' ';

			int end = header.IndexOf(ending, ap + 1);
			if (end == -1)
				return (ending == '"') ? null : header.Substring(ap);

			return header.Substring(ap + 1, end - ap - 1);
		}

		private void LoadMultiPart()
		{
			string boundary = GetParameter(ContentType, "; boundary=");
			if (boundary == null)
				return;

			Stream input = GetSubStream(InputStream);

			//DB: 30/01/11 - Hack to get around non-seekable stream and received HTTP request
			//Not ending with \r\n?
			var ms = new MemoryStream(32*1024);
			input.CopyTo(ms);
			input = ms;
			ms.WriteByte((byte) '\r');
			ms.WriteByte((byte) '\n');

			input.Position = 0;

			//Uncomment to debug
			//var content = new StreamReader(ms).ReadToEnd();
			//Console.WriteLine(boundary + "::" + content);
			//input.Position = 0;

			var multi_part = new HttpMultipart(input, boundary, ContentEncoding);

			HttpMultipart.Element e;
			while ((e = multi_part.ReadNextElement()) != null)
			{
				if (e.Filename == null)
				{
					var copy = new byte[e.Length];

					input.Position = e.Start;
					input.Read(copy, 0, (int) e.Length);

					form.Add(e.Name, (e.Encoding ?? ContentEncoding).GetString(copy));
				}
				else
				{
					//
					// We use a substream, as in 2.x we will support large uploads streamed to disk,
					//
					var sub = new HttpPostedFile(e.Filename, e.ContentType, input, e.Start, e.Length);
					files.AddFile(e.Name, sub);
				}
			}
			EndSubStream(input);
		}

		private static void ThrowValidationException(string name, string key, string value)
		{
			string v = "\"" + value + "\"";
			if (v.Length > 20)
				v = v.Substring(0, 16) + "...\"";

			string msg = String.Format("A potentially dangerous Request.{0} value was " +
			                           "detected from the client ({1}={2}).", name, key, v);

			throw new HttpRequestValidationException(msg);
		}

		private static void ValidateNameValueCollection(string name, NameValueCollection coll)
		{
			if (coll == null)
				return;

			foreach (string key in coll.Keys)
			{
				string val = coll[key];
				if (val != null && val.Length > 0 && IsInvalidString(val))
					ThrowValidationException(name, key, val);
			}
		}

		internal static bool IsInvalidString(string val)
		{
			int validationFailureIndex;

			return IsInvalidString(val, out validationFailureIndex);
		}

		internal static bool IsInvalidString(string val, out int validationFailureIndex)
		{
			validationFailureIndex = 0;

			int len = val.Length;
			if (len < 2)
				return false;

			char current = val[0];
			for (int idx = 1; idx < len; idx++)
			{
				char next = val[idx];
				// See http://secunia.com/advisories/14325
				if (current == '<' || current == '\xff1c')
				{
					if (next == '!' || next < ' '
					    || (next >= 'a' && next <= 'z')
					    || (next >= 'A' && next <= 'Z'))
					{
						validationFailureIndex = idx - 1;
						return true;
					}
				}
				else if (current == '&' && next == '#')
				{
					validationFailureIndex = idx - 1;
					return true;
				}

				current = next;
			}

			return false;
		}

		public void ValidateInput()
		{
			validate_cookies = true;
			validate_query_string = true;
			validate_form = true;
		}

		private bool IsContentType(string ct, bool starts_with)
		{
			if (ct == null || ContentType == null) return false;

			if (starts_with)
				return StrUtils.StartsWith(ContentType, ct, true);

			return String.Compare(ContentType, ct, true, Helpers.InvariantCulture) == 0;
		}

		private void LoadWwwForm()
		{
			using (Stream input = GetSubStream(InputStream))
			{
				using (var s = new StreamReader(input, ContentEncoding))
				{
					var key = new StringBuilder();
					var value = new StringBuilder();
					int c;

					while ((c = s.Read()) != -1)
					{
						if (c == '=')
						{
							value.Length = 0;
							while ((c = s.Read()) != -1)
							{
								if (c == '&')
								{
									AddRawKeyValue(key, value);
									break;
								}
								else
									value.Append((char) c);
							}
							if (c == -1)
							{
								AddRawKeyValue(key, value);
								return;
							}
						}
						else if (c == '&')
							AddRawKeyValue(key, value);
						else
							key.Append((char) c);
					}
					if (c == -1)
						AddRawKeyValue(key, value);

					EndSubStream(input);
				}
			}
		}

		private void AddRawKeyValue(StringBuilder key, StringBuilder value)
		{
			string decodedKey = HttpUtility.UrlDecode(key.ToString(), ContentEncoding);
			form.Add(decodedKey,
			         HttpUtility.UrlDecode(value.ToString(), ContentEncoding));

			key.Length = 0;
			value.Length = 0;
		}

		private class Helpers
		{
			public static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
		}

		public sealed class HttpFileCollection : NameObjectCollectionBase
		{
			internal HttpFileCollection()
			{
			}

			public HttpPostedFile this[string key]
			{
				get { return Get(key); }
			}

			public HttpPostedFile this[int index]
			{
				get { return Get(index); }
			}

			public string[] AllKeys
			{
				get { return BaseGetAllKeys(); }
			}

			internal void AddFile(string name, HttpPostedFile file)
			{
				BaseAdd(name, file);
			}

			public void CopyTo(Array dest, int index)
			{
				/* XXX this is kind of gross and inefficient
				 * since it makes a copy of the superclass's
				 * list */
				object[] values = BaseGetAllValues();
				values.CopyTo(dest, index);
			}

			public string GetKey(int index)
			{
				return BaseGetKey(index);
			}

			public HttpPostedFile Get(int index)
			{
				return (HttpPostedFile) BaseGet(index);
			}

			public HttpPostedFile Get(string key)
			{
				return (HttpPostedFile) BaseGet(key);
			}
		}

		private class HttpMultipart
		{
			private const byte HYPHEN = (byte) '-', LF = (byte) '\n', CR = (byte) '\r';
			private readonly string boundary;
			private readonly byte[] boundary_bytes;
			private readonly byte[] buffer;
			private readonly Stream data;
			private readonly Encoding encoding;
			private readonly StringBuilder sb;
			private bool at_eof;

			// See RFC 2046 
			// In the case of multipart entities, in which one or more different
			// sets of data are combined in a single body, a "multipart" media type
			// field must appear in the entity's header.  The body must then contain
			// one or more body parts, each preceded by a boundary delimiter line,
			// and the last one followed by a closing boundary delimiter line.
			// After its boundary delimiter line, each body part then consists of a
			// header area, a blank line, and a body area.  Thus a body part is
			// similar to an RFC 822 message in syntax, but different in meaning.

			public HttpMultipart(Stream data, string b, Encoding encoding)
			{
				this.data = data;
				//DB: 30/01/11: cannot set or read the Position in HttpListener in Win.NET
				//var ms = new MemoryStream(32 * 1024);
				//data.CopyTo(ms);
				//this.data = ms;

				boundary = b;
				boundary_bytes = encoding.GetBytes(b);
				buffer = new byte[boundary_bytes.Length + 2]; // CRLF or '--'
				this.encoding = encoding;
				sb = new StringBuilder();
			}

			private string ReadLine()
			{
				// CRLF or LF are ok as line endings.
				bool got_cr = false;
				int b = 0;
				sb.Length = 0;
				while (true)
				{
					b = data.ReadByte();
					if (b == -1)
					{
						return null;
					}

					if (b == LF)
					{
						break;
					}
					got_cr = (b == CR);
					sb.Append((char) b);
				}

				if (got_cr)
					sb.Length--;

				return sb.ToString();
			}

			private static string GetContentDispositionAttribute(string l, string name)
			{
				int idx = l.IndexOf(name + "=\"");
				if (idx < 0)
					return null;
				int begin = idx + name.Length + "=\"".Length;
				int end = l.IndexOf('"', begin);
				if (end < 0)
					return null;
				if (begin == end)
					return "";
				return l.Substring(begin, end - begin);
			}

			private string GetContentDispositionAttributeWithEncoding(string l, string name)
			{
				int idx = l.IndexOf(name + "=\"");
				if (idx < 0)
					return null;
				int begin = idx + name.Length + "=\"".Length;
				int end = l.IndexOf('"', begin);
				if (end < 0)
					return null;
				if (begin == end)
					return "";

				string temp = l.Substring(begin, end - begin);
				var source = new byte[temp.Length];
				for (int i = temp.Length - 1; i >= 0; i--)
					source[i] = (byte) temp[i];

				return encoding.GetString(source);
			}

			private bool ReadBoundary()
			{
				try
				{
					string line = ReadLine();
					while (line == "")
						line = ReadLine();
					if (line[0] != '-' || line[1] != '-')
						return false;

					if (!StrUtils.EndsWith(line, boundary, false))
						return true;
				}
				catch
				{
				}

				return false;
			}

			private string ReadHeaders()
			{
				string s = ReadLine();
				if (s == "")
					return null;

				return s;
			}

			private bool CompareBytes(byte[] orig, byte[] other)
			{
				for (int i = orig.Length - 1; i >= 0; i--)
					if (orig[i] != other[i])
						return false;

				return true;
			}

			private long MoveToNextBoundary()
			{
				long retval = 0;
				bool got_cr = false;

				int state = 0;
				int c = data.ReadByte();
				while (true)
				{
					if (c == -1)
						return -1;

					if (state == 0 && c == LF)
					{
						retval = data.Position - 1;
						if (got_cr)
							retval--;
						state = 1;
						c = data.ReadByte();
					}
					else if (state == 0)
					{
						got_cr = (c == CR);
						c = data.ReadByte();
					}
					else if (state == 1 && c == '-')
					{
						c = data.ReadByte();
						if (c == -1)
							return -1;

						if (c != '-')
						{
							state = 0;
							got_cr = false;
							continue; // no ReadByte() here
						}

						int nread = data.Read(buffer, 0, buffer.Length);
						int bl = buffer.Length;
						if (nread != bl)
							return -1;

						if (!CompareBytes(boundary_bytes, buffer))
						{
							state = 0;
							data.Position = retval + 2;
							if (got_cr)
							{
								data.Position++;
								got_cr = false;
							}
							c = data.ReadByte();
							continue;
						}

						if (buffer[bl - 2] == '-' && buffer[bl - 1] == '-')
						{
							at_eof = true;
						}
						else if (buffer[bl - 2] != CR || buffer[bl - 1] != LF)
						{
							state = 0;
							data.Position = retval + 2;
							if (got_cr)
							{
								data.Position++;
								got_cr = false;
							}
							c = data.ReadByte();
							continue;
						}
						data.Position = retval + 2;
						if (got_cr)
							data.Position++;
						break;
					}
					else
					{
						// state == 1
						state = 0; // no ReadByte() here
					}
				}

				return retval;
			}

			public Element ReadNextElement()
			{
				if (at_eof || ReadBoundary())
					return null;

				var elem = new Element();
				string header;
				while ((header = ReadHeaders()) != null)
				{
					if (StrUtils.StartsWith(header, "Content-Disposition:", true))
					{
						elem.Name = GetContentDispositionAttribute(header, "name");
						elem.Filename = StripPath(GetContentDispositionAttributeWithEncoding(header, "filename"));
					}
					else if (StrUtils.StartsWith(header, "Content-Type:", true))
					{
						elem.ContentType = header.Substring("Content-Type:".Length).Trim();

						int csindex = elem.ContentType.IndexOf("utf-8", StringComparison.InvariantCultureIgnoreCase);
						if (csindex > 0)
							elem.Encoding = Encoding.UTF8;
						//TODO: add more encoding support 
					}
				}

				long start = 0;
				start = data.Position;
				elem.Start = start;
				long pos = MoveToNextBoundary();
				if (pos == -1)
					return null;

				elem.Length = pos - start;
				return elem;
			}

			private static string StripPath(string path)
			{
				if (path == null || path.Length == 0)
					return path;

				if (path.IndexOf(":\\") != 1 && !path.StartsWith("\\\\"))
					return path;
				return path.Substring(path.LastIndexOf('\\') + 1);
			}

			public class Element
			{
				public string ContentType;
				public Encoding Encoding;
				public string Filename;
				public long Length;
				public string Name;
				public long Start;

				public override string ToString()
				{
					return "ContentType " + ContentType + ", Name " + Name + ", Filename " + Filename + ", Start " +
					       Start.ToString() + ", Length " + Length.ToString();
				}
			}
		}

		public sealed class HttpPostedFile
		{
			private readonly string content_type;
			private readonly string name;
			private readonly Stream stream;

			internal HttpPostedFile(string name, string content_type, Stream base_stream, long offset, long length)
			{
				this.name = name;
				this.content_type = content_type;
				stream = new ReadSubStream(base_stream, offset, length);
			}

			public string ContentType
			{
				get { return (content_type); }
			}

			public int ContentLength
			{
				get { return (int) stream.Length; }
			}

			public string FileName
			{
				get { return (name); }
			}

			public Stream InputStream
			{
				get { return (stream); }
			}

			public void SaveAs(string filename)
			{
				var buffer = new byte[16*1024];
				long old_post = stream.Position;

				try
				{
					File.Delete(filename);
					using (FileStream fs = File.Create(filename))
					{
						stream.Position = 0;
						int n;

						while ((n = stream.Read(buffer, 0, 16*1024)) != 0)
						{
							fs.Write(buffer, 0, n);
						}
					}
				}
				finally
				{
					stream.Position = old_post;
				}
			}

			private class ReadSubStream : Stream
			{
				private readonly long end;
				private readonly long offset;
				private readonly Stream s;
				private long position;

				public ReadSubStream(Stream s, long offset, long length)
				{
					this.s = s;
					this.offset = offset;
					end = offset + length;
					position = offset;
				}

				public override bool CanRead
				{
					get { return true; }
				}

				public override bool CanSeek
				{
					get { return true; }
				}

				public override bool CanWrite
				{
					get { return false; }
				}

				public override long Length
				{
					get { return end - offset; }
				}

				public override long Position
				{
					get { return position - offset; }
					set
					{
						if (value > Length)
							throw new ArgumentOutOfRangeException();

						position = Seek(value, SeekOrigin.Begin);
					}
				}

				public override void Flush()
				{
				}

				public override int Read(byte[] buffer, int dest_offset, int count)
				{
					if (buffer == null)
						throw new ArgumentNullException("buffer");

					if (dest_offset < 0)
						throw new ArgumentOutOfRangeException("dest_offset", "< 0");

					if (count < 0)
						throw new ArgumentOutOfRangeException("count", "< 0");

					int len = buffer.Length;
					if (dest_offset > len)
						throw new ArgumentException("destination offset is beyond array size");
					// reordered to avoid possible integer overflow
					if (dest_offset > len - count)
						throw new ArgumentException("Reading would overrun buffer");

					if (count > end - position)
						count = (int) (end - position);

					if (count <= 0)
						return 0;

					s.Position = position;
					int result = s.Read(buffer, dest_offset, count);
					if (result > 0)
						position += result;
					else
						position = end;

					return result;
				}

				public override int ReadByte()
				{
					if (position >= end)
						return -1;

					s.Position = position;
					int result = s.ReadByte();
					if (result < 0)
						position = end;
					else
						position++;

					return result;
				}

				public override long Seek(long d, SeekOrigin origin)
				{
					long real;
					switch (origin)
					{
						case SeekOrigin.Begin:
							real = offset + d;
							break;
						case SeekOrigin.End:
							real = end + d;
							break;
						case SeekOrigin.Current:
							real = position + d;
							break;
						default:
							throw new ArgumentException();
					}

					long virt = real - offset;
					if (virt < 0 || virt > Length)
						throw new ArgumentException();

					position = s.Seek(real, SeekOrigin.Begin);
					return position;
				}

				public override void SetLength(long value)
				{
					throw new NotSupportedException();
				}

				public override void Write(byte[] buffer, int offset, int count)
				{
					throw new NotSupportedException();
				}
			}
		}

		internal static class StrUtils
		{
			public static bool StartsWith(string str1, string str2)
			{
				return StartsWith(str1, str2, false);
			}

			public static bool StartsWith(string str1, string str2, bool ignore_case)
			{
				int l2 = str2.Length;
				if (l2 == 0)
					return true;

				int l1 = str1.Length;
				if (l2 > l1)
					return false;

				return (0 == String.Compare(str1, 0, str2, 0, l2, ignore_case, Helpers.InvariantCulture));
			}

			public static bool EndsWith(string str1, string str2)
			{
				return EndsWith(str1, str2, false);
			}

			public static bool EndsWith(string str1, string str2, bool ignore_case)
			{
				int l2 = str2.Length;
				if (l2 == 0)
					return true;

				int l1 = str1.Length;
				if (l2 > l1)
					return false;

				return (0 == String.Compare(str1, l1 - l2, str2, 0, l2, ignore_case, Helpers.InvariantCulture));
			}
		}

		private class WebROCollection : NameValueCollection
		{
			private bool got_id;
			private int id;

			public bool GotID
			{
				get { return got_id; }
			}

			public int ID
			{
				get { return id; }
				set
				{
					got_id = true;
					id = value;
				}
			}

			public void Protect()
			{
				IsReadOnly = true;
			}

			public void Unprotect()
			{
				IsReadOnly = false;
			}

			public override string ToString()
			{
				var result = new StringBuilder();
				foreach (string key in AllKeys)
				{
					if (result.Length > 0)
						result.Append('&');

					if (key != null && key.Length > 0)
					{
						result.Append(key);
						result.Append('=');
					}
					result.Append(Get(key));
				}

				return result.ToString();
			}
		}
	}
}