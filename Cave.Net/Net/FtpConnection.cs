#pragma warning disable SYSLIB0014

using System;
using System.IO;
using System.Net;
using System.Text;
using Cave.IO;

namespace Cave.Net;

/// <summary>Provides a simple asynchronous http fetch.</summary>
public sealed class FtpConnection
{
    #region Private Fields

    static readonly char[] ListSeparator = ['\r', '\n'];

    #endregion Private Fields

    #region Private Methods

    FtpWebRequest CreateRequest(string method, ConnectionString connectionString)
    {
        var uri = connectionString.ToUri();
        var request = (FtpWebRequest)WebRequest.Create(uri);
        request.Credentials = connectionString.GetCredentials();
        request.EnableSsl = EnableSSL;
        request.Method = method;
        return request;
    }

    #endregion Private Methods

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="FtpConnection"/> class.</summary>
    public FtpConnection() { }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets or sets a value indicating whether SSL is enabled for ftp access or not.</summary>
    public bool EnableSSL { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Directly obtains the data of the file represented by the specified connectionstring.</summary>
    /// <param name="connectionString">The full connectionstring for the download.</param>
    /// <returns>Returns an array of byte.</returns>
    public static byte[] Get(ConnectionString connectionString)
    {
        var connection = new FtpConnection();
        return connection.Download(connectionString);
    }

    /// <summary>Directly obtains the data of the specified fileName by using the specified connectionstring as string.</summary>
    /// <param name="connectionString">The full connectionstring for the download.</param>
    /// <returns>Returns the downloaded data as string (utf8).</returns>
    public static string GetString(ConnectionString connectionString) => Encoding.UTF8.GetString(Get(connectionString));

    /// <summary>Downloads a file.</summary>
    /// <param name="connectionString">The full connectionstring for the download.</param>
    /// <returns>Returns an array of bytes.</returns>
    public byte[] Download(ConnectionString connectionString)
    {
        var request = CreateRequest(WebRequestMethods.Ftp.DownloadFile, connectionString);
        using var response = (FtpWebResponse)request.GetResponse();
        switch (response.StatusCode)
        {
            case FtpStatusCode.DataAlreadyOpen:
            case FtpStatusCode.OpeningData:
                break;

            default:
                throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
        }
        using var responseStream = response.GetResponseStream();
        var result = responseStream.ReadAllBytes();
        responseStream.Close();
        return response.StatusCode == FtpStatusCode.ClosingData
            ? result
            : throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
    }

    /// <summary>Downloads a file.</summary>
    /// <param name="connectionString">The full connectionstring for the download.</param>
    /// <param name="stream">Target stream to download to.</param>
    /// <returns>Returns the number of bytes downloaded.</returns>
    public long Download(ConnectionString connectionString, Stream stream)
    {
        var request = CreateRequest(WebRequestMethods.Ftp.DownloadFile, connectionString);
        using var response = (FtpWebResponse)request.GetResponse();
        switch (response.StatusCode)
        {
            case FtpStatusCode.DataAlreadyOpen:
            case FtpStatusCode.OpeningData:
                break;

            default:
                throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
        }
        using var responseStream = response.GetResponseStream();
        var size = responseStream.CopyBlocksTo(stream);
        responseStream.Close();
        return response.StatusCode == FtpStatusCode.ClosingData
            ? size
            : throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
    }

    /// <summary>Obtains a list of files and directories at the current path.</summary>
    /// <param name="connectionString">The full connectionstring for the path to list.</param>
    /// <returns>Returns a new array of strings. This is may be empty but is never null.</returns>
    public string[] List(ConnectionString connectionString)
    {
        var request = CreateRequest(WebRequestMethods.Ftp.ListDirectory, connectionString);
        using var response = (FtpWebResponse)request.GetResponse();
        switch (response.StatusCode)
        {
            case FtpStatusCode.RestartMarker:
            case FtpStatusCode.DataAlreadyOpen:
            case FtpStatusCode.OpeningData:
                break;

            default:
                throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
        }
        using var stream = response.GetResponseStream();
        var reader = new StreamReader(stream);
        var result = reader.ReadToEnd().Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);
        reader.Close();
        return response.StatusCode == FtpStatusCode.ClosingData
            ? result
            : throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
    }

    /// <summary>Uploads a file.</summary>
    /// <param name="connectionString">The full connectionstring for the upload.</param>
    /// <param name="data">Byte array to upload.</param>
    public void Upload(ConnectionString connectionString, byte[] data)
    {
        var request = CreateRequest(WebRequestMethods.Ftp.UploadFile, connectionString);
        var writer = new DataWriter(request.GetRequestStream());
        writer.Write(data);
        using var response = (FtpWebResponse)request.GetResponse();
        if (response.StatusCode != FtpStatusCode.ClosingData)
        {
            throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription));
        }
    }

    /// <summary>Uploads a file.</summary>
    /// <param name="connectionString">The full connectionstring for the upload.</param>
    /// <param name="stream">The stream to upload.</param>
    /// <returns>Returns the number of bytes uploaded.</returns>
    public long Upload(ConnectionString connectionString, Stream stream)
    {
        var request = CreateRequest(WebRequestMethods.Ftp.UploadFile, connectionString);
        using var requestStream = request.GetRequestStream();
        var result = stream.CopyBlocksTo(requestStream);
        requestStream.Close();
        using var response = (FtpWebResponse)request.GetResponse();
        return response.StatusCode != FtpStatusCode.ClosingData
            ? throw new NetworkException(string.Format("Ftp error status: {0} message: '{1}'", response.StatusCode, response.StatusDescription))
            : result;
    }

    #endregion Public Methods
}
