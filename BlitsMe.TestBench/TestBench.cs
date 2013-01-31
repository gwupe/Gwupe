using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BlitsMe.Communication.P2P;
using System.Net;
using System.Threading;
using System.Collections;
using BlitsMe.Communication.P2P.RUDP.Tunnel;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using BlitsMe.Communication.P2P.RUDP.Utils;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using System.IO;
using BlitsMe.Common.Security;
using System.Security.Cryptography;
using log4net;
using BlitsMe.Communication.P2P.RUDP.Connector;

namespace BlitsMe.TestBench
{
    public partial class TestBench : Form
    {
        IUDPTunnel _iudpTunnel;
        TransportManager tcpTunnel;
        Thread runStatsThread;
        bool isServer = false;

        bool stop;
        bool stopServer;
        bool stopTunnel;
        int size;
        //int expectedData = 0;
        List<long> timeSave;
        List<int> sizeSave;
        int sizeInWindow = 0;
        long startTime;
        private static ILog logger;
        AutoResetEvent started = new AutoResetEvent(false);

        public TestBench()
        {
            InitializeComponent();
            logger = LogManager.GetLogger(typeof(TestBench));
            log4net.Config.XmlConfigurator.Configure();
        }

        private void runStats()
        {
            while (!stopTunnel)
            {
                System.Threading.Thread.Sleep(500);
                /*                outgoingStrIntegrity.Text = Convert.ToString(udpTunnel.tunnelIntegrity);
                                dataPacketsSent.Text = udpTunnel.packetCountTransmitDataFirst.ToString();
                                dataPacketsResent.Text = udpTunnel.packetCountTransmitDataResend.ToString();
                                acksReceived.Text = udpTunnel.packetCountReceiveAckValid.ToString();
                                ackWaitInterval.Text = udpTunnel.ackWaitInterval.ToString();
                                acksSent.Text = udpTunnel.packetCountTransmitAckFirst.ToString();
                                acksResent.Text = udpTunnel.packetCountTransmitAckResend.ToString();
                                dataPacketsReceived.Text = udpTunnel.packetCountReceiveDataFirst.ToString();
                                dataResendsReceived.Text = udpTunnel.packetCountReceiveDataResend.ToString(); */
                totalTransferred.Text = Convert.ToString(size / 1024) + " Kb";
                if (timeSave != null && timeSave.Count > 0)
                {
                    speedOverall.Text = String.Format("{0:0.00}", (float)(size / 1024) / (float)((timeSave[timeSave.Count - 1] - startTime) / 1000)) + " Kbps (Overall)";
                    speed.Text = String.Format("{0:0.00}", (float)(sizeInWindow / 1024) / ((float)(timeSave[timeSave.Count - 1] - timeSave[0]) / 1000)) + " Kbps";
                }

            }
        }

        private void startSendStream(object sender, EventArgs e)
        {
            errorLabel.Text = "";
            try
            {
                ITCPOverUdptSocket socket = tcpTunnel.OpenTCPConnection("STREAM");
                progressBar.Minimum = 0;
                progressBar.Maximum = Convert.ToInt32(udpCount.Text);
                progressBar.Value = 1;
                progressBar.Step = 1;
                startTime = DateTime.Now.Ticks / 10000;
                size = 0;
                timeSave = new List<long>(50);
                sizeSave = new List<int>(50);
                sizeInWindow = 0;
                totalTransferred.Text = "0 Kb";
                speed.Text = "0 Kbps";
                timing.Text = "0 ms";
                int myPacketSize = Convert.ToInt32(packetSize.Text);
                int startValue = 0;
                Random rand = new Random((int)DateTime.Now.Ticks);
                byte[] wroteAll = new byte[0]; 
                try
                {
                    for (int expectedData = 0; expectedData < (Convert.ToInt32(udpCount.Text) + startValue) && !stop; expectedData++)
                    {
                        int thisPacketSize = rand.Next(myPacketSize);
                        if (thisPacketSize < 4) { thisPacketSize = 4; }
                        size += thisPacketSize;
                        if (timeSave.Count == timeSave.Capacity)
                        {
                            timeSave.RemoveAt(0);
                            sizeInWindow -= sizeSave[0];
                            sizeSave.RemoveAt(0);
                        }
                        timeSave.Add((long)(DateTime.Now.Ticks / 10000));
                        sizeSave.Add(thisPacketSize);
                        sizeInWindow += thisPacketSize;
                        timing.Text = (((long)(DateTime.Now.Ticks / 10000)) - startTime).ToString() + " ms";
                        byte[] bytes = new byte[thisPacketSize];
                        Array.Copy(BitConverter.GetBytes(expectedData), bytes, 4);
                        socket.Send(bytes, 30000);
                        progressBar.PerformStep();
                        byte[] newWroteAll = new byte[thisPacketSize + wroteAll.Length];
                        Array.Copy(wroteAll, newWroteAll, wroteAll.Length);
                        Array.Copy(bytes, 0, newWroteAll, wroteAll.Length, bytes.Length);
                        wroteAll = newWroteAll;
                        logger.Debug("Wrote " + thisPacketSize + " bytes of data [first byte is " + bytes[0] + "], " + wroteAll.Length + " in total");
                    }
                }
                catch (Exception ex)
                {
                    errorLabel.Text = "Failed to send stream " + ex.Message;
                    logger.Error(ex.StackTrace);
                }
                finally
                {
                    dataPacketsSent.Text = socket.Connection.PacketCountTransmitDataFirst.ToString();
                    dataPacketsResent.Text = socket.Connection.PacketCountTransmitDataResend.ToString();
                    acksReceived.Text = socket.Connection.PacketCountReceiveAckValid.ToString();
                    acksSent.Text = socket.Connection.PacketCountTransmitAckFirst.ToString();
                    acksResent.Text = socket.Connection.PacketCountTransmitAckResend.ToString();
                    dataPacketsReceived.Text = socket.Connection.PacketCountReceiveDataFirst.ToString();
                    dataResendsReceived.Text = socket.Connection.PacketCountReceiveDataResend.ToString();
                    socket.Connection.Close();
                }
                logger.Debug("Sent " + wroteAll.Length + " bytes, hash is " + getHash(wroteAll));
                
            }
            catch (Exception ex)
            {
                errorLabel.Text = "Failed to open a connection to ECHO : " + ex.Message;
            }
        }

