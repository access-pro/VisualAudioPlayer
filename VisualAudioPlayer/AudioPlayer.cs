using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Un4seen.Bass; // http://www.un4seen.com/
using Un4seen.Bass.AddOn.Flac; // http://bass.radio42.com/help/
using Un4seen.Bass.AddOn.Ape;
using Un4seen.Bass.AddOn.Wma;
using Un4seen.Bass.AddOn.Tags;
using Alphaleonis.Win32.Filesystem;
using System.Windows.Forms;
using System.Collections;
using System.Net;
using System.Runtime.InteropServices;
using System.Timers;

namespace VisualAudioPlayer
{
    class AudioPlayer
    {
        #region Declarations
        private bool disposed;
        private int _Stream = 0;
        private SYNCPROC _mySync;
        private DOWNLOADPROC myStreamCreateURL;
        private TAG_INFO _tagInfo;
        private SYNCPROC mySync;
        private int _wmaPlugIn = 0;
        public bool Initialized = false;
        public bool RadioStreamActive = false;
        private static System.Timers.Timer CueTimer;
        public delegate void EndOfAlbumHandler(AudioPlayer sender, EventArgs e);
        public event EndOfAlbumHandler OnAlbumFinishedPlaying;
        public delegate void TrackStartPlayingHandler(AudioPlayer sender, EventArgs e);
        public event TrackStartPlayingHandler OnSongStartPlaying;
        public delegate void EndOfTrackHandler(AudioPlayer sender, EventArgs e);
        public event EndOfTrackHandler OnTrackFinishedPlaying;
        public delegate void AudioFileNotFoundHandler(AudioPlayer sender, AudioPlayerEventArgs e);
        public event AudioFileNotFoundHandler OnAudioFileNotFound;
        public delegate void AudioDirNotFoundHandler(AudioPlayer sender, AudioPlayerEventArgs e);
        public event AudioDirNotFoundHandler OnAudioDirNotFound;
        public delegate void RadioChannelFailedHandler(AudioPlayer sender, EventArgs e);
        public event RadioChannelFailedHandler OnRadioChannelFailed;
        public class AudioPlayerEventArgs : EventArgs
        {
            public Album Album { get; private set; }
            public AudioPlayerEventArgs(Album aAlbum)
            {
                Album = aAlbum;
            }
        }

