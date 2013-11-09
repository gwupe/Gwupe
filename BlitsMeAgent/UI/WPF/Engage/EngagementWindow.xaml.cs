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
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Alert;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.UI.WPF.API;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using Microsoft.Win32;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Engage
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
        private readonly BlitsMeClientAppContext _appContext;
        private ChatWindow _chatWindow;
        private ContactInfoWindow _contactInfoWindow;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EngagementWindow));
        private readonly CollectionViewSource _notificationView;
        private readonly EngagementWindowDataContext _ewDataContext;
        private Alert _thisAlert;
        internal EngagementVisibleContent EngagementVisibleContent;

        internal EngagementWindow(BlitsMeClientAppContext appContext, DispatchingCollection<ObservableCollection<Notification>, Notification> notificationList, Engagement engagement)
        {
            InitializeComponent();
            _appContext = appContext;
            Engagement = engagement;
            RemoteTerminateButton.Visibility = Visibility.Collapsed;
            engagement.PropertyChanged += EngagementOnPropertyChanged;
            try
            {
                ((Components.Functions.RemoteDesktop.Function)Engagement.GetFunction("RemoteDesktop")).RDPConnectionAccepted += EngagementOnRDPConnectionAccepted;
                ((Components.Functions.RemoteDesktop.Function)Engagement.GetFunction("RemoteDesktop")).RDPConnectionClosed += EngagementOnRDPConnectionClosed;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to link into function RemoteDesktop : " + e.Message, e);
            }
            _notificationView = new CollectionViewSource { Source = notificationList };
            _notificationView.Filter += NotificationFilter;
            _notificationView.View.Refresh();
            notificationList.CollectionChanged += NotificationListOnCollectionChanged;
            SetTunnelIndicator(Engagement.IncomingTunnel, IncomingTunnelIndicator);
            SetTunnelIndicator(Engagement.OutgoingTunnel, OutgoingTunnelIndicator);
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
                if (Dispatcher.CheckAccess())
                {
                    var color = (Color)ColorConverter.ConvertFromString("#B9CDE5");
                    MainLayout.Background = new SolidColorBrush(color);
                    RemoteAssistanceButton.Visibility = Visibility.Visible;
                    RemoteTerminateButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Dispatcher.Invoke(new Action(() =>
                                                     {
                                                         var color = (Color)ColorConverter.ConvertFromString("#B9CDE5");
                                                         MainLayout.Background = new SolidColorBrush(color);
                                                         RemoteAssistanceButton.Visibility = Visibility.Visible;
                                                         RemoteTerminateButton.Visibility = Visibility.Collapsed;
                                                     }));
                }
            }
        }

        private void EngagementOnRDPConnectionAccepted(object sender, EventArgs eventArgs)
        {
            _thisAlert = new Alert() { Message = Engagement.SecondParty.Person.Firstname + " is Connected" };
            _appContext.NotificationManager.AddAlert(_thisAlert);
            if (Dispatcher.CheckAccess())
            {
                MainLayout.Background = new SolidColorBrush(Colors.Red);
                //_chatWindow.BubbleCover.Background = new SolidColorBrush(Colors.Red);
                // KickOffButton.Visibility = Visibility.Visible;
                if(this.Engagement.IsRemoteControlActive)
                {
                    RemoteAssistanceButton.Visibility = Visibility.Collapsed;
                    RemoteTerminateButton.Visibility = Visibility.Visible;
                }
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                    {
                        MainLayout.Background = new SolidColorBrush(Colors.Red);
                        //_chatWindow.BubbleCover.Background = new SolidColorBrush(Colors.Red);
                        //KickOffButton.Visibility = Visibility.Visible; ;
                        if (this.Engagement.IsRemoteControlActive)
                        {
                            RemoteAssistanceButton.Visibility = Visibility.Collapsed;
                            RemoteTerminateButton.Visibility = Visibility.Visible;
                        }

                    }));
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
                    SetTunnelIndicator(Engagement.OutgoingTunnel, OutgoingTunnelIndicator);
                }
                else if (propertyChangedEventArgs.PropertyName.Equals("IncomingTunnel"))
                {
                    SetTunnelIndicator(Engagement.IncomingTunnel, IncomingTunnelIndicator);
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

        private void ChatButtonClick(object sender, RoutedEventArgs e)
        {
            ShowChat();
        }

        private void ScorecardButtonClick(object sender, RoutedEventArgs e)
        {
            if (_contactInfoWindow == null)
            {
                _contactInfoWindow = new ContactInfoWindow();
                _contactInfoWindow.DataContext = Engagement.SecondParty;
            }
            EngagementContent.Content = _contactInfoWindow;
            EngagementVisibleContent = EngagementVisibleContent.Scorecard;
        }

        private void SendFileButtonClick(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            Nullable<bool> result = fileDialog.ShowDialog(_appContext.UIManager.CurrentWindow);
            if (result == true)
            {
                string filename = fileDialog.FileName;
                ((Components.Functions.FileSend.Function)Engagement.GetFunction("FileSend")).RequestFileSend(filename);
            }
        }

        private void RemoteAssistanceButtonClick(object sender, RoutedEventArgs e)
        {
            // Request is asynchronous, we request and RDP session and then wait, acceptance on the users side will send a request to us
            try
            {
                ((Components.Functions.RemoteDesktop.Function)Engagement.GetFunction("RemoteDesktop")).RequestRDPSession(this, Engagement);
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to get the client : " + ex.Message, ex);
            }
        }

        private void HistoryButtonClick(object sender, RoutedEventArgs e)
        {
            EngagementContent.Content = null;
        }

        internal void ShowChat()
        {
            BlitsMeClientAppContext.CurrentAppContext.UIManager.GetEngagement(Engagement, this);
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
            Thread thread = new Thread(((Components.Functions.RemoteDesktop.Function)Engagement.GetFunction("RemoteDesktop")).Server.Close) { IsBackground = true };
            thread.Start(); 
            if (this.Engagement.IsRemoteControlActive)
            {
                RemoteAssistanceButton.Visibility = Visibility.Collapsed;
                RemoteTerminateButton.Visibility = Visibility.Visible;
            }
            else
            {
                RemoteAssistanceButton.Visibility = Visibility.Visible;
                RemoteTerminateButton.Visibility = Visibility.Collapsed;
            }
        }

        //public void StopRDPConnection()
        //{
        //    if (Engagement != null)
        //    {
        //        Thread thread =
        //            new Thread(
        //                ((Components.Functions.RemoteDesktop.Function)Engagement.GetFunction("RemoteDesktop")).Server.
        //                    Close) { IsBackground = true };
        //        thread.Start();
        //        if (!this.Engagement.SecondParty.IsRemoteControlActive)
        //        {
        //            RemoteAssistanceButton.Visibility = Visibility.Collapsed;
        //            RemoteTerminateButton.Visibility = Visibility.Visible;
        //        }
        //    }
        //}

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
    }

    internal class EngagementWindowDataContext
    {
        private readonly BlitsMeClientAppContext _appContext;
        public Attendance SecondParty { get; private set; }
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EngagementWindowDataContext));

        public Engagement Engagement { get; private set; }

        public EngagementWindowDataContext(BlitsMeClientAppContext appContext, Engagement engagement)
        {
            _appContext = appContext;
            Engagement = engagement;
            SecondParty = Engagement.SecondParty;
        }
    }
}