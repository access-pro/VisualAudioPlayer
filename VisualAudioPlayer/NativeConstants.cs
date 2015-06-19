using System;
namespace VisualAudioPlayer
{
    public enum AddAlbumStatus
    {
        Error,
        Failed,
        Succeeded
    }
    public enum AccessStatus
    {
        Error,
        ServerDown,
        NotExisting,
        Existing,
        LoginFailed
    }
    public struct LastPlayed
    {
        public Int32 AlbumID;
        public Int32 AlbumTrackID;
        public LastPlayed(Int32 iAlbumID, Int32 iAlbumTrackID)
        {
            AlbumID = iAlbumID;
            AlbumTrackID = iAlbumTrackID;
        }
    }
    public class AlbumTrack
    {
        public Int32 AlbumTrackID;
        public string FileName;
        public string Title;
        public string TrackNo;
        public string DiscNo;
        public Int32 Position;
        public Int16 Frames;

        public AlbumTrack(Int32 iAlbumTrackID, string sFileName, string sTitle, string sTrackNo, string sDiscNo = null, Int32 iPosition = 0, Int16 iFrames = 0)
        {
            AlbumTrackID = iAlbumTrackID;
            FileName = sFileName;
            Title = sTitle;
            TrackNo = sTrackNo;
            DiscNo = sDiscNo;
            Position = iPosition;
            Frames = iFrames;
        }
        public void Reset()
        {
            AlbumTrackID = 0;
            FileName = null;
        }
    }
    public struct Album
    {
        public Int32 AlbumID;
        public Int32 LastTrackPlayed;
        public string LastDatePlayed;

        public Album(Int32 _AlbumID, Int32 _LastTrackPlayed, string _LastDatePlayed)
        {
            AlbumID = _AlbumID;
            LastTrackPlayed = _LastTrackPlayed;
            LastDatePlayed = _LastDatePlayed;
        }
        public void Reset()
        {
            AlbumID = 0;
            LastTrackPlayed = 0;
            LastDatePlayed = null;
        }
    }
    internal static class NativeConstants
    {
        public const byte AC_SRC_ALPHA = 0x01;
        public const byte AC_SRC_OVER = 0x00;
        public const int HTCAPTION = 0x02;
        public const int HTCLIENT = 1;
        public const int HTNOWHERE = 0;
        public const int SW_HIDE = 0;
        public const int ULW_ALPHA = 0x02;
        public const int WM_APPCOMMAND = 0x0319;
        public const int WM_LBUTTONDBLCLK = 0x0203;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MOUSEHOVER = 0x02A1;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_NCHITTEST = 0x84;
        public const int WM_PARENTNOTIFY = 0x0210;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WS_EX_COMPOSITED = 0x02000000;
        public const int WS_EX_LAYERED = 0x80000;
    }
    internal enum ApplicationCommand
    {
        BrowserBackward = 1,
        BrowserForward = 2,
        BrowserRefresh = 3,
        BrowserStop = 4,
        BrowserSearch = 5,
        BrowserFavorites = 6,
        BrowserHome = 7,
        VolumeMute = 8,
        VolumeDown = 9,
        VolumeUp = 10,
        MediaNexttrack = 11,
        MediaPrevioustrack = 12,
        MediaStop = 13,
        MediaPlayPause = 14,
        LaunchMail = 15,
        LaunchMediaSelect = 16,
        LaunchApp1 = 17,
        LaunchApp2 = 18,
        BassDown = 19,
        BassBoost = 20,
        BassUp = 21,
        TrebleDown = 22,
        TrebleUp = 23,
        MicrophoneVolumeMute = 24,
        MicrophoneVolumeDown = 25,
        MicrophoneVolumeUp = 26,
        Help = 27,
        Find = 28,
        New = 29,
        Open = 30,
        Close = 31,
        Save = 32,
        Print = 33,
        Undo = 34,
        Redo = 35,
        Copy = 36,
        Cut = 37,
        Paste = 38,
        ReplyToMail = 39,
        ForwardMail = 40,
        SendMail = 41,
        SpellCheck = 42,
        DictateOrCommandControlToggle = 43,
        MicOnOffToggle = 44,
        CorrectionList = 45,
        MediaPlay = 46,
        MediaPause = 47,
        MediaRecord = 48,
        MediaFastForward = 49,
        MediaRewind = 50,
        MediaChannelUp = 51,
        MediaChannelDown = 52,
        Delete = 53,
        DwmFlip3D = 54
    }
}
