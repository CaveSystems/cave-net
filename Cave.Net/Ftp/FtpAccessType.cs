namespace Cave.Net.Ftp
{
    /// <summary>
    /// Available ftp access types
    /// </summary>
    public enum FtpAccessType
    {
        /// <summary>Client wants to create a directory</summary>
        CreateDirectory = 0x1001,

        /// <summary>Client wants to change into a directory</summary>
        ChangeDirectory,

        /// <summary>Client wants to list the directory contents</summary>
        ListDirectory,

        /// <summary>Client wants to delete a directory</summary>
        DeleteDirectory,

        /// <summary>Client wants to upload a file</summary>
        UploadFile = 0x2001,

        /// <summary>Client wants to download a file</summary>
        DownloadFile,

        /// <summary>Client wants to delete a file</summary>
        DeleteFile,

        /// <summary>Client wants to rename a file or directory</summary>
        RenameFrom = 0x3001,

        /// <summary>Client wants to rename a file or directory</summary>
        RenameTo = 0x3002,
    }
}
