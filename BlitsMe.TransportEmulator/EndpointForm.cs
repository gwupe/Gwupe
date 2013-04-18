using System;
using System.Threading;
using System.Windows.Forms;
using BlitsMe.Communication.P2P.RUDP.Connector;
using BlitsMe.Communication.P2P.RUDP.Connector.API;
using BlitsMe.Communication.P2P.RUDP.Packet.TCP;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.Transport;

namespace BlitsMe.TransportEmulator
{
    public partial class EndpointForm : Form
    {
        private readonly TransportForm _transportForm;
        private readonly ITransportManager _transportManager;
        private DataStreamListener _dataStreamListener;

        public EndpointForm(TransportForm transportForm, ITransportManager transportManager)
        {
            InitializeComponent();
            _transportForm = transportForm;
            _transportManager = transportManager;
            _dataStreamListener = new DataStreamListener("fubar", _transportManager, this);
            _dataStreamListener.Listen();
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            byte[] buffer;
            var rand = new Random();
            int buffSize = rand.Next(1024, 8192);
            textBytes.Text = buffSize.ToString();

            DefaultTcpTransportConnection transportConnection = new DefaultTcpTransportConnection(_transportManager.TCPTransport.OpenConnection("fubar"), sendingReader);

            buffer = new byte[128];
            int doze = rand.Next(1, 5);
            for (int i = 0; i < buffSize; i++)
            {
                buffer[i%128] = (byte)rand.Next(32, 122);
                if (i % 128 == 0)
                {
                    transportConnection.SendDataToTransport(buffer);
                    Thread.Sleep(doze*10);
                }
            }

        }

        public bool ListeningReader(byte[] data, TcpTransportConnection connection)
        {
            String bytesIn = (int.Parse(textBytesIn.Text) + data.Length).ToString();
            SetControlText(textBytesIn, bytesIn);
 
            return true;
        }

        private bool sendingReader(byte[] data, TcpTransportConnection connection)
        {
           
            return true;
        }

        public void SetControlText(Control control, string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<Control, string>(SetControlText), new object[] { control, text });
            }
            else
            {
                control.Text = text;
            }
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
