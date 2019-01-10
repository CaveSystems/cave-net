using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cave.IO;

namespace Cave.Net.Ftp
{
    /// <summary>
    /// Provides the ftp protocol implementation for ftp clients.
    /// </summary>
    /// <seealso cref="TcpAsyncClient{FtpServer}" />
    /// <remarks>
    /// RFC 3659 - Extensions to FTP
    /// RFC 2640 - Internationalization of the File Transfer Protocol
    /// RFC 2389 - Feature negotiation mechanism for the File Transfer Protocol
    /// RFC 2228 - FTP Security Extensions
    /// RFC 959 - File Transfer Protocol
    /// </remarks>
    /// <seealso cref="TcpAsyncClient{CaveFtpServer}" />
    public class FtpServerClient : TcpAsyncClient<FtpServer>
    {
        readonly Dictionary<FtpCommand, Action<string>> commands = new Dictionary<FtpCommand, Action<string>>();

        string currentFileSystemDirectory;
        string currentFtpFolder = "/";

        /// <summary>Initializes a new instance of the <see cref="FtpServerClient"/> class.</summary>
        public FtpServerClient()
        {
            //commands.Add(CaveFtpCommand.ABOR, ABOR);
            //commands.Add(CaveFtpCommand.ACCT, ACCT);
            //commands.Add(CaveFtpCommand.ALLO, ALLO);
            //commands.Add(CaveFtpCommand.APPE, APPE);
            commands.Add(FtpCommand.CDUP, CDUP);
            commands.Add(FtpCommand.CWD, CWD);
            commands.Add(FtpCommand.DELE, DELE);
            //commands.Add(CaveFtpCommand.HELP, HELP);
            commands.Add(FtpCommand.LIST, LIST);
            commands.Add(FtpCommand.MKD, MKD);
            //commands.Add(CaveFtpCommand.MODE, MODE);
            //commands.Add(CaveFtpCommand.NLST, NLST);
            commands.Add(FtpCommand.NOOP, NOOP);
            commands.Add(FtpCommand.PASS, PASS);
            commands.Add(FtpCommand.PASV, PASV);
            commands.Add(FtpCommand.PORT, PORT);
            commands.Add(FtpCommand.PWD, PWD);
            commands.Add(FtpCommand.QUIT, QUIT);
            //commands.Add(CaveFtpCommand.REIN, REIN);
            //commands.Add(CaveFtpCommand.REST, REST);
            commands.Add(FtpCommand.RETR, RETR);
            commands.Add(FtpCommand.RMD, RMD);
            commands.Add(FtpCommand.RNFR, RNFR);
            commands.Add(FtpCommand.RNTO, RNTO);
            //commands.Add(CaveFtpCommand.SITE, SITE);
            //commands.Add(CaveFtpCommand.SMNT, SMNT);
            //commands.Add(CaveFtpCommand.STAT, STAT);
            commands.Add(FtpCommand.STOR, STOR);
            //commands.Add(CaveFtpCommand.STOU, STOU);
            //commands.Add(CaveFtpCommand.STRU, STRU);
            //commands.Add(CaveFtpCommand.SYST, SYST);
            commands.Add(FtpCommand.TYPE, TYPE);
            commands.Add(FtpCommand.USER, USER);
            //tls
            //commands.Add(CaveFtpCommand.AUTH, AUTH);
            //commands.Add(CaveFtpCommand.ADAT, ADAT);
            //commands.Add(CaveFtpCommand.PROT, PROT);
            //commands.Add(CaveFtpCommand.PBSZ, PBSZ);
            //commands.Add(CaveFtpCommand.CCC, CCC);
            //commands.Add(CaveFtpCommand.MIC, MIC);
            //commands.Add(CaveFtpCommand.CONF, CONF);
            //commands.Add(CaveFtpCommand.ENC, ENC);
            //additional commands
            commands.Add(FtpCommand.FEAT, FEAT);
            commands.Add(FtpCommand.OPTS, OPTS);
            commands.Add(FtpCommand.SIZE, SIZE);
            //commands.Add(CaveFtpCommand.MDTM, MDTM);
        }

        #region private class

        enum ReceiveMode
        {
            None = 0,
            Command = 1,
        }

        DataWriter writer;
        DataReader reader;
        ReceiveMode receiveMode = ReceiveMode.None;
        IPEndPoint activeEndPoint;
        TcpListener passiveListener;
        bool utf8;

        #region un/escape filename / folder
        string Escape(string name)
        {
            if (utf8)
            {
                return name.Replace("\r", "\r\0");
            }

            return ASCII.Escape(name, '~');
        }

        string Unescape(string name)
        {
            if (utf8) { return name.Replace("\r\0", "\r"); }
            return ASCII.Unescape(name, '~');
        }
        #endregion

        #region clean passive listener / active endpoint
        void Clean()
        {
            if (passiveListener != null)
            {
                passiveListener.Stop();
                passiveListener = null;
            }
            activeEndPoint = null;
        }
        #endregion

