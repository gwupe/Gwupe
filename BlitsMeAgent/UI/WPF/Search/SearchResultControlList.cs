using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using Gwupe.Agent.Components.Search;
using Gwupe.Agent.UI.WPF.Engage;
using Gwupe.Agent.UI.WPF.Utils;

namespace Gwupe.Agent.UI.WPF.Search
{
    class SearchResultControlList : ObservableListMirror<SearchResult,SearchResultControl>
    {
        private GwupeClientAppContext _appContext;
        private SearchResult _sourceObject;

        public SearchResultControlList(GwupeClientAppContext appContext, Dispatcher dispatcher)
            : base(dispatcher)
        {
            _appContext = appContext;
        }


        protected override SearchResultControl CreateNew(SearchResult sourceObject)
        {
            _sourceObject = sourceObject;
            return new SearchResultControl(_appContext, sourceObject);
        }
        
    }
}
