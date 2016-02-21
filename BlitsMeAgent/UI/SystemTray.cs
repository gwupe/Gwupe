using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.UI
{
    public class SystemTray
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (SystemTray));
        private readonly GwupeClientAppContext _appContext;
        private static readonly Icon IconConnected = Properties.Resources.icon_main;
        private static readonly Icon IconDisconnected = Properties.Resources.icon_disconnected;
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
        private static readonly string DefaultTooltip = "Gwupe" + Program.BuildMarker + " (Offline)";
        private System.ComponentModel.IContainer components;
        public NotifyIcon notifyIcon { get; set; }
        private Timer linkDownIconBlinker;

        public SystemTray()
        {
            this._appContext = GwupeClientAppContext.CurrentAppContext;
            components = new System.ComponentModel.Container();
            notifyIcon = new NotifyIcon(components)
            {
                // This is the actual context menu which will appear
                ContextMenuStrip = new ContextMenuStrip(),
                // This is the icon to user
                //Icon = IconSearchingList[0],
                Icon = IconDisconnected,
                // Whats the default text (to start)
                Text = DefaultTooltip,
                // Of course we want it visible
                Visible = true
            };
            // Set the event handlers
            notifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
            notifyIcon.Click += LaunchDashboardLeftClick;
            notifyIcon.DoubleClick += (sender, args) => GwupeClientAppContext.CurrentAppContext.UIManager.Show();
            //notifyIcon.MouseUp += notifyIcon_MouseUp;
        }

        public void Start()
        {
            linkDownIconBlinker = new Timer();
            linkDownIconBlinker.Tick += OfflineSearch;
            linkDownIconBlinker.Interval = 100;
            linkDownIconBlinker.Start();
        }

        private void LaunchDashboardLeftClick(object sender, EventArgs e)
        {
            if(((MouseEventArgs)e).Button.Equals(MouseButtons.Left)) {
                GwupeClientAppContext.CurrentAppContext.UIManager.Show();
            }
        }

        private Icon PulseIcon() {
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

        
        private void OfflineSearch(object sender, EventArgs e)
        {
            if (_appContext.ConnectionManager.IsOnline())
            {
                if (!notifyIcon.Icon.Equals(IconConnected))
                {
                    notifyIcon.Icon = IconConnected;
                    notifyIcon.Text = "Gwupe" + Program.BuildMarker + " [" + _appContext.CurrentUserManager.ActiveShortCode.Substring(0, 3) + " " + _appContext.CurrentUserManager.ActiveShortCode.Substring(3, 4) + "]";
                }
            }
            else
            {
                if (_appContext.ConnectionManager.Connection.isEstablished())
                {
                    notifyIcon.Text = "Gwupe" + Program.BuildMarker + " (Logging In)";
                } else
                {
                    notifyIcon.Text = "Gwupe" + Program.BuildMarker + " (Connecting)";
                }
                notifyIcon.Icon = PulseIcon();
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
            notifyIcon.ContextMenuStrip.Items.Add(UiUtils.GenerateItem("&Open",
                (o, args) => GwupeClientAppContext.CurrentAppContext.UIManager.Show()));
            ToolStripMenuItem logoutItem = UiUtils.GenerateItem("&Logout", (o, args) => _appContext.LoginManager.Logout());
            if (!_appContext.LoginManager.IsLoggedIn)
            {
                logoutItem.Enabled = false;
            }
            notifyIcon.ContextMenuStrip.Items.Add(logoutItem);
            notifyIcon.ContextMenuStrip.Items.Add(UiUtils.GenerateItem("&Exit", exitItem_Click));
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
