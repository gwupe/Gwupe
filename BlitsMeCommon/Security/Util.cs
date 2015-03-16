using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Gwupe.Common.Security
{
    public class Util
    {
        private static Util oneAndOnly;
        private Random random;
        private static RNGCryptoServiceProvider _rngCsp;
        private static readonly object InitLock = new object();

        private Util()
        {
            if (_rngCsp == null)
            {
                _rngCsp = new RNGCryptoServiceProvider();
            }
            this.random = new Random((int)DateTime.Now.Ticks);
        }

        public static Util getSingleton()
        {
            lock (InitLock)
            {
                if (oneAndOnly == null)
                {
                    oneAndOnly = new Util();
                }
            }
            return oneAndOnly;
        }

        public byte[] AesEncryptBytes(byte[] source, int offset, int length, byte[] encryptionKey)
        {
            var aes = new AesCryptoServiceProvider
            {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                Key = encryptionKey
            };
            byte[] encrypted;
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    csEncrypt.Write(source, offset, length);
                    csEncrypt.FlushFinalBlock();
                    encrypted = msEncrypt.ToArray();
                }
            }
            var encryptedPlusIv = new byte[encrypted.Length + 16];
            Array.Copy(aes.IV, encryptedPlusIv, 16);
            Array.Copy(encrypted, 0, encryptedPlusIv, 16, encrypted.Length);
            return encryptedPlusIv;
        }

        public byte[] AesDecryptBytes(byte[] encryptedPlusIv, int offset, int length, byte[] encryptionKey)
        {
            var encrypted = new byte[length - 16];
            var iv = new byte[16];
            Array.Copy(encryptedPlusIv, offset, iv, 0, 16);
            Array.Copy(encryptedPlusIv, 16 + offset, encrypted, 0, encrypted.Length);
            var aes = new AesCryptoServiceProvider
            {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                Key = encryptionKey,
                IV = iv
            };
            byte[] original;
            using (MemoryStream msDecrypt = new MemoryStream(encrypted))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (MemoryStream msClear = new MemoryStream())
                    {
                        var buffer = new byte[4096];
                        var read = csDecrypt.Read(buffer, 0, buffer.Length);
                        while (read > 0)
                        {
                            msClear.Write(buffer, 0, read);
                            read = csDecrypt.Read(buffer, 0, buffer.Length);
                        }
                        csDecrypt.Flush();
                        original = msClear.ToArray();
                    }
                }
            }
            return original;
        }

        public String generateString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                double nextDouble = random.NextDouble();
                // choose the next char A a or 0
                if (nextDouble < 0.33)
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * nextDouble + 65)));
                else if (nextDouble < 0.66)
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor(10 * nextDouble + 48)));
                else
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * nextDouble + 97)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        public String hashPassword(string password, string token = "")
        {
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password + token);
            byte[] result;
            SHA256 shaM = new SHA256Managed();
            result = shaM.ComputeHash(passwordBytes);
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                str.AppendFormat("{0:x2}", result[i]);
            }
            return str.ToString();
        }

        public String getMD5Hash(byte[] data, int offset, int count)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(data, offset, count);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
