using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlitsMe.Communication
{

    public class P2PUtils
    {

        private static P2PUtils oneAndOnly;
        private static Object lockObj = new Object();

        private P2PUtils()
        {
        }

        public static P2PUtils instance {
            get
            {
                lock (lockObj)
                {
                    if(oneAndOnly == null) {
                        oneAndOnly = new P2PUtils();
                    }
                }
                return oneAndOnly;
            }
            private set { } 
        }

        public String getIPString(byte[] ipBytes)
        {
            StringBuilder str = new StringBuilder();
            foreach (byte ipByte in ipBytes)
            {
                if (str.Length != 0)
                    str.Append(".");
                str.Append(Convert.ToString(ipByte));
            }
            return str.ToString();
        }

        public byte[] getIPBytes(String ip)
        {
            if (ip == null || ip.Equals(""))
            {
                return new byte[] { 0, 0, 0, 0 };
            }
            byte[] ipBytes = new byte[4];
            String[] ipBits = ip.Split(new char[] { '.' });
            int i = 0;
            foreach (String ipElement in ipBits)
            {
                ipBytes[i++] = Convert.ToByte(ipElement);
            }
            return ipBytes;
        }

        public byte[] getPortBytes(int port)
        {
            byte[] portBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                portBytes[3 - i] = (byte)((port >> (i * 8)) & 0x000000FF);
            }
            return portBytes;
        }

        public int getPortInt(byte[] port)
        {
            return (port[0] << 24) + ((port[1] & 0xFF) << 16) + ((port[2] & 0xFF) << 8) + (port[3] & 0xFF);
        }


    }
}
