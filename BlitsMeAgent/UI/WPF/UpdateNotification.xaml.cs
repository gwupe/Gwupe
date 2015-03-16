using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using log4net;

namespace Gwupe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for UpdateNotification.xaml
    /// </summary>
    public partial class UpdateNotification : Window
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (UpdateNotification));
        private readonly GwupeClientAppContext _appContext;
        public ObservableCollection<String> Changes { get { return _appContext.ChangeLog; } }
        public String Version { get { return _appContext.Version(2); } }
        public String Description { get { return _appContext.ChangeDescription; } }
        internal Thread UiThread;
        internal Boolean IsClosed = false;

        public UpdateNotification()
        {
            this.InitializeComponent();
            UiThread = Thread.CurrentThread;
            _appContext = GwupeClientAppContext.CurrentAppContext;
            DataContext = this;
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
}