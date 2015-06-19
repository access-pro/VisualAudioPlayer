using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Deployment.Application;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO;

namespace VisualAudioPlayer
{
    public static class StringIO
    {
        private const string passPhrase = "Pos4pb@se";        // can be any string
        private const string initVector = "1B2c3@D8m5F6g7H8"; // must be 16 bytes
        /* =======================
         * HASHED PASSWORD FORMATS
         * =======================
         * 
         * Version 0:
         * PBKDF2 with HMAC-SHA1, 128-bit salt, 256-bit subkey, 1000 iterations.
         * (See also: SDL crypto guidelines v5.1, Part III)
         * Format: { 0x00, salt, subkey }
         */
        private const int PBKDF2IterCount = 1000; // default for Rfc2898DeriveBytes
        private const int PBKDF2SubkeyLength = 256 / 8; // 256 bits
        private const int SaltSize = 128 / 8; // 128 bits

        public static string HashPassword(string password)
        {
            byte[] salt;
            byte[] subkey;
            using (var deriveBytes = new Rfc2898DeriveBytes(password, SaltSize, PBKDF2IterCount))
            {
                salt = deriveBytes.Salt;
                subkey = deriveBytes.GetBytes(PBKDF2SubkeyLength);
            }

            byte[] outputBytes = new byte[1 + SaltSize + PBKDF2SubkeyLength];
            Buffer.BlockCopy(salt, 0, outputBytes, 1, SaltSize);
            Buffer.BlockCopy(subkey, 0, outputBytes, 1 + SaltSize, PBKDF2SubkeyLength);
            return Convert.ToBase64String(outputBytes);
        }
        public static string Enrypt(string sPassword)
        {
            string sCipherText = "";                 // encrypted text

            // Before encrypting data, we will append plain text to a random
            // salt value, which will be between 4 and 8 bytes long (implicitly
            // used defaults).
            Crypto rijndaelKey = new Crypto(passPhrase, initVector);

            // Encrypt the same plain text data 10 time (using the same key,
            // initialization vector, etc) and see the resulting cipher text;
            // encrypted values will be different.
            for (int i = 0; i < 13; i++)
            {
                sCipherText = rijndaelKey.Encrypt(sPassword);
            }
            return sCipherText;
        }
        public static string Denrypt(string sCipherText)
        {
            string sPassword = "";                  // original plaintext

            // Before encrypting data, we will append plain text to a random
            // salt value, which will be between 4 and 8 bytes long (implicitly
            // used defaults).
            Crypto rijndaelKey = new Crypto(passPhrase, initVector);

            for (int i = 0; i < 13; i++) // Decrypted
            {
                sPassword = rijndaelKey.Decrypt(sCipherText);
            }
            return sPassword;
        }
        public static bool VerifyHashedPassword(string hashedPassword, string password)
        {
            byte[] hashedPasswordBytes = Convert.FromBase64String(hashedPassword);

            // Wrong length or version header.
            if (hashedPasswordBytes.Length != (1 + SaltSize + PBKDF2SubkeyLength) || hashedPasswordBytes[0] != 0x00)
                return false;

            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(hashedPasswordBytes, 1, salt, 0, SaltSize);
            byte[] storedSubkey = new byte[PBKDF2SubkeyLength];
            Buffer.BlockCopy(hashedPasswordBytes, 1 + SaltSize, storedSubkey, 0, PBKDF2SubkeyLength);

            byte[] generatedSubkey;
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, PBKDF2IterCount))
            {
                generatedSubkey = deriveBytes.GetBytes(PBKDF2SubkeyLength);
            }
            return storedSubkey.SequenceEqual(generatedSubkey);
        }
        public static int CountWords(string text, string search)
        {
            int count = (text.Length - text.Replace(search, "").Length) / search.Length;
            return count;
        }
        public static string GetTitleFromPath(string sPath)
        {
            string tmp = sPath.Substring(sPath.LastIndexOf("\\") + 1);
            tmp = StringIO.CleanUpTitle(tmp);
            return tmp;
        }
        public static string Path2Title(string sPath)
        {
            string tmp = sPath.Substring(sPath.LastIndexOf("\\") + 1);
            tmp = StringIO.CleanUpTitle(tmp);
            return tmp;
        }
        public static string CleanUpTitle(string text)
        {
            string tmp = text.Replace("___", " - ");
            tmp = tmp.Replace("_", " ");
            tmp = tmp.Replace("-WEB-", "-");
            tmp = tmp.Replace("-", " - ");
            tmp = tmp.Replace("  ", " ");
            tmp = tmp.Replace(".mp3", " ");
            tmp = tmp.Replace(".", " ");
            tmp = tmp.Replace("  ", " ");
            tmp = tmp.Replace("  ", " ");
            tmp = tmp.Trim();
            return tmp;
        }
        public static string GetSubTitle(string sAlbumDirName, string sAlbumTitle)
        {   // http://msdn.microsoft.com/en-us/library/az24scfc.aspx
            //string tmp = Regex.Match(sAlbumDirName, @"([^a-z]cd\s??\d{1,3})").Groups[1].Value;
            string tmp = Regex.Match(sAlbumDirName, @"[^a-z]([cC][dD]\s?\d{1,3})").Groups[1].Value;
            if (string.IsNullOrEmpty(tmp))
                tmp = Regex.Match(sAlbumTitle, @"[^a-z]([cC][dD]\s?\d{1,3})").Groups[1].Value;
            return tmp;
        }
        public static byte[] StringToByteArray(string str)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetBytes(str);
        }
        public static string ByteArrayToString(byte[] arr)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetString(arr);
        }
        public static string GetCurrentVersion()
        {
#if DEBUG
            return "DEBUG Version";
#else
            string Vers = GetVersion().Major.ToString() + "." + GetVersion().Minor.ToString() + "." + GetVersion().Build.ToString() + "." + GetVersion().Revision.ToString();
            return Vers;
#endif
        }
        private static Version GetVersion()
        {
            try
            {
                return ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            catch
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }
        public static string ReadFile(string filename)
        {
            string s;
           
            TextReader tr = new StreamReader(filename, Encoding.Default); // read in the full cue file

            s = tr.ReadLine().Trim();  // read in file
            tr.Close(); // close the stream

            return s;
        }
        public static string GetPlsUrl(string filename)
        {
            string s;

            // read in the full cue file
            TextReader tr = new StreamReader(filename, Encoding.Default);

            s = tr.ReadLine().Trim(); // read in file
            while (s != null && !s.StartsWith("File1="))
            {
                s = tr.ReadLine();
            }
            tr.Close(); // close the stream;
            if (s == null)
                return null;
            s = s.Trim();
            s = s.Substring(6);
            return s;
        }
    }
}
