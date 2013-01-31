using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace BlitsMe.Agent.UI
{
    public class SystemTray
    {
        private BlitsMeClientAppContext appContext;
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
        private static readonly string DefaultTooltip = "BlitsMe (Offline)";
        private System.ComponentModel.IContainer components;
        public NotifyIcon notifyIcon { get; set; }
        private Timer linkDownIconBlinker;

        public SystemTray(BlitsMeClientAppContext appContext)
        {
            this.appContext = appContext;
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
            linkDownIconBlinker = new Timer();
            linkDownIconBlinker.Tick += offlineSearch;
            linkDownIconBlinker.Interval = 100;
            linkDownIconBlinker.Start();
            // Set the event handlers
            notifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
            notifyIcon.Click += launchDashboardLeftClick;
            notifyIcon.DoubleClick += appContext.OnIconClickLaunchDashboard;
            //notifyIcon.MouseUp += notifyIcon_MouseUp;
        }

        public void launchDashboardLeftClick(object sender, EventArgs e)
        {
            if(((MouseEventArgs)e).Button.Equals(MouseButtons.Left)) {
                appContext.OnIconClickLaunchDashboard(sender, e);
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
            if (appContext.ConnectionManager.IsOnline())
            {
                if (!notifyIcon.Icon.Equals(IconConnected))
                {
                    notifyIcon.Icon = IconConnected;
                    notifyIcon.Text = "BlitsMe (Online)";
                }
            }
            else
            {
                notifyIcon.Text = "BlitsMe (Connecting)";
                notifyIcon.Icon = pulseIcon();
            }
        }

        private void exitItem_Click(object sender, EventArgs e)
        {
            appContext.Shutdown();
        }

        // This is the event handler for a left click on the icon
        private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            notifyIcon.ContextMenuStrip.Items.Clear();
            notifyIcon.ContextMenuStrip.Items.Add(Utils.generateItem("&Search for Help", null));
            notifyIcon.ContextMenuStrip.Items.Add(Utils.generateItem("&Favourites", null));
            notifyIcon.ContextMenuStrip.Items.Add(Utils.generateItem("&Dashboard", appContext.LaunchDebugDashboard));
            notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            notifyIcon.ContextMenuStrip.Items.Add(Utils.generateItem("&Exit", exitItem_Click));
        }

        public void close()
        {
            notifyIcon.Visible = false; // should remove lingering tray icon
        }
    }
}
