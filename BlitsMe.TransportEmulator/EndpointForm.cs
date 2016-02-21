using System;
using System.Threading;
using System.Windows.Forms;
using BlitsMe.Common.Security;
using log4net;

namespace BlitsMe.TransportEmulator
{
    public partial class EndpointForm : Form
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EndpointForm));
        private readonly TransportForm _transportForm;
        private readonly ITransportManager _transportManager;
        private readonly bool _client;
        private DataStreamListener _dataStreamListener;
        private Random rand;
        private Thread dataSendThread;
        private int startTime;
        private byte[] recvBuffer;
        private int marker = 1048576;
        private int lastSendMarker = 0;
        private int lastReceiveMarker = 0;
        private String lastSendMarkMd5;
        private String lastReceiveMarkMd5;

        public EndpointForm(TransportForm transportForm, ITransportManager transportManager, bool client)
        {
            InitializeComponent();
            _transportForm = transportForm;
            _transportManager = transportManager;
            _client = client;
            _dataStreamListener = new DataStreamListener("fubar", _transportManager, this);
            _dataStreamListener.ConnectionAccepted += delegate
                {
                    recvBuffer = new byte[0];
                    Invoke(new Action(() => { inBytes.Text = "0"; }));
                };
            _dataStreamListener.ConnectionClosed += delegate(object sender, NamedConnectionEventArgs args)
                {
                    setMd5Sum();
                };
            _dataStreamListener.Listen();
            rand = new Random();
        }

        private void setMd5Sum()
        {
            Invoke(
                new Action(
                    () =>
                    {
                        md5total.Text = "All"; md5sum.Text = Util.getSingleton().getMD5Hash(recvBuffer, 0, recvBuffer.Length);
                    }));
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            if (dataSendThread == null || !dataSendThread.IsAlive)
            {
                dataSendThread = new Thread(RunDataSend) { IsBackground = true, Name = _client ? "ClientSendThread" : "ServerSendThread" };
                startTime = Environment.TickCount;
                dataSendThread.Start();
            }
        }

        private void RunDataSend()
        {
            byte[] buffer;
            int buffSize = Convert.ToInt32(dataSize.Text) * 1024;
            byte[] data = new byte[buffSize];
            int maxPacketSize = Convert.ToInt16(packetSizeMax.Text);
            int minPacketSize = Convert.ToInt16(packetSizeMin.Text);
            Logger.Info("Sending " + buffSize + " bytes in packets between sizes " + minPacketSize + " and " + maxPacketSize + " to destination");
            DefaultTcpTransportConnection transportConnection =
                new DefaultTcpTransportConnection(_transportManager.TCPTransport.OpenConnection("fubar", 2), ListeningReader);
            int sent = 0;
            int count = 0;
            try
            {
                int sendSize;
                while (sent < buffSize)
                {
                    if (buffSize - sent < minPacketSize)
                    {
                        sendSize = buffSize - sent;
                    }
                    else
                    {
                        sendSize = rand.Next(minPacketSize,
                                             ((buffSize - sent) > maxPacketSize ? maxPacketSize : buffSize - sent) + 1);
                    }
                    buffer = new byte[sendSize];
                    rand.NextBytes(buffer);
                    int startSend = Environment.TickCount;
                    Logger.Info("Sending packet [" + count + "] of " + sendSize + "b");
                    transportConnection.SendDataToTransportSocket(buffer,buffer.Length);
                    sent += sendSize;
                    Logger.Info("Sent packet [" + count + "] of " + sendSize + "b, took " + (Environment.TickCount - startSend) + "ms, " + (buffSize - sent) + "b left.");
                    Array.Copy(buffer, 0, data, sent - sendSize, sendSize);
                    count++;
                    
                    int markerCount = sent / marker;
                    int length = markerCount * marker;
                    Invoke(
                        new Action(
                            () =>
                            {
                                md5total.Text = markerCount.ToString() + "M";
                                md5sum.Text = Util.getSingleton().getMD5Hash(data, 0, length);
                                outBytes.Text = sent.ToString();
                                PacketsOut.Text = count.ToString();
                                outKbps.Text = (sent / (Environment.TickCount - startTime) * 1000 / 1024).ToString();
                            }));
                }
                Logger.Debug("Completed send : " + sent);
                Invoke(new Action(() =>
                    {
                        md5sum.Text = Util.getSingleton().getMD5Hash(data, 0, data.Length);
                        md5total.Text = "All";
                    }));
            }
            catch (Exception e)
            {
                Logger.Error("Exception sending data : " + e.Message, e);
            }
            finally
            {
                transportConnection.Close();
            }
        }


        public bool ListeningReader(byte[] data, int length, TcpTransportConnection connection)
        {
            Logger.Info("Received packet [size=" + length + "]");

            long totalInBytes = Convert.ToInt64(inBytes.Text) + length;
            Invoke(
                new Action(() => { inKbps.Text = (totalInBytes / (Environment.TickCount - startTime) * 1000 / 1024).ToString(); }

                    ));
            Invoke(new Action(() => { inBytes.Text = totalInBytes.ToString(); }));
            byte[] newBuffer = new byte[recvBuffer.Length + length];
            Array.Copy(recvBuffer, newBuffer, recvBuffer.Length);
            Array.Copy(data, 0, newBuffer, recvBuffer.Length, length);
            recvBuffer = newBuffer;
            int markerCount = recvBuffer.Length / marker;
            int markerLength = markerCount * marker;
            Invoke(
                new Action(
                    () =>
                    {
                        md5total.Text = markerCount.ToString() + "M"; md5sum.Text = Util.getSingleton().getMD5Hash(recvBuffer, 0, markerLength);
                    }));
            return true;
        }
    }

    public class DataStreamListener : TcpTransportListener
    {
        private readonly EndpointForm _form;

        public DataStreamListener(string name, ITransportManager transportManager, EndpointForm form)
            : base(name, transportManager)
        {
            _form = form;
        }

        protected override TcpTransportConnection ProcessConnect(ITcpOverUdptSocket socket)
        {
            var connection = new DefaultTcpTransportConnection(socket, _form.ListeningReader);
            return connection;
        }

    }

}
