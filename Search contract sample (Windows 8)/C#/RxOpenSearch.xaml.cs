using SDKTemplate;

using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Search;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using System.Collections.Generic;
using System.Diagnostics;

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace SearchContract
{
    public sealed partial class RxOpenSearch : SDKTemplate.Common.LayoutAwarePage, IDisposable
    {
        private SearchPane _searchPane;
        private HttpClient _httpClient;
        private Random _rand = new Random(100);

        private IDisposable _subscription; // for subscribing/unsubscribing to search contract events

        public RxOpenSearch()
        {
            this.InitializeComponent();
            _searchPane = SearchPane.GetForCurrentView();
            _httpClient = new HttpClient();
        }

        public void Dispose()
        {
            if (_httpClient != null)
                _httpClient.Dispose();
        }

        private async Task<JsonArray> SearchWikipediaAsync(string queryStr)
        {
            var url = 
                "http://en.wikipedia.org/w/api.php?action=opensearch&format=json&search=" 
                + Uri.EscapeDataString(queryStr);

            string result = await _httpClient.GetStringAsync(url);
            //await Task.Delay(rand.Next(50, 1000));

            return JsonArray.Parse(result);
        }

        private IDisposable SubscribeWikipediaSuggest()
        {
            var searchPaneSuggestRequested = // type is IObservable<SearchPaneSuggestionsRequestedEventArgs>
                Observable.FromEventPattern<SearchPaneSuggestionsRequestedEventArgs>(
                    _searchPane, "SuggestionsRequested")
                .Where(ev => ev.EventArgs.QueryText.Length > 2)    // only if the text is longer than 2 characters
                .Select(ev => new {
                    ev.EventArgs.QueryText,
                    Deferral = ev.EventArgs.Request.GetDeferral(),
                    ev.EventArgs.Request.SearchSuggestionCollection
                })
                .Throttle(TimeSpan.FromMilliseconds(750))    // wait until user pauses for 750ms
                .DistinctUntilChanged(ev => ev.QueryText);   // only if the value has changed


            var queried =
                searchPaneSuggestRequested
                .Select(
                    item => 
                        SearchWikipediaAsync(item.QueryText)
                        .ToObservable()
                        .Select(results => new {
                            Results = results,
                            item.Deferral, 
                            item.SearchSuggestionCollection
                        })
                    )
                    .Switch()
                    .ObserveOnDispatcher();


            return queried.Subscribe(
                    data => { 
                        addToSearchResults(data.Results, data.SearchSuggestionCollection, MainPage.SearchPaneMaxSuggestions);
                        data.Deferral.Complete();
                    });
        }

        private void DoWikipediaSearch(SearchPaneSuggestionsRequestedEventArgs request)
        {
            MainPage.Current.NotifyUser("Searching for: " + request.QueryText, NotifyType.StatusMessage);

            var deferral = request.Request.GetDeferral();

            request.Request.SearchSuggestionCollection.AppendQuerySuggestion("FOoo");

            //var resultArray = await SearchWikipediaAsync(request.QueryText);

            //addToSearchResults(
            //    resultArray, 
            //    request.Request.SearchSuggestionCollection, 
            //    MainPage.SearchPaneMaxSuggestions);
            deferral.Complete();
        }

        private IDisposable ImperativeSubscribe()
        {
            var searchPaneSuggestRequested = // type is IObservable<SearchPaneSuggestionsRequestedEventArgs>
                Observable.FromEventPattern<SearchPaneSuggestionsRequestedEventArgs>(
                    _searchPane, "SuggestionsRequested")
                .Where(ev => ev.EventArgs.QueryText.Length > 2)    // only if the text is longer than 2 characters
                .Select(ev => ev.EventArgs)
                // need to get deferral
                .Throttle(TimeSpan.FromMilliseconds(750))    // wait until user pauses for 750ms
                .DistinctUntilChanged(ev => ev.QueryText);   // only if the value has changed

            return searchPaneSuggestRequested
                .ObserveOn(Dispatcher)
                .Subscribe(DoWikipediaSearch);
        }

        private void addToSearchResults(JsonArray jsonArray, SearchSuggestionCollection suggestionResult, int maxResults)
        {
            if (jsonArray.Count > 1) {
                foreach (JsonValue value in jsonArray[1].GetArray()) {
                    suggestionResult.AppendQuerySuggestion(value.GetString());
                    Debug.WriteLine("Suggestion: " + value.GetString());
                    if (suggestionResult.Size >= MainPage.SearchPaneMaxSuggestions) {
                        break;
                    }
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.Current.NotifyUser("Use the search pane to submit a query", NotifyType.StatusMessage);

            Debug.Assert(_subscription == null);
            //_subscription = SubscribeWikipediaSuggest();
            _subscription = ImperativeSubscribe();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _subscription.Dispose();
            _subscription = null;
        }
    }
}
