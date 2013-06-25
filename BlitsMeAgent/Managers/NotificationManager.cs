using System;
using System.Collections.ObjectModel;
using System.Timers;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Alert;
using BlitsMe.Agent.Components.Notification;
using log4net;

namespace BlitsMe.Agent.Managers
{
    class NotificationManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(NotificationManager));
        private readonly BlitsMeClientAppContext _appContext;
        public ObservableCollection<Notification> Notifications;
        public ObservableCollection<Alert> Alerts;
        private readonly System.Timers.Timer _removerTimer;

        internal NotificationManager(BlitsMeClientAppContext appContext)
        {
            this._appContext = appContext;
            Notifications = new ObservableCollection<Notification>();
            _removerTimer = new System.Timers.Timer { Interval = 1000 };
            _removerTimer.Elapsed += RemoveAfterTimeoutRunner;
            _removerTimer.Start();
            Alerts = new ObservableCollection<Alert>();
        }

        internal void DeleteNotification(Notification notification)
        {
            lock (Notifications)
            {
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
            }
        }

        internal void AddNotification(Notification notification)
        {
            notification.Manager = this;
            lock (Notifications)
            {
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
            if (_removerTimer != null)
            {
                _removerTimer.Close();
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
            Notifications.Clear();
            Alerts.Clear();
            _removerTimer.Stop();
        }
    }
}
