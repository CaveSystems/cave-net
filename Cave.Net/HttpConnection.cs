using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using Cave.IO;

namespace Cave.Net
{
    /// <summary>
    /// Provides a simple asynchronous http fetch
    /// </summary>
    public sealed class HttpConnection
    {
        static HttpConnection()
        {
#if !NETSTANDARD20
            try
            {
                new System.Net.Configuration.HttpWebRequestElement().UseUnsafeHeaderParsing = true;
                return;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            try
            {
                var outerType = typeof(System.Net.Configuration.SettingsSection);
                var asm = Assembly.GetAssembly(outerType);
                if (asm != null)
                {
                    // Use the assembly in order to get the internal type for the internal class
                    Type type = asm.GetType("System.Net.Configuration.SettingsSectionInternal");
                    if (type != null)
                    {
                        // Use the internal static property to get an instance of the internal settings class.
                        // If the static instance isn't created allready the property will create it for us.
                        object obj = type.InvokeMember("Section", BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] { });
                        if (obj != null)
                        {
                            // Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
                            FieldInfo field = type.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (field != null)
                            {
                                field.SetValue(obj, true);
                                Trace.WriteLine("UseUnsafeHeaderParsing <green>enabled.");
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
#endif
            Trace.WriteLine("UseUnsafeHeaderParsing <red>disabled.");
        }

        /// <summary>Directly obtains the data of the file represented by the specified connectionstring</summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
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

        /// <summary>Directly obtains the data of the file represented by the specified connectionstring</summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <param name="callback">Callback to run after each block or null</param>
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

        /// <summary>
        /// Directly obtains the data of the file represented by the specified connectionstring
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <param name="stream">Stream to copy the received content to</param>
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

        /// <summary>Directly obtains the data of the file represented by the specified connectionstring</summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <param name="stream">Stream to copy the received content to</param>
        /// <param name="callback">Callback to run after each block or null</param>
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

        /// <summary>
        /// Directly obtains the data of the file represented by the specified connectionstring as string
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <param name="proxy">The proxy.</param>
        /// <returns>Returns downloaded data as string (utf8).</returns>
        public static string GetString(ConnectionString connectionString, ConnectionString? proxy = null)
        {
            return Encoding.UTF8.GetString(Get(connectionString, proxy));
        }

        /// <summary>The headers to use</summary>
        public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();

        #region private functionality
        HttpWebRequest CreateRequest(ConnectionString connectionString)
        {
            var target = connectionString.ToUri();
            HttpWebRequest newRequest;
            newRequest = (HttpWebRequest)WebRequest.Create(target);
            if (Proxy != null)
            {
                newRequest.Proxy = Proxy;
            }

            // set defaults
            newRequest.ProtocolVersion = ProtocolVersion;
            if (UserAgent != null)
            {
                newRequest.UserAgent = UserAgent;
            }

            if (Referer != null)
            {
                newRequest.Referer = Referer;
            }

            if (Accept != null)
            {
                newRequest.Accept = Accept;
            }

            foreach (KeyValuePair<string, string> head in Headers)
            {
                newRequest.Headers[head.Key] = head.Value;
            }
            newRequest.AllowAutoRedirect = true;
            newRequest.CookieContainer = new CookieContainer();
            var credentialCache = new CredentialCache
            {
                { connectionString.ToUri(), "plain", connectionString.GetCredentials() }
            };
            newRequest.Credentials = credentialCache;
            newRequest.KeepAlive = false;
            newRequest.Timeout = (int)Timeout.TotalMilliseconds;
            newRequest.ReadWriteTimeout = (int)Timeout.TotalMilliseconds;
            return newRequest;
        }

        #endregion

        /// <summary>Gets or sets the protocol version.</summary>
        /// <value>The protocol version.</value>
        public Version ProtocolVersion = new Version("1.0");

        /// <summary>Gets or sets the referer.</summary>
        /// <value>The referer.</value>
        public string Referer;

        /// <summary>The accept string</summary>
        public string Accept;

        /// <summary>Gets or sets the user agent.</summary>
        /// <value>The user agent.</value>
        public string UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)";

        /// <summary>Sets the proxy.</summary>
        /// <param name="proxy">The proxy.</param>
        public void SetProxy(ConnectionString proxy)
        {
            Proxy = new WebProxy(proxy.ToString(ConnectionStringPart.Server), true, new string[] { "localhost" }, new NetworkCredential(proxy.UserName, proxy.Password));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpConnection"/> class.
        /// </summary>
        public HttpConnection()
        {
        }

        /// <summary>Gets or sets the proxy.</summary>
        /// <value>The proxy.</value>
        public IWebProxy Proxy;

        /// <summary>
        /// Download Timeout
        /// </summary>
        public TimeSpan Timeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Downloads a file
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <returns>Returns a byte array.</returns>
        public byte[] Download(ConnectionString connectionString)
        {
            HttpWebResponse response = null;
            try
            {
                HttpWebRequest request = CreateRequest(connectionString);
                response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                byte[] result = responseStream.ReadAllBytes(response.ContentLength);
                responseStream.Close();
                return result;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>Downloads a file</summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <param name="callback">Callback to run after each block or null</param>
        /// <param name="userItem">The user item.</param>
        /// <returns></returns>
        public byte[] Download(ConnectionString connectionString, ProgressCallback callback, object userItem = null)
        {
            HttpWebResponse response = null;
            try
            {
                HttpWebRequest request = CreateRequest(connectionString);
                response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                byte[] result = responseStream.ReadBlock((int)response.ContentLength, callback, userItem);
                responseStream.Close();
                return result;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>
        /// Downloads a file
        /// </summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <param name="stream">Target stream to download to.</param>
        /// <returns>Returns the number of bytes downloaded.</returns>
        public long Download(ConnectionString connectionString, Stream stream)
        {
            HttpWebResponse response = null;
            try
            {
                HttpWebRequest request = CreateRequest(connectionString);
                response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                long size = responseStream.CopyBlocksTo(stream);
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

        /// <summary>Downloads a file</summary>
        /// <param name="connectionString">The full connectionstring for the download</param>
        /// <param name="stream">Target stream to download to.</param>
        /// <param name="callback">Callback to run after each block or null</param>
        /// <param name="userItem">The user item.</param>
        /// <returns>Returns the number of bytes downloaded.</returns>
        public long Download(ConnectionString connectionString, Stream stream, ProgressCallback callback, object userItem = null)
        {
            HttpWebResponse response = null;
            try
            {
                HttpWebRequest request = CreateRequest(connectionString);
                if (Proxy != null)
                {
                    request.Proxy = Proxy;
                }

                response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                long size = responseStream.CopyBlocksTo(stream, response.ContentLength, callback, userItem);
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
    }
}