        #region read and run command
        bool ReadCommand()
        {
            if (!ReceiveBuffer.Contains(new byte[] { 13, 10 }))
            {
                return false;
            }

            string line = reader.ReadLine();
            FtpCommand command = line.BeforeFirst(' ').ToUpperInvariant().Parse((FtpCommand)0);
            Trace.TraceInformation("{0}-> {1}", RemoteEndPoint, line);
            if (!commands.TryGetValue(command, out Action<string> action))
            {
                SendAnswer("502 Command not implemented.");
            }
            else
            {
                action(line.AfterFirst(' '));
            }
            return true;
        }
        #endregion

        #region create data connection to client
        bool GetDataConnection(out TcpClient result)
        {
            result = null;
            if (activeEndPoint != null)
            {
                //active mode, connect to client
                TcpClient client = new TcpClient();
                Trace.TraceInformation("{0}: establish data connection to {1}", RemoteEndPoint, activeEndPoint);
                IAsyncResult asyncResult = client.BeginConnect(activeEndPoint.Address, activeEndPoint.Port, null, null);
                WaitHandle waitHandle = asyncResult.AsyncWaitHandle;
                try
                {
                    if (!asyncResult.AsyncWaitHandle.WaitOne(DataConnectionTimeout, false))
                    {
                        client.Close();
                    }
                    else
                    {
                        result = client;
                        client.EndConnect(asyncResult);
                        SendAnswer("150 Using passive mode data transfer");
                    }
                }
                finally
                {
                    waitHandle.Close();
                }
            }
            else if (passiveListener != null)
            {
                //passive mode, wait for incomming connection
                Trace.TraceInformation("{0}: waiting for data connection at {1}", RemoteEndPoint, passiveListener.LocalEndpoint);
                IAsyncResult asyncResult = passiveListener.BeginAcceptTcpClient(null, null);
                WaitHandle waitHandle = asyncResult.AsyncWaitHandle;
                try
                {
                    if (asyncResult.AsyncWaitHandle.WaitOne(DataConnectionTimeout, false))
                    {
                        result = passiveListener.EndAcceptTcpClient(asyncResult);
                        SendAnswer("150 Using passive mode data transfer");
                    }
                }
                finally
                {
                    waitHandle.Close();
                }
            }

            if (result == null)
            {
                SendAnswer("425 Can't open data connection.");
                return false;
            }
            Trace.TraceInformation("{0}: data connection with {1} established", RemoteEndPoint, result.Client.RemoteEndPoint);
            return true;
        }
        #endregion

        #region get available directory entries for LIST
        bool GetDirectoryEntries(out FtpDirectoryEntry[] entries)
        {
            if (currentFileSystemDirectory == null)
            {
                List<FtpDirectoryEntry> result = new List<FtpDirectoryEntry>();
                foreach (KeyValuePair<string, string> root in Server.RootFolders)
                {
                    result.Add(new FtpDirectoryEntry()
                    {
                        Name = root.Key,
                        Type = FtpDirectoryEntryType.Directory,
                    });
                }
                entries = result.ToArray();
                return true;
            }

            FtpDirectoryEntry.GetEntries(currentFileSystemDirectory, utf8, out entries);
            FtpAccessEventArgs e = new FtpAccessEventArgs(this, FtpAccessType.ListDirectory, ".", currentFtpFolder, currentFileSystemDirectory, entries);
            Server.OnCheckAccess(e);
            if (e.Denied)
            {
                SendAnswer("550 Requested action not taken. Access denied.");
                entries = null;
                return false;
            }
            entries = e.Entries;
            return true;
        }
        #endregion

