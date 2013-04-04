using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using BlitsMe.Agent.Components;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Communication.P2P.RUDP.Tunnel.API;
using Microsoft.Win32;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Engage
{
    /// <summary>
    /// Interaction logic for EngagementWindow.xaml
    /// </summary>
    public partial class EngagementWindow : UserControl
    {
        internal Engagement Engagement { get; set; }
        private readonly BlitsMeClientAppContext _appContext;
        private ChatWindow _chatWindow;
        private static readonly ILog Logger = LogManager.GetLogger(typeof (EngagementWindow));
        private readonly CollectionViewSource _notificationView;

        internal EngagementWindow(BlitsMeClientAppContext appContext, DispatchingCollection<ObservableCollection<Notification>,Notification> notificationList, Engagement engagement)
        {
            InitializeComponent();
            _appContext = appContext;
            Engagement = engagement;
            engagement.PropertyChanged += EngagementOnPropertyChanged;
            try
            {
                ((Components.Functions.RemoteDesktop.Function) Engagement.getFunction("RemoteDesktop")).RDPConnectionAccepted += EngagementOnRDPConnectionAccepted;
                ((Components.Functions.RemoteDesktop.Function) Engagement.getFunction("RemoteDesktop")).RDPConnectionClosed += EngagementOnRDPConnectionClosed;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to link into function RemoteDesktop : " + e.Message,e);
            }
            _notificationView = new CollectionViewSource { Source = notificationList };
            _notificationView.Filter += NotificationFilter;
            Notifications.ItemsSource = _notificationView.View;
            _notificationView.View.Refresh();
            notificationList.CollectionChanged += NotificationListOnCollectionChanged;
            SetTunnelIndicator(Engagement.IncomingTunnel, IncomingTunnelIndicator);
            SetTunnelIndicator(Engagement.OutgoingTunnel, OutgoingTunnelIndicator);
            ShowChat();
        }

        private void EngagementOnRDPConnectionClosed(object sender, EventArgs eventArgs)
        {
            if (Dispatcher.CheckAccess())
                EngagementStatus.Text = "";
            else
            {
                Dispatcher.Invoke(new Action(() => { EngagementStatus.Text = ""; }));
            }
        }

        private void EngagementOnRDPConnectionAccepted(object sender, EventArgs eventArgs)
        {
            string message = Engagement.SecondParty.Name + " is viewing your desktop";
            if (Dispatcher.CheckAccess())
                EngagementStatus.Text = message;
            else
            {
                Dispatcher.Invoke(new Action(() => { EngagementStatus.Text = message; }));
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
            EngagementContent.Content = null;
        }

        private void SendFileButtonClick(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            Nullable<bool> result = fileDialog.ShowDialog(_appContext.UIDashBoard);
            if(result == true)
            {
                string filename = fileDialog.FileName;
                ((Components.Functions.FileSend.Function) Engagement.getFunction("FileSend")).RequestFileSend(filename);
            }
        }

        private void RemoteAssistanceButtonClick(object sender, RoutedEventArgs e)
        {
            // Request is asynchronous, we request and RDP session and then wait, acceptance on the users side will send a request to us
            try
            {
                ((Components.Functions.RemoteDesktop.Function)Engagement.getFunction("RemoteDesktop")).RequestRDPSession();
            } catch(Exception ex)
            {
                Logger.Warn("Failed to get the client : " + ex.Message,ex);
            }
        }

        private void HistoryButtonClick(object sender, RoutedEventArgs e)
        {
            EngagementContent.Content = null;
        }


        private void ShowChat()
        {
            if (_chatWindow == null)
            {
                _chatWindow = new ChatWindow(_appContext, this);
            }
            EngagementContent.Content = _chatWindow;
        }

        private void NotificationFilter(object sender, FilterEventArgs eventArgs)
        {
            Notification notification = eventArgs.Item as Notification;
            if (notification != null && notification.From != null && notification.From.Equals(Engagement.SecondParty.Username))
            {
                eventArgs.Accepted = true;
            }
            else
            {
                eventArgs.Accepted = false;
            }
        }
    }
}