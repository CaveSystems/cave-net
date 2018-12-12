namespace Cave.Net.Ftp
{
    /// <summary>
    /// Provides available transfer types
    /// </summary>
    public enum FtpTransferType
    {
        /// <summary>
        /// ASCII - 7BIT US ASCII (DEPRECATED)
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the default type and must be accepted by all FTP
        /// implementations.  It is intended primarily for the transfer
        /// of text files, except when both hosts would find the EBCDIC
        /// type more convenient.
        /// </para><para>
        /// The sender converts the data from an internal character
        /// representation to the standard 8-bit NVT-ASCII
        /// representation(see the Telnet specification).  The receiver
        /// will convert the data from the standard form to his own
        /// internal form.
        /// </para><para>
        /// In accordance with the NVT standard, the (CRLF) sequence
        /// should be used where necessary to denote the end of a line
        /// of text.  (See the discussion of file structure at the end
        /// of the Section on Data Representation and Storage.)
        /// </para><para>
        /// Using the standard NVT-ASCII representation means that data
        /// must be interpreted as 8-bit bytes.
        /// The Format parameter for ASCII and EBCDIC types is discussed
        /// below.
        /// </para>
        /// </remarks>
        ASCII = 'A',

        /// <summary>
        /// IMAGE / BINARY
        /// </summary>
        /// <remarks>
        /// <para>
        /// The data are sent as contiguous bits which, for transfer,
        /// are packed into the 8-bit transfer bytes.  The receiving
        /// site must store the data as contiguous bits.  The structure
        /// of the storage system might necessitate the padding of the
        /// file (or of each record, for a record-structured file) to
        /// some convenient boundary(byte, word or block).  This
        /// padding, which must be all zeros, may occur only at the end
        /// of the file(or at the end of each record) and there must be
        /// a way of identifying the padding bits so that they may be
        /// stripped off if the file is retrieved.The padding
        /// transformation should be well publicized to enable a user to
        /// process a file at the storage site.
        /// </para><para>
        /// Image type is intended for the efficient storage and
        /// retrieval of files and for the transfer of binary data.It
        /// is recommended that this type be accepted by all FTP
        /// implementations.
        /// </para>
        /// </remarks>
        IMAGE = 'I',

        /// <summary>
        /// <para>
        /// This type is intended for efficient transfer between hosts
        /// which use EBCDIC for their internal character
        /// representation.
        /// </para><para>
        /// For transmission, the data are represented as 8-bit EBCDIC
        /// characters.The character code is the only difference
        /// between the functional specifications of EBCDIC and ASCII
        /// types. 
        /// </para><para>
        /// End-of-line (as opposed to end-of-record--see the discussion
        /// of structure) will probably be rarely used with EBCDIC type
        /// for purposes of denoting structure, but where it is	
        /// necessary the(NL) character should be used.
        /// </para>
        /// </summary>
        EBCDIC = 'E',

        /// <summary>
        /// <para>
        /// The data is transferred in logical bytes of the size specified by the obligatory second parameter, Byte size. 
        /// The value of Byte size must be a decimal integer; there is no default value.  The logical byte size is not 
        /// necessarily the same as the transfer byte size.  If there is a difference in byte sizes, then the logical bytes 
        /// should be packed contiguously, disregarding transfer byte boundaries and with any necessary padding at the end.
        /// </para><para>
        /// When the data reaches the receiving host, it will be transformed in a manner dependent on the logical byte size 
        /// and the particular host. This transformation must be invertible (i.e., an identical file can be retrieved if the 
        /// same parameters are used) and should be well publicized by the FTP implementors.
        /// </para><para>
        /// For example, a user sending 36-bit floating-point numbers to a host with a 32-bit word could send that data as 
        /// Local byte with a logical byte size of 36.  The receiving host would then be expected to store the logical bytes 
        /// so that they could be easily manipulated; in this example putting the 36-bit logical bytes into 64-bit double 
        /// words should suffice.
        /// </para>
        /// </summary>
        LOCAL = 'L',
    }
}