        #region check user access
        bool CheckAccess(FtpAccessType accessType, string ftpName)
        {
            bool allow = false;
            string fileSystemDir = null;
            if (ftpName.Contains("/"))
            {
                //absolute path
                if (ftpName == "/")
                {
                    allow = true;
                }
                else
                {
                    string[] parts = ftpName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (Server.RootFolders.TryGetValue(parts.FirstOrDefault(), out string root))
                    {
                        string ftpDir = FileSystem.Combine('/', currentFtpFolder, ftpName);
                        if (!ftpDir.StartsWith(".."))
                        {
                            fileSystemDir = FileSystem.Combine(root, parts.SubRange(1).Join('/'));
                        }
                    }
                }
            }
            else
            {
                if (currentFileSystemDirectory == null)
                {
                    if (Server.RootFolders.TryGetValue(ftpName, out string root))
                    {
                        fileSystemDir = root;
                    }
                }
                else
                {
                    string ftpDir = FileSystem.Combine('/', currentFtpFolder, ftpName);
                    if (ftpDir == "/")
                    {
                        fileSystemDir = null;
                        allow = true;
                    }
                    else if (!ftpDir.StartsWith(".."))
                    {
                        fileSystemDir = FileSystem.Combine(currentFileSystemDirectory, ftpName);
                    }
                }
            }
            if (fileSystemDir != null)
            {
                switch (accessType)
                {
                    case FtpAccessType.CreateDirectory:
                    {
                        allow = !Directory.Exists(fileSystemDir);
                        break;
                    }
                    case FtpAccessType.ChangeDirectory:
                    case FtpAccessType.ListDirectory:
                    case FtpAccessType.DeleteDirectory:
                    {
                        allow = Directory.Exists(fileSystemDir);
                        break;
                    }
                    case FtpAccessType.UploadFile:
                    {
                        allow = true;
                        break;
                    }
                    case FtpAccessType.DeleteFile:
                    case FtpAccessType.DownloadFile:
                    {
                        allow = File.Exists(fileSystemDir);
                        break;
                    }
                    case FtpAccessType.RenameFrom:
                    {
                        allow = File.Exists(fileSystemDir) || Directory.Exists(fileSystemDir);
                        break;
                    }
                    case FtpAccessType.RenameTo:
                    {
                        allow = !File.Exists(fileSystemDir) && !Directory.Exists(fileSystemDir);
                        break;
                    }
                    default: throw new NotImplementedException();
                }
            }
            if (!allow)
            {
                SendAnswer("550 Requested action not taken.");
                return false;
            }
            FtpAccessEventArgs e = new FtpAccessEventArgs(this, accessType, currentFileSystemDirectory, currentFtpFolder, ftpName, null);
            Server.OnCheckAccess(e);
            if (e.Denied)
            {
                SendAnswer("550 Requested action not taken. Access denied.");
                return false;
            }
            return true;
        }
        #endregion

        #region check user login
        bool CheckLogin()
        {
            if (!LoggedIn)
            {
                SendAnswer("530 Login required!");
                return false;
            }
            return true;
        }
        #endregion

        void SendAnswer(string answer)
        {
            Trace.TraceInformation("{0}<- {1}", RemoteEndPoint, answer);
            writer.WriteLine(answer);
        }

        #region ftp commands without login

        #region NOOP
        /// <summary>
        /// NO OPERATION
        /// </summary>
        /// <remarks>
        /// This command does not affect any parameters or previously
        /// entered commands. It specifies no action other than that the
        /// server send an OK reply.
        /// </remarks>
        void NOOP(string nothing)
        {
            SendAnswer("200 nothing happened.");
        }
        #endregion

        #region QUIT
        /// <summary>LOGOUT</summary>
        /// <remarks>
        /// This command terminates a USER and if file transfer is not
        /// in progress, the server closes the control connection.  If
        /// file transfer is in progress, the connection will remain
        /// open for result response and the server will then close it.
        /// If the user-process is transferring files for several USERs
        /// but does not wish to close and then reopen connections for
        /// each, then the REIN command should be used instead of QUIT.
        /// 
        /// An unexpected close on the control connection will cause the
        /// server to take the effective action of an abort (ABOR) and a
        /// logout (QUIT).
        /// </remarks>
        void QUIT(string nothing)
        {
            SendAnswer("221 Service closing control connection");
            Close();
        }
        #endregion

        #region USER
        /// <summary>
        /// USER NAME
        /// </summary>
        /// <remarks>
        /// The argument field is a Telnet string identifying the user.
        /// The user identification is that which is required by the
        /// server for access to its file system.  This command will
        /// normally be the first command transmitted by the user after
        /// the control connections are made (some servers may require
        /// this).  Additional identification information in the form of
        /// a password and/or an account command may also be required by
        /// some servers.  Servers may allow a new USER command to be
        /// entered at any point in order to change the access control
        /// and/or accounting information.This has the effect of
        /// flushing any user, password, and account information already
        /// supplied and beginning the login sequence again.All
        /// transfer parameters are unchanged and any file transfer in
        /// progress is completed under the old access control
        /// parameters.
        /// </remarks>
        void USER(string userName)
        {
            Clean();
            LoggedIn = false;
            UserName = userName;
            SendAnswer("331 received username, need password...");
        }
        #endregion

        #region PASS
        /// <summary>
        /// PASSWORD 
        /// </summary>
        /// <remarks>
        /// The argument field is a Telnet string specifying the user's
        /// password.  This command must be immediately preceded by the
        /// user name command, and, for some sites, completes the user's
        /// identification for access control.  Since password
        /// information is quite sensitive, it is desirable in general
        /// to "mask" it or suppress typeout.  It appears that the
        /// server has no foolproof way to achieve this.  It is
        /// therefore the responsibility of the user-FTP process to hide
        /// the sensitive password information.
        /// </remarks>
        void PASS(string password)
        {
            FtpLoginEventArgs e = new FtpLoginEventArgs(this, UserName, password);
            Server.OnCheckLogin(e);
            if (e.Denied) { SendAnswer("530 invalid username or password."); }
            else { LoggedIn = true; SendAnswer("230 Logged in."); }
        }
        #endregion

