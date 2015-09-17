using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Gwupe.Agent.Components.Person;
using Gwupe.Agent.UI.WPF.Engage;

namespace Gwupe.Agent.UI.WPF.Roster
{
    /// <summary>
    /// Interaction logic for RosterElement.xaml
    /// </summary>
    public partial class RosterElement : UserControl, INotifyPropertyChanged
    {
        private bool _isCurrent;
        private Attendance _attendance;
        private bool _isUnread;
        private bool _isConversationActive;

        internal long LastActive { private set; get; }

        internal Attendance Attendance
        {
            get { return _attendance; }
            set { _attendance = value; OnPropertyChanged(new PropertyChangedEventArgs("Attendance")); }
        }

        private string ToolTip { get; set; }

        public bool IsCurrent
        {
            get { return _isCurrent; }
            set
            {
                if (value) IsUnread = false;
                _isCurrent = value; OnPropertyChanged(new PropertyChangedEventArgs("IsCurrent"));
            }
        }

        public bool IsOffline
        {
            get { return Attendance == null || Attendance.Presence == null || !Attendance.Presence.IsOnline; }
        }

        public bool IsConversationActive
        {
            get { return _isConversationActive; }
            set
            {
                if (value)
                {
                    LastActive = Environment.TickCount;
                }
                _isConversationActive = value; OnPropertyChanged(new PropertyChangedEventArgs("IsConversationActive"));
            }
        }

        public bool IsUnread
        {
            get { return _isUnread; }
            set { _isUnread = value; OnPropertyChanged(new PropertyChangedEventArgs("IsUnread")); }
        }

        internal RosterElement(Attendance attendance)
        {
            InitializeComponent();
            Attendance = attendance;
            ToolTip = "Chat with " + attendance.Party.Firstname;
            IsCurrent = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, e);
        }
    }
}