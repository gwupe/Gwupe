namespace BlitsMe.Agent.UI.WinForms
{
    partial class Dashboard
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
            if (disposing) 
            {
                if(components != null) 
                {
                    components.Dispose();
                }
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Dashboard));
            this.requestConnection = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.blitsMeProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.blitsMeClientStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.shortCode = new System.Windows.Forms.TextBox();
            this.logBox = new System.Windows.Forms.TextBox();
            this.generateUDP = new System.Windows.Forms.Button();
            this.p2pkey = new System.Windows.Forms.TextBox();
            this.p2pip = new System.Windows.Forms.TextBox();
            this.p2pport = new System.Windows.Forms.TextBox();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // requestConnection
            // 
            this.requestConnection.Location = new System.Drawing.Point(175, 9);
            this.requestConnection.Name = "requestConnection";
            this.requestConnection.Size = new System.Drawing.Size(141, 23);
            this.requestConnection.TabIndex = 1;
            this.requestConnection.Text = "Request Connection";
            this.requestConnection.UseVisualStyleBackColor = true;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.blitsMeProgressBar,
            this.blitsMeClientStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 541);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(558, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // blitsMeProgressBar
            // 
            this.blitsMeProgressBar.Name = "blitsMeProgressBar";
            this.blitsMeProgressBar.Size = new System.Drawing.Size(100, 16);
            // 
            // blitsMeClientStatus
            // 
            this.blitsMeClientStatus.Name = "blitsMeClientStatus";
            this.blitsMeClientStatus.Size = new System.Drawing.Size(46, 17);
            this.blitsMeClientStatus.Text = "BlitsMe";
            this.blitsMeClientStatus.ToolTipText = "74ms";
            // 
            // shortCode
            // 
            this.shortCode.Location = new System.Drawing.Point(12, 12);
            this.shortCode.Name = "shortCode";
            this.shortCode.Size = new System.Drawing.Size(157, 20);
            this.shortCode.TabIndex = 3;
            // 
            // logBox
            // 
            this.logBox.Location = new System.Drawing.Point(11, 132);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.Size = new System.Drawing.Size(535, 377);
            this.logBox.TabIndex = 5;
            // 
            // generateUDP
            // 
            this.generateUDP.Location = new System.Drawing.Point(316, 44);
            this.generateUDP.Name = "generateUDP";
            this.generateUDP.Size = new System.Drawing.Size(126, 23);
            this.generateUDP.TabIndex = 6;
            this.generateUDP.Text = "Generate UDP";
            this.generateUDP.UseVisualStyleBackColor = true;
            // 
            // p2pkey
            // 
            this.p2pkey.Location = new System.Drawing.Point(13, 46);
            this.p2pkey.Name = "p2pkey";
            this.p2pkey.Size = new System.Drawing.Size(100, 20);
            this.p2pkey.TabIndex = 7;
            // 
            // p2pip
            // 
            this.p2pip.Location = new System.Drawing.Point(119, 46);
            this.p2pip.Name = "p2pip";
            this.p2pip.Size = new System.Drawing.Size(146, 20);
            this.p2pip.TabIndex = 8;
            // 
            // p2pport
            // 
            this.p2pport.Location = new System.Drawing.Point(271, 46);
            this.p2pport.Name = "p2pport";
            this.p2pport.Size = new System.Drawing.Size(39, 20);
            this.p2pport.TabIndex = 9;
            // 
            // Dashboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(558, 563);
            this.Controls.Add(this.p2pport);
            this.Controls.Add(this.p2pip);
            this.Controls.Add(this.p2pkey);
            this.Controls.Add(this.generateUDP);
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.shortCode);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.requestConnection);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Dashboard";
            this.Text = "BlitsMe Dashboard";
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button requestConnection;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripProgressBar blitsMeProgressBar;
        private System.Windows.Forms.ToolStripStatusLabel blitsMeClientStatus;
        private System.Windows.Forms.TextBox shortCode;
        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.Button generateUDP;
        private System.Windows.Forms.TextBox p2pkey;
        private System.Windows.Forms.TextBox p2pip;
        private System.Windows.Forms.TextBox p2pport;
    }
}