        #region PASV
        /// <summary>
        /// PASSIVE
        /// </summary>
        /// <remarks>
        /// This command requests the server-DTP to "listen" on a data
        /// port (which is not its default data port) and to wait for a
        /// connection rather than initiate one upon receipt of a
        /// transfer command.  The response to this command includes the
        /// host and port address this server is listening on.
        /// </remarks>
        void PASV(string arguments)
        {
            passiveListener?.Stop();
#if NET35 || NET40
#pragma warning disable CS0618 // Obsolete
			passiveListener = new TcpListener(0);
#pragma warning restore CS0618
#else
            passiveListener = TcpListener.Create(0);
#endif
            passiveListener.Start();
            //only ipv4 - unmap
            string addr = LocalEndPoint.Address.ToString().AfterLast(':');
            if (addr.Count(c => c == '.') != 3)
            {
                throw new InvalidCastException("Not an ipv4 address!");
            }

            addr = addr.Replace('.', ',');
            int port = ((IPEndPoint)passiveListener.LocalEndpoint).Port;
            SendAnswer($"227 Entering passive mode ({addr},{port >> 8},{port & 0xff})");
        }
        #endregion

        #region PORT
        /// <summary>
        /// DATA PORT
        /// </summary>
        /// <remarks>
        /// The argument is a HOST-PORT specification for the data port
        /// to be used in data connection.  There are defaults for both 
        /// the user and server data ports, and under normal
        /// circumstances this command and its reply are not needed.  If
        /// this command is used, the argument is the concatenation of a
        /// 32-bit internet host address and a 16-bit TCP port address.
        /// 
        /// This address information is broken into 8-bit fields and the
        /// value of each field is transmitted as a decimal number (in
        /// character string representation).  The fields are separated
        /// by commas.  A port command would be:
        /// 
        /// PORT h1, h2, h3, h4, p1, p2
        /// 
        /// where h1 is the high order 8 bits of the internet host
        /// address.
        /// </remarks>
        void PORT(string arguments)
        {
            passiveListener?.Stop();
            passiveListener = null;
            int[] parts = arguments.Split(',').Select(p => int.Parse(p)).ToArray();
            if (parts.Length != 6)
            {
                throw new ArgumentException("Invalid ip address o port");
            }

            IPAddress address = IPAddress.Parse($"{parts[0]}.{parts[1]}.{parts[2]}.{parts[3]}");
            int port = (parts[4] << 8) + parts[5];
            activeEndPoint = new IPEndPoint(address, port);
            SendAnswer($"200 Ok will connect to {activeEndPoint}");
        }
        #endregion

        #region FEAT		
        /// <summary>
        /// ADDITIONAL SERVER FEATURES
        /// </summary>
        /// <remarks>
        /// The FEAT command consists solely of the word "FEAT".  It has no
        /// parameters or arguments.
        /// 
        /// Where a server-FTP process does not support the FEAT command, it will
        /// respond to the FEAT command with a 500 or 502 reply.This is simply
        /// the normal "unrecognized command" reply that any unknown command
        /// would elicit.Errors in the command syntax, such as giving
        /// parameters, will result in a 501 reply.
        /// 
        /// The FEAT command allows a client to discover which optional commands a
        /// server supports, and how they are supported, and to select among
        /// various options that any FTP command may support.
        /// </remarks>
        /// <param name="args">The arguments.</param>
        void FEAT(string args)
        {
            SendAnswer("211-Extensions supported:");
            SendAnswer(" UTF8");
            SendAnswer(" SIZE");
            SendAnswer("211 End");
        }
        #endregion

        #region OPTS		
        /// <summary>
        /// ADDITIONAL SERVER OPTIONS
        /// </summary>
        /// <remarks>
        /// The OPTS (options) command allows a user-PI to specify the desired
        /// behavior of a server-FTP process when another FTP command (the target
        /// command) is later issued.  The exact behavior, and syntax, will vary
        /// with the target command indicated, and will be specified with the
        /// definition of that command.  Where no OPTS behavior is defined for a
        /// particular command there are no options available for that command.
        /// </remarks>
        /// <param name="args">The arguments.</param>
        void OPTS(string args)
        {
            string opt = args.BeforeFirst(' ').ToUpperInvariant();
            if (opt == "UTF8")
            {
                string mode = args.AfterFirst(' ').BeforeFirst(' ').ToUpperInvariant();
                if (mode == "ON")
                {
                    utf8 = true;
                    writer.StringEncoding = StringEncoding.UTF8;
                    reader.StringEncoding = StringEncoding.UTF8;
                    SendAnswer("200 OK, UTF8 enabled.");
                    return;
                }
                else if (mode == "OFF")
                {
                    utf8 = false;
                    writer.StringEncoding = Server.DefaultEncoding;
                    reader.StringEncoding = Server.DefaultEncoding;
                    SendAnswer("200 OK, UTF8 disabled.");
                    return;
                }
            }
            SendAnswer($"502 Option {opt} not implemented or parameter error.");
        }
        #endregion

        #endregion

        #region ftp commands requiring login

