using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BlitsMe.Agent.Annotations;
using BlitsMe.Agent.UI.WPF.API;
using log4net;

namespace BlitsMe.Agent.UI.WPF.Search
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : UserControl, IDashboardContentControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SearchWindow));
        private readonly SearchResultControlList _searchResults;
        private readonly BlitsMeClientAppContext _appContext;
        private readonly SearchWindowDataContext _dataContext;

        public SearchWindow(BlitsMeClientAppContext appContext)
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
        private Visibility _searchingVisibility = Visibility.Hidden;

        public Visibility SearchingVisibility
        {
            get { return _searchingVisibility; }
            set { _searchingVisibility = value; OnPropertyChanged("SearchingVisibility"); }
        }

        public SearchWindowDataContext()
        {
            BlitsMeClientAppContext.CurrentAppContext.SearchManager.SearchStart +=
                (sender, args) => { SearchingVisibility = Visibility.Visible; };
            BlitsMeClientAppContext.CurrentAppContext.SearchManager.SearchStop +=
                (sender, args) => { SearchingVisibility = Visibility.Hidden; };
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