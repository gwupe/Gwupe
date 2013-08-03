using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using log4net;

namespace BlitsMe.Agent.UI.WPF
{
    /// <summary>
    /// Interaction logic for UpdateNotification.xaml
    /// </summary>
    public partial class UpdateNotification : Window
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (UpdateNotification));
        private readonly BlitsMeClientAppContext _appContext;
        public ObservableCollection<String> Changes { get { return _appContext.ChangeLog; } }
        public String Version { get { return _appContext.Version(2); } }
        public String Description { get { return _appContext.ChangeDescription; } }
        internal Thread UiThread;
        internal Boolean IsClosed = false;

        public UpdateNotification(BlitsMeClientAppContext appContext)
        {
            this.InitializeComponent();
            UiThread = Thread.CurrentThread;
            _appContext = appContext;
            DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            if (_appContext.LoginManager.IsLoggedIn)
            {
                _appContext.UIDashBoard.Show();
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
            IsClosed = true;
            Dispatcher.InvokeShutdown();
        }

    }
}