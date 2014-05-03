using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BlitsMe.Common;
using BlitsMe.Common.Security;
using BlitsMe.Communication.P2P.RUDP.Packet;
using BlitsMe.Communication.P2P.RUDP.Utils;

namespace BlitsMe.Sandbox
{
    class Class1 : ApplicationContext
    {

        private ushort me = ushort.MaxValue - 10;
        private Queue<byte> validate;
        private bool run = true;

        public Class1()
        {
            string[] first = new string[]
            {
                "mo ",
                "hello world ",
                "this is my world ",
                "and I will use it how I like ",
                "but don't be mistaken about how I like to do things ",
                "because its hairy"
            };
            MemoryStream stream = new MemoryStream();
            AesCryptoPacketUtil util = new AesCryptoPacketUtil(Encoding.UTF8.GetBytes("1234567890ABCDEF"));
            byte[] encodedBytes;
            byte[] packetBytes;
            foreach (var stringer in first)
            {
                encodedBytes = Encoding.UTF8.GetBytes(stringer);
                packetBytes = util.EncryptData(encodedBytes, encodedBytes.Length);
                stream.Write(packetBytes, 0, packetBytes.Length);
                Console.WriteLine(stringer);
                Console.Write(" = ");
                foreach (var encodedByte in encodedBytes)
                {
                    Console.Write(encodedByte + ",");
                }
                Console.WriteLine();
                Console.Write(" = ");
                foreach (var encodedByte in packetBytes)
                {
                    Console.Write(encodedByte + ",");
                }
                Console.WriteLine("");
            }
            var allBytes = stream.ToArray();
            byte[] data = new byte[allBytes.Length];
            Array.Copy(allBytes, 0, data, 0, allBytes.Length);
            int length = allBytes.Length;
            string outputString = "";
            var rand = new Random();
            int chunkSize;
            while (length > 0)
            {
                chunkSize = rand.Next(97) + 1;
                byte[] outBytes = util.DecryptData(data, chunkSize < length ? chunkSize : length);
                if (outBytes != null)
                {
                    Console.Write(" = ");
                    foreach (var outByte in outBytes)
                    {
                        Console.Write(outByte + ",");
                    }
                    Console.WriteLine();
                    Console.WriteLine(" = " + Encoding.UTF8.GetString(outBytes));
                }
                length -= chunkSize;
                if (length > 0)
                {
                    var newData = new byte[length];
                    Array.Copy(data, chunkSize, newData, 0, length);
                    data = newData;
                }
            }
        }

        private static void OpenExistingWindow()
        {
            OpenExistingWindow();
            OsUtils.PostMessage((IntPtr)OsUtils.HWND_BROADCAST, OsUtils.WM_SHOWBM, IntPtr.Zero, IntPtr.Zero);
            Console.WriteLine("Sent Show message");
            OsUtils.PostMessage((IntPtr)OsUtils.HWND_BROADCAST, OsUtils.WM_UPGRADEBM, IntPtr.Zero, IntPtr.Zero);
            Console.WriteLine("Sent Upgrade message");
            OsUtils.PostMessage((IntPtr)OsUtils.HWND_BROADCAST, OsUtils.WM_SHUTDOWNBM, IntPtr.Zero, IntPtr.Zero);
            Console.WriteLine("Sent Shutdown message");
        }

        private void TestWaver()
        {
            //Guess2();
            //GuessLocalIps();
        }

        private void RunBufferTest()
        {
            validate = new Queue<byte>();
            CircularBuffer<byte> buffer = new CircularBuffer<byte>(1);

            Thread getter = new Thread(() => runGetter(buffer)) { IsBackground = true };
            getter.Start();
            runAdder(buffer);
            run = false;
            ExitThread();
        }

        private void runGetter(CircularBuffer<byte> buffer)
        {
            Random rand = new Random();
            byte[] otherBuff = new byte[20];
            while (run || buffer.Count > 0)
            {
                byte[] get = buffer.Get(rand.Next(19) + 1, 1000);
                foreach (var b in get)
                {
                    validateGet(b);
                }
                int count = buffer.Get(otherBuff, 1000);
                for (int i = 0; i < count; i++)
                {
                    validateGet(otherBuff[i]);
                }
            }
            if (validate.Count > 0)
            {
                throw new Exception("Oops, " + validate.Count + " still left in the queue.");
            }
            Console.WriteLine("Completed");
        }

        private void validateGet(byte b)
        {
            byte val = validate.Dequeue();
            if (b != val)
                throw new Exception("Got " + val + ", expected " + b);
            Console.WriteLine("Got " + b + ", it matches expected");
        }

        private void runAdder(CircularBuffer<byte> buffer)
        {
            byte[] input;
            Random rand = new Random();
            for (int i = 0; i < 100; i++)
            {
                input = new byte[rand.Next(19) + 1];
                rand.NextBytes(input);
                AddToBuffer(input, buffer);
                if (input.Length > 20)
                {
                    Thread.Sleep(20);
                }
            }
        }

        private void AddToBuffer(byte[] input, CircularBuffer<byte> buffer)
        {
            Console.WriteLine("Adding " + input.Length + " values.");
            foreach (var b in input)
            {
                Console.WriteLine("Added " + b);
                validate.Enqueue(b);
            }
            buffer.Add(input, 1000);
        }

        private List<IPAddress> GetLocalIps()
        {
            var ips = new List<IPAddress>();
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in adapters)
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx))
                {
                    foreach (UnicastIPAddressInformation ipInfo in nic.GetIPProperties().UnicastAddresses)
                    {
                        byte[] address = ipInfo.Address.GetAddressBytes();
                        if (address.Length == 4)
                        {
                            if ((address[0] == 172 && address[1] >= 16 && address[1] <= 31)
                                || (address[0] == 10)
                                || (address[0] == 192 && address[1] == 168))
                                Console.WriteLine(ipInfo.Address);
                            ips.Add(ipInfo.Address);
                        }
                    }
                }
            }
            return ips;
        }

        private void Guess2()
        {
            var scope = new ManagementScope(@"\\localhost\root\cimv2");
            scope.Connect();
            var query = new ObjectQuery(@"SELECT * FROM Win32_NetworkAdapter where NetConnectionStatus = 2");
            var searcher = new ManagementObjectSearcher(scope, query);

            var networkInterfaces = searcher.Get();
            foreach (var networkInterface in networkInterfaces)
            {
                Console.WriteLine(string.Format("{0} - {1}", networkInterface["MACAddress"], networkInterface["Name"]));
            }
        }
    }
}