        #region TYPE		
        /// <summary>
        /// REPRESENTATION TYPE
        /// </summary>
        /// <remarks>
        /// The argument specifies the representation type as described
        /// in the Section on Data Representation and Storage.  Several
        /// types take a second parameter.  The first parameter is
        /// denoted by a single Telnet character, as is the second
        /// Format parameter for ASCII and EBCDIC; the second parameter
        /// for local byte is a decimal integer to indicate Bytesize.
        /// The parameters are separated by a (SP) (Space, ASCII code 32).
        /// 
        /// The default representation type is ASCII Non-print.  If the
        /// Format parameter is changed, and later just the first
        /// argument is changed, Format then returns to the Non-print
        /// default.
        /// </remarks>
        /// <param name="args">The arguments.</param>
        void TYPE(string args)
        {
            if (!CheckLogin())
            {
                return;
            }

            FtpTransferType typeCode = (FtpTransferType)args.FirstOrDefault();
            switch (typeCode)
            {
                //implemented types:
                case FtpTransferType.ASCII:
                case FtpTransferType.IMAGE:
                    TransferType = typeCode;
                    FormatControl = FtpFormatControl.NONPRINT;
                    break;
                //not implemented types:
                default: SendAnswer("504 TypeCode not implemented"); return;
            }

            //The types ASCII and EBCDIC also take a second (optional) parameter; this is to indicate what kind of vertical format
            //control, if any, is associated with a file.  The following data representation types are defined in FTP:
            string optional = args.AfterFirst(' ');
            if (optional.Length > 0)
            {
                FtpFormatControl newFormatControl = (FtpFormatControl)optional.FirstOrDefault();
                switch (newFormatControl)
                {
                    case FtpFormatControl.NONPRINT:
                        FormatControl = FtpFormatControl.NONPRINT;
                        break;

                    default:
                        SendAnswer($"504 FormatControl {newFormatControl} not implemented for TransferType {TransferType}.");
                        return;
                }
            }
            SendAnswer($"200 OK TransferType {TransferType} FormatControl {FormatControl}");
        }
        #endregion

        #region LIST		
        /// <summary>
        /// LIST
        /// </summary>
        /// <remarks>
        /// This command causes a list to be sent from the server to the
        /// passive DTP.  If the pathname specifies a directory or other
        /// group of files, the server should transfer a list of files
        /// in the specified directory.  If the pathname specifies a
        /// file then the server should send current information on the
        /// file.  A null argument implies the user's current working or
        /// default directory.  The data transfer is over the data
        /// connection in type ASCII or type EBCDIC.  (The user must
        /// ensure that the TYPE is appropriately ASCII or EBCDIC).
        /// Since the information on a file may vary widely from system
        /// to system, this information may be hard to use automatically
        /// in a program, but may be quite useful to a human user.
        /// </remarks>
        void LIST(string args)
        {
            if (!CheckLogin())
            {
                return;
            }
            //get entries
            if (!GetDirectoryEntries(out FtpDirectoryEntry[] entries))
            {
                return;
            }
            //get connection
            if (!GetDataConnection(out TcpClient tcpClient))
            {
                return;
            }
            //send data
            try
            {
                using (Stream s = tcpClient.GetStream())
                {
                    DataWriter dataWriter = new DataWriter(s, newLineMode: NewLineMode.CRLF);
                    foreach (FtpDirectoryEntry entry in entries)
                    {
                        dataWriter.WriteLine(entry.ToString());
                        Trace.TraceInformation("{0}: {1}", tcpClient.Client.RemoteEndPoint, entry);
                    }
                    dataWriter.Flush();
                    dataWriter.Close();
                    SendAnswer("226 Transfer complete");
                }
            }
            catch
            {
                tcpClient.Close();
                SendAnswer("426 Connection closed; transfer aborted.");
            }
        }
        #endregion

        #region DELE
        /// <summary>
        /// DELETE 
        /// </summary>
        /// <remarks>
        /// This command causes the file specified in the pathname to be
        /// deleted at the server site.  If an extra level of protection
        /// is desired (such as the query, "Do you really wish to
        /// delete?"), it should be provided by the user-FTP process.
        /// </remarks>
        /// <param name="args">The arguments.</param>
        void DELE(string args)
        {
            if (!CheckLogin())
            {
                return;
            }
            //check file access
            string ftpName = Unescape(args);
            if (!CheckAccess(FtpAccessType.DeleteFile, ftpName))
            {
                return;
            }

            try
            {
                string fileName = FileSystem.Combine(currentFileSystemDirectory, ftpName);
                File.Delete(fileName);
                SendAnswer("250 Requested file action okay, completed.");
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("{0}: Error at delete file {1}: {2}", RemoteEndPoint, ftpName, ex);
                SendAnswer("450 Requested file action not taken.");
            }
        }
        #endregion

