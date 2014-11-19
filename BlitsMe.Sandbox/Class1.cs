using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using BlitsMe.Communication.P2P.P2P.Socket;
using BlitsMe.Communication.P2P.P2P.Tunnel;
using log4net;
using log4net.Config;

namespace BlitsMe.Sandbox
{
    class Class1 : ApplicationContext
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Class1));
        private String path = @"e:\infile";
        private String path2 = @"e:\tmp\outfile";

        public Class1()
        {
            XmlConfigurator.Configure(Assembly.GetExecutingAssembly().GetManifestResourceStream("BlitsMe.Sandbox.log4net.xml"));
            BmUdtSocket sendSocket = new BmUdtSocket();
            BmUdtSocket receiveSocket = new BmUdtSocket();
            var ip =Dns.GetHostAddresses("i.dev.blits.me")[0];
            Logger.Debug("Starting Sandbox");
            var sendPeer = sendSocket.Wave(new IPEndPoint(ip, 11230));
            var receivePeer = receiveSocket.Wave(new IPEndPoint(ip, 11230));
            ThreadPool.QueueUserWorkItem(state => ReceiveBmUdtFile(receiveSocket, sendPeer));
            SendBmUdtFile(sendSocket,receivePeer);
        }

        private void SendBmUdtFile(BmUdtSocket socket, PeerInfo receivePeer)
        {
            socket.Sync(receivePeer, "TEST", new List<SyncType>() { SyncType.All });
            socket.Connect();
            FileStream fs = new FileStream(path, FileMode.Open);
            var buffer = new byte[1024];
            int count = 0;
            int sum = 0;
            do
            {
                count = fs.Read(buffer, 0, 1024);
                sum += count;
                socket.Send(buffer, count);
            } while (sum < fs.Length);
            socket.Close();
        }

        private void ReceiveBmUdtFile(BmUdtSocket socket, PeerInfo sendPeer)
        {
            socket.WaitForSync(sendPeer, "TEST", new List<SyncType>() { SyncType.All });
            socket.ListenOnce();
            FileStream fs = new FileStream(path2, FileMode.OpenOrCreate);

            var buffer2 = new byte[1024];
            do
            {
                try
                {
                    int count = socket.Read(buffer2, 1024);
                    fs.Write(buffer2,0,count);
                }
                catch
                {
                    Logger.Debug("Read threw exception, closing socket.");
                    socket.Close();
                    break;
                }
            } while (true);
            fs.Close();
        }

        private void ReceiveFile()
        {
            using (Udt.Socket socket = new Udt.Socket(AddressFamily.InterNetwork, SocketType.Stream))
            {
                socket.Bind(IPAddress.Loopback, 10000);
                socket.Listen(10);

                using (Udt.Socket client = socket.Accept())
                {
                    // Receive the file length, in bytes
                    byte[] buffer = new byte[8];
                    client.Receive(buffer, 0, sizeof(long));

                    // Receive the file contents (path is where to store the file)
                    var buffer2 = new byte[1024];
                    do
                    {
                        try
                        {
                            client.Receive(buffer2, 0, 1024);
                        }
                        catch
                        {
                            break;
                        }
                    } while (true);
                    //client.ReceiveFile(path2, BitConverter.ToInt64(buffer, 0));
                }
            }
        }

        private void SendFile()
        {
            using (Udt.Socket socket = new Udt.Socket(AddressFamily.InterNetwork, SocketType.Stream))
            using (Udt.StdFileStream fs = new Udt.StdFileStream(path, FileMode.Open))
            {
                socket.Connect(IPAddress.Loopback, 10000);

                // Send the file length, in bytes
                socket.Send(BitConverter.GetBytes(fs.Length), 0, sizeof (long));
                var buffer = new byte[1024];
                int count = 0;
                int sum = 0;
                do
                {
                    count = fs.Read(buffer, 0, 1024);
                    sum += count;
                    socket.Send(buffer, 0, count);
                } while (sum < fs.Length);
                //socket.Close();
            }
        }
    }
}
