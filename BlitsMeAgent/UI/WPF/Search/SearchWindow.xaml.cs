using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Gwupe.Agent.Annotations;
using Gwupe.Agent.UI.WPF.API;
using log4net;
using log4net.Repository.Hierarchy;

namespace Gwupe.Agent.UI.WPF.Search
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : UserControl, IDashboardContentControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SearchWindow));
        private readonly SearchResultControlList _searchResults;
        private readonly GwupeClientAppContext _appContext;
        private readonly SearchWindowDataContext _dataContext;

        public SearchWindow(GwupeClientAppContext appContext)
        {
            this.InitializeComponent();
            _appContext = appContext;
            _searchResults = new SearchResultControlList(_appContext, Dispatcher);
            try
            {
                _searchResults.SetList(_appContext.SearchManager.SearchResults, "Username");
            }
            catch (Exception e)
            {
                Logger.Error("Failed to set the list : " + e.Message, e);
            }
            SearchResults.ItemsSource = _searchResults.List;
            _dataContext = new SearchWindowDataContext();
            DataContext = _dataContext;
        }

        public void SetAsMain(Dashboard dashboard)
        {

        }
    }

    public class SearchWindowDataContext : INotifyPropertyChanged
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (SearchWindowDataContext));
        private Visibility _searchingVisibility = Visibility.Hidden;

        public Visibility SearchingVisibility
        {
            get { return _searchingVisibility; }
            set { _searchingVisibility = value; OnPropertyChanged("SearchingVisibility"); }
        }

        public SearchWindowDataContext()
        {
            GwupeClientAppContext.CurrentAppContext.SearchManager.SearchStart +=
                OnSearchManagerOnSearchStart;
            GwupeClientAppContext.CurrentAppContext.SearchManager.SearchStop +=
                OnSearchManagerOnSearchStop;
        }

        private void OnSearchManagerOnSearchStop(object sender, EventArgs args)
        {
            Logger.Debug("Showing not searching");
            SearchingVisibility = Visibility.Hidden;
        }

        private void OnSearchManagerOnSearchStart(object sender, EventArgs args)
        {
            Logger.Debug("Showing searching");
            SearchingVisibility = Visibility.Visible;
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