        #region MKD
        /// <summary>
        /// MAKE DIRECTORY
        /// </summary>
        /// <remarks>
        /// This command causes the directory specified in the pathname
        /// to be created as a directory (if the pathname is absolute)
        /// or as a subdirectory of the current working directory (if
        /// the pathname is relative).
        /// </remarks>
        void MKD(string args)
        {
            if (!CheckLogin())
            {
                return;
            }
            //check file access
            string ftpName = Unescape(args);
            if (!CheckAccess(FtpAccessType.CreateDirectory, ftpName))
            {
                return;
            }

            try
            {
                string dirName = FileSystem.Combine(currentFileSystemDirectory, ftpName);
                Directory.CreateDirectory(dirName);
                SendAnswer("250 Requested file action okay, completed.");
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("{0}: Error at create directory {1}: {2}", RemoteEndPoint, ftpName, ex);
                SendAnswer("450 Requested file action not taken.");
            }
        }
        #endregion

        #region RMD
        /// <summary>
        /// REMOVE DIRECTORY
        /// </summary>
        /// <remarks>
        /// This command causes the directory specified in the pathname	
        /// to be removed as a directory (if the pathname is absolute)
        /// or as a subdirectory of the current working directory (if
        /// the pathname is relative).
        /// </remarks>
        void RMD(string args)
        {
            if (!CheckLogin())
            {
                return;
            }
            //check file access
            string ftpName = Unescape(args);
            if (!CheckAccess(FtpAccessType.DeleteDirectory, ftpName))
            {
                return;
            }

            try
            {
                string dirName = FileSystem.Combine(currentFileSystemDirectory, ftpName);
                Directory.Delete(dirName, false);
                SendAnswer("250 Requested file action okay, completed.");
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("{0}: Error at delete directory {1}: {2}", RemoteEndPoint, ftpName, ex);
                SendAnswer("450 Requested file action not taken.");
            }
        }
        #endregion

        #region SIZE
        /// <summary>
        /// FILE SIZE
        /// </summary>
        /// <remarks>
        /// The FTP command, SIZE OF FILE (SIZE), is used to obtain the transfer
        /// size of a file from the server-FTP process.  This is the exact number
        /// of octets (8 bit bytes) that would be transmitted over the data
        /// connection should that file be transmitted.  This value will change
        /// depending on the current STRUcture, MODE, and TYPE of the data
        /// connection or of a data connection that would be created were one
        /// created now.  Thus, the result of the SIZE command is dependent on
        /// the currently established STRU, MODE, and TYPE parameters.
        /// 
        /// The SIZE command returns how many octets would be transferred if the
        /// file were to be transferred using the current transfer structure,
        /// mode, and type.This command is normally used in conjunction with
        /// the RESTART (REST) command when STORing a file to a remote server in
        /// STREAM mode, to determine the restart point.The server-PI might
        /// need to read the partially transferred file, do any appropriate
        /// conversion, and count the number of octets that would be generated
        /// when sending the file in order to correctly respond to this command.
        /// Estimates of the file transfer size MUST NOT be returned; only
        /// precise information is acceptable.
        /// </remarks>
        void SIZE(string args)
        {
            if (!CheckLogin())
            {
                return;
            }
            //check file access
            string ftpName = Unescape(args);
            if (!CheckAccess(FtpAccessType.DownloadFile, ftpName))
            {
                return;
            }

            string fileName = FileSystem.Combine(currentFileSystemDirectory, ftpName);
            long size = new FileInfo(fileName).Length;
            SendAnswer($"213 {size} {ftpName}");
        }
        #endregion

