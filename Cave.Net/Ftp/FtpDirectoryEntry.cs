using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Cave.Net.Ftp
{
    /// <summary>
    /// Provides all metadata of a ftp directory entry (file, subdirectory)
    /// </summary>
    public struct FtpDirectoryEntry
    {
        /// <summary>
        /// Gets all entries at the specified folder
        /// </summary>
        /// <param name="folder">Base folder</param>
        /// <param name="utf8">Use utf8 encoding</param>
        /// <param name="entries">Retrieves all entries found</param>
		public static void GetEntries(string folder, bool utf8, out FtpDirectoryEntry[] entries)
        {
            List<FtpDirectoryEntry> list = new List<FtpDirectoryEntry>
            {
                new FtpDirectoryEntry()
                {
                    Name = ".",
                    DateTime = Cave.FileSystem.FileSystem.GetLastWriteTime(folder),
                    Permissions = 775,
                    Type = FtpDirectoryEntryType.Directory,
                }
            };

            string[] dirs = new string[0];
            try { dirs = Directory.GetDirectories(folder); } catch { }
            foreach (string dirName in dirs)
            {
                try
                {
                    FtpDirectoryEntry entry = new FtpDirectoryEntry()
                    {
                        DateTime = Cave.FileSystem.FileSystem.GetLastWriteTime(dirName),
                        Name = Path.GetFileName(dirName),
                        Permissions = 775,
                        Type = FtpDirectoryEntryType.Directory,
                    };
                    if (!utf8 && entry.Name.HasInvalidChars(ASCII.Strings.Printable))
                    {
                        Trace.TraceWarning("File contains non ascii chars and client does not use utf8.");
                        entry.Name = ASCII.Escape(entry.Name, '~');
                    }
                    list.Add(entry);
                }
                catch
                {
                    list.Add(new FtpDirectoryEntry() { Name = dirName, Type = FtpDirectoryEntryType.Directory });
                }
            }

            string[] files = new string[0];
            try { files = Directory.GetFiles(folder); } catch { }
            foreach (string name in files)
            {
                FtpDirectoryEntry entry = new FtpDirectoryEntry()
                {
                    DateTime = Cave.FileSystem.FileSystem.GetLastWriteTime(name),
                    Size = Cave.FileSystem.FileSystem.GetSize(name),
                    Permissions = 664,
                    Name = Path.GetFileName(name),
                    Type = FtpDirectoryEntryType.File,
                };
                if (!utf8 && entry.Name.HasInvalidChars(ASCII.Strings.Printable))
                {
                    Trace.TraceWarning("File contains non ascii chars and client does not use utf8.");
                    entry.Name = ASCII.Escape(entry.Name, '~');
                }
                list.Add(entry);
            }

            entries = list.ToArray();
        }

        /// <summary>The type</summary>
        public FtpDirectoryEntryType Type;

        /// <summary>The name</summary>
        public string Name;

        /// <summary>The (last modification) date time</summary>
        public DateTime DateTime;

        /// <summary>The size</summary>
        public long Size;

        /// <summary>The owner</summary>
        public string Owner;

        /// <summary>The group</summary>
        public string Group;

        /// <summary>The permissions</summary>
        public int Permissions;

        /// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            string owner = (Owner == null ? "root" : ASCII.Clean(Owner).Replace(" ", "")).ForceLength(10);
            string group = (Group == null ? "root" : ASCII.Clean(Group).Replace(" ", "")).ForceLength(10);
            return $"{(char)Type}{Permissions.ToString("0000")}   1 {owner} {group} {Size.ToString().ForceLength(10)} {FormatDate()} {Name}";
        }

        /// <summary>Formats the date.</summary>
        /// <returns></returns>
        string FormatDate()
        {
            if (DateTime < DateTime.Now - TimeSpan.FromDays(180))
            {
                return DateTime.ToString("MMM dd  yyyy", CultureInfo.InvariantCulture);
            }
            else
            {
                return DateTime.ToString("MMM dd HH:mm", CultureInfo.InvariantCulture);
            }
        }
    }
}
