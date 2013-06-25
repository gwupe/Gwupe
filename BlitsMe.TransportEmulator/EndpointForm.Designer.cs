namespace BlitsMe.TransportEmulator
{
    partial class EndpointForm
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
            this.button1 = new System.Windows.Forms.Button();
            this.outBytes = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.totalBytes = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.inBytes = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.packetSizeMin = new System.Windows.Forms.TextBox();
            this.dataSize = new System.Windows.Forms.TextBox();
            this.packetSizeMax = new System.Windows.Forms.TextBox();
            this.outKbps = new System.Windows.Forms.Label();
            this.inKbps = new System.Windows.Forms.Label();
            this.md5sum = new System.Windows.Forms.Label();
            this.PacketsOut = new System.Windows.Forms.Label();
            this.md5total = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.DataPacketSendCount = new System.Windows.Forms.Label();
            this.ExpectedDataPacketsReceived = new System.Windows.Forms.Label();
            this.FutureDataPacketsReceived = new System.Windows.Forms.Label();
            this.OldDataPacketsReceived = new System.Windows.Forms.Label();
            this.ExpectedOverReceivedPercentage = new System.Windows.Forms.Label();
            this.FutureOverReceivedPercentage = new System.Windows.Forms.Label();
            this.AcksSent = new System.Windows.Forms.Label();
            this.AcksResent = new System.Windows.Forms.Label();
            this.NoSpaceInWindow = new System.Windows.Forms.Label();
            this.DataPacketBufferCount = new System.Windows.Forms.Label();
            this.AvgSendWaitTime = new System.Windows.Forms.Label();
            this.DataPacketResendCount = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(203, 97);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Send";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // outBytes
            // 
            this.outBytes.Location = new System.Drawing.Point(137, 141);
            this.outBytes.Name = "outBytes";
            this.outBytes.Size = new System.Drawing.Size(64, 20);
            this.outBytes.TabIndex = 1;
            this.outBytes.Text = "0";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(68, 144);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Bytes Out";
            // 
            // totalBytes
            // 
            this.totalBytes.Location = new System.Drawing.Point(137, 193);
            this.totalBytes.Name = "totalBytes";
            this.totalBytes.Size = new System.Drawing.Size(64, 20);
            this.totalBytes.TabIndex = 5;
            this.totalBytes.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(68, 196);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Total Bytes";
            // 
            // inBytes
            // 
            this.inBytes.Location = new System.Drawing.Point(137, 167);
            this.inBytes.Name = "inBytes";
            this.inBytes.Size = new System.Drawing.Size(64, 20);
            this.inBytes.TabIndex = 7;
            this.inBytes.Text = "0";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(68, 170);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Bytes In";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 13);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(98, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Packet Size (bytes)";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 39);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(94, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Data Size (Kbytes)";
            // 
            // packetSizeMin
            // 
            this.packetSizeMin.Location = new System.Drawing.Point(116, 10);
            this.packetSizeMin.Name = "packetSizeMin";
            this.packetSizeMin.Size = new System.Drawing.Size(39, 20);
            this.packetSizeMin.TabIndex = 11;
            this.packetSizeMin.Text = "1024";
            // 
            // dataSize
            // 
            this.dataSize.Location = new System.Drawing.Point(116, 36);
            this.dataSize.Name = "dataSize";
            this.dataSize.Size = new System.Drawing.Size(85, 20);
            this.dataSize.TabIndex = 12;
            this.dataSize.Text = "1024";
            // 
            // packetSizeMax
            // 
            this.packetSizeMax.Location = new System.Drawing.Point(162, 10);
            this.packetSizeMax.Name = "packetSizeMax";
            this.packetSizeMax.Size = new System.Drawing.Size(39, 20);
            this.packetSizeMax.TabIndex = 13;
            this.packetSizeMax.Text = "8192";
            // 
            // outKbps
            // 
            this.outKbps.AutoSize = true;
            this.outKbps.Location = new System.Drawing.Point(207, 144);
            this.outKbps.Name = "outKbps";
            this.outKbps.Size = new System.Drawing.Size(13, 13);
            this.outKbps.TabIndex = 14;
            this.outKbps.Text = "0";
            // 
            // inKbps
            // 
            this.inKbps.AutoSize = true;
            this.inKbps.Location = new System.Drawing.Point(207, 170);
            this.inKbps.Name = "inKbps";
            this.inKbps.Size = new System.Drawing.Size(13, 13);
            this.inKbps.TabIndex = 15;
            this.inKbps.Text = "0";
            // 
            // md5sum
            // 
            this.md5sum.AutoSize = true;
            this.md5sum.Location = new System.Drawing.Point(68, 227);
            this.md5sum.Name = "md5sum";
            this.md5sum.Size = new System.Drawing.Size(65, 13);
            this.md5sum.TabIndex = 16;
            this.md5sum.Text = "UNKNOWN";
            // 
            // PacketsOut
            // 
            this.PacketsOut.AutoSize = true;
            this.PacketsOut.Location = new System.Drawing.Point(242, 144);
            this.PacketsOut.Name = "PacketsOut";
            this.PacketsOut.Size = new System.Drawing.Size(13, 13);
            this.PacketsOut.TabIndex = 17;
            this.PacketsOut.Text = "0";
            // 
            // md5total
            // 
            this.md5total.AutoSize = true;
            this.md5total.Location = new System.Drawing.Point(12, 227);
            this.md5total.Name = "md5total";
            this.md5total.Size = new System.Drawing.Size(13, 13);
            this.md5total.TabIndex = 18;
            this.md5total.Text = "0";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(75, 273);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(117, 13);
            this.label2.TabIndex = 19;
            this.label2.Text = "DataPacketSendCount";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(32, 295);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(160, 13);
            this.label7.TabIndex = 20;
            this.label7.Text = "ExpectedDataPacketsReceived";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(47, 319);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(145, 13);
            this.label8.TabIndex = 21;
            this.label8.Text = "FutureDataPacketsReceived";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(61, 341);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(131, 13);
            this.label9.TabIndex = 22;
            this.label9.Text = "OldDataPacketsReceived";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(16, 364);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(176, 13);
            this.label10.TabIndex = 23;
            this.label10.Text = "ExpectedOverReceivedPercentage";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(31, 387);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(161, 13);
            this.label11.TabIndex = 24;
            this.label11.Text = "FutureOverReceivedPercentage";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(139, 410);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(53, 13);
            this.label12.TabIndex = 25;
            this.label12.Text = "AcksSent";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(127, 433);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(65, 13);
            this.label13.TabIndex = 26;
            this.label13.Text = "AcksResent";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(92, 455);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(100, 13);
            this.label14.TabIndex = 27;
            this.label14.Text = "NoSpaceInWindow";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(72, 479);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(120, 13);
            this.label15.TabIndex = 28;
            this.label15.Text = "DataPacketBufferCount";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(63, 523);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(129, 13);
            this.label17.TabIndex = 30;
            this.label17.Text = "DataPacketResendCount";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(96, 501);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(96, 13);
            this.label18.TabIndex = 31;
            this.label18.Text = "AvgSendWaitTime";
            // 
            // DataPacketSendCount
            // 
            this.DataPacketSendCount.AutoSize = true;
            this.DataPacketSendCount.Location = new System.Drawing.Point(198, 273);
            this.DataPacketSendCount.Name = "DataPacketSendCount";
            this.DataPacketSendCount.Size = new System.Drawing.Size(13, 13);
            this.DataPacketSendCount.TabIndex = 32;
            this.DataPacketSendCount.Text = "0";
            // 
            // ExpectedDataPacketsReceived
            // 
            this.ExpectedDataPacketsReceived.AutoSize = true;
            this.ExpectedDataPacketsReceived.Location = new System.Drawing.Point(198, 295);
            this.ExpectedDataPacketsReceived.Name = "ExpectedDataPacketsReceived";
            this.ExpectedDataPacketsReceived.Size = new System.Drawing.Size(13, 13);
            this.ExpectedDataPacketsReceived.TabIndex = 33;
            this.ExpectedDataPacketsReceived.Text = "0";
            // 
            // FutureDataPacketsReceived
            // 
            this.FutureDataPacketsReceived.AutoSize = true;
            this.FutureDataPacketsReceived.Location = new System.Drawing.Point(198, 319);
            this.FutureDataPacketsReceived.Name = "FutureDataPacketsReceived";
            this.FutureDataPacketsReceived.Size = new System.Drawing.Size(13, 13);
            this.FutureDataPacketsReceived.TabIndex = 34;
            this.FutureDataPacketsReceived.Text = "0";
            // 
            // OldDataPacketsReceived
            // 
            this.OldDataPacketsReceived.AutoSize = true;
            this.OldDataPacketsReceived.Location = new System.Drawing.Point(198, 341);
            this.OldDataPacketsReceived.Name = "OldDataPacketsReceived";
            this.OldDataPacketsReceived.Size = new System.Drawing.Size(13, 13);
            this.OldDataPacketsReceived.TabIndex = 35;
            this.OldDataPacketsReceived.Text = "0";
            // 
            // ExpectedOverReceivedPercentage
            // 
            this.ExpectedOverReceivedPercentage.AutoSize = true;
            this.ExpectedOverReceivedPercentage.Location = new System.Drawing.Point(198, 364);
            this.ExpectedOverReceivedPercentage.Name = "ExpectedOverReceivedPercentage";
            this.ExpectedOverReceivedPercentage.Size = new System.Drawing.Size(13, 13);
            this.ExpectedOverReceivedPercentage.TabIndex = 36;
            this.ExpectedOverReceivedPercentage.Text = "0";
            // 
            // FutureOverReceivedPercentage
            // 
            this.FutureOverReceivedPercentage.AutoSize = true;
            this.FutureOverReceivedPercentage.Location = new System.Drawing.Point(198, 387);
            this.FutureOverReceivedPercentage.Name = "FutureOverReceivedPercentage";
            this.FutureOverReceivedPercentage.Size = new System.Drawing.Size(13, 13);
            this.FutureOverReceivedPercentage.TabIndex = 37;
            this.FutureOverReceivedPercentage.Text = "0";
            // 
            // AcksSent
            // 
            this.AcksSent.AutoSize = true;
            this.AcksSent.Location = new System.Drawing.Point(198, 410);
            this.AcksSent.Name = "AcksSent";
            this.AcksSent.Size = new System.Drawing.Size(13, 13);
            this.AcksSent.TabIndex = 38;
            this.AcksSent.Text = "0";
            // 
            // AcksResent
            // 
            this.AcksResent.AutoSize = true;
            this.AcksResent.Location = new System.Drawing.Point(198, 433);
            this.AcksResent.Name = "AcksResent";
            this.AcksResent.Size = new System.Drawing.Size(13, 13);
            this.AcksResent.TabIndex = 39;
            this.AcksResent.Text = "0";
            // 
            // NoSpaceInWindow
            // 
            this.NoSpaceInWindow.AutoSize = true;
            this.NoSpaceInWindow.Location = new System.Drawing.Point(198, 455);
            this.NoSpaceInWindow.Name = "NoSpaceInWindow";
            this.NoSpaceInWindow.Size = new System.Drawing.Size(13, 13);
            this.NoSpaceInWindow.TabIndex = 40;
            this.NoSpaceInWindow.Text = "0";
            // 
            // DataPacketBufferCount
            // 
            this.DataPacketBufferCount.AutoSize = true;
            this.DataPacketBufferCount.Location = new System.Drawing.Point(198, 479);
            this.DataPacketBufferCount.Name = "DataPacketBufferCount";
            this.DataPacketBufferCount.Size = new System.Drawing.Size(13, 13);
            this.DataPacketBufferCount.TabIndex = 41;
            this.DataPacketBufferCount.Text = "0";
            // 
            // AvgSendWaitTime
            // 
            this.AvgSendWaitTime.AutoSize = true;
            this.AvgSendWaitTime.Location = new System.Drawing.Point(198, 501);
            this.AvgSendWaitTime.Name = "AvgSendWaitTime";
            this.AvgSendWaitTime.Size = new System.Drawing.Size(13, 13);
            this.AvgSendWaitTime.TabIndex = 43;
            this.AvgSendWaitTime.Text = "0";
            // 
            // DataPacketResendCount
            // 
            this.DataPacketResendCount.AutoSize = true;
            this.DataPacketResendCount.Location = new System.Drawing.Point(198, 523);
            this.DataPacketResendCount.Name = "DataPacketResendCount";
            this.DataPacketResendCount.Size = new System.Drawing.Size(13, 13);
            this.DataPacketResendCount.TabIndex = 44;
            this.DataPacketResendCount.Text = "0";
            // 
            // EndpointForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 585);
            this.Controls.Add(this.DataPacketResendCount);
            this.Controls.Add(this.AvgSendWaitTime);
            this.Controls.Add(this.DataPacketBufferCount);
            this.Controls.Add(this.NoSpaceInWindow);
            this.Controls.Add(this.AcksResent);
            this.Controls.Add(this.AcksSent);
            this.Controls.Add(this.FutureOverReceivedPercentage);
            this.Controls.Add(this.ExpectedOverReceivedPercentage);
            this.Controls.Add(this.OldDataPacketsReceived);
            this.Controls.Add(this.FutureDataPacketsReceived);
            this.Controls.Add(this.ExpectedDataPacketsReceived);
            this.Controls.Add(this.DataPacketSendCount);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.md5total);
            this.Controls.Add(this.PacketsOut);
            this.Controls.Add(this.md5sum);
            this.Controls.Add(this.inKbps);
            this.Controls.Add(this.outKbps);
            this.Controls.Add(this.packetSizeMax);
            this.Controls.Add(this.dataSize);
            this.Controls.Add(this.packetSizeMin);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.inBytes);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.totalBytes);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.outBytes);
            this.Controls.Add(this.button1);
            this.Name = "EndpointForm";
            this.Text = "ReceiverForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox outBytes;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox totalBytes;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox inBytes;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox packetSizeMin;
        private System.Windows.Forms.TextBox dataSize;
        private System.Windows.Forms.TextBox packetSizeMax;
        private System.Windows.Forms.Label outKbps;
        private System.Windows.Forms.Label inKbps;
        private System.Windows.Forms.Label md5sum;
        private System.Windows.Forms.Label PacketsOut;
        private System.Windows.Forms.Label md5total;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label DataPacketSendCount;
        private System.Windows.Forms.Label ExpectedDataPacketsReceived;
        private System.Windows.Forms.Label FutureDataPacketsReceived;
        private System.Windows.Forms.Label OldDataPacketsReceived;
        private System.Windows.Forms.Label ExpectedOverReceivedPercentage;
        private System.Windows.Forms.Label FutureOverReceivedPercentage;
        private System.Windows.Forms.Label AcksSent;
        private System.Windows.Forms.Label AcksResent;
        private System.Windows.Forms.Label NoSpaceInWindow;
        private System.Windows.Forms.Label DataPacketBufferCount;
        private System.Windows.Forms.Label AvgSendWaitTime;
        private System.Windows.Forms.Label DataPacketResendCount;

    }
}