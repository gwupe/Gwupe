using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.ServiceProcess;
using BlitsMe.Communication;
using BlitsMe.Cloud.Messaging.Request;
using log4net;

namespace BlitsMe.Agent.UI.WinForms
{
    public partial class Dashboard : Form
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Dashboard));
        private BlitsMeClientAppContext appContext;
        public Dashboard(BlitsMeClientAppContext appContext)
        {
            InitializeComponent();
            this.appContext = appContext;

            this.Resize += hideIfMinimized;
            this.FormClosing += hideIfClosing;
        }

        private void hideIfMinimized(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                this.Hide();
            }
        }

        private void hideIfClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            this.WindowState = FormWindowState.Minimized;
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {

        }

        public bool logInBox(String message)
        {
            logBox.Text += message + "\r\n";
            return true;
        }
    }
}
