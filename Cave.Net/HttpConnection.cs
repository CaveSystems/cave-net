using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using Cave.IO;

#if !NETSTANDARD2_0_OR_GREATER && !NET5_0_OR_GREATER
using System.Net.Configuration;
using System.Diagnostics;
using System.Reflection;
#endif

namespace Cave.Net
{
    /// <summary>Provides a simple asynchronous http fetch.</summary>
    public sealed class HttpConnection
    {
#if !NETSTANDARD2_0_OR_GREATER && !NET5_0_OR_GREATER
        static HttpConnection()
        {
            try
            {
                new HttpWebRequestElement().UseUnsafeHeaderParsing = true;
                return;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            try
            {
                var outerType = typeof(SettingsSection);
                var asm = Assembly.GetAssembly(outerType);
                if (asm != null)
                {
                    // Use the assembly in order to get the internal type for the internal class
                    var type = asm.GetType("System.Net.Configuration.SettingsSectionInternal");
                    if (type != null)
                    {
                        // Use the internal static property to get an instance of the internal settings class. If the static instance isn't created allready the
                        // property will create it for us.
                        var obj = type.InvokeMember("Section", BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] { });
                        if (obj != null)
                        {
                            // Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
                            var field = type.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (field != null)
                            {
                                field.SetValue(obj, true);
                                Trace.WriteLine("UseUnsafeHeaderParsing enabled.");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            Trace.WriteLine("UseUnsafeHeaderParsing disabled.");
        }
#endif

        #region Public Constructors

        /// <summary>Initializes a new instance of the <see cref="HttpConnection"/> class.</summary>
        public HttpConnection()
        {
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>The accept string.</summary>
        public string Accept { get; set; }

        /// Gets or sets the
        /// <see cref="RequestCachePolicy"/>
        /// .
        public RequestCachePolicy CachePolicy { get; set; } = HttpWebRequest.DefaultCachePolicy;

        /// <summary>Gets or sets the <see cref="CookieContainer"/>.</summary>
        public CookieContainer Cookies { get; set; } = new CookieContainer();

        /// <summary>The headers to use.</summary>
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        /// <summary>Gets or sets a value indicating if caching shall be prevented using multiple measures.</summary>
        /// <remarks>
        /// This is setting <see cref="CachePolicy"/> to <see cref="HttpRequestCacheLevel.NoCacheNoStore"/>, <see cref="HttpRequestHeader.IfModifiedSince"/> and
        /// Header[Pragma] = no-cache and Header[RequestId] = new guid.
        /// </remarks>
        public bool PreventCaching { get; set; }

        /// <summary>Gets or sets the protocol version.</summary>
        /// <value>The protocol version.</value>
        public Version ProtocolVersion { get; set; } = new("1.1");

        /// <summary>Gets or sets the proxy.</summary>
        /// <value>The proxy.</value>
        public IWebProxy Proxy { get; set; }

        /// <summary>Gets or sets the referer.</summary>
        /// <value>The referer.</value>
        public string Referer { get; set; }

        /// <summary>Download Timeout.</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>Gets or sets the user agent.</summary>
        /// <value>The user agent.</value>
        public string UserAgent { get; set; } = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)";

        #endregion Public Properties

        #region Public Methods

        /// <summary>Directly obtains the data of the file represented by the specified connectionstring.</summary>
        /// <param name="connectionString">The full connectionstring for the download.</param>
        /// <param name="stream">Stream to copy the received content to.</param>
        /// <param name="proxy">The proxy.</param>
        /// <returns>Returns the number of bytes copied.</returns>
        public static long Copy(ConnectionString connectionString, Stream stream, ConnectionString? proxy = null)
        {
            var connection = new HttpConnection();
            if (proxy.HasValue)
            {
                connection.SetProxy(proxy.Value);
            }

            return connection.Download(connectionString, stream);
        }

        /// <summary>Directly obtains the data of the file represented by the specified connectionstring.</summary>
        /// <param name="connectionString">The full connectionstring for the download.</param>
        /// <param name="stream">Stream to copy the received content to.</param>
        /// <param name="callback">Callback to run after each block or null.</param>
        /// <param name="proxy">The proxy.</param>
        /// <param name="userItem">The user item.</param>
        /// <returns>Returns the number of bytes copied.</returns>
        public static long Copy(ConnectionString connectionString, Stream stream, ProgressCallback callback, ConnectionString? proxy = null, object userItem = null)
        {
            var connection = new HttpConnection();
            if (proxy.HasValue)
            {
                connection.SetProxy(proxy.Value);
            }

            return connection.Download(connectionString, stream, callback, userItem);
        }

        /// <summary>Directly obtains the data of the file represented by the specified connectionstring.</summary>
        /// <param name="connectionString">The full connectionstring for the download.</param>
        /// <param name="proxy">The proxy.</param>
        /// <returns>Returns the downloaded byte array.</returns>
        public static byte[] Get(ConnectionString connectionString, ConnectionString? proxy = null)
        {
            var connection = new HttpConnection();
            if (proxy.HasValue)
            {
                connection.SetProxy(proxy.Value);
            }

            return connection.Download(connectionString);
        }

        /// <summary>Directly obtains the data of the file represented by the specified connectionstring.</summary>
        /// <param name="connectionString">The full connectionstring for the download.</param>
        /// <param name="callback">Callback to run after each block or null.</param>
        /// <param name="proxy">The proxy.</param>
        /// <param name="userItem">The user item.</param>
        /// <returns>Returns the downloaded byte array.</returns>
        public static byte[] Get(ConnectionString connectionString, ProgressCallback callback, ConnectionString? proxy = null, object userItem = null)
        {
            var connection = new HttpConnection();
            if (proxy.HasValue)
            {
                connection.SetProxy(proxy.Value);
            }

            return connection.Download(connectionString, callback, userItem);
        }

        /// <summary>Directly obtains the data of the file represented by the specified connectionstring as string.</summary>
        /// <param name="connectionString">The full connectionstring for the download.</param>
        /// <param name="proxy">The proxy.</param>
        /// <returns>Returns downloaded data as string (utf8).</returns>
        public static string GetString(ConnectionString connectionString, ConnectionString? proxy = null) => Encoding.UTF8.GetString(Get(connectionString, proxy));

        /// <summary>Performs a post request ath the specified connectionstring.</summary>
        /// <param name="connectionString">The full connectionstring for the post request.</param>
        /// <param name="postData">Post data to send to the server.</param>
        /// <param name="callback">Callback to run after each block or null (will be called from 0..100% for upload and download).</param>
        /// <param name="proxy">The proxy.</param>
        /// <param name="userItem">The user item.</param>
        /// <returns>Returns the downloaded byte array.</returns>
        public static byte[] Post(ConnectionString connectionString, IList<PostData> postData, ProgressCallback callback, ConnectionString? proxy = null, object userItem = null)
        {
            var connection = new HttpConnection();
            if (proxy.HasValue)
            {
                connection.SetProxy(proxy.Value);
            }

            return connection.Post(connectionString, postData, callback, userItem);
        }

        #endregion Public Methods

        #region private functionality

        HttpWebRequest CreateRequest(ConnectionString connectionString)
        {
            var target = connectionString.ToUri();
            HttpWebRequest request;
            request = (HttpWebRequest)WebRequest.Create(target);
#if NETSTANDARD20
            request.AllowReadStreamBuffering = false;
#endif

            // set defaults
            request.ProtocolVersion = ProtocolVersion;
            if (Proxy != null)
            {
                request.Proxy = Proxy;
            }
            if (UserAgent != null)
            {
                request.UserAgent = UserAgent;
            }
            if (Referer != null)
            {
                request.Referer = Referer;
            }
            if (Accept != null)
            {
                request.Accept = Accept;
            }
            foreach (var head in Headers)
            {
                request.Headers[head.Key] = head.Value;
            }
            if (PreventCaching)
            {
                CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                request.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();
                request.Headers["Pragma"] = "no-cache";
                request.Headers["RequestId"] = Guid.NewGuid().ToString("D");
            }
            else if (CachePolicy != null)
            {
                request.CachePolicy = CachePolicy;
            }

            request.AllowAutoRedirect = true;
            request.CookieContainer = Cookies ?? new CookieContainer();
            var credentialCache = new CredentialCache
            {
                { connectionString.ToUri(), "plain", connectionString.GetCredentials() },
            };
            request.Credentials = credentialCache;
            request.KeepAlive = false;
            request.Timeout = (int)Timeout.TotalMilliseconds;
            request.ReadWriteTimeout = (int)Timeout.TotalMilliseconds;
            return request;
        }

        #endregion private functionality

        /// <summary>Downloads a file.</summary>
        /// <param name="connectionString">The full connectionstring for the download.</param>
        /// <returns>Returns a byte array.</returns>
        public byte[] Download(ConnectionString connectionString)
        {
            var request = CreateRequest(connectionString);
            using var response = (HttpWebResponse)request.GetResponse();
            var responseStream = response.GetResponseStream();
            var result = responseStream.ReadAllBytes(response.ContentLength);
            responseStream.Close();
            return result;
        }

        /// <summary>Downloads a file.</summary>
        /// <param name="connectionString">The full connectionstring for the download.</param>
        /// <param name="callback">Callback to run after each block or null.</param>
        /// <param name="userItem">The user item.</param>
        /// <returns>Returns a byte array.</returns>
        public byte[] Download(ConnectionString connectionString, ProgressCallback callback, object userItem = null)
        {
            var request = CreateRequest(connectionString);
            using var response = (HttpWebResponse)request.GetResponse();
            using var responseStream = response.GetResponseStream();
            var result = responseStream.ReadAllBytes(response.ContentLength, callback, userItem);
            responseStream.Close();
            return result;
        }

        /// <summary>Downloads a file.</summary>
        /// <param name="connectionString">The full connectionstring for the download.</param>
        /// <param name="stream">Target stream to download to.</param>
        /// <returns>Returns the number of bytes downloaded.</returns>
        public long Download(ConnectionString connectionString, Stream stream)
        {
            var request = CreateRequest(connectionString);
            using var response = (HttpWebResponse)request.GetResponse();
            var responseStream = response.GetResponseStream();
            var size = responseStream.CopyBlocksTo(stream);
            responseStream.Close();
            return size;
        }

        /// <summary>Downloads a file.</summary>
        /// <param name="connectionString">The full connectionstring for the download.</param>
        /// <param name="stream">Target stream to download to.</param>
        /// <param name="callback">Callback to run after each block or null.</param>
        /// <param name="userItem">The user item.</param>
        /// <returns>Returns the number of bytes downloaded.</returns>
        public long Download(ConnectionString connectionString, Stream stream, ProgressCallback callback, object userItem = null)
        {
            HttpWebResponse response = null;
            try
            {
                var request = CreateRequest(connectionString);
                if (Proxy != null)
                {
                    request.Proxy = Proxy;
                }

                response = (HttpWebResponse)request.GetResponse();
                var responseStream = response.GetResponseStream();
                var size = responseStream.CopyBlocksTo(stream, response.ContentLength, callback, userItem);
                responseStream.Close();
                return size;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>Performs a post request ath the specified connectionstring.</summary>
        /// <param name="connectionString">The full connectionstring for the post request.</param>
        /// <param name="postData">Post data to send to the server.</param>
        /// <param name="callback">Callback to run after each block or null (will be called from 0..100% for upload and download).</param>
        /// <param name="userItem">The user item.</param>
        /// <returns>Returns the downloaded byte array.</returns>
        public byte[] Post(ConnectionString connectionString, IList<PostData> postData, ProgressCallback callback = null, object userItem = null)
        {
            var boundary = $"---boundary-{Base64.UrlChars.Encode(DateTime.Now.Ticks)}";
            var request = CreateRequest(connectionString);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;
            using var requestStream = request.GetRequestStream();
            var writer = new DataWriter(requestStream, StringEncoding.ASCII, NewLineMode.CRLF);

            foreach (var postItem in postData)
            {
                writer.WriteLine();
                writer.WriteLine($"--{boundary}");
                postItem.WriteTo(writer);
            }

            writer.WriteLine();
            writer.WriteLine($"--{boundary}--");
            writer.Close();
            using var response = request.GetResponse();
            using var responseStream = response.GetResponseStream();
            var result = responseStream.ReadAllBytes(response.ContentLength, callback, userItem);
            responseStream.Close();
            return result;
        }

        /// <summary>Sets the proxy.</summary>
        /// <param name="proxy">The proxy.</param>
        public void SetProxy(ConnectionString proxy) => Proxy = new WebProxy(proxy.ToString(ConnectionStringPart.Server), true, new string[] { "localhost" }, new NetworkCredential(proxy.UserName, proxy.Password));
    }
}
