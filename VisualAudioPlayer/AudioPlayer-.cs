using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Un4seen.Bass; // http://www.un4seen.com/
using Un4seen.Bass.AddOn.Flac;
using System.IO;
using System.Windows.Forms;
using System.Collections;
using System.Net;
using System.Runtime.InteropServices;

namespace VisualAudioPlayer
{
    class AudioPlayer
    {
        #region Declarations
        private bool disposed;
        private int iStreamChannel = 0;
        public bool Initialized = false;
        public delegate void EndOfAlbumHandler(AudioPlayer sender, EventArgs e);
        public event EndOfAlbumHandler OnAlbumFinishedPlaying;
        public delegate void TrackStartPlayingHandler(AudioPlayer sender, EventArgs e);
        public event TrackStartPlayingHandler OnSongStartPlaying;
        public delegate void EndOfTrackHandler(AudioPlayer sender, EventArgs e);
        public event EndOfTrackHandler OnTrackFinishedPlaying;
        public delegate void AudioFileNotFoundHandler(AudioPlayer sender, AudioPlayerEventArgs e);
        public event AudioFileNotFoundHandler OnAudioFileNot;
        public class AudioPlayerEventArgs : EventArgs
        {
            public Int32 AlbumTrackID { get; private set; }
            public AudioPlayerEventArgs(Int32 iAlbumTrackID)
            {
                AlbumTrackID = iAlbumTrackID;
            }
        }

        public Album CurrentAlbumPlaying { get; set; }
        public AlbumTrack CurrentTrackPlaying { get; set; }
        public bool IsPlaying { get; set; }
        public bool IsPaused { get; set; }
        public string CurrentTrackTitlePlaying { get; set; }
        private Queue<AlbumTrack> TrackQueue;
        public Queue<AlbumTrack> CurrentTrackList { get; set; }
        #endregion

