using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Gwupe.Agent.Annotations;
using Gwupe.Agent.UI.WPF.API;
using log4net;

namespace Gwupe.Agent.UI.WPF.Team
{
    /// <summary>
    /// Interaction logic for TeamManagement.xaml
    /// </summary>
    public partial class TeamManagement : IDashboardContentControl, IGwupeUserControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TeamManagement));
        private TeamManagementData _dataContext;
        private SignupTeam _signupForm;
        internal DispatchingCollection<ObservableCollection<Components.Person.Team>, Components.Person.Team> Teams;

        public TeamManagement()
        {
            this.InitializeComponent();
            Teams = new DispatchingCollection<ObservableCollection<Components.Person.Team>, Components.Person.Team>(GwupeClientAppContext.CurrentAppContext.TeamManager.Teams, Dispatcher);
            _dataContext = new TeamManagementData(this);
            DataContext = _dataContext;
        }

        public void SetAsMain(Dashboard dashboard)
        {

        }

        public void TeamSelected(object sender, RoutedEventArgs e)
        {
            _dataContext.Content = new TeamSettingsControl();
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Logger.Debug("Adding new Team");
            //GwupeClientAppContext.CurrentAppContext.TeamManager.Teams.Add(new Team() { Name = "Darren" });
            if (_signupForm == null)
            {
                _signupForm = new SignupTeam(Disabler);
                _signupForm.CommitCancelled += (o, args) => ClearContent();
            }
            _signupForm.Reset();
            _dataContext.Content = _signupForm;

        }

        private void ClearContent()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(ClearContent));
            }
            else
            {
                _dataContext.Content = null;
            }
        }
    }

    public class TeamManagementData : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TeamManagementData));
        public Visibility AddButtonVisibility { get { return Teams.Count < 50 ? Visibility.Visible : Visibility.Collapsed; } }
        public DispatchingCollection<ObservableCollection<Components.Person.Team>,Components.Person.Team> Teams { get; private set; }

        private readonly TeamManagement _teamManagement;
        private UserControl _content;

        public UserControl Content
        {
            get { return _content; }
            set
            {
                if (Equals(value, _content)) return;
                _content = value;
                OnPropertyChanged(nameof(Content));
            }
        }

        public TeamManagementData(TeamManagement teamManagement)
        {
            _teamManagement = teamManagement;
            Teams = teamManagement.Teams;
            Teams.CollectionChanged += TeamsOnCollectionChanged;
        }

        private void TeamsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            Logger.Debug("Teams Collection Changed");
            OnPropertyChanged(nameof(Teams));
            OnPropertyChanged(nameof(AddButtonVisibility));
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