        public Album CurrentAlbumPlaying { get; set; }
        public AlbumTrack CurrentTrackPlaying { get; set; }
        public bool IsPlaying { get; set; }
        public bool IsPaused { get; set; }
        public string CurrentTrackTitlePlaying { get; set; }
        public string CurrentRadioChannelPlaying { get; set; }
        private Queue<AlbumTrack> TrackQueue;
        public Queue<AlbumTrack> CurrentTrackList { get; set; }
        #endregion
        public AudioPlayer(IntPtr FromHandle)
        {
            BassNet.Registration("h.wrage@access-pro.de", "2X313823213223");
            // check the version..
            if (Utils.HighWord(Bass.BASS_GetVersion()) != Bass.BASSVERSION)
            {
                MessageBox.Show(new Form() { TopMost = true }, "Wrong Bass Version!");
            }
            try
            {
                if (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, FromHandle)) // init BASS using the default output device 
                {
                    _wmaPlugIn = Bass.BASS_PluginLoad("basswma.dll");
                    // 3) ALTERNATIVLY you might call any 'dummy' method to load the lib!
                    //int[] cbrs = BassWma.BASS_WMA_EncodeGetRates(44100, 2, BASSWMAEncode.BASS_WMA_ENCODE_RATES_CBR);
                    // now basswma.dll is loaded and the additional config options are available...

                    if (Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_WMA_PREBUF, 0) == false)
                    {
                        Console.WriteLine("ERROR: " + Enum.GetName(typeof(BASSError), Bass.BASS_ErrorGetCode()));
                    }
                    // we alraedy create the user callback methods...
                    myStreamCreateURL = new DOWNLOADPROC(MyDownloadProc);
                }
                else
                {
                    MessageBox.Show(new Form() { TopMost = true }, "Audioausgabe nicht möglich.", "Kein Ausgabegerät", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Treiberdatei fehlt: Bass.Net.dll", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            IsPlaying = false;
            CurrentRadioChannelPlaying = null;
            TrackQueue = new Queue<AlbumTrack>();
            CurrentTrackList = new Queue<AlbumTrack>();
            Initialized = true;
            _mySync = new SYNCPROC(EndSync);
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_NET_PLAYLIST, 1);
            CueTimer = new System.Timers.Timer(); // Create a timer.
            CueTimer.Elapsed += new ElapsedEventHandler(OnCueTimer);  // Hook up the Elapsed event for the cue.
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
                    Bass.BASS_PluginFree(_wmaPlugIn);
                    Bass.BASS_Stop();   // close bass
                    Bass.BASS_Free();
                    Bass.BASS_PluginFree(_wmaPlugIn);
                }
                disposed = true;
            }
        }
        public void ResetTrackQueue()
        {
            if (TrackQueue.Count() <= 1)
                return;
            TrackQueue.Clear();
            IsPlaying = false;
        }
        public void FreeStream()
        {
            Bass.BASS_StreamFree(_Stream);  // free the stream
        }
        public void Next()
        {
            Bass.BASS_StreamFree(_Stream);  // free the stream
            CueTimer.Stop();
            if (TrackQueue.Count() <= 1) // raise event EndOfAlbum
            {
                if (TrackQueue.Count() == 1) // clean up
                    TrackQueue.Dequeue();
                if (OnAlbumFinishedPlaying != null)
                    OnAlbumFinishedPlaying(this, new EventArgs());
                return;
            }
            TrackQueue.Dequeue();        // delete prev song
            Play(TrackQueue.Peek());
        }
        public void Pause()
        {
            if (_Stream == 0)
            {
                this.IsPaused = false;
                return;
            }
            switch (Bass.BASS_ChannelIsActive(_Stream))
            {
                case BASSActive.BASS_ACTIVE_PAUSED:
                    Bass.BASS_ChannelPlay(_Stream, false);  // continue play the stream channel
                    break;
                case BASSActive.BASS_ACTIVE_PLAYING:
                    Bass.BASS_ChannelPause(_Stream);
                    break;
                case BASSActive.BASS_ACTIVE_STALLED:
                    if (this.IsPaused)
                        Bass.BASS_ChannelPlay(_Stream, false);  // continue play the stream channel
                    else
                        Bass.BASS_ChannelPlay(_Stream, false);  // continue play the stream channel
                    break;
                case BASSActive.BASS_ACTIVE_STOPPED:
                    Bass.BASS_ChannelPlay(_Stream, true);
                    break;
            }
            this.IsPaused = !this.IsPaused;
        }
        public bool PlayAudioQueue(Album album, Queue<AlbumTrack> queue, Int32 iStartTrack)
        {
            if (queue.Count() == 0)
                return false;
            CurrentTrackList = new Queue<AlbumTrack>(queue);

            if (AlbumPlayedToday(album) && queue.First().AlbumTrackID != album.LastTrackPlayed) // skip first tracks if played today already
            {
                do
                {
                    queue.Dequeue();
                    if (queue.Count() == 0)
                    {
                        queue = new Queue<AlbumTrack>(CurrentTrackList);
                        break;
                    }
                } while (queue.First().AlbumTrackID != album.LastTrackPlayed);
            }
            if (iStartTrack > 0) // skip to StartTrack
            {
                while (queue.First().AlbumTrackID != iStartTrack)
                {
                    queue.Dequeue();
                    if (queue.Count() == 0)
                    {
                        queue = new Queue<AlbumTrack>(CurrentTrackList);
                        break;
                    }
                }
            }
            TrackQueue = new Queue<AlbumTrack>(queue);
            CurrentAlbumPlaying = album;
            AlbumTrack t = TrackQueue.Peek();
            Bass.BASS_StreamFree(_Stream);  // free the stream
            _Stream = 0;
            return Play(t);
        }
        public bool PlayRadioChannel(string url)
        {
            string url2 = null;
            CueTimer.Stop();
            if (string.IsNullOrEmpty(url))
                return false;
            if (Path.GetExtension(url) == ".m3u")
                url = StringIO.ReadFile(url);
            if (Path.GetExtension(url) == ".pls")
                url2 = StringIO.GetPlsUrl(url);
            if (string.IsNullOrEmpty(url2))
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "Die .pls Datei ist fehlerhaft oder enthält keine Webadresse.\n\nDateiname: " + url,
                    ".pls Datei fehlerhaft", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else
                url = url2;
            Cursor.Current = Cursors.WaitCursor;
            CurrentAlbumPlaying.Reset();
            Bass.BASS_StreamFree(_Stream);

            bool isWMA = false;
            if (url != String.Empty)
            {
                _Stream = Bass.BASS_StreamCreateURL(url, 0, BASSFlag.BASS_STREAM_STATUS, myStreamCreateURL, IntPtr.Zero); // create the stream
                if (_Stream == 0)
                {
                    _Stream = BassWma.BASS_WMA_StreamCreateFile(url, 0, 0, BASSFlag.BASS_DEFAULT); // try WMA streams...
                    if (_Stream == 0)
                    {
                        BASSError err = Bass.BASS_ErrorGetCode();
                        switch (err)
                        {
                            case BASSError.BASS_ERROR_FILEFORM:
                                MessageBox.Show(new Form() { TopMost = true },
                                    "Über die angegebene Internetadresse können Sie keine Musik empfangen.\n\nLöschen Sie das Cover und probieren Sie eine andere Internetadresse.\n\nInternetadresse: " + url,
                                    "Dateiformat unbekannt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            default:
                                MessageBox.Show(new Form() { TopMost = true },
                                    "Über die angegebene Internetadresse konnte keine Musik empfangen werden.\n\nInternetadresse: " + url,
                                    "Error: " + GetErrMsg(err), MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                        }
                        //string err= BASS_GetErrorDescription(Bass.BASS_ErrorGetCode());
                        Cursor.Current = Cursors.Default;
                        return false;
                    }
                    isWMA = true;
                }
                _tagInfo = new TAG_INFO(url);
                BASS_CHANNELINFO info = Bass.BASS_ChannelGetInfo(_Stream);
                if (info.ctype == BASSChannelType.BASS_CTYPE_STREAM_WMA)
                    isWMA = true;
                // ok, do some pre-buffering...
                int cnt = 0;
                if (!isWMA) // display buffering for MP3, OGG...
                {
                    while (true)
                    {
                        long len = Bass.BASS_StreamGetFilePosition(_Stream, BASSStreamFilePosition.BASS_FILEPOS_END);
                        if (len == -1)
                            break; // typical for WMA streams
                        // percentage of buffer filled
                        float progress = (
                            Bass.BASS_StreamGetFilePosition(_Stream, BASSStreamFilePosition.BASS_FILEPOS_DOWNLOAD) -
                            Bass.BASS_StreamGetFilePosition(_Stream, BASSStreamFilePosition.BASS_FILEPOS_CURRENT)
                            ) * 100f / len;
                        if (progress == 0f && cnt > 10)
                        {
                            //this.statusBar1.Text = "ERROR: Not receiving music on this channel";
                            if (OnRadioChannelFailed != null)  // raise event: Not receiving music on this channel
                                OnRadioChannelFailed(this, new EventArgs());
                            Cursor.Current = Cursors.Default;
                            return false;
                        }
                        if (progress > 75f)
                        {
                            break; // over 75% full, enough
                        }
                        cnt++;
                        //this.statusBar1.Text = String.Format("Buffering... {0}%", progress);
                    }
                }
                else // display buffering for WMA...
                {
                    while (true)
                    {
                        long len = Bass.BASS_StreamGetFilePosition(_Stream, BASSStreamFilePosition.BASS_FILEPOS_WMA_BUFFER);
                        if (len == -1L)
                            break;
                        // percentage of buffer filled
                        if (len == 0L && cnt > 10)
                        {
                            //this.statusBar1.Text = "ERROR: Not receiving music on this channel";
                            if (OnRadioChannelFailed != null)  // raise event: Not receiving music on this channel
                                OnRadioChannelFailed(this, new EventArgs());
                            Cursor.Current = Cursors.Default;
                            return false;
                        }
                        if (len > 75L)
                        {
                            break; // over 75% full, enough
                        }
                        cnt++;
                        //this.statusBar1.Text = String.Format("Buffering... {0}%", len);
                    }
                }
                // get the meta tags (manually - will not work for WMA streams here)
                //string[] icy = Bass.BASS_ChannelGetTagsICY(_Stream);
                //if (icy == null)
                //{
                //    // try http...
                //    icy = Bass.BASS_ChannelGetTagsHTTP(_Stream);
                //}
                //if (icy != null)
                //{
                //    foreach (string tag in icy)
                //    {
                //        //this.textBox1.Text += "ICY: " + tag + Environment.NewLine;
                //    }
                //}
                // get the initial meta data (streamed title...)
                //icy = Bass.BASS_ChannelGetTagsMETA(_Stream);
                //if (icy != null)
                //{
                //    foreach (string tag in icy)
                //    {
                //        //this.textBox1.Text += "Meta: " + tag + Environment.NewLine;
                //    }
                //}
                //else
                //{
                //    // an ogg stream meta can be obtained here
                //    icy = Bass.BASS_ChannelGetTagsOGG(_Stream);
                //    if (icy != null)
                //    {
                //        foreach (string tag in icy)
                //        {
                //            //this.textBox1.Text += "Meta: " + tag + Environment.NewLine;
                //        }
                //    }
                //}
                // alternatively to the above, you might use the TAG_INFO (see BassTags add-on)
                // This will also work for WMA streams here ;-)
                if (BassTags.BASS_TAG_GetFromURL(_Stream, _tagInfo))
                {
                    // and display what we get
                    //this.textBoxAlbum.Text = _tagInfo.album;
                    //this.textBoxArtist.Text = _tagInfo.artist;
                    //this.textBoxTitle.Text = _tagInfo.title;
                    //this.textBoxComment.Text = _tagInfo.comment;
                    //this.textBoxGenre.Text = _tagInfo.genre;
                    //this.textBoxYear.Text = _tagInfo.year;

                    CurrentTrackTitlePlaying = FormatTitle(_tagInfo);
                    if (OnSongStartPlaying != null) // raise event: Play
                        OnSongStartPlaying(this, new EventArgs());
                }
                mySync = new SYNCPROC(MetaSync); // set a sync to get the title updates out of the meta data...
                Bass.BASS_ChannelSetSync(_Stream, BASSSync.BASS_SYNC_META, 0, mySync, IntPtr.Zero);
                Bass.BASS_ChannelSetSync(_Stream, BASSSync.BASS_SYNC_WMA_CHANGE, 0, mySync, IntPtr.Zero);

                //int rechandle = 0; // start recording...
                //if (Bass.BASS_RecordInit(-1))
                //{
                //    _byteswritten = 0;
                //    myRecProc = new RECORDPROC(MyRecoring);
                //    rechandle = Bass.BASS_RecordStart(44100, 2, BASSFlag.BASS_RECORD_PAUSE, myRecProc, IntPtr.Zero);
                //}
                //this.statusBar1.Text = "Playling...";
                // play the stream
                Bass.BASS_ChannelPlay(_Stream, false);
                // record the stream
                //Bass.BASS_ChannelPlay(rechandle, false);
                IsPlaying = true;
                CurrentRadioChannelPlaying = url;
                Cursor.Current = Cursors.Default;
            }
            return true;
        }
        public bool AlbumPlayedToday(Album album)
        {
            if (string.IsNullOrEmpty(album.LastDatePlayed))
                return false;
            if (album.LastDatePlayed.StartsWith(DateTime.Now.ToString("dd.MM.yyyy")))
                return true;
            else
                return false;
        }
        public void Stop()
        {
            if (_Stream == 0)
                return;
            Bass.BASS_ChannelStop(_Stream);
            Bass.BASS_StreamFree(_Stream);  // free the stream
            CurrentRadioChannelPlaying = null;
        }
        public void DisposeSound()
        {
            IsPlaying = false;

            if (_Stream == 0)
                return;
            //iSound.setSoundStopEventReceiver(null);
            Bass.BASS_MusicFree(_Stream);
            _Stream = 0;
            CurrentAlbumPlaying.Reset();
            CurrentTrackPlaying.Reset();
        }
        private bool Play(AlbumTrack aTrack)
        {
            IsPlaying = false;
            CurrentRadioChannelPlaying = null;
            if (string.IsNullOrEmpty(aTrack.FileName))
                return false;
            string sTrackFileName = aTrack.FileName;
            if (aTrack.FileName.Length > 260 || aTrack.FileName.StartsWith("\\\\"))
                sTrackFileName = @"\\?\unc" + aTrack.FileName.Substring(1);
            else
                sTrackFileName = aTrack.FileName;
            string sExtension = Path.GetExtension(sTrackFileName).ToUpper();
            if (_Stream == 0 || !CurrentTrackPlaying.FileName.Equals(aTrack.FileName))
            {
                try
                {
                    Bass.BASS_StreamFree(_Stream);  // free the stream
                    switch (sExtension)
                    {
                        case ".FLAC":
                            _Stream = BassFlac.BASS_FLAC_StreamCreateFile(sTrackFileName, 0L, 0L, BASSFlag.BASS_DEFAULT); // create a stream channel from a file 
                            break;
                        case ".APE":
                            _Stream = BassApe.BASS_APE_StreamCreateFile(sTrackFileName, 0L, 0L, BASSFlag.BASS_DEFAULT); // create a stream channel from a file 
                            break;
                        case ".WMA":
                            _Stream = BassWma.BASS_WMA_StreamCreateFile(sTrackFileName, 0L, 0L, BASSFlag.BASS_DEFAULT); // create a stream channel from a file 
                            break;
                        case ".WAV":
                        case ".AIFF":
                        case ".MP3":
                        case ".MP2":
                        case ".MP1":
                        case ".OGG":
                            goto default;
                        default:
                            _Stream = Bass.BASS_StreamCreateFile(sTrackFileName, 0L, 0L, BASSFlag.BASS_DEFAULT); // create a stream channel from a file 
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    MessageBox.Show(new Form() { TopMost = true }, ex.Message + "\n\nDateiname: " + sTrackFileName, "Audiodatei fehlerhaft", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }
            } 
            if (aTrack.Position > 0 || aTrack.Frames > 0)
            {
                long lByteCount1 = Bass.BASS_ChannelSeconds2Bytes(_Stream, aTrack.Position);
                long lByteCount2 = Bass.BASS_ChannelSeconds2Bytes(_Stream, aTrack.Position + 1);
                long lDiffBytes = lByteCount2 - lByteCount1;
                long lBytePos = lByteCount1 + ((lDiffBytes / 75) * aTrack.Frames);
                if (!Bass.BASS_ChannelSetPosition(_Stream, lBytePos))  // skipp forward
                {
                    MessageBox.Show(new Form() { TopMost = true }, "Die Position des Titels konnte für die aktuelle Musikdatei nicht gesetzt werden.",
                        "ChannelSetPosition", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            if (TrackQueue.First().Position > 0 || TrackQueue.First().Frames > 0 && aTrack.FileName.Equals(TrackQueue.First().FileName))
            {
                long lSeconds = TrackQueue.ElementAt(1).Position - aTrack.Position;
                CueTimer.Interval = lSeconds * 1000;
                CueTimer.Start();
            }
            if (_Stream != 0)
            {
                Bass.BASS_ChannelPlay(_Stream, false);  // play the stream channel
                CurrentTrackPlaying = aTrack;
            }
            else
            {
                string strTrackPath = aTrack.FileName.Substring(0, aTrack.FileName.LastIndexOf("\\"));
                if (sTrackFileName.StartsWith(@"\\"))
                {
                    string sServerName = FileUtil.GetServerName(strTrackPath);
                    if (!FileUtil.IsServerPingable(sServerName))
                    {
                        MessageBox.Show(new Form() { TopMost = true }, "Der Server '" + sServerName + "' ist zur Zeit Offline.\nWiedergabe nicht möglich.", 
                            "Server down", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return false;
                    }
                }
                if (!Directory.Exists(strTrackPath)) // we should be connected
                {
                    if (OnAudioDirNotFound != null)  // Fire event to rescan album
                        OnAudioDirNotFound(this, new AudioPlayerEventArgs(CurrentAlbumPlaying));  // raise event: AudioFileNot
                    return false;
                }
                if (!File.Exists(aTrack.FileName))
                {
                    //MessageBox.Show(new Form() { TopMost = true }, "Die folgende Datei wurde entweder umbenannt oder existiert nicht mehr:\n\n" + aTrack.FileName + 
                    //       "\n\nDer Ordner des Albums wird jetzt neu eingelesen.", "No such file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    if (OnAudioFileNotFound != null)  // Fire event to rescan album
                        OnAudioFileNotFound(this, new AudioPlayerEventArgs(CurrentAlbumPlaying));  // raise event: AudioFileNot
                }
                // TODO: Check read permission
                return false;
            }
            CurrentTrackTitlePlaying = aTrack.Title;
            if (OnSongStartPlaying != null) // raise event: Play
                OnSongStartPlaying(this, new EventArgs());
            Bass.BASS_ChannelSetSync(_Stream, BASSSync.BASS_SYNC_END, 0, _mySync, IntPtr.Zero);
            IsPlaying = true;
            // xxx raise event: AutoPlayNextTrack
            return true;
        }
        private void OnCueTimer(object source, ElapsedEventArgs e)
        {
            if (TrackQueue.Count() <= 1) // 
                return;
            TrackQueue.Dequeue();        // delete prev song
            CurrentTrackPlaying = TrackQueue.Peek();
            CurrentTrackTitlePlaying = CurrentTrackPlaying.Title;
            if (OnSongStartPlaying != null) // raise event: Play
                OnSongStartPlaying(this, new EventArgs());
            CueTimer.Stop();
            long lSeconds = TrackQueue.ElementAt(0).Position - TrackQueue.ElementAt(0).Position;
            if (lSeconds > 0)
            {
                CueTimer.Interval = lSeconds * 1000;
                CueTimer.Start();
            }
        }
        private void EndSync(int handle, int channel, int data, IntPtr user)
        {
            _Stream = 0;
            if (OnTrackFinishedPlaying != null)  // raise event: PlayNextTrack
                OnTrackFinishedPlaying(this, new EventArgs());
            Next();
        }
        private void MyDownloadProc(IntPtr buffer, int length, IntPtr user)
        {
            if (buffer != IntPtr.Zero && length == 0)
            {
                // the buffer contains HTTP or ICY tags.
                string txt = Marshal.PtrToStringAnsi(buffer);
                //this.textBox1.Text += "Tags: " + txt + Environment.NewLine;
                // you might instead also use "this.BeginInvoke(...)", which would call the delegate asynchron!
            }
        }
        private void MetaSync(int handle, int channel, int data, IntPtr user)
        {
            // BASS_SYNC_META is triggered on meta changes of SHOUTcast streams
            if (_tagInfo.UpdateFromMETA(Bass.BASS_ChannelGetTags(channel, BASSTag.BASS_TAG_META), false, true))
            {
                CurrentTrackTitlePlaying = FormatTitle(_tagInfo);
                if (OnSongStartPlaying != null) // raise event: Play
                    OnSongStartPlaying(this, new EventArgs());
            }
        }
        private string FormatTitle(TAG_INFO ti)
        {
            string sTitle = ti.artist;
            string sTitleLatin1;
            string sTitleUTF8;
            if (ti.title.Length > 0)
            {
                if (sTitle.Length > 0)
                    sTitle += " - ";
                sTitle += ti.title;
            }
            if (ti.year.Length > 0)
                sTitle += " (" + ti.year + ")";

            byte[] bytes = Encoding.Default.GetBytes(sTitle);
            sTitleUTF8 = Encoding.UTF8.GetString(bytes);

            Encoding iso = Encoding.GetEncoding("ISO-8859-1");
            sTitleLatin1 = iso.GetString(bytes);

            if (sTitleUTF8.Contains("�"))
                sTitle = sTitleLatin1;
            else
                sTitle = sTitleUTF8;
            sTitle.Replace("\\", "");
            return sTitle;
        }
        private int _byteswritten = 0;
        private byte[] _recbuffer = new byte[1048510]; // 1MB buffer
        private bool MyRecoring(int handle, IntPtr buffer, int length, IntPtr user)
        {
            // just a dummy here...nothing is really written to disk...
            if (length > 0 && buffer != IntPtr.Zero)
            {
                // copy from managed to unmanaged memory
                // it is clever to NOT alloc the byte[] everytime here, since ALL callbacks should be really fast!
                // and if you would do a 'new byte[]' every time here...the GarbageCollector would never really clean up that memory here
                // even other sideeffects might occure, due to the fact, that BASS micht call this callback too fast and too often...
                Marshal.Copy(buffer, _recbuffer, 0, length);
                // write to file
                // NOT implemented her...;-)
                _byteswritten += length;
                Console.WriteLine("Bytes written = {0}", _byteswritten);
                if (_byteswritten < 800000)
                    return true; // continue recording
                else
                    return false;
            }
            return true;
        }
        private string GetErrMsg(BASSError err)
        {
            switch (err)
            {
                case BASSError.BASS_OK:
	                return "All is OK";
                case BASSError.BASS_ERROR_MEM:
	                return "Memory error";
                case BASSError.BASS_ERROR_FILEOPEN:
	                return "Can't open the file";
                case BASSError.BASS_ERROR_DRIVER:
	                return "can't find a free/valid driver";
                case BASSError.BASS_ERROR_BUFLOST:
	                return "the sample buffer was lost";
                case BASSError.BASS_ERROR_HANDLE:
	                return "invalid handle";
                case BASSError.BASS_ERROR_FORMAT:
	                return "unsupported sample format";
                case BASSError.BASS_ERROR_POSITION:
	                return "invalid position";
                case BASSError.BASS_ERROR_INIT:
	                return "BASS_Init has not been successfully called";
                case BASSError.BASS_ERROR_START:
	                return "BASS_Start has not been successfully called";
                case BASSError.BASS_ERROR_ALREADY:
	                return "already initialized/paused/whatever";
                case BASSError.BASS_ERROR_NOCHAN:
	                return "can't get a free channel";
                case BASSError.BASS_ERROR_ILLTYPE:
	                return "an illegal type was specified";
                case BASSError.BASS_ERROR_ILLPARAM:
	                return "an illegal parameter was specified";
                case BASSError.BASS_ERROR_NO3D:
	                return "no 3D support";
                case BASSError.BASS_ERROR_NOEAX:
	                return "no EAX support";
                case BASSError.BASS_ERROR_DEVICE:
	                return "illegal device number";
                case BASSError.BASS_ERROR_NOPLAY:
	                return "not playing";
                case BASSError.BASS_ERROR_FREQ:
	                return "illegal sample rate";
                case BASSError.BASS_ERROR_NOTFILE:
	                return "the stream is not a file stream";
                case BASSError.BASS_ERROR_NOHW:
	                return "no hardware voices available";
                case BASSError.BASS_ERROR_EMPTY:
	                return "the MOD music has no sequence data";
                case BASSError.BASS_ERROR_NONET:
	                return "no internet connection could be opened";
                case BASSError.BASS_ERROR_CREATE:
	                return "couldn't create the file";
                case BASSError.BASS_ERROR_NOFX:
	                return "effects are not available";
                case BASSError.BASS_ERROR_NOTAVAIL:
	                return "requested data is not available";
                case BASSError.BASS_ERROR_DECODE:
	                return "the channel is/isn't a 'decoding channel'";
                case BASSError.BASS_ERROR_DX:
	                return "a sufficient DirectX version is not installed";
                case BASSError.BASS_ERROR_TIMEOUT:
	                return "connection timedout";
                case BASSError.BASS_ERROR_FILEFORM:
	                return "unsupported file format";
                case BASSError.BASS_ERROR_SPEAKER:
	                return "unavailable speaker";
                case BASSError.BASS_ERROR_VERSION:
	                return "invalid BASS version (used by add-ons)";
                case BASSError.BASS_ERROR_CODEC:
	                return "codec is not available/supported";
                case BASSError.BASS_ERROR_ENDED:
	                return "the channel/file has ended";
                case BASSError.BASS_ERROR_BUSY:
	                return "the device is busy";
                case BASSError.BASS_ERROR_UNKNOWN:
                    goto default;
                default:
                    return "some other mystery problem";
            }
        }
    }
}
