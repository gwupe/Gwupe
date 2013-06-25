using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BlitsMe.Communication.P2P.RUDP.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.Transport;
using log4net.Config;

namespace BlitsMe.TransportEmulator
{
    public partial class TransportForm : Form
    {
        private EndpointForm _formA;
        private EndpointForm _formB;
        private ProxyTransportManager _transportManagerA;
        private ProxyTransportManager _transportManagerB;

        public ITransportManager TransportManagerA
        {
            get { return _transportManagerA; }
        }

        public ITransportManager TransportManagerB
        {
            get { return _transportManagerB; }
        }

        public int Latency
        {
            get { return Convert.ToInt32(latency.Text); }
            set { Invoke(new Action(() => latency.Text = Convert.ToString(value))); }
        }

        public long ClientLatency
        {
            get { return Convert.ToInt64(latencyA.Text); }
            set { Invoke(new Action(() => latencyA.Text = Convert.ToString(value))); }
        }
        public long ServerLatency
        {
            get { return Convert.ToInt64(latencyB.Text); }
            set { Invoke(new Action(() => latencyB.Text = Convert.ToString(value))); }
        }

        public int PacketLoss
        {
            get { return Convert.ToInt32(packetLoss.Text); }
            set { Invoke(new Action(() => packetLoss.Text = Convert.ToString(value))); }
        }

        public int Bandwidth
        {
            get { return Convert.ToInt32(bandwidth.Text); }
            set { Invoke(new Action(() => bandwidth.Text = Convert.ToString(value))); }
        }

        public long ClientPacketLoss
        {
            get { return Convert.ToInt64(packetLossClientOut.Text); }
            set { Invoke(new Action(() => packetLossClientOut.Text = Convert.ToString(value))); }
        }
        public long ServerPacketLoss
        {
            get { return Convert.ToInt64(packetLossServerOut.Text); }
            set { Invoke(new Action(() => packetLossServerOut.Text = Convert.ToString(value))); }
        }

        public long ClientBandwidth
        {
            get { return Convert.ToInt64(bandwidthClientOut.Text); }
            set { Invoke(new Action(() => bandwidthClientOut.Text = Convert.ToString(value))); }
        }
        public long ServerBandwidth
        {
            get { return Convert.ToInt64(bandwithServerOut.Text); }
            set { Invoke(new Action(() => bandwithServerOut.Text = Convert.ToString(value))); }
        }

        public TransportForm()
        {
            InitializeComponent();
            XmlConfigurator.Configure();
            _transportManagerA = new ProxyTransportManager(this, true);
            _transportManagerB = new ProxyTransportManager(this, false);
            _transportManagerA.SetProxy(_transportManagerB);
            _transportManagerB.SetProxy(_transportManagerA);

            _formA = new EndpointForm(this, TransportManagerA, true);
            _formB = new EndpointForm(this, TransportManagerB, false);

            _formA.Text = "Client";
            _formB.Text = "Server";
            _formA.Show();
            _formB.Show();
            this.Location = new Point(400, 100);
            _formA.Location = new Point(100, 100);
            _formB.Location = new Point(800, 100);
        }

    }
}
