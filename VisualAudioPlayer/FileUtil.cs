using System;
using System.Collections.Generic;
using System.Linq;
using Alphaleonis.Win32.Filesystem;
using System.Text;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;

namespace VisualAudioPlayer
{
    public static class FileUtil
    {
        public enum MusicFolderStatus
        {
            Empty,
            MusicFolder,
            RadioFolder,
            ParentFolder,
        }
        public static string[] SupportedID3Extensions = new[] { ".aac", ".aiff", ".ape", ".asf", ".aa", ".aax", ".flac", ".mkv", ".ifd", ".iim", ".ogg", ".wav", ".mpeg", ".mp4", ".mp3", ".mp2", ".mp1" };
        public static string[] SupportedAudioExtensions = new[] { ".mp3", ".flac", ".ogg", ".wav", ".ape", ".aiff", ".mp2", ".mp1", ".m4a" };
        public static string[] SupportedPlaylistExtensions = new[] { ".m3u", ".pls", ".asx" };
        public static string[] SupportedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif" };
        public static string[] PreferedCoverFileNames = new[] { "cover", "folder", "front", "-01", "-001", "voorkant", "thumb" };
        public static string[] UnPreferedCoverFileNames = new[] { "back", "cd-" };
        public static string[] ArtWorkSubFolders = new[] { "pp", "art", "artwork", "scans", "covers", "covers, booklets" };
        private const int WIN_MAX_PATH = 260;
        const uint FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
        const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        const uint FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
        const uint FORMAT_MESSAGE_FROM_HMODULE = 0x00000800;
        const uint FORMAT_MESSAGE_FROM_STRING = 0x00000400;

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PathIsDirectoryEmpty(
            [In, MarshalAs(UnmanagedType.LPTStr)] string pszPath
            );
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint GetShortPathName([MarshalAs(UnmanagedType.LPTStr)] string lpszLongPath,
                                            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszShortPath, uint cchBuffer);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, [Out] StringBuilder lpBuffer, uint nSize, string[] Arguments);
        public static bool IsServerPingable(string sHostname)
        {
            if (string.IsNullOrEmpty(sHostname))
                return false;
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128, but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted. 
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120; // millisconds
            PingReply reply = pingSender.Send(sHostname, timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
                return true;
            else
                return false;
        }
        public static AccessStatus DirectoryExists(string sDir)
        {
            if (!IsServerPath(sDir)) // quickly handle local files
            {
                if (Directory.Exists(sDir)) // local or net Directory Exists
                    return AccessStatus.Existing;
                else
                    return AccessStatus.NotExisting;
            }
            if (Directory.Exists(sDir)) // net Directory Exists
                return AccessStatus.Existing;
            else // net Directory doesnt Exists OR not Connected
            {
                string sBasePath = Path.GetPathRoot(sDir);
                if (IsShareReadable(sDir)) // Base Directory Exists, so we have net access
                    return AccessStatus.NotExisting;
                string sServerName = FileUtil.GetServerName(sDir);
                if (!FileUtil.IsServerPingable(sServerName)) // Is Server down?
                {
                    MessageBox.Show(new Form() { TopMost = true }, GlobaStrings.ServerDown + "\n\nServer: " + sServerName,
                        GlobaStrings.ServerDownCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return AccessStatus.ServerDown;
                }
                string sServerPath = GetServerPath(sDir);
                if (WinNet.Connect(sServerPath))           // Try to onnect to Server
                    return AccessStatus.Existing;
                else
                    return AccessStatus.LoginFailed;
            }
        }
        public static bool FileExists(string sFile)
        {
            if (!IsServerPath(sFile)) // quickly handle local files
                return File.Exists(sFile);
            if (File.Exists(sFile)) // local or net file Exists
                return true;
            else // file doesnt Exists OR just not Connected
            {
                if (IsShareReadable(sFile)) // Base Directory Exists, so we have net access
                    return false;
                string sServerName = FileUtil.GetServerName(sFile);
                if (!FileUtil.IsServerPingable(sServerName)) // Is Server down?
                {
                    MessageBox.Show(new Form() { TopMost = true }, GlobaStrings.ServerDown + "\n\nServer: " + sServerName, GlobaStrings.ServerDownCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }
                string sServerPath = GetServerPath(sFile);
                if (!WinNet.Connect(sServerPath))   // Try to onnect to Server
                    return false;
                //Func<bool> func = () => File.Exists(sFile);
                //Task<bool> task = new Task<bool>(func);
                //task.Start();
                //if (task.Wait(100))
                //{
                //    return task.Result;
                //}
                return (File.Exists(sFile));
            }
        }
        public static bool IsShareReadable(string sShare)
        {
            string remotePath = Path.GetPathRoot(sShare);

            DirectoryInfo di = new DirectoryInfo(remotePath);
            if (!di.Exists)
                return false;
            try
            {
                // you could also call GetDirectories or GetFiles
                // to test them individually
                // this will throw an exception if you don't have 
                // rights to the directory, though
                var acl = di.GetAccessControl();
                return true;
            }
            catch (UnauthorizedAccessException uae)
            {
                if (uae.Message.Contains("read-only"))
                {
                    // seems like it is just read-only
                    return true;
                }
                // no access, sorry
                return false;
            }
        }
        public static string GetDirectoryName(string sDir)
        {
            if (string.IsNullOrEmpty(sDir))
                return null;
            if (sDir.Length > 260 || sDir.StartsWith("\\\\"))
                sDir = @"\\?\unc" + sDir.Substring(1);
            return System.IO.Path.GetDirectoryName(sDir);
        }
        public static string GetPrevDir(string sDir)
        {
            if (string.IsNullOrEmpty(sDir))
                return null;
            if (Path.HasExtension(sDir))
                sDir = Path.GetFullPath(sDir);
            if (sDir.IndexOf("\\") == -1)
                return sDir;
            string sPrevDir = sDir.Substring(0, sDir.LastIndexOf("\\"));
            return sPrevDir;
        }
        public static string GetServerPath(string sFullPath)
        {
            if (string.IsNullOrEmpty(sFullPath))
                return null;
            Uri uri = new Uri(sFullPath);
            return "\\\\" + uri.Host + "\\";
        }
        public static string GetServerName(string sFullPath)
        {
            if (string.IsNullOrEmpty(sFullPath))
                return null;
            if (sFullPath.StartsWith(@"\\?\unc"))
                sFullPath = sFullPath.Replace(@"\\?\unc", @"\");
            Uri uri = new Uri(sFullPath);
            return uri.Host;
        }
        public static string[] GetDirectories(string sDir)
        {
            if (string.IsNullOrEmpty(sDir))
                return null;
            string[] sSubDirs = null;
            string sServerPath = GetServerPath(sDir);
            try
            {
                sSubDirs = Directory.GetDirectories(sDir, "*");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString()+"\n\nFile: " + sDir, "GetNetDirectories: GetDirectories failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return sSubDirs;
        }
        public static string GetLnkTarget(string lnkPath)  // COM "Microsoft Shell Control And Automation"
        {
            if (!lnkPath.EndsWith(".lnk") && !lnkPath.EndsWith(".url"))
                return null;
            if (lnkPath.Contains("Neue Verknüpfung.lnk"))
                return null;
            string sPath = null;
            var shl = new Shell32.Shell();         // Move this to class scope
            lnkPath = System.IO.Path.GetFullPath(lnkPath);

            var dir = shl.NameSpace(Path.GetDirectoryName(lnkPath));
            var itm = dir.Items().Item(Path.GetFileName(lnkPath));
            try
            {
                var lnk = (Shell32.ShellLinkObject)itm.GetLink;
                sPath = lnk.Target.Path;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //MessageBox.Show(ex.ToString() + "\n\nFile: " + lnkPath, "GetLinkFiles: GetLink failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return sPath;
        }
        public static bool IsMusicParentDir(string sAlbumDir, IEnumerable<FileInfo> files)
        {
            if (sAlbumDir == null)
                return false;
            if (!FileUtil.PathIsDirectoryEmpty(sAlbumDir))
                return false;
            string[] subDirs = Directory.GetDirectories(sAlbumDir, "*");
            if (subDirs == null)
                return false;
            foreach (string sSubDir in subDirs) // scan all Discoveries
            {
                if (GetAudioFiles(files).Count() > 0)
                    return true; 
            }
            return false;
        }
        public static MusicFolderStatus GetMusicFolderType(string sAlbumDir)
        {
            if (sAlbumDir == null)
                return MusicFolderStatus.Empty;
            DirectoryInfo dInfo;
            IEnumerable<FileInfo> files = null;
            try
            {
                dInfo = new DirectoryInfo(sAlbumDir);
                files = dInfo.GetFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "GetMusicFolderType: DirectoryInfo failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return MusicFolderStatus.Empty;
            }
            return GetMusicFolderType(sAlbumDir, files);
        }
        public static MusicFolderStatus GetMusicFolderType(string sAlbumDir, IEnumerable<FileInfo> files)
        {
            if (sAlbumDir == null || files.Count() == 0)
                return MusicFolderStatus.Empty;
            IEnumerable<FileInfo> fileQuery;

            fileQuery = GetAudioFiles(files);

            if (fileQuery.Count() > 0)
                return MusicFolderStatus.MusicFolder;

            if (IsMusicParentDir(sAlbumDir, files)) // subdirs contain music
                    return MusicFolderStatus.ParentFolder;

            if (IsMusicRadioDir(sAlbumDir, files))  // No audio files avail
                return MusicFolderStatus.RadioFolder;

            return MusicFolderStatus.Empty; // dir is empty
        }
        public static bool IsMusicRadioDir(string sAlbumDir, IEnumerable<FileInfo> files)
        {
            if (sAlbumDir == null || files.Count() == 0)
                return false;
            IEnumerable<FileInfo> fileQuery;

            fileQuery = GetRadioFiles(files);

            if (fileQuery.Count() > 0)
                return true;
            return false;
        }
        public static bool IsLink(string sFile)
        {
            if (string.IsNullOrEmpty(sFile))
                return false;
            string sExtension = Path.GetExtension(sFile);
            if (sExtension == ".lnk")
                return true;
            return false;
        }
        public static IEnumerable<FileInfo> GetUrlFiles(IEnumerable<FileInfo> files)
        {
            return files.Where(f => f.Extension.ToLower() == ".url")
                     .ToArray();
        }
        public static IEnumerable<FileInfo> GetRadioFiles(IEnumerable<FileInfo> files)
        {
            return files.Where(f => f.Extension.ToLower() == ".url" || f.Extension.ToLower() == ".m3u" || f.Extension.ToLower() == ".pls")
                     .ToArray();
        }
        public static IEnumerable<System.IO.FileInfo> GetID3AudioFiles(string sDir)
        {
            System.IO.DirectoryInfo dInfo;
            try
            {
                dInfo = new System.IO.DirectoryInfo(sDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "GetAudioFiles: DirectoryInfo failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            System.IO.FileInfo[] files =
                dInfo.EnumerateFiles()
                     .Where(f => SupportedID3Extensions.Contains(f.Extension.ToLower()))
                     .ToArray();
            return files;
        }
        public static IEnumerable<FileInfo> GetLinkFiles(IEnumerable<FileInfo> files)
        {
            return files.Where(f => f.Extension.ToLower() == ".lnk")
                     .ToArray();
        }
        public static IEnumerable<FileInfo> GetLinkFiles(string sDir)
        {
            DirectoryInfo dInfo;
            IEnumerable<FileInfo> files = null;
            try
            {
                dInfo = new DirectoryInfo(sDir);
                files = dInfo.GetFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "GetLinkFiles: EnumerateFiles failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return GetLinkFiles(files);
        }
        public static IEnumerable<FileInfo> GetAudioFiles(string sDir)
        {
            DirectoryInfo dInfo;
            IEnumerable<FileInfo> files = null;
            try
            {
                dInfo = new DirectoryInfo(sDir);
                files = dInfo.GetFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "GetAudioFiles: DirectoryInfo failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return GetAudioFiles(files);
        }
        public static IEnumerable<FileInfo> GetAudioFiles(IEnumerable<FileInfo> files)
        {
            return files.Where(f => SupportedAudioExtensions.Contains(f.Extension.ToLower()))
                     .ToArray();
        }
        public static bool IsAudioFile(string sFile)
        {
            string sExtension = Path.GetExtension(sFile);
            if (SupportedAudioExtensions.Contains(sExtension))
                return true;
            else
                return false;
        }
        public static bool IsImageFile(string sFile)
        {
            string sExtension = Path.GetExtension(sFile);
            if (SupportedImageExtensions.Contains(sExtension))
                return true;
            else
                return false;
        }
        public static string GetCueFile(IEnumerable<FileInfo> files)
        {
            files = files.Where(f => f.Extension.ToLower() == ".cue")
                     .ToArray();
            if (files.Count() == 0)
                return null;
            return files.First().FullName;
        }
        public static string GetCueFile(string sDir)
        {
            DirectoryInfo dInfo;
            IEnumerable<FileInfo> files = null;
            try
            {
                dInfo = new DirectoryInfo(sDir);
                files = dInfo.GetFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "GetCueFile: DirectoryInfo failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return GetCueFile(files);
        }
        public static IEnumerable<FileInfo> GetAllFiles(string sDir)
        {
            DirectoryInfo dInfo;
            IEnumerable<FileInfo> files = null;
            try
            {
                dInfo = new DirectoryInfo(sDir);
                files = dInfo.GetFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "GetAllFiles failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return files;
        }
        public static string GetAlbumImage(string sDir)
        {
            DirectoryInfo dInfo;
            IEnumerable<FileInfo> files = null;
            try
            {
                dInfo = new DirectoryInfo(sDir);
                files = dInfo.GetFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "GetAudioFiles: DirectoryInfo failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return GetAlbumImage(files);
        }
        public static string GetAlbumImage(IEnumerable<FileInfo> allFiles)
        {
            if (allFiles.Count() == 0)
                return null;
            IEnumerable<FileInfo> files = allFiles.Where(f => SupportedImageExtensions.Contains(f.Extension.ToLower()))
                     .ToArray();
            if (files.Count() == 0) // search for popular ArtWork SubFolders
            {
                DirectoryInfo dInfo;
                IEnumerable<DirectoryInfo> dirs = null;
                try
                {
                    dInfo = new DirectoryInfo(Path.GetDirectoryName(allFiles.First().FullName));
                    dirs = dInfo.GetDirectories();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString(), "GetDirectories: DirectoryInfo failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
                if (dirs.Count() == 0)
                    return null;
                foreach (DirectoryInfo dir in dirs) // search for prefered file names
                {
                    if (ArtWorkSubFolders.Contains(dir.Name.ToLower()))
                        return GetAlbumImage(dir.FullName);
                    return null;
                }
            }
            if (files.Count() > 1)  // 
            {
                foreach (FileInfo file in files) // search for prefered file names
                {
                    foreach (string pref in PreferedCoverFileNames) // search for best files
                    {
                        if (Path.GetFileNameWithoutExtension(file.Name).ToLower().Contains(pref))
                        {
                            if (PreferedCoverFileNames.Count() > 1 && pref.ToLower().Contains("small"))
                                break;
                            return file.FullName;
                        }
                    }
                }
                foreach (FileInfo file in files) // nothing found
                {
                    foreach (string pref in UnPreferedCoverFileNames) // search remainders
                    {
                        if (!Path.GetFileNameWithoutExtension(file.Name).ToLower().Contains(pref))
                            return file.FullName;
                    }
                }
            }
            return files.First().FullName;
        }
        public static bool IsServerPath(string longPath)
        {
            return longPath.StartsWith("\\") && longPath.IndexOf(@"\", 2) > -1;
        }
        public static string GetShortPath(string sLongFileName)
        {
            int max = (int)GetShortPathName(sLongFileName, null, 0);
            StringBuilder shortNameBuffer = new StringBuilder(max);
            if (0 == GetShortPathName(sLongFileName, shortNameBuffer, (uint)shortNameBuffer.MaxCapacity))
            {
                StringBuilder err = new StringBuilder(1024);
                uint nLastError = (uint)Marshal.GetLastWin32Error();
                IntPtr lpMsgBuf = IntPtr.Zero;

                uint dwChars = FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, lpMsgBuf, nLastError, 0, err, 1024, null);
                if (dwChars == 0)
                {
                    // Handle the error.
                }
                MessageBox.Show(new Form() { TopMost = true }, err.ToString(), "GetShortPath", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return sLongFileName;
            }
            else
                return shortNameBuffer.ToString();
        }
        public static byte[] ReadFile(string sPath)  //Open file into a filestream and read data in a byte array.
        {
            if (string.IsNullOrEmpty(sPath))
                return null;
            byte[] data = null;  // Initialize byte array with a null value initially.
            FileInfo fInfo;
            long numBytes;

            try   // Use FileInfo object to get file size.
            {
                fInfo = new FileInfo(sPath);
                numBytes = fInfo.Length;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ReadFile: FileInfo failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return data;
            }
            System.IO.FileStream fStream = new System.IO.FileStream(sPath, System.IO.FileMode.Open, System.IO.FileAccess.Read);  // Open FileStream to read file
            System.IO.BinaryReader br = new System.IO.BinaryReader(fStream);  //Use BinaryReader to read file stream into byte array.

            //When you use BinaryReader, you need to supply number of bytes to read from file. 
            //In this case we want to read entire file. So supplying total number of bytes.
            data = br.ReadBytes((int)numBytes);
            return data;
        }
        public static List<string> FindSimilarDirs(string sDir)
        {
            string sAlbumPath = FileUtil.GetPrevDir(sDir);
            string sAlbumName = Path.GetFileName(sDir);
            List<string> lMatches = new List<string>();
            DirectoryInfo dInfo;
            IEnumerable<DirectoryInfo> dirs = null;
            try
            {
                dInfo = new DirectoryInfo(sAlbumPath);
                dirs = dInfo.GetDirectories();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "FindSimilarDirs: DirectoryInfo failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return lMatches;
            }
            if (dirs.Count() == 0)
                return lMatches;
            foreach (DirectoryInfo dir in dirs) // search for matching file names
            {
                if (dir.DirectoryName.Contains(sAlbumName))
                    lMatches.Add(dir.FullName);
            }
            return lMatches;
        }
        public static List<string> FindLostDir(string sDir)
        {
            string sAlbumPath = FileUtil.GetPrevDir(sDir);
            string sAlbumName = Path.GetFileName(sDir);
            string sPrevAlbumName = Path.GetFileName(sAlbumPath);
            List<string> lMatches = new List<string>();
            DirectoryInfo dInfo;
            IEnumerable<DirectoryInfo> dirs = null;
            try
            {
                dInfo = new DirectoryInfo(sAlbumPath);
                dirs = dInfo.GetDirectories("*", System.IO.SearchOption.AllDirectories);
                //dirs = dInfo.GetDirectories();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "FindLostDir: DirectoryInfo failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return lMatches;
            }
            if (dirs.Count() == 0)
                return lMatches;
            foreach (DirectoryInfo dir in dirs) // search for matching file names
            {
                if (dir.FullName.EndsWith(sAlbumName))
                    lMatches.Add(dir.FullName);
            }
            foreach (DirectoryInfo dir in dirs) // search for matching file names
            {
                if (dir.FullName.EndsWith(sPrevAlbumName))
                    lMatches.Add(dir.FullName);
            }
            return lMatches;
        }
        public static Image GetBestImage(string sDir)
        {
            DirectoryInfo dInfo;
            IEnumerable<FileInfo> files = null;
            try
            {
                dInfo = new DirectoryInfo(sDir);
                files = dInfo.GetFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //MessageBox.Show(ex.Message.ToString(), "GetBestImage: DirectoryInfo failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            ID3Tag id3 = new ID3Tag(files);
            return GetBestImage(files, id3);
        }
        public static Image GetBestImage(IEnumerable<FileInfo> files, ID3Tag id3)
        {
            if (files.Count() == 0)
                return null;
            int iID3ImageHeight = 0;
            int iFileImageHeight = 0;
            int iAamazonImageHeight = 0;

            Image imgFile = null;
            string sImageFile = FileUtil.GetAlbumImage(files);  // Try to find global image file first
            if (!string.IsNullOrEmpty(sImageFile))
            {
                imgFile = Gfx.ByteToImage(FileUtil.ReadFile(sImageFile));
                if (imgFile != null)
                {
                    if (imgFile.Height == 196)
                        return imgFile;
                    if (imgFile.Height > 196)
                    {
                        imgFile = Gfx.ResizeImage(imgFile, new Size(196, 196));
                        return imgFile;
                    }
                    iFileImageHeight = imgFile.Height;
                }
            }
            Image imgID3 = id3.GetImage();  // Try to find alternate ID3 Image
            if (imgID3 != null)
            {
                if (imgID3.Height == 196)
                    return imgID3;
                if (imgID3.Height > 196)
                {
                    imgID3 = Gfx.ResizeImage(imgID3, new Size(196, 196));
                    return imgID3;
                }
                iID3ImageHeight = imgID3.Height;
            }
            string sArtist = id3.GetArtist();
            string sTitle = id3.GetAlbumTitle();
            String url = Aamazon.LargeImage(sArtist, sTitle); // download cover from amazon
            Image imgAamazon = Gfx.GetImageFromURL(url);
            if (imgAamazon != null)
            {
                if (imgAamazon.Height == 196)
                    return imgAamazon;
                if (imgAamazon.Height > 196)
                {
                    imgAamazon = Gfx.ResizeImage(imgAamazon, new Size(196, 196));
                    return imgAamazon;
                }
                iAamazonImageHeight = imgAamazon.Height;
                if (iFileImageHeight == 0)
                {
                    string sFile = files.First().DirectoryName + "\\cover" + url.Substring(url.LastIndexOf("."));
                    try // execute insert query
                    {
                        imgID3.Save(sFile); // save image file to album dir
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.ToString(), "Internal failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            // Todo: download cover from LastFm
            // if (url == null)
            //    iFileImage = LastFm.AlbumArt(sArtist, sTitle); // download cover from amazon
            // Todo: download cover from imdb id3 tags
            // create imdb id and search online db
            if (imgAamazon == null && imgID3 == null && imgFile == null)
                return null;
            if (imgAamazon == null && imgID3 == null)
                return imgFile;
            if (imgAamazon == null && imgFile == null)
                return imgID3;
            if (imgID3 == null && imgFile == null)
                return imgAamazon;
            if (iID3ImageHeight == 0 && iFileImageHeight == 0 && iAamazonImageHeight == 0)
                return imgFile;
            if (iID3ImageHeight > iAamazonImageHeight)
                return imgID3;
            return imgAamazon;
        }
    }
}
