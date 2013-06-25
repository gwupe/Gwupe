namespace BlitsMe.TransportEmulator
{
    partial class TransportForm
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
            this.latency = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.latencyA = new System.Windows.Forms.Label();
            this.latencyB = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.bandwithServerOut = new System.Windows.Forms.Label();
            this.bandwidthClientOut = new System.Windows.Forms.Label();
            this.bandwidth = new System.Windows.Forms.TextBox();
            this.packetLossServerOut = new System.Windows.Forms.Label();
            this.packetLossClientOut = new System.Windows.Forms.Label();
            this.packetLoss = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // latency
            // 
            this.latency.Location = new System.Drawing.Point(155, 28);
            this.latency.Name = "latency";
            this.latency.Size = new System.Drawing.Size(37, 20);
            this.latency.TabIndex = 0;
            this.latency.Text = "50";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(75, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Latency (ms)";
            // 
            // latencyA
            // 
            this.latencyA.AutoSize = true;
            this.latencyA.Location = new System.Drawing.Point(198, 31);
            this.latencyA.Name = "latencyA";
            this.latencyA.Size = new System.Drawing.Size(13, 13);
            this.latencyA.TabIndex = 2;
            this.latencyA.Text = "0";
            // 
            // latencyB
            // 
            this.latencyB.AutoSize = true;
            this.latencyB.Location = new System.Drawing.Point(239, 31);
            this.latencyB.Name = "latencyB";
            this.latencyB.Size = new System.Drawing.Size(13, 13);
            this.latencyB.TabIndex = 3;
            this.latencyB.Text = "0";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(75, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "BW (KBps)";
            // 
            // bandwithServerOut
            // 
            this.bandwithServerOut.AutoSize = true;
            this.bandwithServerOut.Location = new System.Drawing.Point(239, 57);
            this.bandwithServerOut.Name = "bandwithServerOut";
            this.bandwithServerOut.Size = new System.Drawing.Size(13, 13);
            this.bandwithServerOut.TabIndex = 7;
            this.bandwithServerOut.Text = "0";
            // 
            // bandwidthClientOut
            // 
            this.bandwidthClientOut.AutoSize = true;
            this.bandwidthClientOut.Location = new System.Drawing.Point(198, 57);
            this.bandwidthClientOut.Name = "bandwidthClientOut";
            this.bandwidthClientOut.Size = new System.Drawing.Size(13, 13);
            this.bandwidthClientOut.TabIndex = 6;
            this.bandwidthClientOut.Text = "0";
            // 
            // bandwidth
            // 
            this.bandwidth.Location = new System.Drawing.Point(155, 54);
            this.bandwidth.Name = "bandwidth";
            this.bandwidth.Size = new System.Drawing.Size(37, 20);
            this.bandwidth.TabIndex = 5;
            this.bandwidth.Text = "50";
            // 
            // packetLossServerOut
            // 
            this.packetLossServerOut.AutoSize = true;
            this.packetLossServerOut.Location = new System.Drawing.Point(239, 83);
            this.packetLossServerOut.Name = "packetLossServerOut";
            this.packetLossServerOut.Size = new System.Drawing.Size(13, 13);
            this.packetLossServerOut.TabIndex = 11;
            this.packetLossServerOut.Text = "0";
            // 
            // packetLossClientOut
            // 
            this.packetLossClientOut.AutoSize = true;
            this.packetLossClientOut.Location = new System.Drawing.Point(198, 83);
            this.packetLossClientOut.Name = "packetLossClientOut";
            this.packetLossClientOut.Size = new System.Drawing.Size(13, 13);
            this.packetLossClientOut.TabIndex = 10;
            this.packetLossClientOut.Text = "0";
            // 
            // packetLoss
            // 
            this.packetLoss.Location = new System.Drawing.Point(155, 80);
            this.packetLoss.Name = "packetLoss";
            this.packetLoss.Size = new System.Drawing.Size(37, 20);
            this.packetLoss.TabIndex = 9;
            this.packetLoss.Text = "5";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(75, 83);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "PacketLoss (%)";
            // 
            // TransportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.packetLossServerOut);
            this.Controls.Add(this.packetLossClientOut);
            this.Controls.Add(this.packetLoss);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.bandwithServerOut);
            this.Controls.Add(this.bandwidthClientOut);
            this.Controls.Add(this.bandwidth);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.latencyB);
            this.Controls.Add(this.latencyA);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.latency);
            this.Name = "TransportForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "TransportForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox latency;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label latencyA;
        private System.Windows.Forms.Label latencyB;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label bandwithServerOut;
        private System.Windows.Forms.Label bandwidthClientOut;
        internal System.Windows.Forms.TextBox bandwidth;
        private System.Windows.Forms.Label packetLossServerOut;
        private System.Windows.Forms.Label packetLossClientOut;
        internal System.Windows.Forms.TextBox packetLoss;
        private System.Windows.Forms.Label label5;
    }
}