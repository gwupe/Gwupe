using System;
using System.Windows.Controls;
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
        }

        public void SetAsMain(Dashboard dashboard)
        {

        }

        private void btnPrev_Click(object sender, System.Windows.RoutedEventArgs e)
        {
           
        }

        private void btnNext_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}