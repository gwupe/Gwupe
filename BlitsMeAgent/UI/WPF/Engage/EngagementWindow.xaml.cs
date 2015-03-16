using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Gwupe.Agent.Annotations;
using Gwupe.Agent.Components;
using Gwupe.Agent.Components.Alert;
using Gwupe.Agent.Components.Notification;
using Gwupe.Agent.Components.Person;
using Gwupe.Agent.Managers;
using Gwupe.Agent.UI.WPF.API;
using Gwupe.Communication.P2P.RUDP.Tunnel.API;
using Microsoft.Win32;
using log4net;

namespace Gwupe.Agent.UI.WPF.Engage
{
    public enum EngagementVisibleContent
    {
        Chat,
        Scorecard
    };

    /// <summary>
    /// Interaction logic for EngagementWindow.xaml
    /// </summary>
    public partial class EngagementWindow : UserControl, IDashboardContentControl
    {
        internal Engagement Engagement { get; set; }
        private readonly GwupeClientAppContext _appContext;
        private ChatWindow _chatWindow;
        //private ContactInfoControl _contactInfoControl;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EngagementWindow));
        private readonly CollectionViewSource _notificationView;
        private readonly EngagementWindowDataContext _ewDataContext;
        private Alert _thisAlert;
        internal EngagementVisibleContent EngagementVisibleContent;
        private ContactSettings _contactSettings;

        internal EngagementWindow(GwupeClientAppContext appContext, DispatchingCollection<ObservableCollection<Notification>, Notification> notificationList, Engagement engagement)
        {
            InitializeComponent();
            _appContext = appContext;
            Engagement = engagement;
            engagement.PropertyChanged += EngagementOnPropertyChanged;
            try
            {
                ((Components.Functions.RemoteDesktop.Function)Engagement.GetFunction("RemoteDesktop")).Server.ServerConnectionOpened += EngagementOnRDPConnectionAccepted;
                ((Components.Functions.RemoteDesktop.Function)Engagement.GetFunction("RemoteDesktop")).Server.ServerConnectionClosed += EngagementOnRDPConnectionClosed;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to link into function RemoteDesktop : " + e.Message, e);
            }
            _notificationView = new CollectionViewSource { Source = notificationList };
            _notificationView.Filter += NotificationFilter;
            _notificationView.View.Refresh();
            notificationList.CollectionChanged += NotificationListOnCollectionChanged;
            //SetTunnelIndicator(Engagement.IncomingTunnel, IncomingTunnelIndicator);
            //SetTunnelIndicator(Engagement.OutgoingTunnel, OutgoingTunnelIndicator);
            ShowChat();
            _ewDataContext = new EngagementWindowDataContext(_appContext, engagement);
            DataContext = _ewDataContext;
        }

        public EngagementWindow()
        {

        }

        private void EngagementOnRDPConnectionClosed(object sender, EventArgs eventArgs)
        {
            if (_thisAlert != null)
            {
                _appContext.NotificationManager.DeleteAlert(_thisAlert);
            }
            if (Dispatcher.CheckAccess())
            {
                IndicateRdpConnection();
            }
            else
            {
                Dispatcher.Invoke(new Action(IndicateRdpConnection));
            }
        }

        private void EngagementOnRDPConnectionAccepted(object sender, EventArgs eventArgs)
        {
            _thisAlert = new Alert() { Message = Engagement.SecondParty.Person.Firstname + " is Connected", ClickCommand =
                () => _appContext.UIManager.Dashboard.ActivateEngagement(Engagement.SecondParty)
            };
            _appContext.NotificationManager.AddAlert(_thisAlert);
            if (Dispatcher.CheckAccess())
            {
                IndicateRdpConnection();
            }
            else
            {
                Dispatcher.Invoke(new Action(IndicateRdpConnection));
            }
        }

        private void IndicateRdpConnection()
        {
            if (!((Components.Functions.RemoteDesktop.Function)Engagement.GetFunction("RemoteDesktop")).Server.Closed)
            {
                MainLayout.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A7EBB"));
                RemoteAssistanceButton.Visibility = Visibility.Collapsed;
                RemoteTerminateButton.Visibility = Visibility.Visible;
            }
            else
            {
                MainLayout.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B9CDE5"));
                RemoteAssistanceButton.Visibility = Visibility.Visible;
                RemoteTerminateButton.Visibility = Visibility.Collapsed;
            }
        }

        private void NotificationListOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            // Multithreaded handling, make sure only dispatcher updates UI objects
            if (Dispatcher.CheckAccess())
                _notificationView.View.Refresh();
            else
                Dispatcher.Invoke(new Action(() => _notificationView.View.Refresh()));
        }

