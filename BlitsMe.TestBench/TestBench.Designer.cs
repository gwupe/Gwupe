namespace BlitsMe.TestBench
{
    partial class TestBench
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.testUDP = new System.Windows.Forms.Button();
            this.udpCount = new System.Windows.Forms.TextBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.outgoingStreamIntegrity = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.acksSent = new System.Windows.Forms.TextBox();
            this.dataPacketsReceived = new System.Windows.Forms.TextBox();
            this.errorLabel = new System.Windows.Forms.Label();
            this.peerIp = new System.Windows.Forms.TextBox();
            this.serverProgress = new System.Windows.Forms.ProgressBar();
            this.dataPacketsSent = new System.Windows.Forms.TextBox();
            this.dataPacketsResent = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.dataResendsReceived = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.packetSize = new System.Windows.Forms.TextBox();
            this.speed = new System.Windows.Forms.Label();
            this.totalTransferred = new System.Windows.Forms.Label();
            this.speedOverall = new System.Windows.Forms.Label();
            this.acksReceived = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.ackWaitInterval = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.streamListener = new System.Windows.Forms.Button();
            this.acksResent = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.outgoingStrIntegrity = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.waitSync = new System.Windows.Forms.Button();
            this.initSync = new System.Windows.Forms.Button();
            this.startClient = new System.Windows.Forms.Button();
            this.externalPort = new System.Windows.Forms.Label();
            this.externalIP = new System.Windows.Forms.Label();
            this.peerPort = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.facilitatorPort = new System.Windows.Forms.TextBox();
            this.facilitatorIP = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.tcpServerPort = new System.Windows.Forms.TextBox();
            this.tcpPort = new System.Windows.Forms.Label();
            this.startTcpServerConnector = new System.Windows.Forms.Button();
            this.label16 = new System.Windows.Forms.Label();
            this.tcpServer = new System.Windows.Forms.TextBox();
            this.startTcpClientConnector = new System.Windows.Forms.Button();
            this.timing = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // testUDP
            // 
            this.testUDP.Enabled = false;
            this.testUDP.Location = new System.Drawing.Point(62, 271);
            this.testUDP.Name = "testUDP";
            this.testUDP.Size = new System.Drawing.Size(234, 23);
            this.testUDP.TabIndex = 0;
            this.testUDP.Text = "Stream Data";
            this.testUDP.UseVisualStyleBackColor = true;
            this.testUDP.Click += new System.EventHandler(this.startSendStream);
            // 
            // udpCount
            // 
            this.udpCount.Location = new System.Drawing.Point(6, 26);
            this.udpCount.Name = "udpCount";
            this.udpCount.Size = new System.Drawing.Size(38, 20);
            this.udpCount.TabIndex = 1;
            this.udpCount.Text = "1000";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(6, 52);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(354, 23);
            this.progressBar.TabIndex = 2;
            // 
            // outgoingStreamIntegrity
            // 
            this.outgoingStreamIntegrity.AutoSize = true;
            this.outgoingStreamIntegrity.Location = new System.Drawing.Point(50, 100);
            this.outgoingStreamIntegrity.Name = "outgoingStreamIntegrity";
            this.outgoingStreamIntegrity.Size = new System.Drawing.Size(80, 13);
            this.outgoingStreamIntegrity.TabIndex = 3;
            this.outgoingStreamIntegrity.Text = "Tunnel Integrity";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(381, 126);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(121, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Data Packets Received";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(446, 178);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Acks Sent";
            // 
            // acksSent
            // 
            this.acksSent.Location = new System.Drawing.Point(508, 175);
            this.acksSent.Name = "acksSent";
            this.acksSent.Size = new System.Drawing.Size(100, 20);
            this.acksSent.TabIndex = 8;
            // 
            // dataPacketsReceived
            // 
            this.dataPacketsReceived.Location = new System.Drawing.Point(508, 123);
            this.dataPacketsReceived.Name = "dataPacketsReceived";
            this.dataPacketsReceived.Size = new System.Drawing.Size(100, 20);
            this.dataPacketsReceived.TabIndex = 9;
            // 
            // errorLabel
            // 
            this.errorLabel.AutoSize = true;
            this.errorLabel.ForeColor = System.Drawing.Color.Red;
            this.errorLabel.Location = new System.Drawing.Point(12, 511);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Size = new System.Drawing.Size(0, 13);
            this.errorLabel.TabIndex = 11;
            // 
            // peerIp
            // 
            this.peerIp.Location = new System.Drawing.Point(62, 43);
            this.peerIp.Name = "peerIp";
            this.peerIp.Size = new System.Drawing.Size(129, 20);
            this.peerIp.TabIndex = 12;
            this.peerIp.Text = "127.0.0.1";
            // 
            // serverProgress
            // 
            this.serverProgress.Location = new System.Drawing.Point(376, 52);
            this.serverProgress.Name = "serverProgress";
            this.serverProgress.Size = new System.Drawing.Size(267, 23);
            this.serverProgress.TabIndex = 14;
            // 
            // dataPacketsSent
            // 
            this.dataPacketsSent.Location = new System.Drawing.Point(136, 123);
            this.dataPacketsSent.Name = "dataPacketsSent";
            this.dataPacketsSent.Size = new System.Drawing.Size(100, 20);
            this.dataPacketsSent.TabIndex = 19;
            // 
            // dataPacketsResent
            // 
            this.dataPacketsResent.Location = new System.Drawing.Point(136, 149);
            this.dataPacketsResent.Name = "dataPacketsResent";
            this.dataPacketsResent.Size = new System.Drawing.Size(100, 20);
            this.dataPacketsResent.TabIndex = 18;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(21, 152);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(109, 13);
            this.label3.TabIndex = 17;
            this.label3.Text = "Data Packets Resent";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(33, 126);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(97, 13);
            this.label4.TabIndex = 16;
            this.label4.Text = "Data Packets Sent";
            // 
            // dataResendsReceived
            // 
            this.dataResendsReceived.Location = new System.Drawing.Point(508, 149);
            this.dataResendsReceived.Name = "dataResendsReceived";
            this.dataResendsReceived.Size = new System.Drawing.Size(100, 20);
            this.dataResendsReceived.TabIndex = 21;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(378, 152);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(124, 13);
            this.label5.TabIndex = 20;
            this.label5.Text = "Data Resends Received";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(164, 29);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(61, 13);
            this.label6.TabIndex = 22;
            this.label6.Text = "PacketSize";
            // 
            // packetSize
            // 
            this.packetSize.Location = new System.Drawing.Point(124, 26);
            this.packetSize.Name = "packetSize";
            this.packetSize.Size = new System.Drawing.Size(38, 20);
            this.packetSize.TabIndex = 23;
            this.packetSize.Text = "4096";
            // 
            // speed
            // 
            this.speed.AutoSize = true;
            this.speed.Location = new System.Drawing.Point(4, 78);
            this.speed.Name = "speed";
            this.speed.Size = new System.Drawing.Size(40, 13);
            this.speed.TabIndex = 24;
            this.speed.Text = "0 Kbps";
            // 
            // totalTransferred
            // 
            this.totalTransferred.AutoSize = true;
            this.totalTransferred.Location = new System.Drawing.Point(325, 78);
            this.totalTransferred.Name = "totalTransferred";
            this.totalTransferred.Size = new System.Drawing.Size(29, 13);
            this.totalTransferred.TabIndex = 25;
            this.totalTransferred.Text = "0 Kb";
            // 
            // speedOverall
            // 
            this.speedOverall.AutoSize = true;
            this.speedOverall.Location = new System.Drawing.Point(90, 78);
            this.speedOverall.Name = "speedOverall";
            this.speedOverall.Size = new System.Drawing.Size(82, 13);
            this.speedOverall.TabIndex = 30;
            this.speedOverall.Text = "0 Kbps (Overall)";
            // 
            // acksReceived
            // 
            this.acksReceived.Location = new System.Drawing.Point(136, 175);
            this.acksReceived.Name = "acksReceived";
            this.acksReceived.Size = new System.Drawing.Size(100, 20);
            this.acksReceived.TabIndex = 32;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(50, 178);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(80, 13);
            this.label9.TabIndex = 31;
            this.label9.Text = "Acks Received";
            // 
            // ackWaitInterval
            // 
            this.ackWaitInterval.Location = new System.Drawing.Point(136, 201);
            this.ackWaitInterval.Name = "ackWaitInterval";
            this.ackWaitInterval.Size = new System.Drawing.Size(100, 20);
            this.ackWaitInterval.TabIndex = 38;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(41, 204);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(89, 13);
            this.label12.TabIndex = 37;
            this.label12.Text = "Ack Wait Interval";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.timing);
            this.groupBox1.Controls.Add(this.streamListener);
            this.groupBox1.Controls.Add(this.acksResent);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.ackWaitInterval);
            this.groupBox1.Controls.Add(this.progressBar);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.testUDP);
            this.groupBox1.Controls.Add(this.udpCount);
            this.groupBox1.Controls.Add(this.outgoingStreamIntegrity);
            this.groupBox1.Controls.Add(this.dataResendsReceived);
            this.groupBox1.Controls.Add(this.outgoingStrIntegrity);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.acksReceived);
            this.groupBox1.Controls.Add(this.serverProgress);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.dataPacketsReceived);
            this.groupBox1.Controls.Add(this.speedOverall);
            this.groupBox1.Controls.Add(this.acksSent);
            this.groupBox1.Controls.Add(this.dataPacketsResent);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.dataPacketsSent);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.packetSize);
            this.groupBox1.Controls.Add(this.speed);
            this.groupBox1.Controls.Add(this.totalTransferred);
            this.groupBox1.Location = new System.Drawing.Point(12, 92);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(649, 317);
            this.groupBox1.TabIndex = 39;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Data Stream";
            // 
            // streamListener
            // 
            this.streamListener.Enabled = false;
            this.streamListener.Location = new System.Drawing.Point(456, 271);
            this.streamListener.Name = "streamListener";
            this.streamListener.Size = new System.Drawing.Size(157, 23);
            this.streamListener.TabIndex = 42;
            this.streamListener.Text = "Start Stream Listener";
            this.streamListener.UseVisualStyleBackColor = true;
            this.streamListener.Click += new System.EventHandler(this.streamListener_Click);
            // 
            // acksResent
            // 
            this.acksResent.Location = new System.Drawing.Point(508, 201);
            this.acksResent.Name = "acksResent";
            this.acksResent.Size = new System.Drawing.Size(100, 20);
            this.acksResent.TabIndex = 41;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(434, 204);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(68, 13);
            this.label7.TabIndex = 40;
            this.label7.Text = "Acks Resent";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(47, 29);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(71, 13);
            this.label13.TabIndex = 39;
            this.label13.Text = "Num Packets";
            // 
            // outgoingStrIntegrity
            // 
            this.outgoingStrIntegrity.Location = new System.Drawing.Point(136, 97);
            this.outgoingStrIntegrity.Name = "outgoingStrIntegrity";
            this.outgoingStrIntegrity.Size = new System.Drawing.Size(100, 20);
            this.outgoingStrIntegrity.TabIndex = 7;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.waitSync);
            this.groupBox2.Controls.Add(this.initSync);
            this.groupBox2.Controls.Add(this.startClient);
            this.groupBox2.Controls.Add(this.externalPort);
            this.groupBox2.Controls.Add(this.externalIP);
            this.groupBox2.Controls.Add(this.peerPort);
            this.groupBox2.Controls.Add(this.label15);
            this.groupBox2.Controls.Add(this.facilitatorPort);
            this.groupBox2.Controls.Add(this.facilitatorIP);
            this.groupBox2.Controls.Add(this.label14);
            this.groupBox2.Controls.Add(this.peerIp);
            this.groupBox2.Location = new System.Drawing.Point(12, 11);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(649, 75);
            this.groupBox2.TabIndex = 40;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Connection";
            // 
            // waitSync
            // 
            this.waitSync.Enabled = false;
            this.waitSync.Location = new System.Drawing.Point(541, 41);
            this.waitSync.Name = "waitSync";
            this.waitSync.Size = new System.Drawing.Size(79, 23);
            this.waitSync.TabIndex = 21;
            this.waitSync.Text = "Wait for Sync";
            this.waitSync.UseVisualStyleBackColor = true;
            this.waitSync.Click += new System.EventHandler(this.waitSync_Click);
            // 
            // initSync
            // 
            this.initSync.Enabled = false;
            this.initSync.Location = new System.Drawing.Point(456, 41);
            this.initSync.Name = "initSync";
            this.initSync.Size = new System.Drawing.Size(79, 23);
            this.initSync.TabIndex = 20;
            this.initSync.Text = "Init Sync";
            this.initSync.UseVisualStyleBackColor = true;
            this.initSync.Click += new System.EventHandler(this.initSync_Click);
            // 
            // startClient
            // 
            this.startClient.Location = new System.Drawing.Point(457, 16);
            this.startClient.Name = "startClient";
            this.startClient.Size = new System.Drawing.Size(163, 20);
            this.startClient.TabIndex = 18;
            this.startClient.Text = "Init Tunnel";
            this.startClient.UseVisualStyleBackColor = true;
            this.startClient.Click += new System.EventHandler(this.initWave);
            // 
            // externalPort
            // 
            this.externalPort.AutoSize = true;
            this.externalPort.Location = new System.Drawing.Point(325, 20);
            this.externalPort.Name = "externalPort";
            this.externalPort.Size = new System.Drawing.Size(13, 13);
            this.externalPort.TabIndex = 17;
            this.externalPort.Text = "0";
            // 
            // externalIP
            // 
            this.externalIP.AutoSize = true;
            this.externalIP.Location = new System.Drawing.Point(242, 20);
            this.externalIP.Name = "externalIP";
            this.externalIP.Size = new System.Drawing.Size(40, 13);
            this.externalIP.TabIndex = 16;
            this.externalIP.Text = "0.0.0.0";
            // 
            // peerPort
            // 
            this.peerPort.Location = new System.Drawing.Point(198, 43);
            this.peerPort.Name = "peerPort";
            this.peerPort.Size = new System.Drawing.Size(38, 20);
            this.peerPort.TabIndex = 15;
            this.peerPort.Text = "0";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(7, 46);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(29, 13);
            this.label15.TabIndex = 14;
            this.label15.Text = "Peer";
            // 
            // facilitatorPort
            // 
            this.facilitatorPort.Location = new System.Drawing.Point(198, 17);
            this.facilitatorPort.Name = "facilitatorPort";
            this.facilitatorPort.Size = new System.Drawing.Size(38, 20);
            this.facilitatorPort.TabIndex = 13;
            this.facilitatorPort.Text = "10230";
            // 
            // facilitatorIP
            // 
            this.facilitatorIP.Location = new System.Drawing.Point(62, 17);
            this.facilitatorIP.Name = "facilitatorIP";
            this.facilitatorIP.Size = new System.Drawing.Size(129, 20);
            this.facilitatorIP.TabIndex = 1;
            this.facilitatorIP.Text = "epiphany.baselineit.net";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(7, 20);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(52, 13);
            this.label14.TabIndex = 0;
            this.label14.Text = "Facilitator";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.tcpServerPort);
            this.groupBox3.Controls.Add(this.tcpPort);
            this.groupBox3.Controls.Add(this.startTcpServerConnector);
            this.groupBox3.Controls.Add(this.label16);
            this.groupBox3.Controls.Add(this.tcpServer);
            this.groupBox3.Controls.Add(this.startTcpClientConnector);
            this.groupBox3.Location = new System.Drawing.Point(12, 415);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(649, 82);
            this.groupBox3.TabIndex = 41;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "TCP Tunnel";
            // 
            // tcpServerPort
            // 
            this.tcpServerPort.Location = new System.Drawing.Point(587, 25);
            this.tcpServerPort.Name = "tcpServerPort";
            this.tcpServerPort.Size = new System.Drawing.Size(40, 20);
            this.tcpServerPort.TabIndex = 6;
            // 
            // tcpPort
            // 
            this.tcpPort.AutoSize = true;
            this.tcpPort.Location = new System.Drawing.Point(136, 28);
            this.tcpPort.Name = "tcpPort";
            this.tcpPort.Size = new System.Drawing.Size(13, 13);
            this.tcpPort.TabIndex = 5;
            this.tcpPort.Text = "0";
            // 
            // startTcpServerConnector
            // 
            this.startTcpServerConnector.Location = new System.Drawing.Point(479, 48);
            this.startTcpServerConnector.Name = "startTcpServerConnector";
            this.startTcpServerConnector.Size = new System.Drawing.Size(141, 23);
            this.startTcpServerConnector.TabIndex = 4;
            this.startTcpServerConnector.Text = "Start Server Connector";
            this.startTcpServerConnector.UseVisualStyleBackColor = true;
            this.startTcpServerConnector.Click += new System.EventHandler(this.startTcpServerConnector_Click);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(437, 28);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(38, 13);
            this.label16.TabIndex = 3;
            this.label16.Text = "Server";
            // 
            // tcpServer
            // 
            this.tcpServer.Location = new System.Drawing.Point(481, 25);
            this.tcpServer.Name = "tcpServer";
            this.tcpServer.Size = new System.Drawing.Size(100, 20);
            this.tcpServer.TabIndex = 2;
            // 
            // startTcpClientConnector
            // 
            this.startTcpClientConnector.Location = new System.Drawing.Point(6, 23);
            this.startTcpClientConnector.Name = "startTcpClientConnector";
            this.startTcpClientConnector.Size = new System.Drawing.Size(124, 23);
            this.startTcpClientConnector.TabIndex = 1;
            this.startTcpClientConnector.Text = "Start Client Connector";
            this.startTcpClientConnector.UseVisualStyleBackColor = true;
            this.startTcpClientConnector.Click += new System.EventHandler(this.startTcpClientConnector_Click);
            // 
            // timing
            // 
            this.timing.AutoSize = true;
            this.timing.Location = new System.Drawing.Point(211, 78);
            this.timing.Name = "timing";
            this.timing.Size = new System.Drawing.Size(29, 13);
            this.timing.TabIndex = 43;
            this.timing.Text = "0 ms";
            // 
            // TestBench
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(673, 547);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.errorLabel);
            this.Name = "TestBench";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button testUDP;
        private System.Windows.Forms.TextBox udpCount;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label outgoingStreamIntegrity;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox acksSent;
        private System.Windows.Forms.TextBox dataPacketsReceived;
        private System.Windows.Forms.Label errorLabel;
        private System.Windows.Forms.TextBox peerIp;
        private System.Windows.Forms.ProgressBar serverProgress;
        private System.Windows.Forms.TextBox dataPacketsSent;
        private System.Windows.Forms.TextBox dataPacketsResent;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox dataResendsReceived;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox packetSize;
        private System.Windows.Forms.Label speed;
        private System.Windows.Forms.Label totalTransferred;
        private System.Windows.Forms.Label speedOverall;
        private System.Windows.Forms.TextBox acksReceived;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox ackWaitInterval;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button startClient;
        private System.Windows.Forms.Label externalPort;
        private System.Windows.Forms.Label externalIP;
        private System.Windows.Forms.TextBox peerPort;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox facilitatorPort;
        private System.Windows.Forms.TextBox facilitatorIP;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Button initSync;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label tcpPort;
        private System.Windows.Forms.Button startTcpServerConnector;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox tcpServer;
        private System.Windows.Forms.Button startTcpClientConnector;
        private System.Windows.Forms.TextBox tcpServerPort;
        private System.Windows.Forms.TextBox outgoingStrIntegrity;
        private System.Windows.Forms.TextBox acksResent;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button waitSync;
        private System.Windows.Forms.Button streamListener;
        private System.Windows.Forms.Label timing;
    }
}

