using System.IO;
using System.IO.Compression;
using System.Text;

namespace Gwupe.Common
{
    public class Misc
    {

        private static Misc oneAndOnly;
        private static readonly object InitLock = new object();

        private Misc()
        {
        }

        public static Misc Instance()
        {
            lock (InitLock)
            {
                if (oneAndOnly == null)
                {
                    oneAndOnly = new Misc();
                }
            }
            return oneAndOnly;
        }

        public byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            return Zip(bytes);
        }

        public byte[] Zip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }
    }
}