        private void EngagementOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            // Only run event handler if the dispatcher is not shutting down.
            if (!Dispatcher.HasShutdownStarted)
            {
                if (propertyChangedEventArgs.PropertyName.Equals("OutgoingTunnel"))
                {
                    //SetTunnelIndicator(Engagement.OutgoingTunnel, OutgoingTunnelIndicator);
                }
                else if (propertyChangedEventArgs.PropertyName.Equals("IncomingTunnel"))
                {
                    //SetTunnelIndicator(Engagement.IncomingTunnel, IncomingTunnelIndicator);
                }
            }
        }

        private void SetTunnelIndicator(IUDPTunnel tunnel, Shape tunnelIndicator)
        {
            if (tunnel != null && tunnel.IsTunnelEstablished)
            {
                if (Dispatcher.CheckAccess())
                {
                    tunnelIndicator.Fill = Brushes.GreenYellow;
                }
                else
                {
                    Dispatcher.Invoke(new Action(() => tunnelIndicator.Fill = Brushes.GreenYellow));
                }
            }
            else
            {
                if (Dispatcher.CheckAccess())
                {
                    tunnelIndicator.Fill = Brushes.DarkGray;
                }
                else
                {
                    Dispatcher.Invoke(new Action(() => tunnelIndicator.Fill = Brushes.DarkGray));
                }
            }
        }

        private void SendFileButtonClick(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            Nullable<bool> result = fileDialog.ShowDialog(_appContext.UIManager.Dashboard);
            if (result == true)
            {
                string filename = fileDialog.FileName;
                ((Components.Functions.FileSend.Function)Engagement.GetFunction("FileSend")).RequestFileSend(
                    filename);
            }
        }

        private void RemoteAssistanceButtonClick(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                if (_appContext.CurrentUserManager.CurrentUser.Guest)
                {
                    _appContext.UIManager.PromptGuestSignup();
                }
                else
                {

                    try
                    {
                        ((Components.Functions.RemoteDesktop.Function)Engagement.GetFunction("RemoteDesktop"))
                            .RequestRdpSession();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("Failed to get the client : " + ex.Message, ex);
                    }
                }
            });

        }

        private void HistoryButtonClick(object sender, RoutedEventArgs e)
        {
            EngagementContent.Content = null;
        }

        internal void ShowChat()
        {
            //GwupeClientAppContext.CurrentAppContext.UIManager.GetEngagement(Engagement, this);
            _chatWindow = _chatWindow ?? new ChatWindow(_appContext, this) { Notifications = { ItemsSource = _notificationView.View } };
            EngagementContent.Content = _chatWindow;
            Engagement.IsUnread = false;
            EngagementVisibleContent = EngagementVisibleContent.Chat;
        }

        private void NotificationFilter(object sender, FilterEventArgs eventArgs)
        {
            Notification notification = eventArgs.Item as Notification;
            if (notification != null && notification.AssociatedUsername != null && notification.AssociatedUsername.Equals(Engagement.SecondParty.Person.Username))
            {
                eventArgs.Accepted = true;
            }
            else
            {
                eventArgs.Accepted = false;
            }
        }

        public void SetAsMain(Dashboard dashboard)
        {
            if (dashboard.ActiveContent.Content == this)
                if (_chatWindow != null && EngagementContent.Content == _chatWindow)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                    {
                        _chatWindow.messageBox.Focus();
                        Keyboard.Focus(_chatWindow.messageBox);
                    }));
                }
        }

        private void KickOffButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(
                state =>
                    ((Components.Functions.RemoteDesktop.Function)Engagement.GetFunction("RemoteDesktop")).Server.Close());
        }

        private bool _isRemoteControlActive = false;
        public bool IsRemoteControlActive
        {
            get
            {
                return _isRemoteControlActive;
            }
            set
            {
                if ((_isRemoteControlActive != value) && (_isRemoteControlActive == null || !_isRemoteControlActive.Equals(value)))
                {
                    _isRemoteControlActive = value;
                }
            }
        }

        private void ContactSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_appContext.CurrentUserManager.CurrentUser.Guest)
            {
                ThreadPool.QueueUserWorkItem(state => _appContext.UIManager.PromptGuestSignup());
            }
            else if (Engagement.SecondParty.Person.Guest)
            {
                ThreadPool.QueueUserWorkItem(state => _appContext.UIManager.Alert("This user is a guest, for to get unattended access to their desktop and for other features, they need to be logged in."));
            } else
            {
                if (_ewDataContext.ContactSettings == null)
                {
                    _ewDataContext.ContactSettings = new ContactSettings(_ewDataContext);
                }
                ThreadPool.QueueUserWorkItem(state =>
                {
                    if (_ewDataContext.ContactSettings.PresentModal())
                    {

                    } /*
                    if (_appContext.ConnectionManager.IsOnline())
                    {
                        _ewDataContext.ContactSettingsEnabled = true;
                    }*/

                });
            }
        }

    }

    internal class EngagementWindowDataContext : INotifyPropertyChanged
    {
        private readonly GwupeClientAppContext _appContext;
        private bool _contactSettingsEnabled;
        private ContactSettings _contactSettings;
        public Attendance SecondParty { get; private set; }
        public String Name { get { return SecondParty.Person.Name; } }
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EngagementWindowDataContext));

        public Engagement Engagement { get; private set; }

        public bool ContactSettingsEnabled
        {
            get { return _contactSettingsEnabled; }
            set { _contactSettingsEnabled = value; OnPropertyChanged("ContactSettingsEnabled"); }
        }

        public ContactSettings ContactSettings
        {
            get;
            set;
        }

        public EngagementWindowDataContext(GwupeClientAppContext appContext, Engagement engagement)
        {
            _appContext = appContext;
            Engagement = engagement;
            SecondParty = Engagement.SecondParty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}