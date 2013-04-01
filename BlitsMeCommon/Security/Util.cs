using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace BlitsMe.Common.Security
{
    public class Util
    {
        private static Util oneAndOnly;
        private Random random;
        private Util()
        {
            this.random = new Random((int)DateTime.Now.Ticks);
        }

        public static Util getSingleton()
        {
            if (oneAndOnly == null)
            {
                oneAndOnly = new Util();
            }
            return oneAndOnly;
        }

        public String generateString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
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
            byte[] hash = md5.ComputeHash(data,offset,count);

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
