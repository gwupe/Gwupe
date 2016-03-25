using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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
        private TeamSettingsControl _teamSettings;
        private UiHelper _uiHelper;
        internal DispatchingCollection<ObservableCollection<Components.Person.Team>, Components.Person.Team> Teams;

        public TeamManagement()
        {
            this.InitializeComponent();
            Teams = new DispatchingCollection<ObservableCollection<Components.Person.Team>, Components.Person.Team>(GwupeClientAppContext.CurrentAppContext.TeamManager.Teams, Dispatcher);
            _dataContext = new TeamManagementData(this);
            _uiHelper = new UiHelper(Dispatcher,Disabler,null,null);
            TeamList.ItemContainerStyle.Setters.Add(new EventSetter(ListBoxItem.SelectedEvent, new RoutedEventHandler(TeamSelected)));
            DataContext = _dataContext;
            GwupeClientAppContext.CurrentAppContext.TeamManager.Teams.CollectionChanged += TeamsOnCollectionChanged;
        }

        private void TeamsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            var teamSettings = _dataContext.Content as TeamSettingsControl;
            if (teamSettings != null)
            {
                // If single throws an exception, then it doesn't exist
                try
                {
                    GwupeClientAppContext.CurrentAppContext.TeamManager.Teams.Single(chooseTeam => chooseTeam.Username.Equals(teamSettings.Team.Username));
                }
                catch (Exception)
                {
                    Logger.Debug("Team " + teamSettings.Team.Username +
                                 " is no longer in our list, closing settings control.");
                    // this team is no longer active
                    _dataContext.Content = null;
                }
            }
        }

        public void RetreiveTeams()
        {
            // We Need to remove this team from our list
            _uiHelper.Disabler.DisableInputs(true, "Refreshing");
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    GwupeClientAppContext.CurrentAppContext.TeamManager.RetrieveTeams();
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to retreive the team list", e);
                }
                finally
                {
                    _uiHelper.Disabler.DisableInputs(false);
                }
            });

        }

        public void SetAsMain(Dashboard dashboard)
        {

        }

        public void TeamSelected(object sender, RoutedEventArgs e)
        {
            ListBoxItem item = sender as ListBoxItem;
            if (item != null)
            {
                SelectTeam(item.DataContext as Components.Person.Team);
            }
        }

        public void SelectTeam(String uniqueHandle)
        {
            try
            {
                SelectTeam(GwupeClientAppContext.CurrentAppContext.TeamManager.GetTeamByUniqueHandle(uniqueHandle));
            }
            catch (Exception e)
            {
                Logger.Error("Error activating new team", e);
                ClearContent();
            }

        }

        public void SelectTeam(Components.Person.Team team)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => SelectTeam(team)));
                return;
            }
            if (_teamSettings == null)
            {
                _teamSettings = new TeamSettingsControl(Disabler, this);
            }
            _teamSettings.Team = team;
            _dataContext.Content = _teamSettings;
            TeamList.SelectedItem = team;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Logger.Debug("Adding new Team");
            if (_signupForm == null)
            {
                _signupForm = new SignupTeam(Disabler, this);
                _signupForm.CommitCancelled += (o, args) => ClearContent();
            }
            _signupForm.Reset();
            TeamList.SelectedItem = null;
            _dataContext.Content = _signupForm;

        }

        internal void ClearContent()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(ClearContent));
            }
            else
            {
                _dataContext.Content = null;
                TeamList.SelectedIndex = -1;
            }
        }
    }

    public class TeamManagementData : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TeamManagementData));
        public Visibility AddButtonVisibility { get { return Teams.Count < 50 ? Visibility.Visible : Visibility.Collapsed; } }
        public DispatchingCollection<ObservableCollection<Components.Person.Team>, Components.Person.Team> Teams { get; private set; }

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