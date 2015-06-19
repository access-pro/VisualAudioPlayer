using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VisualAudioPlayer
{
    class LastFm
    {
        private const string APIKey = "0B5QM4HJ9M2PXFHG9602";
        private const string Secret = "V9iFwk5ZLCdtEkPiUx4jFEYCo/4Ujbx13uWOnhTB";
        //download their C# API and get an API account (which is free for non-commercial use, and instant). XXXXXX and YYYYYY is your lastFM login:
        public static string AbsUrlOfArt(string album, string artist)
        {
            Lastfm.Services.Session session = new Lastfm.Services.Session(APIKey, Secret);
            Lastfm.Services.Artist lArtist = new Lastfm.Services.Artist(artist, session);
            Lastfm.Services.Album lAlbum = new Lastfm.Services.Album(lArtist, album, session);

            return lAlbum.GetImageURL();
        }
        public static Image AlbumArt(string album, string artist)
        {
            Stream stream = null;
            try
            {
                WebRequest req = WebRequest.Create(AbsUrlOfArt(album, artist));
                WebResponse response = req.GetResponse();
                stream = response.GetResponseStream();
                Image img = Image.FromStream(stream);

                return img;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

    }
}
