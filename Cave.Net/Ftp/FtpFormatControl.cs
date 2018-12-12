namespace Cave.Net.Ftp
{
    /// <summary>
    /// Provides all available ftp formats
    /// </summary>
    public enum FtpFormatControl
    {
        /// <summary>
        /// NON PRINT
        /// </summary>
        /// <remarks>
        /// This is the default format to be used if the second
        /// (format) parameter is omitted.  Non-print format must be
        /// accepted by all FTP implementations.
        /// 
        /// The file need contain no vertical format information.  If
        /// it is passed to a printer process, this process may
        /// assume standard values for spacing and margins.
        /// 
        /// Normally, this format will be used with files destined
        /// for processing or just storage.
        /// </remarks>
        NONPRINT = 'N',

        /// <summary>
        /// TELNET FORMAT CONTROLS
        /// </summary>
        /// <remarks>
        /// The file contains ASCII/EBCDIC vertical format controls
        /// (i.e., (CR), (LF), (NL), (VT), (FF)) which the printer
        /// process will interpret appropriately.  (CRLF), in exactly
        /// this sequence, also denotes end-of-line.
        /// </remarks>
        TELNET = 'T',

        /// <summary>
        /// CARRIAGE CONTROL (ASA)
        /// </summary>
        /// <remarks>
        /// The file contains ASA (FORTRAN) vertical format control
        /// characters.  (See RFC 740 Appendix C; and Communications
        /// of the ACM, Vol. 7, No. 10, p. 606, October 1964.)  In a
        /// line or a record formatted according to the ASA Standard,
        /// the first character is not to be printed.  Instead, it
        /// should be used to determine the vertical movement of the
        /// paper which should take place before the rest of the
        /// record is printed.
        /// 
        /// The ASA Standard specifies the following control
        /// characters:
        /// 
        /// Character     Vertical Spacing
        /// 
        /// blank         Move paper up one line
        /// 0             Move paper up two lines
        /// 1             Move paper to top of next page
        /// +             No movement, i.e., overprint
        /// 
        /// Clearly there must be some way for a printer process to
        /// distinguish the end of the structural entity.  If a file
        /// has record structure (see below) this is no problem;
        /// records will be explicitly marked during transfer and
        /// storage.  If the file has no record structure, the (CRLF)
        /// end-of-line sequence is used to separate printing lines,
        /// but these format effectors are overridden by the ASA
        /// controls.
        /// </remarks>
        CARRIAGE = 'C',
    }
}
