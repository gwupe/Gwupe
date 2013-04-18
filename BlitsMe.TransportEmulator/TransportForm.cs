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

        public TransportForm()
        {
            InitializeComponent();
            _transportManagerA = new ProxyTransportManager();
            _transportManagerB = new ProxyTransportManager();
            _transportManagerA.SetProxy(_transportManagerB);
            _transportManagerB.SetProxy(_transportManagerA);

            _formA = new EndpointForm(this, TransportManagerA);
            _formB = new EndpointForm(this, TransportManagerB);

            _formA.Text = "Endpoint A";
            _formB.Text = "Endpoing B";
            _formA.Show();
            _formB.Show();
        }
    }
}