        public AudioPlayer()
        {

            try
            {
                if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))  // init BASS using the default output device 
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
            TrackQueue = new Queue<AlbumTrack>();
            CurrentTrackList = new Queue<AlbumTrack>();
            Initialized = true;
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
                    // free BASS
                    Bass.BASS_Free();

                }
                disposed = true;
            }
        }
        public void Next()
        {
            if (TrackQueue.Count() <= 1) // raise event EndOfAlbum
            {
                if (TrackQueue.Count() == 1) // clean up
                    TrackQueue.Dequeue();
                if (OnAlbumFinishedPlaying != null)
                    OnAlbumFinishedPlaying(this, new EventArgs());
                return;
            }
            TrackQueue.Dequeue();        // delete prev song
            Stop();
            Play(TrackQueue.Peek());
        }
        public void Pause()
        {
            if (iStreamChannel == 0)
                return;
            Bass.BASS_ChannelPause(iStreamChannel);
        }
        public bool PlayAudioQueue(Album album, Queue<AlbumTrack> queue)
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
            TrackQueue = new Queue<AlbumTrack>(queue);
            CurrentAlbumPlaying = album;
            AlbumTrack t = TrackQueue.Peek();
            return Play(t);
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
            if (iStreamChannel == 0)
                return;
            Bass.BASS_ChannelStop(iStreamChannel);
        }
        public void DisposeSound()
        {
            IsPlaying = false;

            if (iStreamChannel == 0)
                return;
            //iSound.setSoundStopEventReceiver(null);
            //iSoundEngine.StopAllSounds();
            CurrentAlbumPlaying.Reset();
            CurrentTrackPlaying.Reset();
        }
        private bool Play(AlbumTrack aTrack)
        {
            if (string.IsNullOrEmpty(aTrack.FileName))
                return false;
            string sTrackFileName;
            //if (aTrack.FileName.Length > 260)
            //{
            //    string strShortPath = aTrack.FileName.Substring(0, aTrack.FileName.LastIndexOf("\\"));
            //    string sFile = aTrack.FileName.Substring(aTrack.FileName.LastIndexOf("\\") + 1);
            //    strShortPath = FileUtil.GetShortPath(strShortPath);
            //    if (!Directory.Exists(strShortPath))
            //        return false;
            //    sTrackFileName = Path.Combine(strShortPath, sFile);
            //    if (!File.Exists(sTrackFileName))
            //        sTrackFileName = FileUtil.GetShortPath(aTrack.FileName);
            //    sTrackFileName = FileUtil.GetShortPath((@"\\wrage\music\Classic\The Decca Sound (50 Albums Collection) By Dready (2011)\CD 47 Beethoven - String Quartets Opp.95, 130 & 133 (Takács Quartet)\04 - String Quartet in F minor, Op.95 ''Quartetto serioso'' - IV. Larghetto espressivo — Allegretto agitato — Allegro.flac"));
            //        return false;
            //}
            //else
            sTrackFileName=aTrack.FileName;

            CurrentTrackPlaying = aTrack;
            string sExtension = Path.GetExtension(aTrack.FileName);
            switch (sExtension.ToUpper())
            {
                case ".FLAC":
                    iStreamChannel = BassFlac.BASS_FLAC_StreamCreateFile(aTrack.FileName, 0L, 0L, BASSFlag.BASS_DEFAULT); // create a stream channel from a file 
                    break;
                case "WAV":
                case "AIFF":
                case "MP3":
                case "MP2":
                case "MP1":
                case "OGG":
                    goto default;
                default:
                    iStreamChannel = Bass.BASS_StreamCreateFile(aTrack.FileName, 0L, 0L, BASSFlag.BASS_DEFAULT); // create a stream channel from a file 
                    break;
            }
            if (iStreamChannel != 0)
            {
                Bass.BASS_ChannelPlay(iStreamChannel, false);  // play the stream channel
            }
            else
            {
                string strTrackPath = sTrackFileName.Substring(0, sTrackFileName.LastIndexOf("\\"));
                if (sTrackFileName.StartsWith("\\\\"))
                {
                    string sServerName = FileUtil.GetServerName(strTrackPath);
                    if (!FileUtil.IsServerPingable(sServerName))
                    {
                        MessageBox.Show(new Form() { TopMost = true }, "Der Server '" + sServerName + "' ist zur Zeit Offline.\nWiedergabe nicht möglich.", "Server down", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return false;
                    }
                }
                if (!Directory.Exists(strTrackPath)) // we should be connected
                {
                    MessageBox.Show(new Form() { TopMost = true }, "Der Ordner des Albums wurde umbenannt oder existiert nicht mehr.\nWiedergabe nicht möglich.", "No such directory", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                if (!File.Exists(sTrackFileName))
                {
                    MessageBox.Show(new Form() { TopMost = true }, "Die folgende Datei wurde entweder umbenannt oder existiert nicht mehr:\n\n" + sTrackFileName + "\nDer Ordner des Albums wird jetzt neu eingelesen.", "No such file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Fire event to rescan album
                    if (OnAudioFileNot != null)  // raise event: AudioFileNot
                        OnAudioFileNot(this, new AudioPlayerEventArgs(aTrack.AlbumTrackID));
                    return false;
                }
                // TODO: Check read permission
                return false;
            }
            //iSound.setSoundStopEventReceiver(this);
            //iSoundEngine.Play2D(sTrackFileName);
            if (OnSongStartPlaying != null) // raise event: PlayNextTrack
                OnSongStartPlaying(this, new EventArgs());
            IsPlaying = true;
            CurrentTrackTitlePlaying = sTrackFileName;
            // xxx raise event: AutoPlayNextTrack
            return true;
        }
        public void OnSoundStopped()
        {        // free the stream
            Bass.BASS_StreamFree(iStreamChannel);
            iStreamChannel = 0;
                if (OnTrackFinishedPlaying != null)  // raise event: PlayNextTrack
                    OnTrackFinishedPlaying(this, new EventArgs());
                Next();
        }
    }
}
