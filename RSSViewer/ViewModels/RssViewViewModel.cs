using Jasily.ViewModel;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.AcceptHandlers;
using RSSViewer.LocalDb;
using RSSViewer.RssItemHelper;
using RSSViewer.Services;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RSSViewer.ViewModels
{
    public class RssViewViewModel : BaseViewModel
    {
        private CancelableTaskScheduler _searchScheduler = new CancelableTaskScheduler();
        private string _searchText = string.Empty;
        private RssItemGroupViewModel _selectedGroup;
        private string _statusText;
        private Dictionary<(string, string), RssItemViewModel> _itemsIndexes;

        public RssViewViewModel()
        {
            var serviceProvider = App.RSSViewerHost.ServiceProvider;
            this.Analytics = new AnalyticsViewModel(this);
            this.LoggerMessage = serviceProvider.GetRequiredService<ViewerLoggerViewModel>();
            serviceProvider.GetRequiredService<RunRulesService>().AddedSingleRuleEffectedRssItemsStateChanged += obj => 
                Application.Current.Dispatcher.InvokeAsync(() =>
                    this.OnRssItemsStateChanged(obj));
            serviceProvider.AddListener(EventNames.RssItemsStateChanged, this.OnRssItemsStateChanged);

            this.IncludeView.PropertyChanged += this.QueryOptionsViewModel_PropertyChanged;
            this.SortByView.PropertyChanged += this.QueryOptionsViewModel_PropertyChanged;
            this.PropertyChanged += this.QueryOptionsViewModel_PropertyChanged;
        }

        private async void QueryOptionsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, this) && e.PropertyName == nameof(this.SearchText))
            {
                await this.SearchAsync(300);
            } 
            else if (ReferenceEquals(sender, this.IncludeView))
            {
                await this.SearchAsync(500);
            }
            else if (ReferenceEquals(sender, this.SortByView))
            {
                await this.SearchAsync(0);
            }
        }

        private void OnRssItemsStateChanged(object sender, IEnumerable<(IRssItem, RssItemState)> e)
        {
            App.Current.Dispatcher.InvokeAsync(() =>
            {
                if (this._itemsIndexes is null)
                {
                    // first run.
                    return;
                }

                foreach (var (rssItem, state) in e)
                {
                    if (rssItem is RssItem item)
                    {
                        var viewModel = this._itemsIndexes.GetValueOrDefault(item.GetKey());
                        if (viewModel != null)
                        {
                            viewModel.RssItem.State = state;
                            viewModel.RefreshProperties();
                        }
                    }
                }

                this.Analytics.RefreshProperties();
            });
        }

        private void OnRssItemsStateChanged(IRssItemsStateChangedInfo obj)
        {
            foreach (var state in new[] { RssItemState.Accepted, RssItemState.Rejected })
            {
                foreach (var item in obj.GetItems(state))
                {
                    var viewModel = this._itemsIndexes.GetValueOrDefault(item.GetKey());
                    if (viewModel != null)
                    {
                        viewModel.RssItem.State = state;
                        viewModel.RefreshProperties();
                    }
                }
            }
            this.Analytics.RefreshProperties();
        }

        public string SearchText
        {
            get => this._searchText;
            set => this.ChangeModelProperty(ref this._searchText, value);

        }

        public IncludeViewModel IncludeView { get; } = new IncludeViewModel();

        public SortByViewModel SortByView { get; } = new SortByViewModel();

        public AnalyticsViewModel Analytics { get; }

        public ViewerLoggerViewModel LoggerMessage { get; }

        public ObservableCollection<RssItemGroupViewModel> Groups { get; } = new ObservableCollection<RssItemGroupViewModel>();

        public string StatusText
        {
            get => this._statusText;
            private set => this.ChangeModelProperty(ref this._statusText , value);
        }

        public RssItemGroupViewModel SelectedGroup
        {
            get => this._selectedGroup;
            set => this.ChangeModelProperty(ref this._selectedGroup, value);
        }

        public async Task SearchAsync(int delay = 300)
        {
            SearchInfo GetSearchInfo()
            {
                return new SearchInfo(
                    this.SearchText,
                    this.IncludeView.GetStateValues(),
                    this.SortByView.SortBy);
            }

            var searchInfo = GetSearchInfo();

            if (delay > 0)
            {
                await Task.Delay(delay);
            }

            if (searchInfo.Equals(GetSearchInfo()))
            {
                try
                {
                    await this._searchScheduler.RunAsync(token => this.SearchCoreAsync(searchInfo, token));
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                this.Analytics.RefreshProperties();
            }
        }

        private async Task SearchCoreAsync(SearchInfo searchInfo, CancellationToken token)
        {
            var searchText = searchInfo.SearchText.Trim();

            var sc = App.RSSViewerHost.ServiceProvider.GetRequiredService<SyncService>();
            await sc.SyncAsync();

            token.ThrowIfCancellationRequested();

            var sw = Stopwatch.StartNew();

            var items = await App.RSSViewerHost.Query().SearchAsync(searchText, searchInfo.IncludeState, token);
            token.ThrowIfCancellationRequested();

            switch (searchInfo.SortBy)
            {
                case SortBy.Title:
                    items = items.OrderBy(z => z.Title).ToArray();
                    break;

                case SortBy.Time:
                    // already is time sort
                    items = items.Reverse().ToArray();
                    break;
            }

            var groupService = App.RSSViewerHost.ServiceProvider.GetRequiredService<GroupService>();
            var groupsMap = await Task.Run(() => groupService.GetGroupsMap(items));
            token.ThrowIfCancellationRequested();

            var groupList = new List<RssItemGroupViewModel>();

            var allItemsGroup = new RssItemGroupViewModel { DisplayName = "<ALL>" };
            groupList.Add(allItemsGroup);

            var emptyItemsGroup = new RssItemGroupViewModel { DisplayName = "<>" };
            groupList.Add(emptyItemsGroup);

            allItemsGroup.Items.AddRange(items.Select(z => new RssItemViewModel(z)).ToArray());
            var itemsIndexes = allItemsGroup.Items.ToDictionary(z => z.RssItem.GetKey());

            RssItemViewModel FromCreated(RssItem rssItem) => itemsIndexes[rssItem.GetKey()];

            await Task.Run(() =>
            {
                groupList.AddRange(groupsMap.Where(z =>
                {
                    if (z.Key == string.Empty)
                    {
                        emptyItemsGroup.Items.AddRange(z.Value.Select(x => FromCreated(x)));
                        return false;
                    }
                    return true;
                }).Select(z =>
                {
                    var gvm = new RssItemGroupViewModel { DisplayName = z.Key };
                    gvm.Items.AddRange(z.Value.Select(x => FromCreated(x)));
                    return gvm;
                }).OrderBy(z => z.DisplayName));
            });
            token.ThrowIfCancellationRequested();

            this.Groups.Clear();
            groupList.ForEach(this.Groups.Add);
            this.SelectedGroup = allItemsGroup;
            this._itemsIndexes = itemsIndexes;

            sw.Stop();

            var descState = string.Join(", ", searchInfo.IncludeState.Select(z => z.ToString().ToLower()));
            var desc = $"\"{searchText}\" from ({descState}) orderby ({searchInfo.SortBy.ToString().ToLower()})";
            this.LoggerMessage.AddLine($"Query {desc} takes {sw.Elapsed.TotalSeconds}s.");
        }

        public async Task HandleAsync(RssItemViewModel[] items, IRssItemHandler handler)
        {
            var rssItems = items.Select(z => ((IRssItem)z.RssItem, z.RssItem.State)).ToArray();
            var changes = await (handler.Accept(rssItems)).ToListAsync();
            if (changes.Count > 0)
            {
                await App.RSSViewerHost.Modify().AcceptAsync(changes
                    .Where(z => z.Item2 == RssItemState.Accepted)
                    .Select(z => z.Item1)
                    .Cast<RssItem>()
                    .ToList());

                await App.RSSViewerHost.Modify().RejectAsync(changes
                    .Where(z => z.Item2 == RssItemState.Rejected)
                    .Select(z => z.Item1)
                    .Cast<RssItem>()
                    .ToList());

                //this.OnRssItemsStateChanged(
                //    RssItemsStateChangedInfo.Create(changes.Select(z => ((RssItem)z.Item1, z.Item2))));
            }
        }

        public struct SearchInfo : IEquatable<SearchInfo>
        {
            public string SearchText { get; }

            public SearchInfo(string searchText, RssItemState[] includeState, SortBy sortBy) : this()
            {
                this.SearchText = searchText;
                this.IncludeState = includeState;
                this.SortBy = sortBy;
            }

            public RssItemState[] IncludeState { get; }

            public SortBy SortBy { get; }

            public bool Equals(SearchInfo other)
            {
                if (this.SearchText != other.SearchText)
                    return false;

                if (!this.IncludeState.SequenceEqual(other.IncludeState))
                    return false;

                if (this.SortBy != other.SortBy)
                    return false;

                return true;
            }
        }
    }
}
