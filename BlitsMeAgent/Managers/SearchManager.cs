using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Components.Person;
using BlitsMe.Agent.Components.Search;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Elements;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Managers
{
    internal class SearchManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (SearchManager));
        private BlitsMeClientAppContext _appContext;
        internal ObservableCollection<SearchResult> SearchResults;
        private Object _listWriteLock = new object();

        public SearchManager(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
            SearchResults = new ObservableCollection<SearchResult>();
        }

        internal void Search(String search)
        {
            SearchRq request = new SearchRq() { query = search };
            if (_appContext.ConnectionManager.IsOnline())
            {
                _appContext.ConnectionManager.Connection.RequestAsync(request, ResponseHandler);
            } else
            {
                Logger.Warn("Cannot search, not online");
            }
        }

        private void ResponseHandler(Request req, Response res)
        {
            if (res.isValid())
            {
                SearchRs response = (SearchRs) res;
                SearchRq request = (SearchRq) req;
                // populate the list
                lock (_listWriteLock)
                {
                    SearchResults.Clear();
                    if(response.results != null)
                    {
                        foreach (var resultElement in response.results)
                        {
                            SearchResults.Add(new SearchResult(resultElement));
                        }
                    }
                }
            } else
            {
                Logger.Error("Search returned an error : " + res.errorMessage);
            }
        }


        internal void Close()
        {

        }
    }
}
