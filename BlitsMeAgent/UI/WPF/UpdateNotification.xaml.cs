using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using Gwupe.Agent.Annotations;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for UpdateNotification.xaml
    /// </summary>
    public partial class UpdateNotification : Window
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (UpdateNotification));
        private readonly GwupeClientAppContext _appContext;


        internal Thread UiThread;
        internal Boolean IsClosed = false;

        public UpdateNotification()
        {
            this.InitializeComponent();
            UiThread = Thread.CurrentThread;
            _appContext = GwupeClientAppContext.CurrentAppContext;
            DataContext = new UpdateNotificationData();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            if (_appContext.LoginManager.IsLoggedIn)
            {
                _appContext.UIManager.Show();
            }
        }

        public new void Show()
        {
            if (Dispatcher.CheckAccess())
            {
                Logger.Debug("Showing UpdateNotification");
                base.Show();
                Activate();
                Topmost = true;
            } else
            {
                Dispatcher.Invoke(new Action(Show));
            }
        }

        public new void Close()
        {
            if (!IsClosed)
            {
                Logger.Debug("Closing Update Notification");
                IsClosed = true;
                Dispatcher.InvokeShutdown();
            }
        }

    }

    public class UpdateNotificationData : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (UpdateNotificationData));

        public ObservableCollection<String> Changes { get { return GwupeClientAppContext.CurrentAppContext.ChangeLog; } }

        public String Version
        {
            get
            {
#if DEBUG
                return GwupeClientAppContext.CurrentAppContext.Version(2);
#else
                return GwupeClientAppContext.CurrentAppContext.Version(1);
#endif
            }
        }

        public bool? NoUpdateNotifications
        {
            get
            {
                try
                {
                    return !GwupeClientAppContext.CurrentAppContext.Reg.NotifyUpdate;
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to get NoUpdateNotifications Setting : ",e);
                }
                return null;
            }
            set
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        GwupeClientAppContext.CurrentAppContext.Reg.NotifyUpdate = value == false;
                        OnPropertyChanged("NoUpdateNotifications");

                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to set registry variable for NoUpdateNotifications", ex);
                    }
                });

            }
        }
        public String Description { get { return GwupeClientAppContext.CurrentAppContext.ChangeDescription; } }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}