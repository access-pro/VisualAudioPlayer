using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Alphaleonis.Win32.Filesystem;
using System.Timers;
using System.Diagnostics;
using Cyotek.Windows.Forms;
using System.Security.Permissions;
using FolderSelect;
using System.Threading.Tasks;
using System.Linq;

namespace VisualAudioPlayer
{
    public partial class VisualAudioPlayer : Form
    {
        private static System.Timers.Timer PlayTimer;
        private static System.Timers.Timer PauseTimer;
        private static System.Timers.Timer HelpTimer;
        private static System.Timers.Timer FadeTimer;
        private static bool bLineVisible = false;
        private static bool bHelpActive = false;
        private static Int64 iTimePlayed; // 10 sec. counter
        private static AudioPlayer aPlayer;
        private static string sCaption;
        private static Point pNowPlaying = new Point();
        private Queue<string> HelpQueue = new Queue<string>();
        private static Pen pPauseBlinkerOn;
        private static Pen pPauseBlinkerOff;
        private int iAlbumIndexPlaying = -1;
        private static AlbumDB albumDB = new AlbumDB();
        private delegate void WatchHandler(object sender, System.IO.FileSystemEventArgs e);

        public VisualAudioPlayer()
        {
            InitializeComponent();
            sCaption = "VisualAudioPlayer - Version " + StringIO.GetCurrentVersion();
            this.Text = sCaption;
            coverImageList.Images.Add("cache", (Image)Properties.Resources.ImageMissing);
            albumDB.OnNewAlbumAdded += new AlbumDB.AutoDiscoveryHandler(AlbumDB_AlbumAdded);
            albumDB.OpenConnection();
            //int albumCount = albumDB.dbLoadImages(albumListView, coverImageList);
            //if (albumCount == 0) // album deleted
            //{
            //    LabelStartInfo.Visible = true;
            //    LabelStartInfo.BringToFront();
            //}
            List<string> lDiscoveryPaths = albumDB.GetDiscoveryPaths();
            if (lDiscoveryPaths.Count > 0)
            {
                foreach (string sDiscoveryPath in lDiscoveryPaths) // check every Discovery path
                {
                    if (!StartWatcher(sDiscoveryPath)) // start watching db path
                        break;
                }
                //bWatcherStarted = true;
            }
            aPlayer = new AudioPlayer(this.Handle);
            if (!aPlayer.Initialized)
            {
                // VisualAudioPlayer.Close
            }
            aPlayer.OnAlbumFinishedPlaying += new AudioPlayer.EndOfAlbumHandler(AudioPlayer_AlbumFinishedPlaying);
            aPlayer.OnSongStartPlaying += new AudioPlayer.TrackStartPlayingHandler(AudioPlayer_SongStartPlaying);
            aPlayer.OnTrackFinishedPlaying += new AudioPlayer.EndOfTrackHandler(AudioPlayer_TrackFinishedPlaying);
            aPlayer.OnAudioFileNotFound += new AudioPlayer.AudioFileNotFoundHandler(AudioPlayer_AudioFileNotFound);
            aPlayer.OnAudioDirNotFound += new AudioPlayer.AudioDirNotFoundHandler(AudioPlayer_AudioDirNotFound);
            aPlayer.OnRadioChannelFailed += new AudioPlayer.RadioChannelFailedHandler(AudioPlayer_RadioChannelFailed);
            PlayTimer = new System.Timers.Timer(10000); // Create a timer with a 10 sec interval.
            PauseTimer = new System.Timers.Timer(750); // Create a timer with a 2/3 sec interval.
            HelpTimer = new System.Timers.Timer(8000); // Create a timer with a 8 sec interval.
            FadeTimer = new System.Timers.Timer(10); // Create a timer with a 1/10 sec interval.
            PlayTimer.Elapsed += new ElapsedEventHandler(OnPlayEvent);  // Hook up the Elapsed event for the timer.
            PauseTimer.Elapsed += new ElapsedEventHandler(OnPauseTimerEvent);  // Hook up event to blink music visual when pausinhg
            HelpTimer.Elapsed += new ElapsedEventHandler(OnHelpTimerEvent);  // Hook up event to for help timeout
            FadeTimer.Elapsed += new ElapsedEventHandler(OnFadeTimerEvent);  // Hook up event to blink music visual when pausinhg
            HelpQueue.Enqueue(GlobaStrings.StartHelp0);
            HelpQueue.Enqueue(GlobaStrings.StartHelp1);
            HelpQueue.Enqueue(GlobaStrings.StartHelp2);
            HelpQueue.Enqueue(GlobaStrings.StartHelp3);
            HelpQueue.Enqueue(GlobaStrings.StartHelp4);
            HelpQueue.Enqueue(GlobaStrings.StartHelp5);
            HelpQueue.Enqueue(GlobaStrings.StartHelp6);
            HelpQueue.Enqueue(GlobaStrings.StartHelp7);
            HelpQueue.Enqueue(GlobaStrings.StartHelp8);
            pPauseBlinkerOn = new Pen(Color.GreenYellow, 2);
            pPauseBlinkerOff = new Pen(Color.Black, 2);
            Rectangle rBounds = albumDB.GetBounds(); // Set form pos and size
            if (rBounds.Height > 0)
                this.Bounds = rBounds;
            //System.Threading.Timer timer = new System.Threading.Timer(obj => { this.Invoke(new Action(() => ContinuePlay())); }, null, 1000, System.Threading.Timeout.Infinite);
            // TODO: AudioBooks continue play at last position -10 sek.
        }
        private void UpdateStartHelp()
        {
            if (!bHelpActive)
                return;
            if (HelpQueue.Count == 0)
            {
                FadeTimer.Start();
                return;
            }
            if (HelpQueue.Count == 1)
            {
                Point mousePos = albumListView.PointToClient(Control.MousePosition);
                ListViewHitTestInfo hitTest = albumListView.HitTest(mousePos);
                if (hitTest.Item == null)
                    return;
                ToolTip mytip = new ToolTip();

                mytip.Show(hitTest.Item.ToolTipText, this, mousePos.X, mousePos.Y, 2000);
                HelpTimer.Start();
            }
            labelAlbumTitle.Text = HelpQueue.Dequeue(); // Show next help
        }
        private void AudioPlayer_TrackFinishedPlaying(AudioPlayer sender, EventArgs e)
        {
            // Aufruf der Methode aus dem Worker-Thread
            try
            {
                this.Invoke(new Action(() => aPlayer.Next()));
            }
            catch
            {
                // Some error handling
            }
            this.Invoke((Action<Int32>)IncreaseTrackPlayCount, sender.CurrentTrackPlaying.AlbumTrackID);
        }
        private void AudioPlayer_AudioFileNotFound(AudioPlayer sender, AudioPlayer.AudioPlayerEventArgs e)
        {
            // Aufruf der Methode aus dem Worker-Thread
            this.Invoke((Action<Int32>)RescanAlbumTrackListByAlbumID, e.Album.AlbumID);
            aPlayer.Next(); // try play next track
        }
        private void RescanAlbumTrackListByAlbumID(Int32 iAlbumID)
        {
            ListViewItem lvItem = albumListView.FindItemWithText(iAlbumID.ToString());
            if (lvItem != null) // album actualy exdsits
                ScanAlbumTrackList(lvItem);
        }
        private void RescanAlbumTrackListByIndex(int iIndex)
        {
            if (iIndex == albumListView.Items.Count) // album deleted
                iIndex = 0;
            ListViewItem lvItem = albumListView.Items[iIndex];
            string sAlbumPath = lvItem.Tag.ToString().Substring(2);
            Int32 iAlbumID = Convert.ToInt32(lvItem.Text);
            Task t = Task.Factory.StartNew(() => albumDB.dbCashTrackList(iAlbumID, sAlbumPath));
        }
        private bool ScanAlbumTrackList(ListViewItem lvItem)
        {
            if (lvItem == null) // album deleted
                return false;
            this.labelAlbumTitle.Text = "Loading traks...";
            string sAlbumPath = lvItem.Tag.ToString().Substring(2);
            Int32 iAlbumID = Convert.ToInt32(lvItem.Text);
            FileUtil.MusicFolderStatus stat = FileUtil.GetMusicFolderType(sAlbumPath); // all audio files deleted?
            if (stat == FileUtil.MusicFolderStatus.MusicFolder) // just reload
            {
                albumListView.Items[lvItem.Index].Selected = true;
                int iCnt = albumDB.dbReloadTrackList(iAlbumID, lvItem.Tag.ToString().Substring(2));
                if (stat == FileUtil.MusicFolderStatus.MusicFolder && iCnt > 0)
                    return true; // Plays album
                
            }
            string sParentPath = FileUtil.GetPrevDir(lvItem.Tag.ToString().Substring(2)); // dublicate album?
            int index = GetIndexByListViewTag(sParentPath);
            if (index == -1)  // Parent not added jet
            {
                stat = FileUtil.GetMusicFolderType(sParentPath);
                if (stat == FileUtil.MusicFolderStatus.MusicFolder) // music moved to parent folder?
                {
                    // Todo: Compare size of audio files
                    albumDB.dbReplaceAlbumFolder(iAlbumID, sParentPath);
                    lvItem.Selected = true;
                    lvItem.Tag = "1|" + sParentPath;
                    ReloadAlbum(lvItem);
                    return true; // Plays album
                }
            }
            else
            {
                albumListView.Items[index].Selected = true;
            }
            if (MessageBox.Show("Im Ordner des Albums befinden sich keine Audiodatein mehr.\nAlbum enternen?", "Wiedergabe nicht möglich",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    DeleteAlbum(lvItem);
            return false;
        }
        private void AudioPlayer_AudioDirNotFound(AudioPlayer sender, AudioPlayer.AudioPlayerEventArgs e)
        {
            this.Invoke((Func<Int32, bool>)RecoverMusicFolder, e.Album.AlbumID); // Aufruf der Methode aus dem Worker-Thread
        }
        private bool RecoverMusicFolder(Int32 iAlbumID)
        {
            ListViewItem lvItem = albumListView.FindItemWithText(iAlbumID.ToString());
            if (lvItem == null) // album deleted
                return false;
            string sPath = lvItem.Tag.ToString().Substring(2);
            string sParentPath = FileUtil.GetPrevDir(sPath);
            int iParentIndex = GetIndexByListViewTag(sParentPath);
            string sNewPath = null;
            if (FileUtil.IsServerPath(sParentPath))
            {
                string sServerName = FileUtil.GetServerName(sParentPath);
                if (!FileUtil.IsServerPingable(sServerName))
                {
                    MessageBox.Show(new Form() { TopMost = true }, GlobaStrings.ServerDown + "\n\nServer: " + sServerName, GlobaStrings.ServerDownCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
            }
            if (iParentIndex > -1)  // parent Folder already added
            {                                  // Cant recover
                if (MessageBox.Show("Der Ordner des Albums wurde umbenannt oder existiert nicht mehr.\nAlbum enternen?", "Wiedergabe nicht möglich",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        DeleteAlbum(lvItem);
                albumListView.Items[iParentIndex].EnsureVisible();
                return false;
            }
            else
            {
                if (GetIndexByListViewTag(sParentPath) == -1) // check if album dir already added to ListView
                {
                    FileUtil.MusicFolderStatus stat = FileUtil.GetMusicFolderType(sParentPath); // Music moved to parent Folder?
                    if (stat == FileUtil.MusicFolderStatus.MusicFolder)
                    {
                        albumDB.dbReplaceAlbumFolder(iAlbumID, sParentPath);
                        lvItem.Selected = true;
                        lvItem.Tag = "1|" + sParentPath;
                        lvItem.ToolTipText = StringIO.Path2Title(sParentPath);
                        ReloadAlbum(lvItem, true);
                        return true;
                    }
                }
            }
            List<string> lMatches = FileUtil.FindSimilarDirs(sPath); // Check for new dirs with similar dir names
            if (lMatches.Count == 0) // nothing found
            {
                foreach (string path in lMatches) // search for new dir
                {
                    if (GetIndexByListViewTag(path) == -1)
                    {
                        sNewPath = path;
                        // Todo: Compare size of audio files?
                        break;
                    }
                }
                if (sNewPath != null) // replace with new dir
                {
                    albumDB.dbReplaceAlbumFolder(iAlbumID, sNewPath);
                    lvItem.Selected = true;
                    lvItem.Tag = "1|" + sNewPath;
                    lvItem.ToolTipText = StringIO.Path2Title(sNewPath);
                    ReloadAlbum(lvItem, true);
                    return true;
                }
            }
            lMatches = FileUtil.FindLostDir(sPath); // Check for identical dir in parents sub-folders
            if (lMatches.Count == 0) // nothing found
            {
                foreach (string path in lMatches) // search for new dir
                {
                    int index = GetIndexByListViewTag(path);
                    if (index == -1)  //
                    {
                        sNewPath = path;  // Todo: Compare size of audio files?
                        break;
                    }
                    else
                    {   // Select and play
                        DeleteAlbum(lvItem);
                        lvItem = albumListView.Items[index];
                        sNewPath = lvItem.Tag.ToString().Substring(2);
                        break;
                    }
                }
                if (sNewPath != null) // replace with new dir
                {
                    albumDB.dbReplaceAlbumFolder(iAlbumID, sNewPath);
                    lvItem.Selected = true;
                    lvItem.Tag = "1|" + sNewPath;
                    lvItem.ToolTipText = StringIO.Path2Title(sNewPath);
                    ReloadAlbum(lvItem, true);
                    return true;
                }
            }
            if (MessageBox.Show("Der Ordner des Albums wurde umbenannt oder existiert nicht mehr.\nAlbum enternen?", "Wiedergabe nicht möglich",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    DeleteAlbum(lvItem);
            return false;
        }
        private void IncreaseTrackPlayCount(Int32 iAlbumTrackID)
        {
                albumDB.dbIncPlayCount(iAlbumTrackID); //
        }
        private void AudioPlayer_SongStartPlaying(AudioPlayer sender, EventArgs e)
        {
            try
            {
                // Aufruf der Methode aus dem Worker-Thread
                this.Invoke((Action<AlbumTrack>)SetCaptionLabel, sender.CurrentTrackPlaying);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "Internal SQL failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex.Message);
            }
        }
        private void SetCaptionLabel(AlbumTrack aTrack)
        {
            if (bHelpActive)
                return;
            string text = aTrack.TrackNo + " - " + aTrack.Title;
            if (aTrack.DiscNo.Length > 0)
                if (aTrack.DiscNo != "0")
                    text = text + " (CD " + aTrack.DiscNo + ")";
            labelAlbumTitle.Text = text;
        }
        private void albumListView_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    UpdateStartHelp();
                    PlaySelectedAlbum();
                    break;
            }
        }
        private void AudioPlayer_RadioChannelFailed(AudioPlayer sender, EventArgs e)
        {
            // Aufruf der Methode aus dem Worker-Thread
            this.Invoke((Action<string>)RadioChannelFailed, sender.CurrentRadioChannelPlaying);
        }
        private void RadioChannelFailed(string sRadioChannel)
        {
            MessageBox.Show(new Form() { TopMost = true }, "Der Radiokanal kann nicht wiedergegeben werden.\n\nRadiokanal: " + sRadioChannel,
                "Wiedergabefehler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void AudioPlayer_AlbumFinishedPlaying(AudioPlayer sender, EventArgs e)
        {
            // Aufruf der Methode aus dem Worker-Thread
            this.Invoke((Action<Int32>)PlayNextAlbum, sender.CurrentAlbumPlaying.AlbumID);
        }
        private void PlayNextAlbum(Int32 iFinishedAlbumID)
        {
            int index;
            ListViewItem item1 = this.albumListView.FindItemWithText(iFinishedAlbumID.ToString());
            if (item1 == null) // album deleted
            {
                index = 0;
            }
            else
            {
                if (this.albumListView.Items.Count == item1.Index) // last album 
                {
                    index = 0;
                }
                else // next album 
                {
                    index = item1.Index + 1;
                }
            }
            this.albumListView.SelectedIndices.Clear();
            try
            {
                this.albumListView.SelectedIndices.Add(index);
            }
            catch
            {
                this.albumListView.SelectedIndices.Add(0);
            }
            PlaySelectedAlbum();
        }
        private void PlaySelectedAlbum(Int32 iStartTrack = 0)
        {
            if (albumListView.SelectedItems.Count == 0)
                return;
            ListViewItem lvItem = albumListView.SelectedItems[0];
            lvItem.EnsureVisible();
            Int32 iAlbumID = Convert.ToInt32(lvItem.Text);
            Int32 iMediaTypeID = Convert.ToInt32(lvItem.Tag.ToString().Substring(0, 1));
            string sAlbumPath = lvItem.Tag.ToString().Substring(2);
            if (PauseTimer.Enabled)
                PauseTimer.Stop();
            if (IsSelectedAlbumPlaying())
            {
                if (iMediaTypeID == 2) // Radio
                {
                    PauseAudio();
                    return;
                }
                else
                {
                    aPlayer.Next();
                    return;
                }
            }
            switch (iMediaTypeID)
            {
                case 1: // Files
                    Queue<AlbumTrack> TrackListQueue = albumDB.dbGetTrackListQueue(iAlbumID);
                    if (TrackListQueue.Count == 0) // Album not jet loaded
                    {
                        if (ScanAlbumTrackList(lvItem)) // First Init
                        {
                            TrackListQueue = albumDB.dbGetTrackListQueue(iAlbumID);
                            if (TrackListQueue.Count == 0)
                                return;
                        }
                        else
                        {
                            return;
                        }
                    }
                    Album currAlbum = albumDB.dbGetAlbum(iAlbumID);
                    AccessStatus aStat = FileUtil.DirectoryExists(sAlbumPath);
                    switch (aStat)
                    {
                        case AccessStatus.NotExisting:
                            if (RecoverMusicFolder(iAlbumID))
                            {
                                TrackListQueue = albumDB.dbGetTrackListQueue(iAlbumID);
                                if (TrackListQueue.Count == 0)
                                    return;
                            }
                            else
                            {
                                return;
                            }
                            break;
                        case AccessStatus.ServerDown:
                            return;
                        case AccessStatus.LoginFailed:
                            return;
                    }
                    FlushAlbum(); // clean up previouse album
                    SetCoverNowPlaying(lvItem);
                    if (!aPlayer.PlayAudioQueue(currAlbum, TrackListQueue, iStartTrack)) // play audio queue
                    {
                        return;
                    }
                    break;
                case 2: // Radio
                    string sRadioChannel = albumDB.dbGetChannelURL(iAlbumID);
                    FlushAlbum(); // clean up previouse album
                    SetCoverNowPlaying(lvItem);
                    if (!aPlayer.PlayRadioChannel(sRadioChannel)) // play audio queue
                        return;
                    break;
                case 3: // CD
                    break;
            }
            PlayTimer.Start();
            RescanAlbumTrackListByIndex(lvItem.Index + 1);
        }
        private void SetCoverNowPlaying(ListViewItem lvItem)
        {
            if (iAlbumIndexPlaying != -1) // 
                coverImageList.Images[iAlbumIndexPlaying + 1] = coverImageList.Images[0];
            iAlbumIndexPlaying = lvItem.Index;
            try // avoid out of memory
            {
                coverImageList.Images[0] = coverImageList.Images[lvItem.ImageIndex];
                coverImageList.Images[lvItem.ImageIndex] = Gfx.DrawCoverNowPlaying(coverImageList.Images[lvItem.ImageIndex]);
                lvItem.EnsureVisible();
                albumListView.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Internal failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void albumListView_DragDrop(object sender, DragEventArgs e) // someone droped something
        {
            string[] fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (fileNames == null)
                return;
            if (fileNames.Length == 0)
                return;
            Task t = Task.Factory.StartNew(() => AutoDiscoverTree(fileNames));
        }
        private void AutoDiscoverTree(string[] fileNames) // someone droped something
        {
            Array.Sort(fileNames);
            foreach (string sFile in fileNames)
            {
                List<string> lDbDirs = null;
                albumDB.DiscoverNewAlbum(sFile, lDbDirs); // check for new albums in all Discovery dirs  
                return;
            }
            if (albumListView.Items.Count == 0)
            {
                return;
            }
            if (albumListView.Items.Count > 2) // TODO: 3-7 than 2nd row
                return;
            //UpdateStartHelp();
        }
        private void albumListView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        private bool AddAlbumPath(string sFile, bool bSilent)
        {
            string sAlbumDir;

            if (Directory.Exists(sFile))  // Directory 
            {
                sAlbumDir = sFile;
            }
            else // file 
            {
                if (!Path.HasExtension(sFile)) // Not a Directory and strange file
                {
                    if (!bSilent)
                        MessageBox.Show(new Form() { TopMost = true }, "Bitte nur Musikordner oder Verknüpfungen auf Musikordner importieren.", 
                            "Unbekannter Dateityp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                if (FileUtil.IsLink(sFile))  // its a Link?
                {
                    string sLnkTargetFile = FileUtil.GetLnkTarget(sFile);
                    if (sFile == null)
                        return true;
                    if (FileUtil.FileExists(sLnkTargetFile))
                    {
                        if (!bSilent)
                            MessageBox.Show(new Form() { TopMost = true }, "Bitte nur Verknüpfungen auf Musikordner importieren und nicht auf Dateien.\n\nVerknüpfung: " +
                                   sFile + "\n\nZieldatei: ", "Bad link", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                    if (FileUtil.DirectoryExists(sLnkTargetFile) == AccessStatus.NotExisting) // LnkTarget NotExisting
                    {
                        if (!bSilent)
                            MessageBox.Show(new Form() { TopMost = true }, "Der Zielordner der Verknüpfung existiert nicht.\n\nVerknüpfung: " +
                                sFile + "\n\nZielordner: " + sLnkTargetFile, "Bad link", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                    sAlbumDir = sLnkTargetFile;
                }
                else
                {
                    sAlbumDir = Path.GetDirectoryName(sFile);
                }
            }
            if (GetIndexByListViewTag(sAlbumDir) > -1) // check if album dir already added to ListView
            {
                if (!bSilent)
                    MessageBox.Show(new Form() { TopMost = true }, "Dieses Album wurde bereits hinzugefügt.\n\nAlbumordner: " + sAlbumDir,
                        "Doppeltes Album", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            if (!albumDB.IsNewPath(sAlbumDir))
                return false;
            if (FileUtil.PathIsDirectoryEmpty(sAlbumDir)) // 
                albumDB.ScanPath(sAlbumDir, false);
            else
            {
                //Task t = Task.Factory.StartNew(() => albumDB.dbAddAlbum(sAlbumDir));
                if (albumDB.dbAddAlbum(sAlbumDir, bSilent, albumListView) == AddAlbumStatus.Error)
                    return false;
                else
                {
                    UpdateStartHelp();
                    rezizeForm();
                    Application.DoEvents();
                }
            }
            return true;
        }
        private void VisualAudioPlayer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (albumDB.IsDiscovering)
            {
                albumDB.StopDiscovering = true;
                Application.DoEvents();
            }
            FlushAlbum();
            albumDB.SetBounds(this.Bounds);
        }
        private void FlushAlbum()
        {
            if (!aPlayer.IsPlaying)
                return;
            if (albumListView.SelectedItems.Count == 0)
            {
                return;
            }
            ListViewItem lvItem = albumListView.SelectedItems[0];
            Int32 iAlbumID = Convert.ToInt32(lvItem.Text);
            PlayTimer.Stop();
            aPlayer.ResetTrackQueue();
            aPlayer.Stop();
            if (iTimePlayed > 0 && aPlayer.CurrentRadioChannelPlaying != null)
            {
                this.Invoke((Action<Int32, Int64>)albumDB.dbUpdateTimePlayed, iAlbumID, iTimePlayed);
                this.Invoke((Action<Int32, Int32>)albumDB.dbUpdateLastTrackPlayed, iAlbumID, aPlayer.CurrentTrackPlaying.AlbumTrackID);
                //albumDB.dbUpdateTimePlayed(iAlbumID, iTimePlayed);
                //albumDB.dbUpdateLastTrackPlayed(iAlbumID, aPlayer.CurrentTrackPlaying.AlbumTrackID);
            }
            this.Invoke((Action<Int32, Int32>)albumDB.dbUpdateLastPlayed, iAlbumID, aPlayer.CurrentTrackPlaying.AlbumTrackID);
            //albumDB.dbUpdateLastPlayed(iAlbumID, aPlayer.CurrentTrackPlaying.AlbumTrackID);
            iTimePlayed = 0;
            if (iAlbumIndexPlaying != -1) // 
            this.Text = sCaption;
        }
        private bool PauseAudio()
        {
            if (!aPlayer.IsPlaying) // Todo: Continue from last pos in db
                return false;
            aPlayer.Pause();
            UpdateStartHelp();
            if (PauseTimer.Enabled)
            {
                PauseTimer.Stop();
                if (!aPlayer.IsPaused)
                {
                    if (bLineVisible)
                        BlinkPauseIndicator();
                }
            }
            else
                PauseTimer.Start();
            return true;
        }
        private void albumListView_KeyUp(object sender, KeyEventArgs e)
        {
            if (albumListView.SelectedItems.Count == 0)
                return;
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    DeleteSelectedAlbum();
                    break;
                case Keys.Play:
                case Keys.Pause:
                case Keys.MediaPlayPause:
                case Keys.Space:
                    PauseAudio();
                    break;
                case Keys.MediaPreviousTrack:
                    break;
                case Keys.MediaNextTrack:
                    break;
                case Keys.MediaStop:
                    break;
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down: 
                    UpdateStartHelp();
                    break;
            }
        }
        private void BlinkPauseIndicator()
        {
            if (albumListView.SelectedItems.Count == 0 || iAlbumIndexPlaying == -1) // nothing selected
                return;
            ListViewItem lvItem = albumListView.Items[iAlbumIndexPlaying];
            System.Drawing.Rectangle rect = albumListView.GetItemRect(iAlbumIndexPlaying, ItemBoundsPortion.Entire);
            int yPos = rect.Y - 1;
            using (Graphics g = this.albumListView.CreateGraphics())
            {
                if (bLineVisible)
                    g.DrawLine(pPauseBlinkerOff, lvItem.Position.X, yPos, lvItem.Position.X + 194, yPos);
                else
                    g.DrawLine(pPauseBlinkerOn, lvItem.Position.X, yPos, lvItem.Position.X + 194, yPos);
            }
            bLineVisible = !bLineVisible;
        }
        private void DeleteSelectedAlbum()
        {
            if (albumListView.SelectedItems.Count == 0) // nothing selected
                return;
            if (MessageBox.Show("Dieses Album wirklich löschen?", "Album löschen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;
            ListViewItem lvItem = albumListView.SelectedItems[0];
            DeleteAlbum(lvItem);
        }
        private void DeleteAlbum(ListViewItem lvItem) // 
        {
            Int32 iAlbumID = Convert.ToInt32(lvItem.Text);
            if (iAlbumIndexPlaying == lvItem.Index) // Is this album currently playing?
            {
                iAlbumIndexPlaying = -1;
                FlushAlbum();
            }
            //if (iAlbumIndexPlaying == lvItem.Index) // Is selected album currently playing?
            //    iAlbumIndexPlaying = -1;
            if (iAlbumIndexPlaying > lvItem.Index) // correct index for cash image
                iAlbumIndexPlaying--;
            string sAlbumPath = GetSelectedPath();
            albumDB.dbDeleteAlbum(iAlbumID, true); // delete permanently
            int iImageIndex = lvItem.ImageIndex;
            int iIndex = lvItem.Index;
            DecrementImageIndex(iIndex);
            lvItem.Remove();
            Image img = coverImageList.Images[iImageIndex];
            coverImageList.Images.RemoveAt(iImageIndex);
            img.Dispose();
            labelAlbumTitle.Text = "Ready";
        }
        private void DecrementImageIndex(int iImageIndex) // adjust the image index of any list view items that follow this one
        {
            for (int i = iImageIndex+1; i < albumListView.Items.Count; i++)
                albumListView.Items[i].ImageIndex--;
        }
        private int GetIndexByListViewTag(string sTag)
        {
            if (albumListView.Items.Count == 0)
                return -1;
            foreach (ListViewItem item in albumListView.Items)
            {
                if (item.Tag.ToString().EndsWith(sTag))
                    return item.Index;
            }
            return -1;
        }
        private bool IsSelectedAlbumPlaying() // Is selected album currently playing?
        {
            if (aPlayer.CurrentAlbumPlaying.AlbumID == 0)
                return false;
            ListViewItem lvItem = albumListView.SelectedItems[0];
            Int32 iAlbumID = Convert.ToInt32(lvItem.Text);
            if (iAlbumID == aPlayer.CurrentAlbumPlaying.AlbumID)
                return true;
            return false;
        }
        private void rezizeForm()
        {
            Rectangle resolution = Screen.PrimaryScreen.Bounds;
            int scrolbar = 18;
            int colums = (this.Width - 16) / albumListView.TileSize.Width;
            double d = (float)albumListView.Items.Count / (float)colums;
            int rows = (int)Math.Ceiling(d);
            int height = (albumListView.TileSize.Height * rows) + 42;
            int width = (albumListView.TileSize.Width * colums) + 22;
            if (albumListView.Width > (albumListView.Items.Count * albumListView.TileSize.Width))
                return;
            // Grow horizontaly?
            if ((resolution.Width - (this.Left + this.Width)) > albumListView.TileSize.Width)
            {
                this.Width = (albumListView.TileSize.Width * (colums + 1)) + albumListView.Margin.Left + albumListView.Margin.Right + 24 + scrolbar;
                return;
            }
            // Grow verticaly?
            if ((resolution.Height - (this.Top + this.Height)) > albumListView.TileSize.Height)
            {
                Rectangle screenRectangle = RectangleToScreen(this.ClientRectangle);
                int titleHeight = screenRectangle.Top - this.Top;
                this.Height = (albumListView.TileSize.Height * (rows)) + albumListView.Top + albumListView.Margin.Top + albumListView.Margin.Bottom + titleHeight + 12;
                // Todo: Optimize horizontal width 
                return;
            }
        }
        private static void OnPlayEvent(object source, ElapsedEventArgs e)
        {
            iTimePlayed++;
        }
        private void albumsContextMenu_Opening(object sender, CancelEventArgs e)
        {
            if (albumListView.SelectedItems.Count == 0)
            {
                albumsContextMenu.Items[0].Enabled = false;
                albumsContextMenu.Items[1].Enabled = false;
                albumsContextMenu.Items[2].Enabled = false;
                albumsContextMenu.Items[3].Enabled = false;
                albumsContextMenu.Items[4].Enabled = true;
                albumsContextMenu.Items[5].Enabled = true;
                albumsContextMenu.Items[6].Enabled = false;
            }
            else
            {
                albumsContextMenu.Items[0].Enabled = true;
                albumsContextMenu.Items[1].Enabled = true;
                albumsContextMenu.Items[2].Enabled = true;
                albumsContextMenu.Items[3].Enabled = true;
                albumsContextMenu.Items[4].Enabled = true;
                albumsContextMenu.Items[5].Enabled = true;
                albumsContextMenu.Items[6].Enabled = true;
            }
        }
        private void MenuItemEditPlaylist_Click(object sender, EventArgs e)
        {
            if (albumListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(new Form() { TopMost = true }, "Bitte wählen Sie ein Album aus um diese Funktion duchzuführen.", "Keine Auswahl", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ListViewItem lvItem = albumListView.SelectedItems[0];
            Int32 iAlbumID = Convert.ToInt32(lvItem.Text);
            TrackListForm TLForm = new TrackListForm(lvItem.ToolTipText.ToString());
            string sAlbumPath = lvItem.Tag.ToString().Substring(2);
            albumDB.BuildCheckedListView(iAlbumID, sAlbumPath, TLForm.TrackList);
            if (TLForm.TrackList.Items.Count == 0)
                return;
            // Resize Form
            int BorderWidth = (SystemInformation.Border3DSize.Width + SystemInformation.BorderSize.Width + SystemInformation.FrameBorderSize.Width) * 2;
            int BorderHeight = (SystemInformation.Border3DSize.Height + SystemInformation.BorderSize.Height + SystemInformation.FrameBorderSize.Height) * 2;
            int TitlebarHeight = SystemInformation.CaptionHeight;
            TLForm.Height = ((TLForm.TrackList.Items[0].Bounds.Height+2) * TLForm.TrackList.Items.Count) + TitlebarHeight + BorderHeight;
            TLForm.Width = TLForm.TrackList.GetItemRect(0).Width + BorderWidth + 3;
            if (TLForm.Width < TLForm.labelAlbumTitle.Width)
                TLForm.Width = TLForm.labelAlbumTitle.Width + 12;
            TLForm.ShowDialog();
        }
        private void MenuItemChooseCover_Click(object sender, EventArgs e)
        {
            if (albumListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(new Form() { TopMost = true }, "Bitte wählen Sie ein Album aus um diese Funktion duchzuführen.", "Keine Auswahl", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ListViewItem lvItem = albumListView.SelectedItems[0];
            Int32 iAlbumID = Convert.ToInt32(lvItem.Text);
            string sAlbumPath = albumListView.SelectedItems[0].Tag.ToString().Substring(2);
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif) | *.jpg; *.jpeg; *.png; *.gif";
            dialog.InitialDirectory = @sAlbumPath;
            dialog.Title = "Bitte wählen Sie eine neue Bilddatei aus:";
            if (dialog.ShowDialog() == DialogResult.Cancel)
                return;
            string fileName = dialog.FileName;
            albumDB.dbReplaceCoverImage(albumListView, iAlbumID, fileName);
            if (IsSelectedAlbumPlaying()) 
                RepaintPlaySatus();
        }
        private void OpenFolderMenuItem_Click(object sender, EventArgs e)
        {
            if (albumListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(new Form() { TopMost = true }, "Bitte wählen Sie ein Album aus um diese Funktion duchzuführen.", "Keine Auswahl", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string sAlbumPath = GetSelectedPath();
            if (FileUtil.DirectoryExists(sAlbumPath) == AccessStatus.NotExisting) // Album folder may have been deleted
            {
                MessageBox.Show(new Form() { TopMost = true }, "Der Musikordner wurde umbenannt oder existiert nicht mehr.\n\nOrdnername: " + sAlbumPath, "No such directory", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            try  // 
            {
                Process.Start(@sAlbumPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //Console.WriteLine(ex.Message);
            }
        }
        private void MenuItemDeleteAlbum_Click(object sender, EventArgs e)
        {
            DeleteSelectedAlbum();
        }
        private void MenuItemReload_Click(object sender, EventArgs e) // delete selected album data and reload
        {
            if (albumListView.SelectedItems.Count == 0)
                return;
            ReloadAlbum(albumListView.SelectedItems[0], false);
        }
        private void ReloadAlbum(ListViewItem lvItem, bool bSilent = false)
        {
            //Cursor.Current = Cursors.WaitCursor;
            int iIndex = lvItem.Index;
           
            string sAlbumPath = lvItem.Tag.ToString().Split('|').ElementAt(1);
            Image img = FileUtil.GetBestImage(sAlbumPath); // New Image
            if (img == null)
            {
                Cursor.Current = Cursors.Default;
                if (!bSilent)
                    MessageBox.Show("Keine Bilddatei verfügbar.", "Keine Bilddatei", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            byte[] imageData = Gfx.ImageToByte(img);
            Int32 iAlbumID = Convert.ToInt32(lvItem.Text);
            albumDB.dbReloadAlbum(iAlbumID, imageData, sAlbumPath); //
            // Overwrite 
            ImageList coverImageList = albumListView.LargeImageList;
            if (IsSelectedAlbumPlaying()) // 
            {
                coverImageList.Images[0] = Gfx.DrawCoverImage(coverImageList.Images[lvItem.ImageIndex], img);
                coverImageList.Images[lvItem.ImageIndex] = Gfx.DrawCoverNowPlaying(img);
            }
            else
            {
                coverImageList.Images[lvItem.ImageIndex] = Gfx.DrawCoverImage(coverImageList.Images[lvItem.ImageIndex], img);
            }
            albumListView.Refresh();
            //Cursor.Current = Cursors.Default;
        }
        private void MenuItemAddFolder_Click(object sender, EventArgs e)
        {
            string sInitialDir = albumDB.GetLastAddFolderDir();
            if (sInitialDir == null)
                sInitialDir = FileUtil.GetPrevDir(GetSelectedPath());
            if (sInitialDir == null)
                sInitialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            FolderSelectDialog fbd = new FolderSelectDialog();
            fbd.Title = "Wählen Sie einen Ordner aus der Musikdateien enthält:";
            if (Directory.Exists(sInitialDir))
                fbd.InitialDirectory = sInitialDir;
            else
            {
                sInitialDir = GetLastDir();
                if (sInitialDir == null)
                    fbd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                else
                    fbd.InitialDirectory = sInitialDir;
            }
            if (fbd.ShowDialog(IntPtr.Zero))
            {
                AddAlbumPath(fbd.FileName, false);
                albumDB.SetLastAddFolderDir(FileUtil.GetPrevDir(fbd.FileName));
            }
        }
        private string GetLastDir()
        {
            string sPath = GetSelectedPath();
            if (Directory.Exists(sPath))
                return sPath;
            sPath = albumDB.GetLastSelectedDir(); //
            if (sPath == null)
                sPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            return sPath;
        }
        private string GetSelectedPath()
        {
            if (albumListView.SelectedItems.Count == 0)
                return null;
            ListViewItem lvItem = albumListView.SelectedItems[0];
            string sPath = albumListView.SelectedItems[0].Tag.ToString().Substring(2);
            return sPath;
        }
        private void AlbumDB_AlbumAdded(AlbumDB sender, AlbumDB.AlbumDBEventArgs e) // Aufruf der Methode aus dem Worker-Thread
        {
            try
            {
                this.Invoke((Action<Int32, string, string, Image, Boolean, int>)AddListViewAlbum, e.AlbumID, e.AlbumPath, e.AlbumTitle, e.AlbumImage, e.IsParentFolder, e.MediaTypeID);
            }
            catch
            {
                // Some error handling
            }
        }
        public void AddListViewAlbum(Int32 iAlbumID, string sPath, string sTitle, Image img, Boolean bIsParentFolder, int iMediaTypeID)
        {
            if (iAlbumID == 0 || string.IsNullOrEmpty(sPath) || img == null)
                return;
            string sAlbumID = iAlbumID.ToString();
            if (img.Height > 500 || img.Width > 500)
                img = Gfx.ImageResize(img, 500, 500);
            coverImageList.Images.Add(sAlbumID, img);
            dynamic iImageIndex = coverImageList.Images.IndexOfKey(sAlbumID);
            ListViewItem lvItem = new ListViewItem(sAlbumID);
            lvItem.ImageIndex = iImageIndex;
            lvItem.Tag = iMediaTypeID + "|" + sPath;
            //item.Group = albumListView.Groups[iGroup];
            lvItem.ToolTipText = sTitle;
            albumListView.Items.Add(lvItem);
            albumListView.EnsureVisible(lvItem.Index);
            UpdateStartHelp();
            rezizeForm();
            Application.DoEvents();
        }
        private void OnPauseTimerEvent(object sender, ElapsedEventArgs e) // Aufruf der Methode aus dem Worker-Thread
        {
            try
            {
                this.Invoke(new Action(() => BlinkPauseIndicator()));
            }
            catch
            {
                // Some error handling
            }
        }
        private void OnHelpTimerEvent(object sender, ElapsedEventArgs e) // Aufruf der Methode aus dem Worker-Thread
        {
            try
            {
                this.Invoke(new Action(() => UpdateStartHelp()));
            }
            catch
            {
                // Some error handling
            }
        }
        private void MenuItemWatchDir_Click(object sender, EventArgs e)
        {
            string sAlbumPath = GetSelectedPath();
            sAlbumPath = FileUtil.GetPrevDir(Path.GetDirectoryName(sAlbumPath));
            FolderSelectDialog fbd = new FolderSelectDialog();
            fbd.Title = "Wählen Sie den Ordner aus den Sie automatisch nach neuen Alben überwachen wollen:";
            if (Directory.Exists(sAlbumPath))
                fbd.InitialDirectory = sAlbumPath;
            else
                fbd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            // Todo: load last path from db
            if (!fbd.ShowDialog(IntPtr.Zero))
                return;
            if (MessageBox.Show("Möchten Sie wirklich den Ordner automatisch nach neuen Alben automatisch überwachen lassen?\n\nOrdner: " + fbd.FileName, "Ordner überwachen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;
            // Todo: count number of audio dirs and warn user if > 10
            albumDB.ScanPath(fbd.FileName); // check AutoDiscoveryPath 
        }
        private void OnFadeTimerEvent(object sender, ElapsedEventArgs e) // Aufruf der Methode aus dem Worker-Thread
        {
            this.Invoke(new Action(() => FadeOutLabel()));
        }
        private void FadeOutLabel()
        {
            if (labelAlbumTitle.ForeColor.GetBrightness() <= 0.01)
            {
                FadeTimer.Enabled = false;
                bHelpActive = false;
                if (aPlayer.IsPlaying)
                    SetCaptionLabel(aPlayer.CurrentTrackPlaying);
                else
                    labelAlbumTitle.Text = "Ready";
                labelAlbumTitle.ForeColor = SystemColors.MenuHighlight;
                return;
            }
            HslColor hsl = new HslColor(labelAlbumTitle.ForeColor);
            hsl.L -= 0.002; // Brightness is here lightness
            labelAlbumTitle.ForeColor = (System.Drawing.Color)hsl.ToRgbColor();
        }
        protected override void WndProc(ref Message m)
        {
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();

            /* Handle some standard media player keys. To test this, use: (for example)
             * HandleRef handle = new HandleRef(this, this.Handle);
             * NativeMethods.SendMessage(handle, NativeConstants.WM_APPCOMMAND, this.Handle, new IntPtr((int)ApplicationCommand.MediaPause)); */
            if (HandledAppCommand(ref m))
            {
                base.WndProc(ref m);
                return;
            }
            base.WndProc(ref m);
        }
        private bool HandledAppCommand(ref Message m)
        {
            if (m.Msg == NativeConstants.WM_APPCOMMAND)
            {
                switch ((ApplicationCommand)m.LParam.ToInt32())
                {
                    case ApplicationCommand.MediaFastForward:
                        break;
                    case ApplicationCommand.MediaPause:
                        goto default;
                    case ApplicationCommand.MediaPlay:
                        goto default;
                    case ApplicationCommand.MediaPlayPause:
                        if (PauseAudio())
                            goto default;
                        break;
                    case ApplicationCommand.MediaNexttrack:
                        goto default;
                    case ApplicationCommand.MediaPrevioustrack:
                        goto default;
                    case ApplicationCommand.MediaStop:
                        break;
                    case ApplicationCommand.VolumeDown:
                        break;
                    case ApplicationCommand.VolumeUp:
                        break;
                    case ApplicationCommand.VolumeMute:
                        break;
                    case ApplicationCommand.Close:
                        goto default;
                    default:
                        m.Result = new IntPtr(1);
                        return true;
                }
            }
            return false;
        }
        private bool StartWatcher(string sWatchDir)
        {
            if (FileUtil.DirectoryExists(sWatchDir) == AccessStatus.NotExisting)
                return false;
            WatchHandler mymethod = new WatchHandler(OnWatcherChange); // Todo: array of WatchHandler
            Watcher w = new Watcher(sWatchDir, "*.*", mymethod);
            w.StartWatch();
            return true;
        }
        private void OnWatcherChange(object sender, System.IO.FileSystemEventArgs e)
        {
            this.Invoke((Func<string, bool, bool>)AddAlbumPath, e.FullPath, true); // add Album
        }
        private void RepaintPlaySatus()
        {
            if (albumListView.SelectedItems.Count == 0)
                return;
            if (aPlayer.IsPaused)
                return;
            if (aPlayer.IsPlaying)
            {
                using (Graphics g = this.albumListView.CreateGraphics())
                {
                    if (pNowPlaying.X > 0 || pNowPlaying.Y > 0)
                        g.DrawLine(pPauseBlinkerOn, pNowPlaying.X, pNowPlaying.Y, pNowPlaying.X + 194, pNowPlaying.Y);
                }
                return;
            }
        }
        private void VisualAudioPlayer_Shown(object sender, EventArgs e)
        {
            albumListView.Refresh();
            Application.DoEvents();
            albumListView.Items.AddRange(albumDB.dbLoadImages(coverImageList));
            if (albumListView.Items.Count > 0)
            {
                this.labelAlbumTitle.Text = "Welcome to VisualAudioPlayer!";
                LastPlayed lp = albumDB.dbGetLastPlayed();
                //int iAlbumIndex = -1;
                string sAlbumID = lp.AlbumID.ToString();
                //ListViewItem lvItem[] = albumListView.Items.Find(sAlbumID, false);
                ListViewItem lvItem = albumListView.FindItemWithText(sAlbumID, false, 0);
                //foreach (ListViewItem it in albumListView.Items)
                //{
                //    if (it != null)
                //        if (it.Text == lp.AlbumID.ToString())
                //            iAlbumIndex = it.Index;
                //}
                //if (iAlbumIndex == -1)
                //    return;
                if (lvItem != null)
                {
                    albumListView.Items[lvItem.Index].Selected = true;
                    PlaySelectedAlbum(lp.AlbumTrackID);
                }
            }
            else
            {
                bHelpActive = true;
                UpdateStartHelp();
            }
            if (albumListView.Items.Count == 0)
                bHelpActive = true;
            albumDB.DiscoverNewAlbums();
            //albumListView.Refresh();
        }
        private void albumListView_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
            {
                // call RepaintPlaySatus() after 1 sec.
                System.Threading.Timer timer = new System.Threading.Timer(obj => { this.Invoke(new Action(() => RepaintPlaySatus())); }, null, 1000, System.Threading.Timeout.Infinite);
            }
        }
    }
}
