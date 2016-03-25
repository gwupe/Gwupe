using System;
using System.Windows.Controls;
using System.Windows.Input;
using Gwupe.Agent.Components.Person;
using Gwupe.Agent.Components.Search;
using Gwupe.Agent.Managers;
using Gwupe.Agent.UI.WPF.Engage;
using log4net;

namespace Gwupe.Agent.UI.WPF.Search
{
    /// <summary>
    /// Interaction logic for SearchResultControl.xaml
    /// </summary>
    public partial class SearchResultControl : UserControl
    {
        private readonly GwupeClientAppContext _appContext;
        private SearchResult SearchResult { get; set; }

        public SearchResultControl(GwupeClientAppContext appContext, SearchResult sourceObject)
        {
            InitializeComponent();
            _appContext = appContext;
            SearchResult = sourceObject;
            DataContext = SearchResult;
            AddPersonButton.Command = new AddPerson(_appContext, SearchResult.Person);
        }

        private void MessagePersonButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            
        }

        private void ChatPersonButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GwupeClientAppContext.CurrentAppContext.UIManager.Dashboard.ActivateEngagement(SearchResult.Username);
        }
    }

    // Command which send messages
    public class AddPerson : ICommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddPerson));
        private readonly GwupeClientAppContext _appContext;
        private readonly Person _person;

        internal AddPerson(GwupeClientAppContext appContext, Person person)
        {
            _appContext = appContext;
            _person = person;
        }

        public void Execute(object parameter)
        {
            _appContext.RosterManager.SubscribePerson(_person);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
