using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using BlitsMe.Common.Security;
using BlitsMe.Communication.P2P.P2P.Connector;
using BlitsMe.Communication.P2P.P2P.Socket;
using BlitsMe.Communication.P2P.P2P.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Connector;
using BlitsMe.Communication.P2P.RUDP.Packet;
using BlitsMe.Communication.P2P.RUDP.Socket;
using BlitsMe.Communication.P2P.RUDP.Utils;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Microsoft.Win32;
using Socket = Udt.Socket;

namespace BlitsMe.UDTTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private UdpClient _udpClient;
        //private Socket _udtSocket;
        //private Socket _udtConnection;
        private static readonly ILog Logger = LogManager.GetLogger(typeof (MainWindow));
        private BmUdtSocket _socket;

        public MainWindow()
        {
            InitializeComponent();
            XmlConfigurator.Configure(Assembly.GetExecutingAssembly().GetManifestResourceStream("BlitsMe.UDTTester.log4net.xml"));
            ResetSockets();
        }

        private void ResetSockets()
        {
            
            try
            {
                if (_socket != null)
                {
                    _socket.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to close sockets");
            }
            //_socket = new BmUdtSocket();
            var aes = new AesCryptoPacketUtil(Encoding.UTF8.GetBytes("0123456789ABCDEF"));
            _socket = new BmUdtEncryptedSocket() { EncryptData = aes.EncryptData, DecryptData = aes.DecryptData };
            //IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            //_udpClient = new UdpClient(endPoint);
            //_udtSocket = new Udt.Socket(AddressFamily.InterNetwork, SocketType.Stream);
        }

        private void StartListenerClick(object sender, RoutedEventArgs e)
        {
            DisableClientServerButtons();
            Thread thread = new Thread(ListenMessage) { IsBackground = true };
            thread.Start();
        }

        private void WaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var facilitatorIp = Dns.GetHostAddresses(Facilitator.Text)[0];
                Dispatcher.Invoke(new Action(() => FacilitatorIpLabel.Content = "(" + facilitatorIp + ")"));
                var self = _socket.Wave(new IPEndPoint(facilitatorIp, 11230));
                if (self.ExternalEndPoint == null)
                {
                    SelfIpLabel.Content = "Unknown";
                }
                else
                {
                    SelfIpLabel.Content = self.ExternalEndPoint.Address + ":" + self.ExternalEndPoint.Port;
                }
                InitSyncWithButton.IsEnabled = true;
                WaitSyncFromButton.IsEnabled = true;
                WaveButton.IsEnabled = false;
                Status.Text = "Successfully waved";
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() => ErrorBlock.Text = ex.Message + "\n" + ex.StackTrace));
            }

        }

        private void InitSyncClick(object sender, RoutedEventArgs e)
        {
            EnableClientServerButtons();
            InitSyncWithButton.IsEnabled = false;
            WaitSyncFromButton.IsEnabled = false;
            try
            {
                var peerInfo = new PeerInfo
                {
                    ExternalEndPoint = new IPEndPoint(IPAddress.Parse(Destination.Text), Convert.ToInt32(DestinationPort.Text))
                };
                _socket.Sync(peerInfo, "test");
                Status.Text = "Synced with " + Destination.Text + ":" + DestinationPort.Text;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() => ErrorBlock.Text = ex.Message + "\n" + ex.StackTrace));
            }

        }

        private void WaitSyncClick(object sender, RoutedEventArgs e)
        {
            try
            {
                InitSyncWithButton.IsEnabled = false;
                WaitSyncFromButton.IsEnabled = false;
                var peerInfo = new PeerInfo
                {
                    ExternalEndPoint = new IPEndPoint(IPAddress.Parse(Destination.Text), Convert.ToInt32(DestinationPort.Text))
                };
                _socket.WaitForSync(peerInfo, "test");
                EnableClientServerButtons();
                Status.Text = Destination.Text + ":" + DestinationPort.Text + " synced with me";
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() => ErrorBlock.Text = ex.Message + "\n" + ex.StackTrace));
            }
        }

        private void ConnectClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SendMessageButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() => ErrorBlock.Text = ex.Message + "\n" + ex.StackTrace));
            }

        }

        private void ResetClick(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void Reset()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(Reset));
                return;
            }
            ResetSockets();
            WaveButton.IsEnabled = true;
            InitSyncWithButton.IsEnabled = false;
            WaitSyncFromButton.IsEnabled = false;
            SendMessageButton.IsEnabled = false;
            DisableClientServerButtons();
            ErrorBlock.Text = "";
            Status.Text = "Reset system";
            DestinationPort.Text = "";
        }

        public void ListenMessage()
        {
            try
            {
                byte[] buffer = new byte[512];
                _socket.ListenOnce();
                int read = _socket.Read(buffer, 512);
                Dispatcher.Invoke(new Action(() => ListenerOutput.Text = Encoding.UTF8.GetString(buffer, 0, read)));
                _socket.Close();
            }
            catch (Exception e)
            {
                Dispatcher.Invoke(new Action(() => ErrorBlock.Text = e.Message + "\n" + e.StackTrace));
            }
        }

        private void SendMessageClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _socket.Connect();
                var message = Encoding.UTF8.GetBytes(Message.Text);
                _socket.Send(message, message.Length);
                _socket.Close();
                Status.Text = "Sent " + Message.Text;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() => ErrorBlock.Text = ex.Message + "\n" + ex.StackTrace));
            }

        }

        private void SendFileButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var fileDialog = new OpenFileDialog();
                Nullable<bool> result = fileDialog.ShowDialog(this);
                if (result == true)
                {
                    DisableClientServerButtons();
                    string filename = fileDialog.FileName;
                    Udt.StdFileStream fs = new Udt.StdFileStream(filename, FileMode.Open);
                    byte[] buffer = new byte[16834];
                    _socket.Connect();
                    Status.Text = "Sending size " + fs.Length;
                    _socket.Send(BitConverter.GetBytes(fs.Length), sizeof(long));
                    int read = fs.Read(buffer, 0, 16834);
                    Status.Text = "Sending filename " + filename;
                    while (read > 0)
                    {
                        _socket.Send(buffer, read);
                        read = fs.Read(buffer, 0, 16834);
                    }
                    fs.Close();
                    _socket.Close();
                    Status.Text = "Sent " + filename;
                    SendFileButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() => ErrorBlock.Text = ex.Message + "\n" + ex.StackTrace));
            }
        }


        private void ReceiveFileButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DisableClientServerButtons();
            byte[] buffer = new byte[sizeof(long)];
            _socket.ListenOnce();
            _socket.Read(buffer, sizeof(long));
            long length = BitConverter.ToInt64(buffer, 0);
            Status.Text = Status.Text + " (" + length + " bytes)";
            // Receive the file contents (path is where to store the file)
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "FileSend.bin");
            Udt.StdFileStream fs = new Udt.StdFileStream(path, FileMode.Create);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var fsBuffer = new byte[16834];
            int totalRead = 0;
            int read = totalRead = _socket.Read(fsBuffer, 16834);
            while (totalRead < length)
            {
                fs.Write(fsBuffer, 0, read);
                totalRead += read = _socket.Read(fsBuffer, (int)((length - totalRead > 16834) ? 16834 : length - totalRead));
            }
            fs.Close();
            _socket.Close();
            sw.Stop();
            Status.Text = "File Received, " + length + " bytes in " + sw.Elapsed.TotalSeconds + " seconds (" + (sw.Elapsed.TotalSeconds == 0 ? "?" : (length / 1024 / sw.Elapsed.TotalSeconds).ToString()) + "kBps)";
        }

        private void DisableClientServerButtons()
        {
            StartListenerButton.IsEnabled = false;
            ConnectClientButton.IsEnabled = false;
            SendFileButton.IsEnabled = false;
            ReceiveFileButton.IsEnabled = false;
            ProxyInButton.IsEnabled = false;
            ProxyOutButton.IsEnabled = false;
        }

        private void EnableClientServerButtons()
        {
            StartListenerButton.IsEnabled = true;
            ConnectClientButton.IsEnabled = true;
            SendFileButton.IsEnabled = true;
            ReceiveFileButton.IsEnabled = true;
            ProxyInButton.IsEnabled = true;
            ProxyOutButton.IsEnabled = true;
        }



        private void ProxyInButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DisableClientServerButtons();
            try
            {
                var proxy = new StreamProxy(
                    new BmTcpSocket(new IPEndPoint(IPAddress.Loopback, Convert.ToInt32(IncomingPort.Text))),
                    _socket);
                proxy.Start();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() => ErrorBlock.Text = ex.Message + "\n" + ex.StackTrace));
            }
        }

        private void ProxyOutButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DisableClientServerButtons();
            try
            {
                var proxy = new StreamProxy(_socket,
                    new BmTcpSocket(new IPEndPoint(IPAddress.Parse(ProxyAddress.Text), Convert.ToInt32(ProxyPort.Text))));
                proxy.Start();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() => ErrorBlock.Text = ex.Message + "\n" + ex.StackTrace));
            }
        }


    }
}
