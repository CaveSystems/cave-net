namespace Cave.Net.Ftp
{
    /// <summary>
    /// Provides all available FTP commands
    /// </summary>
    public enum FtpCommand
    {
        #region ACCESS CONTROL COMMANDS

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
        USER,

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
        PASS,

        /// <summary>
        /// ACCOUNT
        /// </summary>
        /// <remarks>
        /// The argument field is a Telnet string identifying the user's
        /// account.  The command is not necessarily related to the USER
        /// command, as some sites may require an account for login and
        /// others only for specific access, such as storing files.  In
        /// the latter case the command may arrive at any time.
        /// 
        /// There are reply codes to differentiate these cases for the
        /// automation: when account information is required for login,
        /// the response to a successful PASSword command is reply code
        /// 332.  On the other hand, if account information is NOT
        /// required for login, the reply to a successful PASSword
        /// command is 230; and if the account information is needed for
        /// a command issued later in the dialogue, the server should
        /// return a 332 or 532 reply depending on whether it stores
        /// (pending receipt of the ACCounT command) or discards the
        /// command, respectively.
        /// </remarks>
        ACCT,

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
        CWD,

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
        CDUP,

        /// <summary>
        /// STRUCTURE MOUNT
        /// </summary>
        /// <remarks>
        /// This command allows the user to mount a different file
        /// system data structure without altering his login or
        /// accounting information.  Transfer parameters are similarly
        /// unchanged.  The argument is a pathname specifying a
        /// directory or other system dependent file group designator.
        /// </remarks>
        SMNT,

        /// <summary>
        /// REINITIALIZE
        /// </summary>
        /// <remarks>
        /// This command terminates a USER, flushing all I/O and account
        /// information, except to allow any transfer in progress to be
        /// completed.  All parameters are reset to the default settings
        /// and the control connection is left open.  This is identical
        /// to the state in which a user finds himself immediately after
        /// the control connection is opened.  A USER command may be
        /// expected to follow.
        /// </remarks>
        REIN,

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
        QUIT,

        #endregion

        #region TRANSFER PARAMETER COMMANDS

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
        PORT,

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
        PASV,

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
        TYPE,

        /// <summary>
        /// FILE STRUCTURE
        /// </summary>
        /// <remarks>
        /// The argument is a single Telnet character code specifying
        /// file structure described in the Section on Data
        /// Representation and Storage.
        /// 
        /// The following codes are assigned for structure:
        /// 
        /// F - File (no record structure)
        /// R - Record structure
        /// P - Page structure
        /// 
        /// The default structure is File.
        /// </remarks>
        STRU,

        /// <summary>
        /// TRANSFER MODE
        /// </summary>
        /// <remarks>
        /// The argument is a single Telnet character code specifying
        /// the data transfer modes described in the Section on
        /// Transmission Modes.
        /// 
        /// The following codes are assigned for transfer modes:
        /// 
        /// S - Stream
        /// B - Block
        /// C - Compressed
        /// 
        /// The default transfer mode is Stream.
        /// </remarks>
        MODE,

        #endregion

        #region FTP SERVICE COMMANDS

        /// <summary>RETRIEVE</summary>
        /// <remarks>
        /// This command causes the server-DTP to transfer a copy of the
        /// file, specified in the pathname, to the server- or user-DTP
        /// at the other end of the data connection.  The status and
        /// contents of the file at the server site shall be unaffected.
        /// </remarks>
        RETR,

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
        STOR,

        /// <summary>
        /// STORE UNIQUE
        /// </summary>
        /// <remarks>
        /// This command behaves like STOR except that the resultant
        /// file is to be created in the current directory under a name
        /// unique to that directory.  The 250 Transfer Started response
        /// must include the name generated.
        /// </remarks>
        STOU,

        /// <summary>
        /// APPEND (with create)
        /// </summary>
        /// <remarks>
        /// This command causes the server-DTP to accept the data
        /// transferred via the data connection and to store the data in
        /// a file at the server site.  If the file specified in the
        /// pathname exists at the server site, then the data shall be
        /// appended to that file; otherwise the file specified in the
        /// pathname shall be created at the server site.
        /// </remarks>
        APPE,

        /// <summary>
        /// ALLOCATE 
        /// </summary>
        /// <remarks>
        /// This command may be required by some servers to reserve
        /// sufficient storage to accommodate the new file to be
        /// transferred.The argument shall be a decimal integer
        /// representing the number of bytes (using the logical byte
        /// size) of storage to be reserved for the file.For files
        /// sent with record or page structure a maximum record or page
        /// size (in logical bytes) might also be necessary; this is
        /// indicated by a decimal integer in a second argument field of
        /// the command.  This second argument is optional, but when
        /// present should be separated from the first by the three
        /// Telnet characters (SP) R (SP).  This command shall be
        /// followed by a STORe or APPEnd command.  The ALLO command
        /// should be treated as a NOOP (no operation) by those servers
        /// which do not require that the maximum size of the file be
        /// declared beforehand, and those servers interested in only
        /// the maximum record or page size should accept a dummy value
        /// in the first argument and ignore it.
        /// </remarks>
        ALLO,

        /// <summary>
        /// RESTART 
        /// </summary>
        /// <remarks>
        /// The argument field represents the server marker at which
        /// file transfer is to be restarted.  This command does not
        /// cause file transfer but skips over the file to the specified
        /// data checkpoint.  This command shall be immediately followed
        /// by the appropriate FTP service command which shall cause
        /// file transfer to resume.
        /// </remarks>
        REST,

        /// <summary>
        /// RENAME FROM
        /// </summary>
        /// <remarks>
        /// This command specifies the old pathname of the file which is
        /// to be renamed.  This command must be immediately followed by
        /// a "rename to" command specifying the new file pathname.
        /// </remarks>
        RNFR,

        /// <summary>
        /// This command specifies the new pathname of the file
        /// specified in the immediately preceding "rename from"
        /// command.  Together the two commands cause a file to be
        /// renamed.
        /// </summary>
        RNTO,

        /// <summary>
        /// ABORT  
        /// </summary>
        /// <remarks>
        /// This command tells the server to abort the previous FTP
        /// service command and any associated transfer of data.  The
        /// abort command may require "special action", as discussed in
        /// the Section on FTP Commands, to force recognition by the
        /// server.  No action is to be taken if the previous command
        /// has been completed (including data transfer).  The control
        /// connection is not to be closed by the server, but the data
        /// connection must be closed.
        /// 
        /// There are two cases for the server upon receipt of this	
        /// command: (1) the FTP service command was already completed,
        /// or (2) the FTP service command is still in progress.
        /// In the first case, the server closes the data connection
        /// (if it is open) and responds with a 226 reply, indicating
        /// that the abort command was successfully processed.
        /// 
        /// In the second case, the server aborts the FTP service in
        /// progress and closes the data connection, returning a 426
        /// reply to indicate that the service request terminated
        /// abnormally.  The server then sends a 226 reply,
        /// indicating that the abort command was successfully
        /// processed.
        /// </remarks>
        ABOR,

        /// <summary>
        /// DELETE 
        /// </summary>
        /// <remarks>
        /// This command causes the file specified in the pathname to be
        /// deleted at the server site.  If an extra level of protection
        /// is desired (such as the query, "Do you really wish to
        /// delete?"), it should be provided by the user-FTP process.
        /// </remarks>
        DELE,

        /// <summary>
        /// REMOVE DIRECTORY
        /// </summary>
        /// <remarks>
        /// This command causes the directory specified in the pathname	
        /// to be removed as a directory (if the pathname is absolute)
        /// or as a subdirectory of the current working directory (if
        /// the pathname is relative).
        /// </remarks>
        RMD,

        /// <summary>
        /// MAKE DIRECTORY
        /// </summary>
        /// <remarks>
        /// This command causes the directory specified in the pathname
        /// to be created as a directory (if the pathname is absolute)
        /// or as a subdirectory of the current working directory (if
        /// the pathname is relative).
        /// </remarks>
        MKD,

        /// <summary>
        /// PRINT WORKING DIRECTORY
        /// </summary>
        /// <remarks>
        /// This command causes the name of the current working
        /// directory to be returned in the reply.
        /// </remarks>
        PWD,

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
        LIST,

        /// <summary>
        /// NAME LIST 
        /// </summary>
        /// <remarks>
        /// This command causes a directory listing to be sent from
        /// server to user site.  The pathname should specify a
        /// directory or other system-specific file group descriptor; a
        /// null argument implies the current directory.  The server
        /// will return a stream of names of files and no other
        /// information.  The data will be transferred in ASCII or
        /// EBCDIC type over the data connection as valid pathname
        /// strings separated by (CRLF) or (NL).  (Again the user must
        /// ensure that the TYPE is correct.)  This command is intended
        /// to return information that can be used by a program to
        /// further process the files automatically.  For example, in
        /// the implementation of a "multiple get" function.
        /// </remarks>
        NLST,

        /// <summary>
        /// SITE PARAMETERS
        /// </summary>
        /// <remarks>
        /// This command is used by the server to provide services
        /// specific to his system that are essential to file transfer
        /// but not sufficiently universal to be included as commands in
        /// the protocol.  The nature of these services and the
        /// specification of their syntax can be stated in a reply to
        /// the HELP SITE command.
        /// </remarks>
        SITE,

        /// <summary>
        /// SYSTEM
        /// </summary>
        /// <remarks>
        /// This command is used to find out the type of operating
        /// system at the server.  The reply shall have as its first
        /// word one of the system names listed in the current version
        /// of the Assigned Numbers document [4].
        /// </remarks>
        SYST,

        /// <summary>
        ///  This command shall cause a status response to be sent over
        ///  the control connection in the form of a reply.  The command
        ///  may be sent during a file transfer (along with the Telnet IP
        ///  and Synch signals--see the Section on FTP Commands) in which
        ///  case the server will respond with the status of the
        ///  operation in progress, or it may be sent between file
        ///  transfers.  In the latter case, the command may have an
        ///  argument field.  If the argument is a pathname, the command
        ///  is analogous to the "list" command except that data shall be
        ///  transferred over the control connection.  If a partial
        ///  pathname is given, the server may respond with a list of
        ///  file names or attributes associated with that specification.
        ///  If no argument is given, the server should return general
        ///  status information about the server FTP process.  This
        ///  should include current values of all transfer parameters and
        ///  the status of connections.
        /// </summary>
        STAT,

        /// <summary>
        /// HELP
        /// </summary>
        /// <remarks>
        /// This command shall cause the server to send helpful
        /// information regarding its implementation status over the
        /// control connection to the user.  The command may take an
        /// argument (e.g., any command name) and return more specific
        /// information as a response.  The reply is type 211 or 214.
        /// It is suggested that HELP be allowed before entering a USER
        /// command. The server may use this reply to specify
        /// site-dependent parameters, e.g., in response to HELP SITE.
        /// </remarks>
        HELP,

        /// <summary>
        /// NO OPERATION
        /// </summary>
        /// <remarks>
        /// This command does not affect any parameters or previously
        /// entered commands. It specifies no action other than that the
        /// server send an OK reply.
        /// </remarks>
        NOOP,
        #endregion

        #region RFC 2228 extension commands		
        /// <summary>
        /// AUTHENTICATION/SECURITY MECHANISM (AUTH)
        /// </summary>
        /// <remarks>
        /// The argument field is a Telnet string identifying a supported
        /// mechanism.  This string is case-insensitive.  Values must be
        /// registered with the IANA, except that values beginning with "X-"
        /// are reserved for local use.
        /// 
        /// If the server does not recognize the AUTH command, it must respond
        /// with reply code 500.  This is intended to encompass the large
        /// deployed base of non-security-aware ftp servers, which will
        /// respond with reply code 500 to any unrecognized command.  If the
        /// server does recognize the AUTH command but does not implement the
        /// security extensions, it should respond with reply code 502.
        /// 
        /// If the server does not understand the named security mechanism, it
        /// should respond with reply code 504.
        /// 
        /// If the server is not willing to accept the named security
        /// mechanism, it should respond with reply code 534.
        /// 
        /// If the server is not able to accept the named security mechanism,
        /// such as if a required resource is unavailable, it should respond
        /// with reply code 431.
        /// 
        /// If the server is willing to accept the named security mechanism,
        /// but requires security data, it must respond with reply code 334.
        /// 
        /// If the server is willing to accept the named security mechanism,
        /// and does not require any security data, it must respond with reply
        /// code 234.
        /// 
        /// If the server is responding with a 334 reply code, it may include
        /// security data as described in the next section.
        /// 
        /// Some servers will allow the AUTH command to be reissued in order
        /// to establish new authentication.The AUTH command, if accepted,
        /// removes any state associated with prior FTP Security commands.
        /// The server must also require that the user reauthorize(that is,
        /// reissue some or all of the USER, PASS, and ACCT commands) in this
        /// case (see section 4 for an explanation of "authorize" in this
        /// context).
        /// </remarks>
        AUTH,

        /// <summary>
        /// AUTHENTICATION/SECURITY DATA (ADAT)
        /// </summary>
        /// <remarks>
        /// The argument field is a Telnet string representing base 64 encoded
        /// security data (see Section 9, "Base 64 Encoding").  If a reply
        /// code indicating success is returned, the server may also use a
        /// string of the form "ADAT=base64data" as the text part of the reply
        /// if it wishes to convey security data back to the client.
        /// 
        /// The data in both cases is specific to the security mechanism
        /// specified by the previous AUTH command.  The ADAT command, and the
        /// associated replies, allow the client and server to conduct an
        /// arbitrary security protocol.  The security data exchange must
        /// include enough information for both peers to be aware of which
        /// optional features are available.  For example, if the client does
        /// not support data encryption, the server must be made aware of
        /// this, so it will know not to send encrypted command channel
        /// replies.  It is strongly recommended that the security mechanism
        /// provide sequencing on the command channel, to insure that commands
        /// are not deleted, reordered, or replayed.
        /// 
        /// The ADAT command must be preceded by a successful AUTH command,
        /// and cannot be issued once a security data exchange completes
        /// (successfully or unsuccessfully), unless it is preceded by an AUTH
        /// command to reset the security state.
        /// 
        /// If the server has not yet received an AUTH command, or if a prior
        /// security data exchange completed, but the security state has not
        /// been reset with an AUTH command, it should respond with reply code
        /// 503.
        ///
        /// If the server cannot base 64 decode the argument, it should
        /// respond with reply code 501.
        /// 
        /// If the server rejects the security data (if a checksum fails, for
        /// instance), it should respond with reply code 535.
        /// 
        /// If the server accepts the security data, and requires additional
        /// data, it should respond with reply code 335.
        /// 
        /// If the server accepts the security data, but does not require any
        /// additional data (i.e., the security data exchange has completed
        /// successfully), it must respond with reply code 235.
        /// 
        /// If the server is responding with a 235 or 335 reply code, then it
        /// may include security data in the text part of the reply as
        /// specified above.
        /// 
        /// If the ADAT command returns an error, the security data exchange
        /// will fail, and the client must reset its internal security state.
        /// If the client becomes unsynchronized with the server (for example,
        /// the server sends a 234 reply code to an AUTH command, but the
        /// client has more data to transmit), then the client must reset the
        /// server's security state.
        /// </remarks>
        ADAT,

        /// <summary>
        /// DATA CHANNEL PROTECTION LEVEL (PROT)
        /// </summary>
        /// <remarks>
        /// The argument is a single Telnet character code specifying the data
        /// channel protection level.
        /// 
        /// This command indicates to the server what type of data channel
        /// protection the client and server will be using.  The following
        /// codes are assigned:
        /// 
        /// C - Clear
        /// S - Safe
        /// E - Confidential
        /// P - Private
        /// 
        /// The default protection level if no other level is specified is
        /// Clear.  The Clear protection level indicates that the data channel
        /// will carry the raw data of the file transfer, with no security
        /// applied.  The Safe protection level indicates that the data will
        /// be integrity protected.  The Confidential protection level
        /// indicates that the data will be confidentiality protected.  The
        /// Private protection level indicates that the data will be integrity
        /// and confidentiality protected.
        /// It is reasonable for a security mechanism not to provide all data
        /// channel protection levels.It is also reasonable for a mechanism
        /// to provide more protection at a level than is required(for
        /// instance, a mechanism might provide Confidential protection, but
        /// include integrity-protection in that encoding, due to API or other
        /// considerations).
        /// The PROT command must be preceded by a successful protection
        /// buffer size negotiation.
        /// If the server does not understand the specified protection level,
        /// it should respond with reply code 504.
        /// If the current security mechanism does not support the specified
        /// protection level, the server should respond with reply code 536.
        /// If the server has not completed a protection buffer size
        /// negotiation with the client, it should respond with a 503 reply
        /// code.
        /// The PROT command will be rejected and the server should reply 503
        /// if no previous PBSZ command was issued.
        /// 
        /// If the server is not willing to accept the specified protection
        /// level, it should respond with reply code 534.
        /// 
        /// If the server is not able to accept the specified protection
        /// level, such as if a required resource is unavailable, it should
        /// respond with reply code 431.
        /// 
        /// Otherwise, the server must reply with a 200 reply code to indicate
        /// that the specified protection level is accepted.
        /// </remarks>
        PROT,

        /// <summary>PROTECTION BUFFER SIZE (PBSZ)</summary>
        /// <remarks>
        /// The argument is a decimal integer representing the maximum size,
        /// in bytes, of the encoded data blocks to be sent or received during
        /// file transfer.  This number shall be no greater than can be
        /// represented in a 32-bit unsigned integer.
        /// 
        /// This command allows the FTP client and server to negotiate a
        /// maximum protected buffer size for the connection.There is no
        /// default size; the client must issue a PBSZ command before it can
        /// issue the first PROT command.
        /// The PBSZ command must be preceded by a successful security data
        /// exchange.
        /// If the server cannot parse the argument, or if it will not fit in
        /// 32 bits, it should respond with a 501 reply code.
        /// 
        /// If the server has not completed a security data exchange with the
        /// client, it should respond with a 503 reply code.
        /// 
        /// Otherwise, the server must reply with a 200 reply code.  If the
        /// size provided by the client is too large for the server, it must
        /// use a string of the form "PBSZ=number" in the text part of the
        /// reply to indicate a smaller buffer size.  The client and the
        /// server must use the smaller of the two buffer sizes if both buffer
        /// sizes are specified.
        /// </remarks>
        PBSZ,

        /// <summary>CLEAR COMMAND CHANNEL (CCC)</summary>
        /// <remarks>
        /// This command does not take an argument.
        /// 
        /// It is desirable in some environments to use a security mechanism
        /// to authenticate and/or authorize the client and server, but not to
        /// perform any integrity checking on the subsequent commands.  This
        /// might be used in an environment where IP security is in place,
        /// insuring that the hosts are authenticated and that TCP streams
        /// cannot be tampered, but where user authentication is desired.
        /// 
        /// If unprotected commands are allowed on any connection, then an
        /// attacker could insert a command on the control stream, and the
        /// server would have no way to know that it was invalid.  In order to
        /// prevent such attacks, once a security data exchange completes
        /// successfully, if the security mechanism supports integrity, then
        /// integrity (via the MIC or ENC command, and 631 or 632 reply) must
        /// be used, until the CCC command is issued to enable non-integrity
        /// protected control channel messages.The CCC command itself must
        /// be integrity protected.
        /// Once the CCC command completes successfully, if a command is not
        /// protected, then the reply to that command must also not be
        /// protected.  This is to support interoperability with clients which
        /// do not support protection once the CCC command has been issued.
        /// This command must be preceded by a successful security data
        /// exchange.
        /// 
        /// If the command is not integrity-protected, the server must respond
        /// with a 533 reply code.
        /// If the server is not willing to turn off the integrity
        /// requirement, it should respond with a 534 reply code.
        /// 
        /// Otherwise, the server must reply with a 200 reply code to indicate
        /// that unprotected commands and replies may now be used on the
        /// command channel.
        /// </remarks>
        CCC,

        /// <summary>INTEGRITY PROTECTED COMMAND (MIC)</summary>
        MIC,

        /// <summary>CONFIDENTIALITY PROTECTED COMMAND (CONF)</summary>
        CONF,

        /// <summary>PRIVACY PROTECTED COMMAND (ENC)</summary>
        ENC,

        #endregion

        #region RFC 2389 extension commands		
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
        OPTS,
        #endregion

        #region RFC 2640 extension commands
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
        FEAT,
        #endregion

        #region RFC 3659 extension commands

        /// <summary>
        /// FILE MODIFICATION TIME (MDTM)
        /// </summary>
        /// <remarks>
        /// The FTP command, MODIFICATION TIME (MDTM), can be used to determine
        /// when a file in the server NVFS was last modified.  This command has
        /// existed in many FTP servers for many years, as an adjunct to the REST
        /// command for STREAM mode, thus is widely available.  However, where
        /// supported, the "modify" fact that can be provided in the result from
        /// the new MLST command is recommended as a superior alternative.
        /// 
        /// When attempting to restart a RETRieve, the user-FTP can use the MDTM
        /// command or the "modify" fact to check if the modification time of the
        /// source file is more recent than the modification time of the
        /// partially transferred file.If it is, then most likely the source
        /// file has changed, and it would be unsafe to restart the previously
        /// incomplete file transfer.
        /// 
        /// Because the user- and server-FTPs' clocks are not necessarily
        /// synchronised, user-FTPs intending to use this method should usually
        /// obtain the modification time of the file from the server before the
        /// initial RETRieval, and compare that with the modification time before
        /// a RESTart.  If they differ, the files may have changed, and RESTart
        /// would be inadvisable.Where this is not possible, the user-FTP
        /// should make sure to allow for possible clock skew when comparing
        /// times.
        /// 
        /// When attempting to restart a STORe, the User FTP can use the MDTM
        /// command to discover the modification time of the partially
        /// transferred file.  If it is older than the modification time of the
        /// file that is about to be STORed, then most likely the source file has
        /// changed, and it would be unsafe to restart the file transfer.
        /// </remarks>
        MDTM,

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
        SIZE,

        #endregion
    }
}
