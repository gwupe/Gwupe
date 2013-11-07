using System;
using System.Collections.ObjectModel;
using System.Timers;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Alert;
using BlitsMe.Agent.Components.Functions.RemoteDesktop;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.UI.WPF.Engage;
using log4net;

namespace BlitsMe.Agent.Managers
{
    class NotificationManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(NotificationManager));
        public ObservableCollection<Notification> Notifications;
        public ObservableCollection<Alert> Alerts;
        private readonly Timer _removerTimer;
        internal bool IsClosed { get; private set; }

        internal NotificationManager()
        {
            Notifications = new ObservableCollection<Notification>();
            _removerTimer = new Timer { Interval = 1000 };
            _removerTimer.Elapsed += RemoveAfterTimeoutRunner;
            _removerTimer.Start();
            Alerts = new ObservableCollection<Alert>();
            BlitsMeClientAppContext.CurrentAppContext.LoginManager.LoggedOut += (sender, args) => Reset();
        }

        internal void DeleteNotification(Notification notification)
        {
            lock (Notifications)
            {
                if (Notifications.Contains(notification))
                {
                    if (notification.Message == "TerminateRDP")
                    {
                        BlitsMeClientAppContext.CurrentAppContext.UIManager.StopRemoteConnection();
                    }
                    if (Notifications.Remove(notification))
                    {
                        Logger.Debug("Successfully removed notification [" + notification.ToString() + "]");
                    }
                    else
                    {
                        Logger.Warn("Failed to remove notication [" + notification.ToString() + "]");
                    }
                    if (Notifications.Count == 0)
                    {
                        _removerTimer.Stop();
                    }
                } else
                {
                    Logger.Warn("Cannot remote notification " + notification + ", it doesn't exist.");
                }
            }
        }

        internal void AddNotification(Notification notification)
        {
            notification.Manager = this;
            lock (Notifications)
            {
                /*
                switch (notification.Flag)
                {
                    case "ReceiveFileRequest":
                        BlitsMeClientAppContext.CurrentAppContext.UIManager.ReceiveNotificationChat(notification.Message,notification.Flag);
                        break;
                    case "RDPRequest":
                        BlitsMeClientAppContext.CurrentAppContext.UIManager.ReceiveNotificationChat(notification.Message, notification.Flag);
                        break;
                    case "":
                        Notifications.Add(notification);
                        break;
                }
                 */
                Notifications.Add(notification);
            }
            if (_removerTimer.Enabled == false)
            {
                _removerTimer.Start();
            }
        }

        internal void DeleteAlert(Alert alert)
        {
            lock (Alerts)
            {
                if (Alerts.Remove(alert))
                {
                    Logger.Debug("Successfully removed alert [" + alert.ToString() + "]");
                }
                else
                {
                    Logger.Warn("Failed to remove notication [" + alert.ToString() + "]");
                }
            }
        }

        internal void AddAlert(Alert alert)
        {
            alert.Manager = this;
            lock(Alerts)
            {
                Alerts.Add(alert);
            }
        }

        internal void Close()
        {
            if (!IsClosed)
            {
                Logger.Debug("Closing NotificationManager");
                IsClosed = true;
                _reset();
                if (_removerTimer != null)
                {
                    _removerTimer.Close();
                }
            }
        }

        private void RemoveAfterTimeoutRunner(object sender, ElapsedEventArgs e)
        {
            long nowTime = DateTime.Now.Ticks;
            Notification[] localNotifications;
            lock (Notifications)
            {
                localNotifications = new Notification[Notifications.Count];
                Notifications.CopyTo(localNotifications, 0);
            }

            foreach (Notification notification in localNotifications)
            {
                if (notification.DeleteTimeout > 0)
                {
                    TimeSpan elapsed = new TimeSpan(nowTime - notification.NotifyTime);
                    if (elapsed.TotalSeconds > notification.DeleteTimeout)
                    {
                        this.DeleteNotification(notification);
                    }
                }
            }
        }

        public void Reset()
        {
            Logger.Debug("Resetting Notification Manager, clearing notifications, alerts and timers");
            _reset();
        }

        private void _reset()
        {
            Notifications.Clear();
            Alerts.Clear();
            _removerTimer.Stop();
        }
    }
}