        private void streamListener_Click(object sender, EventArgs e)
        {
            tcpTunnel.ListenTCP("STREAM", startIncomingStream);
        }

        private bool startIncomingStream(ITCPOverUdptSocket socket)
        {
            Thread go = new Thread(new ThreadStart(delegate() { processIncomingStream(socket); }));
            go.IsBackground = true;
            go.Start();
            return true;
        }

        private void processIncomingStream(ITCPOverUdptSocket socket) {
            try
            {
                int read = -1;
                byte[] readAll = new byte[0];
                while (read != 0)
                {
                    byte[] tmpRead = new byte[4096];
                    read = socket.Read(tmpRead, tmpRead.Length);
                    byte[] newReadAll = new byte[read + readAll.Length];
                    Array.Copy(readAll, newReadAll, readAll.Length);
                    Array.Copy(tmpRead, 0, newReadAll, readAll.Length, read);
                    readAll = newReadAll;
                    logger.Debug("Read " + read + " bytes of data [first byte is " + tmpRead[0] + "], " + readAll.Length + " in total");
                }
                logger.Debug("Socket seems to be closed, read " + readAll.Length + " in total, hash is " + getHash(readAll));
            }
            catch (Exception ex)
            {
                errorLabel.Text = "Failed to read from stream : " + ex.Message;
                logger.Error(ex.StackTrace);
            }
        }
        private String getHash(byte[] data)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(data);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private void initWave(object sender, EventArgs e)
        {
            errorLabel.Text = "";
            if (_iudpTunnel == null)
            {
                waitSync.Enabled = true;
                initSync.Enabled = true;
                startClient.Text = "Shutdown Tunnel";
                _iudpTunnel = new UDPTunnel(0);
                setExternalIP();
                runStatsThread = new Thread(new ThreadStart(runStats));
                runStatsThread.IsBackground = true;
                runStatsThread.Start();
            }
            else
            {
                _iudpTunnel.Close();
                _iudpTunnel = null;
                startClient.Text = "Init Tunnel";
                waitSync.Enabled = false;
                initSync.Enabled = false;
                testUDP.Enabled = false;
                streamListener.Enabled = false;
                if (runStatsThread != null) { runStatsThread.Abort(); }
            }
        }

        private void setExternalIP()
        {
            try
            {
                IPAddress[] addresses = System.Net.Dns.GetHostAddresses(facilitatorIP.Text);
                if (addresses.Length == 0)
                {
                    throw new ArgumentException(
                        "Unable to retrieve address from specified host name.",
                        "hostName"
                    );
                }
                PeerInfo local = _iudpTunnel.Wave(new IPEndPoint(IPAddress.Parse(addresses[0].ToString()), Convert.ToInt32(facilitatorPort.Text)), 10000);
                externalIP.Text = local.externalEndPoint.Address.ToString();
                externalPort.Text = local.externalEndPoint.Port.ToString();
            }
            catch (Exception ex)
            {
                errorLabel.Text = "Failed to get wave : " + ex.Message;
            }
        }


        private void initSync_Click(object sender, EventArgs e)
        {
            errorLabel.Text = "";
            try
            {
                PeerInfo peer = new PeerInfo();
                peer.externalEndPoint = new IPEndPoint(IPAddress.Parse(peerIp.Text), Convert.ToInt32(peerPort.Text));
                _iudpTunnel.SyncWithPeer(peer, 15000);
                initSync.Enabled = false;
                waitSync.Enabled = false;
                testUDP.Enabled = true;
                streamListener.Enabled = true;
                tcpTunnel = new TransportManager(_iudpTunnel);
            }
            catch (Exception ex)
            {
                errorLabel.Text = "Failed to synchronize : " + ex.Message;
            }
        }

        private void waitSync_Click(object sender, EventArgs e)
        {
            errorLabel.Text = "";
            try
            {
                PeerInfo peer = new PeerInfo();
                peer.externalEndPoint = new IPEndPoint(IPAddress.Parse(peerIp.Text), Convert.ToInt32(peerPort.Text));
                _iudpTunnel.WaitForSyncFromPeer(peer, 10000);
                initSync.Enabled = false;
                waitSync.Enabled = false;
                testUDP.Enabled = true;
                streamListener.Enabled = true;
                tcpTunnel = new TransportManager(_iudpTunnel);
            }
            catch (Exception ex)
            {
                errorLabel.Text = "Failed to synchronize : " + ex.Message;
            }
        }

        private void startTcpClientConnector_Click(object sender, EventArgs e)
        {
            try
            {
                ProxyTcpConnector connector = new ProxyTcpConnector("VNC", tcpTunnel);
                connector.Listen(15000);
            }
            catch (Exception ex)
            {
                errorLabel.Text = "Failed to Setup VNC proxy connector : " + ex.Message;
            }
        }

        private void startTcpServerConnector_Click(object sender, EventArgs e)
        {
            try
            {
                VNCListener listener = new VNCListener(tcpTunnel);
                listener.Listen();
            }
            catch (Exception ex)
            {
                errorLabel.Text = "Failed to setup VNC listener : " + ex.Message;
            }
        }
        
    }
}
