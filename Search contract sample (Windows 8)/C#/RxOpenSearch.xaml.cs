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
        private SearchPane searchPane;
        private HttpClient httpClient;
        private Random rand = new Random(100);

        public RxOpenSearch()
        {
            this.InitializeComponent();
            searchPane = SearchPane.GetForCurrentView();
            httpClient = new HttpClient();
        }

        ~RxOpenSearch()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (httpClient != null)
                httpClient.Dispose();
        }

        /// <summary>
        ///  Searches Wikipedia for the given query string, return as a json array
        /// </summary>
        private async Task<JsonArray> SearchWikipedia(string queryStr)
        {
            var url = 
                "http://en.wikipedia.org/w/api.php?action=opensearch&format=json&search=" 
                + Uri.EscapeDataString(queryStr);

            string result = await httpClient.GetStringAsync(url);
            await Task.Delay(rand.Next(50, 6000));

            return JsonArray.Parse(result);

                    //.ToObservable()
                    //.Select<string, JsonArray>(
                    //    result => {
                    //        // inject a delay to simulate the time for the web response
                    //        return JsonArray.Parse(result);
                    //    });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.Current.NotifyUser("Use the search pane to submit a query", NotifyType.StatusMessage);

            // IObservable<SearchPaneSuggestionsRequestedEventArgs>
            var userSearch =
                WindowsObservable
                    .FromEventPattern<SearchPane, SearchPaneSuggestionsRequestedEventArgs>
                        (h => searchPane.SuggestionsRequested += h,  // lambda for attaching an event handler
                         h => searchPane.SuggestionsRequested -= h)  // lambda for removing event handler
                    .Select(ev => ev.EventArgs) // select the Event argument (don't need the sender)
                    .Where(args => args.QueryText.Length > 2)    // only if the text is longer than 2 characters
                    .Throttle(TimeSpan.FromMilliseconds(750))    // wait until user pauses for 750ms
                    .DistinctUntilChanged();                     // only if the value has changed

            userSearch.Select(
                item => 
                    SearchWikipedia(item.QueryText).ContinueWith(result => new { req = item.Request, result })
                )
                .Switch()
                .Subcribe(
                    data => addToSearchResults(data.result, data.req.SearchSuggestionCollection, MainPage.SearchPaneMaxSuggestions)
                    )
            );


            userSearch.Subscribe(
                async item => {
                    var array = await SearchWikipedia(item.QueryText);
                    addToSearchResults(array, item.Request.SearchSuggestionCollection, MainPage.SearchPaneMaxSuggestions);
                });

            //userSearch.Select(next => SearchWikipedia(next.QueryText))
            //    .Switch()
            //    .Subscribe(printJsonArray);

                // ,
                // to do: add OnError handler and OnCompleted
                //error => 

#if false
            JsonArray parsedResponse = JsonArray.Parse(response);
            }
#endif

        }

        private void printJsonArray(JsonArray jsonArray)
        {
            if (jsonArray.Count > 1) {
                foreach (JsonValue value in jsonArray[1].GetArray()) {
                    Debug.WriteLine(value.GetString());
                }
            }
        }

        private void addToSearchResults(JsonArray jsonArray, SearchSuggestionCollection suggestionResult, int maxResults)
        {
            if (jsonArray.Count > 1) {
                foreach (JsonValue value in jsonArray[1].GetArray()) {
                    suggestionResult.AppendQuerySuggestion(value.GetString());
                    if (suggestionResult.Size >= MainPage.SearchPaneMaxSuggestions) {
                        break;
                    }
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
        }
    }
}
