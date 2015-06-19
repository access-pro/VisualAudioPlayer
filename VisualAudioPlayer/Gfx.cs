using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Media.Imaging;
using Cyotek.Windows.Forms;
using System.Security.Cryptography;
using System.IO;
using System.Drawing.Imaging;
using System.Net;

namespace VisualAudioPlayer
{
    class Gfx
    {
        public static Bitmap WriteText(Bitmap bmp, string sText, SolidBrush brush)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                RectangleF rectf = new RectangleF(0, 0, 500, 50);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawString(sText, new Font("Tahoma", 30, System.Drawing.FontStyle.Bold), brush, rectf);
                g.Flush();
            }
            return new Bitmap(bmp);
        }
        public static Bitmap WriteSubTitle(Bitmap bmp, string sText, SolidBrush brush)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                RectangleF rectf = new RectangleF(200, 80, 200, 50);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawString(sText, new Font("Tahoma", 30, System.Drawing.FontStyle.Bold), brush, rectf);
                g.Flush();
            }
            return new Bitmap(bmp);
        }
        public static Image ByteToImage(byte[] outbyte)
        {
            if (outbyte == null)
                return null;
            Image img = null;
            ImageConverter converter = new ImageConverter();
            try
            {
                img = (Image)converter.ConvertTo(outbyte, typeof(Image));
            }
            catch (Exception ex1)
            {
                Console.WriteLine(ex1.Message);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();

                ms.Write(outbyte, 0, outbyte.Length);
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                try
                {
                    img = Image.FromStream(ms);
                }
                catch (Exception ex2)
                {
                    Console.WriteLine(ex2.Message);
                    return null;
                }
            }
            return img;
        }
        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            byte[] imageData;
            try
            {
                imageData = (byte[])converter.ConvertTo(img, typeof(byte[]));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); 
                MemoryStream ms = new MemoryStream();
                img.Save(ms, ImageFormat.Bmp);
                return ms.ToArray();
            }
            return imageData;
        }
        public static byte[] BitmapToByte(Bitmap bm)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(bm, typeof(byte[]));
        }
        public static Image BitmapToImage(Bitmap bm)
        {
            ImageConverter converter = new ImageConverter();
            return (Image)converter.ConvertTo(bm, typeof(Image));
        }
        private static Brush RandomBrush()
        {
            Brush result = Brushes.Transparent;
            Random rnd = new Random();
            Type brushesType = typeof(Brushes);
            PropertyInfo[] properties = brushesType.GetProperties();
            int random = rnd.Next(properties.Length);
            result = (Brush)properties[random].GetValue(null, null);
            return result;
        }
        private static Color RandomColor()
        {
            Random rnd = new Random();
            KnownColor[] names = (KnownColor[])Enum.GetValues(typeof(KnownColor));
            KnownColor randomColorName = names[rnd.Next(names.Length)];
            Color randomColor = Color.FromKnownColor(randomColorName);

            return randomColor;
        }
        public static Color RandomLightColor(double hue)
        {
            HslColor hsl = new HslColor(hue, 1, 0.9);
            Color randomColor = (System.Drawing.Color)hsl.ToRgbColor();
            return randomColor;
        }
        public static Color RandomDarkColor(double hue)
        {
            HslColor hsl = new HslColor(hue, 1, 0.1);
            Color randomColor = (System.Drawing.Color)hsl.ToRgbColor();
            return randomColor;
        }
        public static string GetMD5Hash(Image img)
        {
            byte[] ImageData = ImageToByte(img);
            return GetMD5Hash(ImageData);
        }
        public static string GetMD5Hash(byte[] data)
        {
            byte[] checksum = null;
            using (var md5 = MD5.Create())
            {
                checksum = md5.ComputeHash(data);
            }
            return BitConverter.ToString(checksum).Replace("-", String.Empty);
        }
        public static bool VerifyMd5Hash(Image image, string hash)
        {
            string hashOfInput = GetMD5Hash(ImageToByte(image));  // Hash the image. 
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;   // compare the hashes.
            return (0 == comparer.Compare(hashOfInput, hash));
        }
        public static string GetHash(Image image)
        {
            SHA256Managed sha = new SHA256Managed();

            MemoryStream ms = new MemoryStream();
            if (ImageFormat.Jpeg.Equals(image.RawFormat))
            {
                image.Save(ms, ImageFormat.Jpeg);
            }
            else if (ImageFormat.Png.Equals(image.RawFormat))
            {
                image.Save(ms, ImageFormat.Bmp);
            }
            else if (ImageFormat.Gif.Equals(image.RawFormat))
            {
                image.Save(ms, ImageFormat.Gif);
            }

            byte[] imageBytes = ms.ToArray();

            byte[] checksum = sha.ComputeHash(imageBytes);
            //return Convert.ToBase64String(checksum);
            return BitConverter.ToString(checksum).Replace("-", String.Empty);
        }
        public static int GetByteCode(byte[] bytes)
        {
            if (bytes == null)
                return 0;

            int chksum = 0;

            foreach (byte data in bytes)
            {
                chksum += data;
            }
            return chksum;
        }
        public static Image ImageResize(Image SourceImage, Int32 NewHeight, Int32 NewWidth)
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(NewWidth, NewHeight, SourceImage.PixelFormat);

            if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed | bitmap.PixelFormat == PixelFormat.Format4bppIndexed | bitmap.PixelFormat == PixelFormat.Format8bppIndexed | bitmap.PixelFormat == PixelFormat.Undefined | bitmap.PixelFormat == PixelFormat.DontCare | bitmap.PixelFormat == PixelFormat.Format16bppArgb1555 | bitmap.PixelFormat == PixelFormat.Format16bppGrayScale)
            {
                throw new NotSupportedException("Pixel format of the image is not supported.");
            }

            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(SourceImage, 0, 0, bitmap.Width, bitmap.Height);
            g.Dispose();
            return bitmap;
        }
        public static Image DrawCoverNowPlaying(Image image)
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(image.Width, image.Height, image.PixelFormat);
            Pen p = new Pen(Color.Silver, 2);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(image, 3, 3, bitmap.Width - 8, bitmap.Height - 6);
                g.DrawRectangle(p, 0, 0, image.Width-2, bitmap.Height);
                g.Flush();
            }
            return (Image)bitmap;
        }
        public static Image DrawCoverImage(Image oldImage, Image newImage)
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(oldImage.Width, oldImage.Height, oldImage.PixelFormat);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(newImage, 0, 0, bitmap.Width, bitmap.Height);
                g.Flush();
            }
            return (Image)bitmap;
        }
        public static Image ResizeImage(Image imgToResize, System.Drawing.Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }
        /// <summary>
        /// Gets the image from URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public static Image GetImageFromURL(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;
            if (url.ToLower() == "error")
                return null;
            Image img = null;
            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse();
                Stream stream = httpWebReponse.GetResponseStream();
                img = System.Drawing.Image.FromStream(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return img;
        }
    }
}