        #region RETR
        /// <summary>RETRIEVE</summary>
        /// <param name="args">The arguments.</param>
        /// <remarks>
        /// This command causes the server-DTP to transfer a copy of the
        /// file, specified in the pathname, to the server- or user-DTP
        /// at the other end of the data connection.  The status and
        /// contents of the file at the server site shall be unaffected.
        /// </remarks>
        void RETR(string args)
        {
            if (!CheckLogin())
            {
                return;
            }
            //check file access
            string ftpName = Unescape(args);
            string fileName = FileSystem.Combine(currentFileSystemDirectory, ftpName);
            if (!CheckAccess(FtpAccessType.DownloadFile, ftpName))
            {
                return;
            }
            //get connection
            if (!GetDataConnection(out TcpClient tcpClient))
            {
                return;
            }
            //send data
            try
            {
                using (Stream f = File.OpenRead(fileName))
                using (Stream s = tcpClient.GetStream())
                {
                    switch (TransferType)
                    {
                        case FtpTransferType.ASCII:
                            switch (FormatControl)
                            {
                                case FtpFormatControl.NONPRINT:
                                    DataReader r = new DataReader(f, StringEncoding.ASCII);
                                    DataWriter w = new DataWriter(s, StringEncoding.ASCII);
                                    while (f.Position < f.Length)
                                    {
                                        w.Write(r.ReadString((int)Math.Min(4096, r.Available)));
                                    }
                                    break;
                                default:
                                    throw new NotImplementedException($"TransferType {TransferType} FormatControl {FormatControl} is not implemented!");
                            }
                            break;
                        case FtpTransferType.IMAGE:
                            switch (FormatControl)
                            {
                                case FtpFormatControl.NONPRINT:
                                    f.CopyBlocksTo(s);
                                    break;
                                default:
                                    throw new NotImplementedException($"TransferType {TransferType} FormatControl {FormatControl} is not implemented!");
                            }
                            break;
                        default:
                            throw new NotImplementedException($"TransferType {TransferType} is not implemented!");
                    }
                    s.Flush();
                    SendAnswer("226 Transfer complete");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("{0}: Error at retrieve file {1}: {2}", RemoteEndPoint, fileName, ex);
                SendAnswer("426 Connection closed; transfer aborted.");
            }
            finally
            {
                tcpClient.Close();
            }
        }
        #endregion

        string renameFrom;

        #region RNFR
        /// <summary>
        /// RENAME FROM
        /// </summary>
        /// <remarks>
        /// This command specifies the old pathname of the file which is
        /// to be renamed.  This command must be immediately followed by
        /// a "rename to" command specifying the new file pathname.
        /// </remarks>
        void RNFR(string args)
        {
            if (!CheckLogin())
            {
                return;
            }
            //check file access
            string ftpName = Unescape(args);
            string fileName = FileSystem.Combine(currentFileSystemDirectory, ftpName);
            if (!CheckAccess(FtpAccessType.RenameFrom, ftpName))
            {
                return;
            }

            renameFrom = fileName;
            SendAnswer("350 Requested file action pending further information.");
        }
        #endregion

        #region RNFR
        /// <summary>
        /// This command specifies the new pathname of the file
        /// specified in the immediately preceding "rename from"
        /// command.  Together the two commands cause a file to be
        /// renamed.
        /// </summary>
        void RNTO(string args)
        {
            if (!CheckLogin())
            {
                return;
            }
            //check file access
            string source = renameFrom;
            renameFrom = null;
            if (source == null)
            {
                SendAnswer("550 Requested action not taken. Need to set file with RNFR first!");
                return;
            }
            string ftpName = Unescape(args);
            string target = FileSystem.Combine(currentFileSystemDirectory, ftpName);
            if (!CheckAccess(FtpAccessType.RenameTo, ftpName))
            {
                return;
            }

            try
            {
                if (Directory.Exists(source))
                {
                    Directory.Move(source, target);
                    SendAnswer("250 File action completed.");
                    return;
                }
                if (File.Exists(source))
                {
                    File.Move(source, target);
                    SendAnswer("250 File action completed.");
                    return;
                }
            }
            catch { }
            SendAnswer("550 Source file or directory vanished!");
        }
        #endregion

        #region STOR
        /// <summary>
        /// STORE
        /// </summary>
        /// <remarks>
        /// This command causes the server-DTP to accept the data
        /// transferred via the data connection and to store the data as
        /// a file at the server site.  If the file specified in the
        /// pathname exists at the server site, then its contents shall
        /// be replaced by the data being transferred.  A new file is
        /// created at the server site if the file specified in the
        /// pathname does not already exist.
        /// </remarks>
        void STOR(string args)
        {
            if (!CheckLogin())
            {
                return;
            }
            //check file access
            string ftpName = Unescape(args);
            string fileName = FileSystem.Combine(currentFileSystemDirectory, ftpName);
            if (!CheckAccess(FtpAccessType.UploadFile, ftpName))
            {
                return;
            }
            //get connection
            if (!GetDataConnection(out TcpClient tcpClient))
            {
                return;
            }
            //receive data
            try
            {
                using (Stream f = File.Create(fileName))
                using (Stream s = tcpClient.GetStream())
                {
                    switch (TransferType)
                    {
                        case FtpTransferType.ASCII:
                        case FtpTransferType.IMAGE:
                            switch (FormatControl)
                            {
                                case FtpFormatControl.NONPRINT:
                                    s.CopyBlocksTo(f);
                                    break;
                                default:
                                    throw new NotImplementedException($"TransferType {TransferType} FormatControl {FormatControl} is not implemented!");
                            }
                            break;
                        default:
                            throw new NotImplementedException($"TransferType {TransferType} is not implemented!");
                    }
                    f.Close();
                }
                SendAnswer("226 Transfer complete");
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("{0}: Error at retrieve file {1}: {2}", RemoteEndPoint, fileName, ex);
                SendAnswer("426 Connection closed; transfer aborted.");
            }
            finally
            {
                tcpClient.Close();
            }
        }
        #endregion

        #region PWD
        /// <summary>
        /// PRINT WORKING DIRECTORY
        /// </summary>
        /// <remarks>
        /// This command causes the name of the current working
        /// directory to be returned in the reply.
        /// </remarks>
        void PWD(string args)
        {
            if (!CheckLogin())
            {
                return;
            }

            SendAnswer($"257 \"{Escape(currentFtpFolder)}\" is the current directory.");
        }
        #endregion

        #region CDUP
        /// <summary>
        /// CHANGE TO PARENT DIRECTORY
        /// </summary>
        /// <remarks>
        /// This command is a special case of CWD, and is included to
        /// simplify the implementation of programs for transferring
        /// directory trees between operating systems having different
        /// syntaxes for naming the parent directory.  The reply codes
        /// shall be identical to the reply codes of CWD.  See
        /// Appendix II for further details.
        /// </remarks>
        void CDUP(string nothing) { CWD(".."); }
        #endregion

        #region CWD
        /// <summary>
        /// CHANGE WORKING DIRECTORY
        /// </summary>
        /// <remarks>
        /// This command allows the user to work with a different
        /// directory or dataset for file storage or retrieval without
        /// altering his login or accounting information.  Transfer
        /// parameters are similarly unchanged.  The argument is a
        /// pathname specifying a directory or other system dependent
        /// file group designator.
        /// </remarks>
        void CWD(string args)
        {
            if (!CheckLogin())
            {
                return;
            }

            string ftpName = Unescape(args);
            if (!CheckAccess(FtpAccessType.ChangeDirectory, ftpName))
            {
                return;
            }

            string fileSystemDir = currentFileSystemDirectory;
            string ftpDir = currentFtpFolder;
            if (ftpName.Contains("/"))
            {
                //absolute path
                if (ftpName == "/")
                {
                    fileSystemDir = null;
                    ftpDir = "/";
                }
                else
                {
                    string[] parts = ftpName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (Server.RootFolders.TryGetValue(parts.FirstOrDefault(), out string root))
                    {
                        fileSystemDir = FileSystem.Combine(root, parts.SubRange(1).Join('/'));
                        ftpDir = FileSystem.Combine('/', parts);
                        if (ftpDir.StartsWith(".."))
                        {
                            throw new DirectoryNotFoundException();
                        }
                    }
                }
            }
            else
            {
                if (currentFileSystemDirectory == null)
                {
                    if (Server.RootFolders.TryGetValue(ftpName, out string root))
                    {
                        fileSystemDir = root;
                        ftpDir = "/" + ftpName;
                    }
                }
                else
                {
                    ftpDir = FileSystem.Combine('/', currentFtpFolder, ftpName);
                    if (ftpDir == "/")
                    {
                        fileSystemDir = null;
                    }
                    else if (ftpDir.StartsWith(".."))
                    {
                        throw new DirectoryNotFoundException();
                    }
                    else
                    {
                        fileSystemDir = FileSystem.Combine(currentFileSystemDirectory, ftpName);
                    }
                }
            }
            if (fileSystemDir == null && ftpDir == "/")
            {
                //virtual root
            }
            else
            {
                //check if directory exists
                if (!Directory.Exists(fileSystemDir))
                {
                    throw new DirectoryNotFoundException();
                }
            }
            currentFileSystemDirectory = fileSystemDir;
            currentFtpFolder = ftpDir;
            SendAnswer("250 Requested file action okay, completed.");
        }
        #endregion

        #endregion

        #endregion

        #region event overrides

        /// <summary>
        /// Calls the <see cref="E:Cave.Net.NetClientBase.Connected" /> event (if set).
        /// </summary>
        protected override void OnConnect()
        {
            base.OnConnect();

            //todo, do ssl
            receiveMode = ReceiveMode.Command;

            reader = new DataReader(Stream, newLineMode: NewLineMode.CRLF, encoding: Server.DefaultEncoding);
            writer = new DataWriter(Stream, newLineMode: NewLineMode.CRLF, encoding: Server.DefaultEncoding);

            SendAnswer($"220 [{NetTools.HostName}] {Server.ServerVersionString} ready.");
        }

        /// <summary>
        /// Calls the <see cref="E:Cave.Net.TcpServerClient.Received" /> event (if set).
        /// </summary>
        protected override void OnReceived(byte[] buffer, int offset, int length)
        {
            base.OnReceived(buffer, offset, length);

            while (true)
            {
                switch (receiveMode)
                {
                    case ReceiveMode.None: break;
                    case ReceiveMode.Command: if (!ReadCommand()) { return; } break;
                    default: throw new NotImplementedException();
                }
            }
        }
        #endregion

        #region public properties

        /// <summary>Gets the name of the user.</summary>
        /// <value>The name of the user.</value>
        public string UserName { get; private set; }

        /// <summary>Gets a value indicating whether the user (<see cref="UserName"/>) is logged in or not.</summary>
        /// <value><c>true</c> if [logged in]; otherwise, <c>false</c>.</value>
        public bool LoggedIn { get; private set; }

        /// <summary>Gets the data connection timeout.</summary>
        /// <value>The data connection timeout.</value>
        public TimeSpan DataConnectionTimeout { get; private set; } = TimeSpan.FromSeconds(5);

        /// <summary>Gets the type of the transfer.</summary>
        /// <value>The type of the transfer.</value>
        public FtpTransferType TransferType { get; private set; } = FtpTransferType.ASCII;

        /// <summary>Gets the format control.</summary>
        /// <value>The format control.</value>
        public FtpFormatControl FormatControl { get; private set; } = FtpFormatControl.NONPRINT;

        #endregion

        /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            passiveListener?.Stop();
            passiveListener = null;
        }
    }
}