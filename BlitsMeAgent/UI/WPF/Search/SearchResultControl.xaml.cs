using System;
using System.Windows.Controls;
using System.Windows.Input;
using BlitsMe.Agent.Components.Chat;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.Components.Search;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Search
{
    /// <summary>
    /// Interaction logic for SearchResultControl.xaml
    /// </summary>
    public partial class SearchResultControl : UserControl
    {
        private readonly BlitsMeClientAppContext _appContext;
        private SearchResult SearchResult { get; set; }

        public SearchResultControl(BlitsMeClientAppContext appContext, SearchResult sourceObject)
        {
            InitializeComponent();
            _appContext = appContext;
            SearchResult = sourceObject;
            DataContext = SearchResult;
            AddPersonButton.Command = new AddPerson(_appContext, SearchResult.Person);
        }
    }

    // Command which send messages
    public class AddPerson : ICommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddPerson));
        private readonly BlitsMeClientAppContext _appContext;
        private readonly Person _person;

        internal AddPerson(BlitsMeClientAppContext appContext, Person person)
        {
            _appContext = appContext;
            _person = person;
        }

        public void Execute(object parameter)
        {
            _appContext.RosterManager.AddPerson(_person);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
