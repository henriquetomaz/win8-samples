//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

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

namespace SearchContract
{
    public sealed partial class OpenSearch : SDKTemplate.Common.LayoutAwarePage, IDisposable
    {
        private SearchPane searchPane;
        private HttpClient httpClient;
        private Task<string> currentHttpTask = null;
        private Random rand = new Random(100);

        public OpenSearch()
        {
            this.InitializeComponent();
            searchPane = SearchPane.GetForCurrentView();
            httpClient = new HttpClient();
        }

        ~OpenSearch()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (httpClient != null)
            {
                httpClient.Dispose();
                httpClient = null;
            }
        }

        private async Task GetSuggestionsAsync(string str, SearchSuggestionCollection suggestions)
        {
            // Cancel the previous suggestion request if it is not finished.
            if (currentHttpTask != null)
            {
                currentHttpTask.AsAsyncOperation<string>().Cancel();
                //Debug.WriteLine("   - cancelling task {0}      status: {1}", currentHttpTask.Id, currentHttpTask.Status);
            }

            // Get the suggestions from an open search service.
            currentHttpTask = httpClient.GetStringAsync(str);
            Debug.WriteLine("   + task created, id: {0}      string: {1}", currentHttpTask.Id, str);

            string response = await currentHttpTask;

            // inject a delay to simulate the time for the web response
            await Task.Delay(rand.Next(50, 6000));

            JsonArray parsedResponse = JsonArray.Parse(response);
            if (parsedResponse.Count > 1)
            {
                foreach (JsonValue value in parsedResponse[1].GetArray())
                {
                    suggestions.AppendQuerySuggestion(value.GetString());
                    //Debug.WriteLine("\n                   Search Result ({0}): {1}", str, value.GetString());

                    if (suggestions.Size >= MainPage.SearchPaneMaxSuggestions)
                    {
                        break;
                    }
                }
            }
        }

        private async void OnSearchPaneSuggestionsRequested(SearchPane sender, SearchPaneSuggestionsRequestedEventArgs e)
        {
            var queryText = e.QueryText;
            if (string.IsNullOrEmpty(queryText))
            {
                MainPage.Current.NotifyUser("Use the search pane to submit a query", NotifyType.StatusMessage);
            }
            else
            {
                var url = "http://en.wikipedia.org/w/api.php?action=opensearch&format=json&search={searchTerms}";
                
                // The deferral object is used to supply suggestions asynchronously for example when fetching suggestions from a web service.
                var request = e.Request;
                var deferral = request.GetDeferral();

                try
                {
                    Task task = GetSuggestionsAsync(Regex.Replace(url, "{searchTerms}", Uri.EscapeDataString(queryText)), request.SearchSuggestionCollection);
                    //Debug.WriteLine("* created task {0}    query: {1}", task.Id, queryText);

                    await task;
                    //Debug.WriteLine("@ await returned, id {0}    status {1} ", task.Id, task.Status);

                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        if (request.SearchSuggestionCollection.Size > 0)
                        {
                            MainPage.Current.NotifyUser("Suggestions provided for query: " + queryText, NotifyType.StatusMessage);
                        }
                        else
                        {
                            MainPage.Current.NotifyUser("No suggestions provided for query: " + queryText, NotifyType.StatusMessage);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // We have canceled the task.
                }
                catch (FormatException)
                {
                    MainPage.Current.NotifyUser(@"Suggestions could not be retrieved, please verify that the URL points to a valid service (for example http://contoso.com?q={searchTerms}", NotifyType.ErrorMessage);
                }
                catch (Exception)
                {
                    MainPage.Current.NotifyUser("Suggestions could not be displayed, please verify that the service provides valid OpenSearch suggestions", NotifyType.ErrorMessage);
                }
                finally
                {
                    deferral.Complete();
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.Current.NotifyUser("Use the search pane to submit a query", NotifyType.StatusMessage);
            // This event should be registered when your app first creates its main window after receiving an activated event, like OnLaunched, OnSearchActivated.
            // Typically this occurs in App.xaml.cs.
            searchPane.SuggestionsRequested += new TypedEventHandler<SearchPane, SearchPaneSuggestionsRequestedEventArgs>(OnSearchPaneSuggestionsRequested);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            searchPane.SuggestionsRequested -= new TypedEventHandler<SearchPane, SearchPaneSuggestionsRequestedEventArgs>(OnSearchPaneSuggestionsRequested);
        }
    }
}
