using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
// http://github.com/mono/taglib-sharp
using Alphaleonis.Win32.Filesystem;
// http://www.un4seen.com/
using Un4seen.Bass.AddOn.Tags;

namespace VisualAudioPlayer
{
    public class ID3Tag
    {
        public IEnumerable<FileInfo> AudioFiles;
        public ID3Tag(IEnumerable<FileInfo> allFiles)
        {
            if (allFiles.Count() == 0)
                return;
            AudioFiles = FileUtil.GetAudioFiles(allFiles);
        }
        public string GetAlbumCaption()
        {
            if (AudioFiles.Count() == 0)
                return null;
            foreach (FileInfo fi in AudioFiles)
            {
                try
                {
                    //string sAudioFile = @"\\?\unc" + fi.FullName.Substring(1);
                    TAG_INFO tagInfo = BassTags.BASS_TAG_GetFromFile(fi.FullName);
                    if (tagInfo == null)
                        return null;
                    return tagInfo.artist + " - " + tagInfo.album;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return null;
        }
        public string GetArtist()
        {
            if (AudioFiles.Count() == 0)
                return null;
            foreach (FileInfo fi in AudioFiles)
            {
                try
                {
                    //string sAudioFile = @"\\?\unc" + fi.FullName.Substring(1);
                    TAG_INFO tagInfo = GetTagInfo(fi.FullName);
                    if (tagInfo != null)
                        if (!string.IsNullOrEmpty(tagInfo.artist))
                            return tagInfo.artist;
                    using (TagLib.File tagFile = TagLib.File.Create(fi.FullName))
                    {
                        if (tagFile.Tag.AlbumArtists != null)
                            return tagFile.Tag.AlbumArtists.FirstOrDefault();
                        else
                            return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return null;
        }
        public static TAG_INFO GetTagInfo(string fileName)
        {
            //TAG_INFO taginfo = new TAG_INFO();
            //if (BassTags.BASS_TAG_GetFromFile(fileName, false, taginfo))
            //{
            //    taginfo = BassTags.BASS_TAG_GetFromFile(fileName);
            //}
            //else
            //{
            //    taginfo = BassTags.BASS_TAG_GetFromFile(fileName, true, true);
            //}
            return BassTags.BASS_TAG_GetFromFile(fileName);
        }
        public string GetAlbumTitle()
        {
            if (AudioFiles.Count() == 0)
                return null;
            foreach (FileInfo fi in AudioFiles)
            {
                try
                {
                    //string sAudioFile = @"\\?\unc" + fi.FullName.Substring(1);
                    TAG_INFO tagInfo = GetTagInfo(fi.FullName);
                    if (tagInfo != null)
                        if (!string.IsNullOrEmpty(tagInfo.album))
                            return tagInfo.album;
                    using (TagLib.File tagFile = TagLib.File.Create(fi.FullName))
                    {
                        if (tagFile.Tag.Album != null)
                            return tagFile.Tag.Album;
                        else
                            return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return null;
        }
        public static AlbumTrack GetTrackData(FileInfo fi)
        {
            AlbumTrack aTrack;
            try
            {
                //string sAudioFile = @"\\?\unc" + fi.FullName.Substring(1);
                TAG_INFO tagInfo = GetTagInfo(fi.FullName);
                if (tagInfo != null)
                {
                    aTrack = new AlbumTrack(0, fi.FullName, tagInfo.artist + " - " + tagInfo.title, tagInfo.track, tagInfo.disc);
                    return aTrack;
                }
                using (TagLib.File tagFile = TagLib.File.Create(fi.FullName)) // maybe other lib can do it
                {
                    if (tagFile == null)
                        return null;
                    aTrack = new AlbumTrack(0, fi.FullName, tagFile.Tag.Performers.First() + " - " + tagFile.Tag.Title, tagFile.Tag.Track.ToString(), tagFile.Tag.Disc.ToString());
                    return aTrack;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }
        public Image GetImage()
        {
            if (AudioFiles.Count() == 0)
                return null;
            Image img = GetBassID3Image(AudioFiles.First().FullName);
            if (img == null) // maybe other lib can read more files
                return GetID3Image(AudioFiles.First().FullName);
            return img; // no pics found in dir
        }
        public byte[] GetImageData()
        {
            if (AudioFiles.Count() == 0)
                return null;
            
            byte[] ImageData = GetBassID3ImageData(AudioFiles.First().FullName);
            if (ImageData == null) // maybe other lib can read more files
                return GetID3ImageData(AudioFiles.First().FullName);
            return ImageData;
        }
        private byte[] GetBassID3ImageData(string sAudioFile)
        {
            if (sAudioFile == null)
                return null;
            System.Drawing.Image img = GetBassID3Image(sAudioFile);
            return Gfx.ImageToByte(img);
        }
        private static Image GetBassID3Image(string sAudioFile)
        {
            if (sAudioFile == null)
                return null;
            sAudioFile = @"\\?\unc" + sAudioFile.Substring(1);
            try
            {
                TAG_INFO ti = BassTags.BASS_TAG_GetFromFile(sAudioFile);
                if (ti.PictureCount == 0)
                    return null;
                return ti.PictureGetImage(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }
        private static Image GetID3Image(string sAudioFile)
        {
            if (sAudioFile == null)
                return null;

            byte[] ImageData = GetID3ImageData(sAudioFile);
            if (ImageData == null)
                return null;
            return Gfx.ByteToImage(ImageData);
        }
        private static byte[] GetID3ImageData(string sAudioFile)
        {
            if (sAudioFile == null)
                return null;
            try
            {
                using (TagLib.File tagFile = TagLib.File.Create(sAudioFile))
                {
                    byte[] data = GetImageData(tagFile);
                    if (data != null)
                        return data; // pic avail in this file
                    else
                        return null; // assuming ALL files have cover or nothing
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }
        private static byte[] GetImageData(TagLib.File tagFile)
        {
            try
            {
                byte[] data = tagFile.Tag.Pictures[0].Data.Data;
                return data; // pic found
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null; // no pic found
            }
        }
    }
}