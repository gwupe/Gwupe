using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using log4net;
using log4net.Repository.Hierarchy;

namespace BlitsMe.Agent.UI
{
    public class SystemTray
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (SystemTray));
        private readonly BlitsMeClientAppContext _appContext;
        private static readonly Icon IconConnected = Properties.Resources.icon_main;
        private bool pulseUp = true;
        private int pulseLocation = 0;
        private static Icon[] IconSearchingList = { 
                                                      Properties.Resources.icon_pulse_0,
                                                      Properties.Resources.icon_pulse_10,
                                                      Properties.Resources.icon_pulse_20,
                                                      Properties.Resources.icon_pulse_30,
                                                      Properties.Resources.icon_pulse_40,
                                                      Properties.Resources.icon_pulse_50,
                                                      Properties.Resources.icon_pulse_60,
                                                      Properties.Resources.icon_pulse_70,
                                                      Properties.Resources.icon_pulse_80,
                                                      Properties.Resources.icon_pulse_90,
                                                      Properties.Resources.icon_pulse_100
                                                  };
        private static readonly string DefaultTooltip = "BlitsMe" + Program.BuildMarker + " (Offline)";
        private System.ComponentModel.IContainer components;
        public NotifyIcon notifyIcon { get; set; }
        private Timer linkDownIconBlinker;

        public SystemTray()
        {
            this._appContext = BlitsMeClientAppContext.CurrentAppContext;
            components = new System.ComponentModel.Container();
            notifyIcon = new NotifyIcon(components)
            {
                // This is the actual context menu which will appear
                ContextMenuStrip = new ContextMenuStrip(),
                // This is the icon to user
                Icon = IconSearchingList[0],
                // Whats the default text (to start)
                Text = DefaultTooltip,
                // Of course we want it visible
                Visible = true
            };
            // Set the event handlers
            notifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
            notifyIcon.Click += launchDashboardLeftClick;
            notifyIcon.DoubleClick += _appContext.OnIconClickLaunchDashboard;
            //notifyIcon.MouseUp += notifyIcon_MouseUp;
        }

        public void Start()
        {
            linkDownIconBlinker = new Timer();
            linkDownIconBlinker.Tick += offlineSearch;
            linkDownIconBlinker.Interval = 100;
            linkDownIconBlinker.Start();
        }

        public void launchDashboardLeftClick(object sender, EventArgs e)
        {
            if(((MouseEventArgs)e).Button.Equals(MouseButtons.Left)) {
                _appContext.OnIconClickLaunchDashboard(sender, e);
            }
        }

        public Icon pulseIcon() {
            if (pulseUp)
            {
                if (pulseLocation == IconSearchingList.Length - 1)
                {
                    pulseUp = false;
                }
                else
                {
                    pulseLocation++;
                }
            }
            else
            {
                if (pulseLocation == 0)
                {
                    pulseUp = true;
                }
                else
                {
                    pulseLocation--;
                }
            }
            return IconSearchingList[pulseLocation];
        }

        public void offlineSearch(object sender, EventArgs e)
        {
            if (_appContext.ConnectionManager.IsOnline())
            {
                if (!notifyIcon.Icon.Equals(IconConnected))
                {
                    notifyIcon.Icon = IconConnected;
                    notifyIcon.Text = "BlitsMe" + Program.BuildMarker + " (Online)";
                }
            }
            else
            {
                if (_appContext.ConnectionManager.Connection.isEstablished())
                {
                    notifyIcon.Text = "BlitsMe" + Program.BuildMarker + " (Logging In)";
                    notifyIcon.Icon = pulseIcon();
                } else
                {
                    notifyIcon.Text = "BlitsMe" + Program.BuildMarker + " (Connecting)";
                    notifyIcon.Icon = pulseIcon();
                }
            }
        }

        private void exitItem_Click(object sender, EventArgs e)
        {
            _appContext.Shutdown();
        }

        // This is the event handler for a left click on the icon
        private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            notifyIcon.ContextMenuStrip.Items.Clear();
            notifyIcon.ContextMenuStrip.Items.Add(Utils.generateItem("&Exit", exitItem_Click));
        }

        public void Close()
        {
            if (!IsClosed)
            {
                Logger.Debug("Closing System Tray");
                IsClosed = true;
                notifyIcon.Visible = false; // should remove lingering tray icon
            }
        }

        internal bool IsClosed { get; private set; }
    }
}
