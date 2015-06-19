using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlServerCe;
using Alphaleonis.Win32.Filesystem;
using System.Windows.Forms;
using System.Drawing;
using System.Data;
using System.Collections;
using System.Net;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using CueSharp;

namespace VisualAudioPlayer
{
    public class AlbumDB
    {
        private SqlCeConnection connection;
        private bool disposed;
        public delegate void AutoDiscoveryHandler(AlbumDB sender, AlbumDBEventArgs e);
        public event AutoDiscoveryHandler OnNewAlbumAdded;
        public bool IsDiscovering { get; private set; }
        public bool StopDiscovering { get; set; }
        public bool BackUpOnExit { get; set; }
        public class AlbumDBEventArgs : EventArgs
        {
            public Int32 AlbumID { get; private set; }
            public string AlbumPath { get; private set; }
            public string AlbumTitle { get; private set; }
            public Image AlbumImage { get; private set; }
            public bool IsParentFolder { get; private set; }
            public int MediaTypeID { get; private set; }
            public AlbumDBEventArgs(Int32 iAlbumID, string sAlbumPath, string sAlbumTitle, Image iAlbumImage, bool bIsParentFolder, int iMediaTypeID)
            {
                AlbumID = iAlbumID;
                AlbumPath = sAlbumPath;
                AlbumTitle = sAlbumTitle;
                AlbumImage = iAlbumImage;
                IsParentFolder = bIsParentFolder;
                MediaTypeID = iMediaTypeID;
            }
        }
        private class SQLParamValue
        {
            public string Param { get; set; }
            public dynamic Value { get; set; }
            public SQLParamValue(string sParam, dynamic dValue)
            {
                Param = sParam;
                Value = dValue;
            }
        }
#if DEBUG
        private string SqlCeConnectionString = "C:\\Users\\hine\\Documents\\Programing\\Visual Studio 2010\\Projects\\VisualAudioPlayer\\VisualAudioPlayer\\AlbumDB.sdf";
#else
        private string SqlCeConnectionString = "|DataDirectory|\\AlbumDB.sdf";
#endif
        public AlbumDB()
        {
            StopDiscovering = false;
            BackUpOnExit = false;
        }
        public void BackUpDb()
        {
            SqlCeConnectionString = string.Format("Data Source={0};", SqlCeConnectionString);
            try // Open connection
            {
                connection = new SqlCeConnection(SqlCeConnectionString);
                connection.Open();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "SQL connection failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
        }
        public void OpenConnection()
        {
            SqlCeConnectionString = string.Format("Data Source={0};", SqlCeConnectionString);
            try // Open connection
            {
                connection = new SqlCeConnection(SqlCeConnectionString);
                connection.Open();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "SQL connection failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
        }
        public void CloseConnection()
        {
            connection.Close();
            connection.Dispose();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                }
                disposed = true;
            }
        }
        public List<string> GetDiscoveryPaths() // called only once on startup
        {
            List<string> lDiscoveryPaths = DGetArray("AutoDiscovery", "AutoDiscoveryPath");
            return lDiscoveryPaths;
        }
        public void DiscoverNewAlbums() // called only once on startup
        {
            List<string> lDiscoveryPaths = DGetArray("AutoDiscovery", "AutoDiscoveryPath");
            if (lDiscoveryPaths == null)
                return;
            this.IsDiscovering = true;
            List<string> lDbDirs = DGetArray("Albums", "Path");
            int iDirCount = lDiscoveryPaths.Count;
            foreach (string sDiscoveryPath in lDiscoveryPaths) // check every Discovery path
            {
                //Task t = Task.Factory.StartNew(() => dbDiscoverNewAlbum(sDiscoveryPath, lDbDirs)); // check for new albums in all Discovery dirs  
                DiscoverNewAlbum(sDiscoveryPath, lDbDirs); // check for new albums in all Discovery dirs  
            }
            this.IsDiscovering = false;
        }
        public void DiscoverNewAlbum(string sDiscoveryPath, List<string> lDbDirs)
        {
            this.IsDiscovering = true;
            IEnumerable<FileInfo> allFiles = FileUtil.GetAllFiles(sDiscoveryPath);
            if (allFiles.Count() > 0)
            {
                IEnumerable<FileInfo> LinkFiles = FileUtil.GetLinkFiles(allFiles); // first scan for link files
                if (LinkFiles.Count() > 0)
                {
                    foreach (FileInfo LinkFile in LinkFiles) // search for link files
                    {
                        string sLnkTarget = FileUtil.GetLnkTarget(LinkFile.FullName);
                        if (!string.IsNullOrEmpty(sLnkTarget))
                        {
                            string sAlbumDir = System.IO.Path.GetFullPath(sLnkTarget);
                            if (lDbDirs.FirstOrDefault(s => s.Contains(sAlbumDir)) == null)
                            {
                                if (dbAddAlbum(sAlbumDir, true, null, allFiles) == AddAlbumStatus.Error) // try to add link files folder
                                {
                                    this.IsDiscovering = false;
                                    return; // failed so stop importing
                                }
                            }
                        }
                        if (StopDiscovering)
                        {
                            this.IsDiscovering = false;
                            return; // form exiting
                        }
                    }
                }
            }
            string[] subDirs = FileUtil.GetDirectories(sDiscoveryPath);
            DiscoverPath(sDiscoveryPath, lDbDirs); // try to add base dir
            if (subDirs == null)  // scan DiscoveryPath for sub-directories
            {
                this.IsDiscovering = false;
                return; // form exiting
            }
            foreach (string sNewDir in subDirs) // scan all Discovery 
            {
                if (StopDiscovering)
                {
                    this.IsDiscovering = false;
                    return; // form exiting
                }
                if (!DiscoverPath(sNewDir, lDbDirs)) // try to add
                    break; // failed so stop importing
            }
            this.IsDiscovering = false;
        }
        private bool DiscoverPath(string sPath, List<string> lDbDirs)
        {
            if (lDbDirs != null) // first check cashed db pathes
                if (lDbDirs.FirstOrDefault(s => s.Contains(sPath)) != null) 
                    return true;
            IEnumerable<FileInfo> allFiles = FileUtil.GetAllFiles(sPath);
            if (allFiles.Count() > 0) // This dir contains files
            {
                if (dbAddAlbum(sPath, true, null, allFiles) == AddAlbumStatus.Error) // try to add level 2 sub dir (again)
                    return false; // bad error so stop importing
            }
            string[] subDirs = FileUtil.GetDirectories(sPath);
            if (subDirs.Count() == 0)  // no sub dirs
                return true;
            foreach (string sNewSubDir in subDirs) // recursive search
            {
                if (!DiscoverPath(sNewSubDir, lDbDirs)) // try to add sub dir
                    return false; // bad error so stop importing
            }
            return true;
        }
        public void ScanPath(string sDiscoveryPath, bool bAddAutoDiscovery = true)
        {
            if (string.IsNullOrEmpty(sDiscoveryPath))
                return;
            AddNewAlbumPath(sDiscoveryPath); // try to add Folder
            List<string> lDbDirs = DGetArray("Albums", "Path");
            DiscoverNewAlbum(sDiscoveryPath, lDbDirs); // scan new folder and register for watching
        }
        private void AddNewAlbumPath(string sDiscoveryPath, bool bAddAutoDiscovery = true)
        {
            if (string.IsNullOrEmpty(sDiscoveryPath))
                return;
            if (bAddAutoDiscovery)
            {
                List<string> lDiscoveryPaths = DGetArray("AutoDiscovery", "AutoDiscoveryPath");
                foreach (string sDir in lDiscoveryPaths) // check if higher dir already added
                {
                    if (sDiscoveryPath.Contains(sDir))  // parent or same folder
                        return; // Folder already added
                    if (sDir.Contains(sDiscoveryPath))
                        DDel("AutoDiscovery", "AutoDiscoveryPath", sDir); // del lower dir in db
                }
                DAddRecord("AutoDiscovery", "AutoDiscoveryPath", sDiscoveryPath);
            }
            else
            {
                DiscoverNewAlbum(sDiscoveryPath, null); // check for new albums in all Discovery dirs  
            }
        }
        public NetworkCredential GetCredentials(string sServerName)
        {
            if (string.IsNullOrEmpty(sServerName))
                return null;
            if (connection.Database == null)
                return null;
            if (sServerName.StartsWith("\\\\"))
                sServerName = FileUtil.GetServerName(sServerName);

            using (SqlCeCommand cmd = new SqlCeCommand())
            {
                cmd.CommandText = "SELECT * FROM [UserCredentials] WHERE UserID = 1 AND ServerName =@ServerName";
                cmd.Connection = connection;
                cmd.CommandType = CommandType.Text;

                SqlCeParameter param = new SqlCeParameter();
                param.ParameterName = "@ServerName";
                param.SqlDbType = SqlDbType.NVarChar;
                param.Direction = ParameterDirection.Input;
                param.Value = sServerName;
                cmd.Parameters.Add(param);

                try
                {
                    using (SqlCeDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            NetworkCredential credentials = new NetworkCredential();
                            Int32 ordinal = reader.GetOrdinal("Password"); // VarBinary column storing string.
                            byte[] outbyte = (byte[])reader[ordinal];
                            //string sEnryptedPassword = System.Text.Encoding.Default.GetString(outbyte); 
                            string sEnryptedPassword = StringIO.ByteArrayToString(outbyte);
                            sEnryptedPassword = sEnryptedPassword.Replace("\0", "");
                            string sPassword = StringIO.Denrypt(sEnryptedPassword);
                            credentials.Password = sPassword; // Denrypt Password
                            credentials.UserName = reader["UserName"].ToString();
                            return credentials;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            return null;
        }
        public void AddCredentials(string sServerName, string sUserName, string sHashedPassword)
        {
            if (string.IsNullOrEmpty(sServerName) || string.IsNullOrEmpty(sUserName) || string.IsNullOrEmpty(sHashedPassword))
                return;
            if (connection.Database == null)
                return;
            byte[] PasswordData = StringIO.StringToByteArray(sHashedPassword);
            string query = "INSERT INTO [UserCredentials] (ServerName, UserName, Password) Values(@ServerName, @UserName, @Password)";

            SqlCeCommand cmd = new SqlCeCommand();
            cmd.Connection = connection;
            cmd.CommandText = query;
            cmd.CommandType = CommandType.Text;

            SqlCeParameter param = new SqlCeParameter();
            param.ParameterName = "@ServerName";
            param.SqlDbType = SqlDbType.NVarChar;
            param.Direction = ParameterDirection.Input;
            param.Value = sServerName;
            cmd.Parameters.Add(param);

            SqlCeParameter param2 = new SqlCeParameter();
            param2.ParameterName = "@UserName";
            param2.SqlDbType = SqlDbType.NVarChar;
            param2.Direction = ParameterDirection.Input;
            param2.Value = sUserName;
            cmd.Parameters.Add(param2);

            SqlCeParameter param3 = new SqlCeParameter();
            param3.ParameterName = "@Password";
            param3.SqlDbType = SqlDbType.Binary;
            param3.Direction = ParameterDirection.Input;
            param3.Value = PasswordData;
            cmd.Parameters.Add(param3);

            try
            {
                // xxx should return if failed or not
                //iReturnValue = (Int32)cmd.ExecuteScalar();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public string GetHashedPassword(string sUserName, string sServerName)
        {
            if (string.IsNullOrEmpty(sUserName) || string.IsNullOrEmpty(sServerName))
                return null;
            if (connection.Database == null)
                return null;

            SqlCeCommand cmd = new SqlCeCommand();
            cmd.CommandText = "SELECT * FROM [UserCredentials] WHERE UserName ='@UserName' AND ServerName ='@ServerName'";
            cmd.Connection = connection;
            cmd.CommandType = CommandType.Text;

            SqlCeParameter param = new SqlCeParameter();
            param.ParameterName = "@UserName";
            param.SqlDbType = SqlDbType.NVarChar;
            param.Direction = ParameterDirection.Input;
            param.Value = sUserName;
            cmd.Parameters.Add(param);

            SqlCeParameter param2 = new SqlCeParameter();
            param2.ParameterName = "@ServerName";
            param2.SqlDbType = SqlDbType.NVarChar;
            param2.Direction = ParameterDirection.Input;
            param2.Value = sServerName;
            cmd.Parameters.Add(param2);

            try
            {
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Int32 ordinal = reader.GetOrdinal("Password"); // VarBinary column storing string.
                        byte[] outbyte = (byte[])reader[ordinal];
                        dynamic str = System.Text.Encoding.Default.GetString(outbyte);
                        return str;
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
            return null;
        }
        public void dbDeleteAlbum(Int32 iAlbumID, bool bDropAlbum)
        {
            if (bDropAlbum)
            {
                DDel("Albums", "AlbumID", iAlbumID);
                DDel("AlbumTracks", "AlbumID", iAlbumID);
            }
            else
                DSetValue("Albums", "Status", 0, "AlbumID", iAlbumID);
        }
        public void dbUpdateTimePlayed(Int32 iAlbumID, Int64 iTimePlayed)
        {
            DSetValue("Albums", "TimePlayed", iTimePlayed, "AlbumID", iAlbumID);
        }
        public void dbIncPlayCount(Int32 iAlbumTrackID)
        {
            if (iAlbumTrackID == 0)
                return;
            DInc("AlbumTracks", "PlayCount", 1, "AlbumTrackID", iAlbumTrackID);
        }
        public Int32 dbGetAlbumIDByPath(string sAlbumDir)
        {
            dynamic dAlbumID;
            if (string.IsNullOrEmpty(sAlbumDir))
                return 0;
            dAlbumID = Dlookup("AlbumID", "Albums", "Path", sAlbumDir);
            if (dAlbumID == null)
                return 0;
            return Convert.ToInt32(dAlbumID);
        }
        public bool IsNewPath(string sAlbumDir)
        {
            return (Dlookup("AlbumID", "Albums", "Path", sAlbumDir) == null);
        }
        public string dbGetChannelURL(Int32 iAlbumID)
        {
            dynamic dChannelURL;
            if (iAlbumID==0)
                return null;
            dChannelURL = Dlookup("ChannelURL", "RadioChannel", "AlbumID", iAlbumID);
            if (dChannelURL == null)
                return null;
            return Convert.ToString(dChannelURL);
        }
        public string GetLastAddFolderDir()
        {
            string sLastAddFolderDir = Dlookup("LastAddFolderDir", "UserSettings", "UserID", 1);
            return sLastAddFolderDir;
        }
        public void SetLastAddFolderDir(string sLastAddFolderDir)
        {
            DSetValue("UserSettings", "LastAddFolderDir", sLastAddFolderDir, "UserID", 1);
        }
        public string GetLastSelectedDir()
        {
            string sLastDir = Dlookup("LastSelectedDir", "UserSettings", "UserID", 1);
            return sLastDir;
        }
        public void dbSetMinLastTrackPlayed(Int32 iAlbumTrackID)
        {
            if (iAlbumTrackID == 0)
                return;
            dynamic dAlbumID = Dlookup("AlbumID", "AlbumTracks", "AlbumTrackID", iAlbumTrackID);
            if (dAlbumID == null)
                return;
            Int32 iAlbumID = Convert.ToInt32(dAlbumID);
            dynamic dLastTrackPlayed = Dlookup("LastTrackPlayed", "AlbumTracks", "AlbumID", iAlbumID);
            if (dLastTrackPlayed == null)
                return;
            Int32 LastTrackPlayed = Convert.ToInt32(dLastTrackPlayed);
            if (iAlbumTrackID < LastTrackPlayed)
            {
                DSetValue("Albums", "LastTrackPlayed", iAlbumTrackID, "AlbumID", iAlbumID);
            }
        }
        //public int dbLoadImages(ListView aListView, ImageList cImageList)
        //{
        //    if (connection.Database == null)
        //        return 0;
        //    int albumCount = 0, cnt = 0;
        //    Cursor.Current = Cursors.WaitCursor;

        //    try
        //    {
        //        SqlCeCommand command = connection.CreateCommand();
        //        command.CommandText = "SELECT * FROM Albums WHERE Status = 1 AND ParentAlbumID = 0 ORDER BY TimePlayed DESC";

        //        using (SqlCeDataReader reader = command.ExecuteReader())
        //        {
        //            //aListView.BeginUpdate();
        //            while (reader.Read())
        //            {
        //                //var items = new ListViewItem[53709];

        //                //for (int i = 0; i < items.Length; ++i)
        //                //{
        //                //    items[i] = new ListViewItem(i.ToString());
        //                //}

        //                //theListView.Items.AddRange(items);

        //                Image img = getReaderImage(reader);
        //                if (img == null)
        //                    img = GetDummyCover(reader["Path"].ToString(), reader["Title"].ToString());
        //                dbAddListViewAlbum(aListView, reader["AlbumID"].ToString(), reader["Path"].ToString(), reader["Title"].ToString(), img, (int)reader["GroupID"]);
        //                if (++cnt == 350)
        //                {
        //                    aListView.EndUpdate();
        //                    Cursor.Current = Cursors.Default;
        //                    return albumCount;
        //                }
        //                albumCount++;
        //            }
        //            //aListView.EndUpdate();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //    Cursor.Current = Cursors.Default;
        //    return albumCount;
        //}
        public ListViewItem[] dbLoadImages(ImageList cImageList)
        {
            if (connection.Database == null)
                return null;
            string sAlbumID;
            List<ListViewItem> items = new List<ListViewItem>();
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                SqlCeCommand command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Albums WHERE Status = 1 AND ParentAlbumID = 0 ORDER BY TimePlayed DESC";

                using (SqlCeDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sAlbumID=reader["AlbumID"].ToString();
                        Image img = getReaderImage(reader);
                        if (img == null)
                            img = GetDummyCover(reader["Path"].ToString(), reader["Title"].ToString());
                        cImageList.Images.Add(sAlbumID, img);
                        ListViewItem lvItem = new ListViewItem(sAlbumID);
                        lvItem.ImageIndex = items.Count + 1;
                        lvItem.Tag = reader["MediaTypeID"].ToString() + "|" + reader["Path"].ToString();
                        lvItem.ToolTipText = reader["Title"].ToString();
                        items.Add(lvItem);
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "dbLoadImages", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
            Cursor.Current = Cursors.Default;
            return items.ToArray();
        }
        public Rectangle GetBounds()
        {
            Rectangle rBounds = new Rectangle();
            SqlCeCommand cmd = new SqlCeCommand();
            cmd.CommandText = "SELECT * FROM [UserSettings] WHERE UserID=1";
            cmd.Connection = connection;
            cmd.CommandType = CommandType.Text;

            try
            {
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        rBounds.X = (int)reader["FormX"];
                        rBounds.Y = (int)reader["FormY"];
                        rBounds.Height = (int)reader["FormHeight"];
                        //rBounds.Width = (int)reader["FormWidth"] + 21;
                        rBounds.Width = (int)reader["FormWidth"];
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
            return rBounds;;
        }
        public void SetBounds(Rectangle rBounds)
        {
            try // execute insert query
            {
                using (SqlCeCommand cmd = new SqlCeCommand())
                {
                    cmd.CommandText = "UPDATE UserSettings SET FormX="+rBounds.X+", FormY="+rBounds.Y+", FormHeight="+rBounds.Height+", FormWidth="+rBounds.Width+" WHERE UserID=1";
                    cmd.Connection = connection;
                    cmd.CommandType = CommandType.Text;
                    int rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
        }
        public void AddListViewCover(ListView albumListView, string sAlbumID, string sPath, string sAlbumTitle, Image img, int iMediaTypeID)
        {
            if (string.IsNullOrEmpty(sAlbumID) || string.IsNullOrEmpty(sPath) || img == null)
                return;
            ImageList ilImages = albumListView.LargeImageList;
            ilImages.Images.Add(sAlbumID, img);
            dynamic index = ilImages.Images.IndexOfKey(sAlbumID);
            ListViewItem lvItem = new ListViewItem(sAlbumID);
            lvItem.ImageIndex = index;
            lvItem.Tag = iMediaTypeID + "|" + sPath;
            //item.Group = albumListView.Groups[iGroup];
            lvItem.ToolTipText = sAlbumTitle;
            albumListView.Items.Add(lvItem);
            lvItem.EnsureVisible();
        }
        public AddAlbumStatus dbAddAlbum(string sAlbumDir, bool bSilent = false, ListView lv = null, IEnumerable<FileInfo> files = null)
        {
            if (string.IsNullOrEmpty(sAlbumDir))
                return AddAlbumStatus.Failed;
            if (!Directory.Exists(sAlbumDir)) // its not a Directory?
                return AddAlbumStatus.Failed;
            if (connection.Database == null)
                return AddAlbumStatus.Failed;
            string sAlbumTitle = "";
            string sRadioChannelURL = "";
            bool bIsParentFolder = false;
            byte[] imageData = null;
            Image img = null;
            Int32 iMediaTypeID = 1;
            Int32 iAlbumID = 0;
            if (files == null) // 
                files = FileUtil.GetAllFiles(sAlbumDir);
            FileUtil.MusicFolderStatus stat = FileUtil.GetMusicFolderType(sAlbumDir, files);
            ID3Tag id3 = new ID3Tag(files);
            if (stat == FileUtil.MusicFolderStatus.Empty)
            {
                if (!bSilent)
                    MessageBox.Show(new Form() { TopMost = true }, "Der Ordner enthält keine Audiodateien.\n\nOrdner: " + sAlbumDir, 
                        "Invalid music folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return AddAlbumStatus.Failed;
            }
            if (stat == FileUtil.MusicFolderStatus.ParentFolder) // subdirs with music
            {
                //string sImageFile = FileUtil.GetAlbumImage(files);
                //if (string.IsNullOrEmpty(sImageFile))
                //    return AddAlbumStatus.Failed;
                //imageData = FileUtil.ReadFile(sImageFile);  // Read Image Bytes into a byte array
                //img = Gfx.ByteToImage(imageData);
                //iHashCode = Gfx.GetByteCode(imageData);
                //bIsParentFolder = true;
                // Todo: we need navigation controls
                return AddAlbumStatus.Failed;
            }
            else if (stat == FileUtil.MusicFolderStatus.RadioFolder) // 
            {
                iMediaTypeID = 2;
                sAlbumTitle = Path.GetFileName(sAlbumDir);
                IEnumerable<FileInfo> RadioFiles = FileUtil.GetRadioFiles(files);
                if (Path.GetExtension(RadioFiles.First().FullName) == ".url")
                    sRadioChannelURL = FileUtil.GetLnkTarget(RadioFiles.First().FullName);
                else
                    sRadioChannelURL = RadioFiles.First().FullName;
            }
            else // Files
            {
                sAlbumTitle = GetTitle(sAlbumDir, id3);
            }
            img = FileUtil.GetBestImage(files, id3);
            imageData = Gfx.ImageToByte(img);
            try // execute insert query
            {
                using (SqlCeCommand command = new SqlCeCommand("INSERT INTO Albums (Path, Image, IsParentFolder, Title, MediaTypeID) Values(@Path,@Image,@IsParentFolder,@Title,@MediaTypeID)", connection))
                {
                    command.Parameters.AddWithValue("@Path", sAlbumDir);
                    command.Parameters.AddWithValue("@Image", imageData);
                    command.Parameters.AddWithValue("@IsParentFolder", bIsParentFolder);
                    command.Parameters.AddWithValue("@Title", sAlbumTitle);
                    command.Parameters.AddWithValue("@MediaTypeID", iMediaTypeID);
                    command.CommandType = CommandType.Text;
                    int rowsAffected = command.ExecuteNonQuery();
                    command.CommandText = "SELECT @@IDENTITY";
                    iAlbumID = Convert.ToInt32(command.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message.ToString(), "Internal SQL failure: Add Album", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
                return AddAlbumStatus.Error;
            }
            if (iAlbumID == 0)
                return AddAlbumStatus.Failed;
            if (stat == FileUtil.MusicFolderStatus.ParentFolder)
            {
                //string[] subDirs = Directory.GetDirectories(sAlbumDir, "*");
                //if (subDirs == null)
                //    return AddAlbumStatus.Failed;
                //foreach (string sSubDir in subDirs)  // loop for sub dirs
                //{
                //    // Todo: dbAddAlbum
                //}

                // Todo: we need navigation controls
                return AddAlbumStatus.Failed;
            }
            else if (stat == FileUtil.MusicFolderStatus.RadioFolder) // 
            {
                if (AddRadioChannel(iAlbumID, sRadioChannelURL) == 0) // just in case
                {
                    dbDeleteAlbum(iAlbumID, true); // delete album
                    return AddAlbumStatus.Failed;
                }
            }
            else
            {
                //if (dbAddTracks(iAlbumID, sAlbumDir) == 0) // just in case
                //{
                //    dbDeleteAlbum(iAlbumID, true); // delete album
                //    return AddAlbumStatus.Failed;
                //}
            }
            if (img == null)
                img = GetDummyCover(sAlbumDir, sAlbumTitle);
            if (lv == null)
            {
                if (OnNewAlbumAdded != null)  // raise event: AutoPathChanged
                    OnNewAlbumAdded(this, new AlbumDBEventArgs(iAlbumID, sAlbumDir, sAlbumTitle, img, bIsParentFolder, iMediaTypeID));
            }
            else
            {
                AddListViewCover(lv, iAlbumID.ToString(), sAlbumDir, sAlbumTitle, img, iMediaTypeID);
            }
            BackUpOnExit = true;
            return AddAlbumStatus.Succeeded;
        }
        private string GetTitle(string sAlbumDir, ID3Tag id3)
        {
            if (string.IsNullOrEmpty(sAlbumDir) && id3.AudioFiles.Count() == 0)
                return null;
            string sAlbumTitle = Path.GetFileNameWithoutExtension(sAlbumDir);
            sAlbumTitle = StringIO.CleanUpTitle(sAlbumTitle);
            string sID3Title = id3.GetAlbumCaption();
            if (sID3Title != null)
                if (sID3Title.Length > sAlbumTitle.Length)
                    sAlbumTitle = sID3Title;
            string sCueFile = FileUtil.GetCueFile(id3.AudioFiles);
            if (sCueFile != null)
            {
                CueSheet cue = new CueSheet(sCueFile);
                string sCueTitle = cue.Title + " - " + cue.Performer;
                if (sCueTitle.Length > sAlbumTitle.Length)
                    sAlbumTitle = sCueTitle;
            }
            return sAlbumTitle;
        }
        private Image GetDummyCover(string sAlbumDir, string sAlbumTitle = "")
        {
            if (string.IsNullOrEmpty(sAlbumDir))
                return null;
            string sAlbumDirName = Path.GetFileName(sAlbumDir);
            Bitmap bmp = Properties.Resources.ImageMissing;
            Bitmap bmBG = new Bitmap(500, 500);
            Random rnd = new Random();
            double hue = rnd.Next(255);
            using (Graphics g = Graphics.FromImage(bmBG))
            {
                using (SolidBrush lb = new SolidBrush(Gfx.RandomLightColor(hue)))
                {
                    g.FillRectangle(lb, 0, 0, 500, 500);
                }
                g.DrawImage(bmp, 0, 0, 500, 500);
            }
            using (SolidBrush db = new SolidBrush(Gfx.RandomDarkColor(hue)))
            {
                string sSubTitle = StringIO.GetSubTitle(sAlbumDirName, sAlbumTitle);
                if (!string.IsNullOrEmpty(sSubTitle))
                {
                    bmBG = Gfx.WriteSubTitle(bmBG, sSubTitle, db);
                    sAlbumTitle = sAlbumTitle.Replace(sSubTitle, "");
                    sAlbumTitle = sAlbumTitle.Replace("()", "");
                    sAlbumTitle = sAlbumTitle.Replace("  ", " ");
                }
                bmBG = Gfx.WriteText(bmBG, sAlbumTitle, db);
            }
            return (Image)bmBG;
        }
        public void dbReloadAlbum(Int32 iAlbumID, byte[] imageData, string sAlbumPath)
        {
            Task t1 = Task.Factory.StartNew(() => DSetValue("Albums", "Image", imageData, "AlbumID", iAlbumID));
            Task t2 = Task.Factory.StartNew(() => dbReloadTrackList(iAlbumID, sAlbumPath));
        }
        public int dbReloadTrackList(Int32 iAlbumID, string sAlbumPath)
        {
            DDel("AlbumTracks", "AlbumID", iAlbumID);
            return AddTracks(iAlbumID, sAlbumPath);
        }
        public void dbCashTrackList(Int32 iAlbumID, string sAlbumPath)
        {
            if (Dlookup("AlbumID", "Albums", "AlbumID", iAlbumID) == null)
                return;
            AddTracks(iAlbumID, sAlbumPath);
        }
        public void dbReplaceCoverImage(ListView albumListView, Int32 iAlbumID, string sImageFile)
        {
            if (string.IsNullOrEmpty(sImageFile) || iAlbumID == 0)
                return;
            if (!FileUtil.FileExists(sImageFile)) //
                return;
            Cursor.Current = Cursors.WaitCursor;
            string sAlbumID = iAlbumID.ToString();
            byte[] imageData = FileUtil.ReadFile(sImageFile);  // Read Image Bytes into a byte array
            ImageList coverImageList = albumListView.LargeImageList;
            ListViewItem lvItem = albumListView.SelectedItems[0];
            Image img = Gfx.ByteToImage(imageData);
            coverImageList.Images[lvItem.ImageIndex] = Gfx.DrawCoverImage(coverImageList.Images[lvItem.ImageIndex], img);
            albumListView.Refresh();
            Task t = Task.Factory.StartNew(() => DSetValue("Albums", "Image", imageData, "AlbumID", iAlbumID)); //
            Cursor.Current = Cursors.Default;
        }
        public void dbReplaceAlbumFolder(Int32 iAlbumID, string sAlbumPath)
        {
            if (string.IsNullOrEmpty(sAlbumPath) || iAlbumID == 0)
                return;
            Cursor.Current = Cursors.WaitCursor;
            DDel("AlbumTracks", "AlbumID", iAlbumID);
            AddTracks(iAlbumID, sAlbumPath);
            DSetValue("Albums", "Path", sAlbumPath, "AlbumID", iAlbumID);
            Cursor.Current = Cursors.Default;
        }
        private Image getReaderImage(SqlCeDataReader reader)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                byte[] outbyte = new byte[100];
                Int32 ordinal = 0;
                Image img = null;

                ordinal = reader.GetOrdinal("Image"); // VarBinary column storing Bmp.
                outbyte = (byte[])reader[ordinal];
                ms.Write(outbyte, 0, outbyte.Length);
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                outbyte = null;
                try
                {
                    img = Image.FromStream(ms);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return img;
            }
        }
        private int AddTracks(Int32 iAlbumID, string sAlbumDir)
        {
            if (connection.Database == null)
                return 0;
            IEnumerable<FileInfo> fileQuery = FileUtil.GetAudioFiles(sAlbumDir);
            string sFileName;
            string sTitle;
            string sCueFile = null;
            Int16 i = 0;
            Int32 iPosition = 0;
            AlbumTrack aTrack;
            if (fileQuery.Count() == 0)
                return 0;
            if (fileQuery.Count() == 1)
            {
                sCueFile = FileUtil.GetCueFile(sAlbumDir);
                if (sCueFile != null)
                {
                    sFileName = Path.Combine(sAlbumDir, fileQuery.First().Name);
                    CueSheet cue = new CueSheet(sCueFile);
                    if (cue != null)
                    {
                        if (cue.Tracks.First().DataFile.Filename.Length > 0)
                        {
                            sFileName = Path.Combine(sAlbumDir, cue.Tracks.First().DataFile.Filename);
                            if (File.Exists(sFileName))
                            {
                                foreach (Track t in cue.Tracks)  // add relative Tracks
                                {
                                    i++;
                                    if (t.Indices.Count() > 0) 
                                        iPosition = (t.Indices[0].Minutes * 60) + t.Indices[0].Seconds;
                                    AddTrack(iAlbumID, sFileName, t.TrackNumber.ToString() + "/" + cue.Tracks.Count().ToString() + " " + t.Title, i.ToString(), null, iPosition, (Int16)t.Indices[0].Frames);
                                }
                            }
                        }
                    }
                    return cue.Tracks.Count();
                }
            }
            foreach (FileInfo f in fileQuery) // add by filename only
            {
                i++;
                sFileName = Path.Combine(sAlbumDir, f.Name);
                aTrack = ID3Tag.GetTrackData(f);
                if (aTrack == null)
                {
                    sTitle = StringIO.CleanUpTitle(Path.GetFileNameWithoutExtension(f.Name));
                    AddTrack(iAlbumID, sFileName, sTitle, i.ToString());
                }
                else
                    AddTrack(iAlbumID, sFileName, aTrack.Title, aTrack.TrackNo, aTrack.DiscNo);
            }
            return fileQuery.Count();
        }
        private void AddTrack(Int32 iAlbumID, string sFileName, string sTitle, string sTrackNo, string sDiscNo = "", Int32 iPosition = 0, Int16 iFrames = 0)
        {
            if (connection.Database == null)
                return;
            if (sDiscNo == null)
                sDiscNo = "1";
            try  // Insert into the SqlCe table. 
            {
                using (SqlCeCommand command = new SqlCeCommand("INSERT INTO AlbumTracks (AlbumID, FileName, Title, Position, Frames, TrackNo, DiscNo) Values(@AlbumID,@FileName,@Title,@Position,@Frames,@TrackNo,@DiscNo)", connection))
                {
                    command.Parameters.AddWithValue("@AlbumID", iAlbumID);
                    command.Parameters.AddWithValue("@FileName", sFileName);
                    command.Parameters.AddWithValue("@Title", sTitle);
                    command.Parameters.AddWithValue("@Position", iPosition);
                    command.Parameters.AddWithValue("@Frames", iFrames);
                    command.Parameters.AddWithValue("@TrackNo", sTrackNo);
                    command.Parameters.AddWithValue("@DiscNo", sDiscNo);
                    command.ExecuteNonQuery(); // ExecuteNonQuery is best for inserts.
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
        }
        private int AddRadioChannel(Int32 iAlbumID, string sRadioChannelURL)
        {
            if (connection.Database == null)
                return 0;
            if (string.IsNullOrEmpty(sRadioChannelURL) || iAlbumID == 0)
                return 0;
            int rowsAffected = 0;
            try
            {
                using (SqlCeCommand command = new SqlCeCommand("INSERT INTO RadioChannel (AlbumID, ChannelURL) Values(@AlbumID,@ChannelURL)", connection))
                {
                    command.Parameters.AddWithValue("@AlbumID", iAlbumID);
                    command.Parameters.AddWithValue("@ChannelURL", sRadioChannelURL);
                    rowsAffected = command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Internal SQL failure: dbAddRadioChannel", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
            return rowsAffected;
        }
        public void BuildCheckedListView(Int32 iAlbumID, string sAlbumPath, ListView albumListView)
        {
            if (iAlbumID == 0)
                return;
            if (connection.Database == null)
                return;
            if (BuildCheckedLV(iAlbumID, albumListView))
                return;
            AddTracks(iAlbumID, sAlbumPath);
            BuildCheckedLV(iAlbumID, albumListView);
        }
        public bool BuildCheckedLV(Int32 iAlbumID, ListView albumListView)
        {
            bool bHasRows = false;
            string text;
            try // execute query
            {
                using (SqlCeCommand command = new SqlCeCommand("SELECT * FROM AlbumTracks WHERE AlbumID=" + iAlbumID + " ORDER BY FileName ASC", connection))
                {
                    using (SqlCeDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ListViewItem item = new ListViewItem();
                            item.Checked = (bool)reader["Enabled"];
                            text = reader["TrackNo"].ToString() + " - " + reader["Title"].ToString();
                            if (reader["DiscNo"].ToString().Length > 0)
                                if (reader["DiscNo"].ToString() != "0")
                                    text = "CD " + reader["DiscNo"].ToString() + " - " + text;
                            item.Text = text;
                            item.Name = reader["AlbumTrackID"].ToString();
                            albumListView.Items.Add(item);
                            bHasRows = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
            return bHasRows;
        }
        public Queue<AlbumTrack> dbGetTrackListQueue(Int32 iAlbumID)
        {
            if (connection.Database == null)
                return null;
            Queue<AlbumTrack> TrackListQueue = new Queue<AlbumTrack>();
            try // execute query
            {
                using (SqlCeCommand command = new SqlCeCommand("SELECT * FROM AlbumTracks WHERE Enabled=1 AND AlbumID=" + iAlbumID + " ORDER BY FileName ASC", connection))
                //using (SqlCeCommand command = new SqlCeCommand("SELECT * FROM AlbumTracks WHERE AlbumID=" + iAlbumID + " ORDER BY FileName ASC", connection))
                {
                    using (SqlCeDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string FileName = reader["FileName"].ToString();
                            AlbumTrack aTrack = new AlbumTrack((Int32)reader["AlbumTrackID"], FileName, reader["Title"].ToString(),
                                reader["TrackNo"].ToString(), reader["DiscNo"].ToString(), (Int32)reader["Position"], (Int16)reader["Frames"]);
                            TrackListQueue.Enqueue(aTrack);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
                return null;
            }
            return TrackListQueue;
        }
        public LastPlayed dbGetLastPlayed()
        {
            LastPlayed lp = new LastPlayed(0, 0);
            if (connection.Database == null)
                return lp;
            try // execute query
            {
                using (SqlCeCommand command = new SqlCeCommand("SELECT * FROM UserSettings WHERE UserID=1", connection))
                {
                    using (SqlCeDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            lp.AlbumID = (Int32)reader["LastAlbumPlayed"];
                            lp.AlbumTrackID = (Int32)reader["LastTrackPlayed"];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "dbGetLastPlayed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
            return lp;
        }
        public void dbUpdateLastPlayed(Int32 iAlbumID, Int32 iAlbumTrackID)
        {
            if (connection.Database == null || iAlbumID == 0 || iAlbumTrackID == 0)
                return;
            try // execute insert query.
            {
                // Insert into the SqlCe table. ExecuteNonQuery is best for inserts.
                using (SqlCeCommand command = new SqlCeCommand("UPDATE UserSettings SET LastAlbumPlayed = " + iAlbumID + ", LastTrackPlayed = " + iAlbumTrackID + " WHERE UserID=1", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "dbUpdateLastPlayed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
        }
        public void dbUpdateTrackEnabled(string sAlbumTrackID, bool bEnabled)
        {
            if (connection.Database == null)
                return;
            int sEnabled = (bEnabled) ? (1) : (0);
            if (string.IsNullOrEmpty(sAlbumTrackID))
                return;
            try //execute insert query.
            {
                // Insert into the SqlCe table. ExecuteNonQuery is best for inserts.
                using (SqlCeCommand command = new SqlCeCommand("UPDATE AlbumTracks SET Enabled = " + sEnabled + " WHERE AlbumTrackID=" + sAlbumTrackID, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void dbUpdateLastTrackPlayed(Int32 iAlbumID, Int32 iAlbumTrackID)
        {
            if (connection.Database == null)
                return;
            try // Open connection and execute insert query.
            {
                string sNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // "yyyy-MM-dd hh:mm:ss"
                // Insert into the SqlCe table. ExecuteNonQuery is best for inserts.
                using (SqlCeCommand command = new SqlCeCommand("UPDATE Albums SET LastTrackPlayed = " + iAlbumTrackID + ", LastDatePlayed='" + sNow + "' WHERE AlbumID=" + iAlbumID, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
        }
        public Album dbGetAlbum(Int32 iAlbumID)
        {
            Album a = new Album();
            if (connection.Database == null)
                return a;
            try // Open connection
            {   
                using (SqlCeCommand command = new SqlCeCommand("SELECT * FROM Albums WHERE AlbumID=" + iAlbumID, connection))
                {
                    using (SqlCeDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            a.AlbumID = (Int32)reader["AlbumID"];
                            a.LastTrackPlayed = (Int32)reader["LastTrackPlayed"];
                            a.LastDatePlayed = reader["LastDatePlayed"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
            return a;
        }
        public long getTimePlayed(Int32 iAlbumID)
        {
            if (connection.Database == null)
                return 0;
            Int64 iTimePlayed = 0;
            try //Open connection and execute insert query.
            {
                // Insert into the SqlCe table. ExecuteNonQuery is best for inserts.
                using (SqlCeCommand command = new SqlCeCommand("SELECT TimePlayed FROM Albums WHERE AlbumID=" + iAlbumID, connection))
                {
                    //command.ExecuteNonQuery();
                    using (SqlCeDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            iTimePlayed = (Int64)reader[0];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
            return iTimePlayed;
        }
        public bool DSetValue(string sTable, string sSetField, dynamic dSetValue, string sIDField, Int32 iID) // xxx variable number of param/value pairs
        {
            if (string.IsNullOrEmpty(sSetField) || string.IsNullOrEmpty(sTable) || dSetValue == null)
                return false;
            if (connection.Database == null)
                return false;
            string query = "UPDATE [" + sTable + "] SET " + sSetField + "=@" + sSetField + " WHERE " + sIDField + "=" + iID;

            SqlCeCommand cmd = new SqlCeCommand();
            cmd.Connection = connection;
            cmd.CommandText = query;
            cmd.CommandType = CommandType.Text;

            SqlCeParameter param = new SqlCeParameter();
            param.ParameterName = "@" + sSetField;
            param.SqlDbType = GetDBType(dSetValue.GetType());
            param.Direction = ParameterDirection.Input;
            param.Value = dSetValue;

            cmd.Parameters.Add(param);

            try
            {
                // xxx should return if failed or not
                //iReturnValue = (Int32)cmd.ExecuteScalar();
                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
        private Int32 DAddRecord(string sTable, string sParam, dynamic dValue) // xxx variable number of param/value pairs
        {
            if (string.IsNullOrEmpty(sTable) || string.IsNullOrEmpty(sParam) || string.IsNullOrEmpty(dValue))
                return 0;
            if (connection.Database == null)
                return 0;
            Int32 iReturnValue = 0;
            string query;
            query = "INSERT INTO [" + sTable + "] (" + sParam + ") Values(@" + sParam + ")";

            SqlCeCommand cmd = new SqlCeCommand();
            cmd.Connection = connection;
            cmd.CommandText = query;
            cmd.CommandType = CommandType.Text;

            SqlCeParameter param = new SqlCeParameter();
            param.ParameterName = "@" + sParam;
            param.SqlDbType = GetDBType(dValue.GetType());
            param.Direction = ParameterDirection.Input;
            param.Value = dValue;

            cmd.Parameters.Add(param);

            try
            {
                // xxx should return primary key
                //iReturnValue = (Int32)cmd.ExecuteScalar();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
            return iReturnValue;
        }
        private dynamic Dlookup(string sSearchField, string sTable, string sParam, dynamic dValue)
        { // xxx may use Generics http://msdn.microsoft.com/en-us/library/512aeb7t%28v=vs.100%29.aspx
            // xxx check what data type is sSearchField and return propper datatype
            if (string.IsNullOrEmpty(sSearchField) || string.IsNullOrEmpty(sTable) || string.IsNullOrEmpty(sParam) || dValue == null)
                return null;
            if (connection.Database == null)
                return null;

            var dReturnValue = "";
            string query = "SELECT [" + sSearchField + "] FROM " + sTable + " WHERE " + sParam + " =@"  + sParam;

            SqlCeCommand cmd = new SqlCeCommand();
            cmd.Connection = connection;
            cmd.CommandText = query;
            cmd.CommandType = CommandType.Text;

            SqlCeParameter param = new SqlCeParameter();
            param.ParameterName = "@"  + sParam;
            param.SqlDbType = GetDBType(dValue.GetType());
            param.Direction = ParameterDirection.Input;
            param.Value = dValue;

            cmd.Parameters.Add(param);

            try
            {
                dReturnValue = (dynamic)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return dReturnValue;
        }
        private bool DInc(string sTable, string sSetField, int iIncrement, string sIDName, Int32 iID) // xxx variable number of param/value pairs
        { // xxx check if sSetField is numeric
            if (iID == 0 || iIncrement == 0 || string.IsNullOrEmpty(sTable) || string.IsNullOrEmpty(sSetField) || string.IsNullOrEmpty(sIDName))
                return false;
            if (connection.Database == null)
                return false;
            string query = "UPDATE [" + sTable + "] SET " + sSetField + "=(" + sSetField + " + " + iIncrement + ") WHERE " + sIDName + "=" + iID;

            SqlCeCommand cmd = new SqlCeCommand();
            cmd.Connection = connection;
            cmd.CommandText = query;
            cmd.CommandType = CommandType.Text;

            try
            {
                // xxx should return if failed or not
                //iReturnValue = (Int32)cmd.ExecuteScalar();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
        private bool DDel(string sTable, string sParam, dynamic dValue) // xxx variable number of param/value pairs
        {
            int rowsAffected;
            if (string.IsNullOrEmpty(sTable) || string.IsNullOrEmpty(sParam))
                return false;
            if (connection.Database == null)
                return false;
            string query = "DELETE [" + sTable + "] WHERE " + sParam + "=@" + sParam;

            SqlCeCommand cmd = new SqlCeCommand();
            cmd.Connection = connection;
            cmd.CommandText = query;
            cmd.CommandType = CommandType.Text;

            SqlCeParameter param = new SqlCeParameter();
            param.ParameterName = "@" + sParam;
            param.SqlDbType = GetDBType(dValue.GetType());
            param.Direction = ParameterDirection.Input;
            param.Value = dValue;
            cmd.Parameters.Add(param);

            try
            {
                rowsAffected = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            if (rowsAffected == 0) // returns 0 if failed
                return false;
            else
                return true;
        }
        private SqlDbType GetDBType(System.Type type)
        {
            SqlCeParameter param;
            System.ComponentModel.TypeConverter tc;
            param = new SqlCeParameter();
            tc = System.ComponentModel.TypeDescriptor.GetConverter(param.DbType);
            if (tc.CanConvertFrom(type))
            {
                param.DbType = (DbType)tc.ConvertFrom(type.Name);
            }
            else
            {
                switch (type.Name)
                {
                    case "Char":
                        param.SqlDbType = SqlDbType.Char;
                        break;
                    case "SByte":
                        param.SqlDbType = SqlDbType.SmallInt;
                        break;
                    case "UInt16":
                        param.SqlDbType = SqlDbType.SmallInt;
                        break;
                    case "UInt32":
                        param.SqlDbType = SqlDbType.Int;
                        break;
                    case "UInt64":
                        param.SqlDbType = SqlDbType.Decimal;
                        break;
                    case "Byte[]":
                        param.SqlDbType = SqlDbType.Image;
                        break;

                    default:
                        try
                        {
                            param.DbType = (DbType)tc.ConvertFrom(type.Name);
                        }
                        catch
                        {
                            // Some error handling
                        }
                        break;
                }
            }
            return param.SqlDbType;
        }
        private List<string> DGetArray(string sTable, string sSetField, SQLParamValue sqlWhere = null)
        {
            if (string.IsNullOrEmpty(sTable) || string.IsNullOrEmpty(sSetField))
                return null;
            if (connection.Database == null)
                return null;
            string query;
            List<string> lArray = new List<string>();
            SqlCeCommand cmd = new SqlCeCommand();
            cmd.Connection = connection;

            if (sqlWhere == null)  // no where clause
            {
                query = "SELECT [" + sSetField + "] FROM " + sTable;
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;
            }
            else
            {
                query = "SELECT [" + sSetField + "] FROM " + " [" + sTable + "] WHERE " + sqlWhere.Param + "=@" + sqlWhere.Param;
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;
                SqlCeParameter param = new SqlCeParameter();
                param.ParameterName = "@" + sqlWhere.Param;
                param.SqlDbType = GetDBType(sqlWhere.Value.GetType());
                param.Direction = ParameterDirection.Input;
                param.Value = sqlWhere.Value;
                cmd.Parameters.Add(param);
            }
            try
            {
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lArray.Add(reader[0].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            return lArray;
        }
        private SqlCeDataReader DGetReader(string sTable, string sParam, dynamic dValue)
        {
            if (string.IsNullOrEmpty(sTable) || string.IsNullOrEmpty(sParam))
                return null;
            if (connection.Database == null)
                return null;
            string query = "SELECT * FROM " + sTable + " WHERE " + sParam + " =@" + sParam;

            SqlCeCommand cmd = new SqlCeCommand();
            cmd.Connection = connection;
            cmd.CommandText = query;
            cmd.CommandType = CommandType.Text;

            SqlCeParameter param = new SqlCeParameter();
            param.ParameterName = "@" + sParam;
            param.SqlDbType = GetDBType(dValue.GetType());
            param.Direction = ParameterDirection.Input;
            param.Value = dValue;

            cmd.Parameters.Add(param);

            return cmd.ExecuteReader();
        }
    }
}