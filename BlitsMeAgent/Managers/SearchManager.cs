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
        private readonly BlitsMeClientAppContext _appContext;
        internal ObservableCollection<SearchResult> SearchResults;
        private readonly Object _listWriteLock = new object();
        internal bool IsClosed { get; private set; }
        public event EventHandler SearchStart;

        protected virtual void OnSearchStart()
        {
            EventHandler handler = SearchStart;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public event EventHandler SearchStop;

        protected virtual void OnSearchStop()
        {
            EventHandler handler = SearchStop;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public SearchManager()
        {
            _appContext = BlitsMeClientAppContext.CurrentAppContext;
            SearchResults = new ObservableCollection<SearchResult>();
            _appContext.LoginManager.LoggedOut += (sender, args) => Reset();
        }

        internal void Search(String search)
        {
            SearchRq request = new SearchRq() { query = search, pageSize = 20 };
            if (_appContext.ConnectionManager.IsOnline())
            {
                OnSearchStart();
                _appContext.ConnectionManager.Connection.RequestAsync<SearchRq,SearchRs>(request, SearchResponseHandler);
            } else
            {
                Logger.Warn("Cannot search, not online");
            }
        }

        private void SearchResponseHandler(SearchRq request, SearchRs response, Exception e)
        {
            OnSearchStop();
            if (e == null)
            {
                // populate the list
                lock (_listWriteLock)
                {
                    SearchResults.Clear();
                    if(response.results != null)
                    {
                        foreach (var resultElement in response.results)
                        {
                            var searchResult = new SearchResult(resultElement);
                            SearchResults.Add(searchResult);
                            if (resultElement.user.hasAvatar)
                            {
                                try
                                {
                                    _appContext.ConnectionManager.Connection.RequestAsync<VCardRq, VCardRs>(
                                        new VCardRq(resultElement.user.user),
                                        delegate(VCardRq rq, VCardRs rs, Exception arg3)
                                            { ResponseHandler(rq, rs, arg3, searchResult); });
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("Failed to get the vcard for " + resultElement.user.user, ex);
                                }
                            }
                        }
                    }
                }
            } else
            {
                Logger.Error("Search returned an error : " + e.Message,e);
            }
        }

        private void ResponseHandler(VCardRq vCardRq, VCardRs vCardRs, Exception e, SearchResult searchResult)
        {
            if (e == null)
            {
                if (!String.IsNullOrWhiteSpace(vCardRs.userElement.avatarData))
                {
                    try
                    {
                        searchResult.Person.SetAvatarData(vCardRs.userElement.avatarData);
                    }
                    catch (Exception e1)
                    {
                        Logger.Error("Failed to set avatar data for " + vCardRq.username, e);
                    }
                }
            }
            else
            {
                Logger.Error("Failed to get vcard for " + vCardRq.username, e);
            }
        }

        internal void Close()
        {
            if (!IsClosed)
            {
                Logger.Debug("Closing SearchManager");
                IsClosed = true;
            }
        }

        internal void Reset()
        {
            Logger.Debug("Resetting Search Manager, clearing results");
            SearchResults.Clear();
        }
    }
}
