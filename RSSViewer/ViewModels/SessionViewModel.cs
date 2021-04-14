using Jasily.ViewModel;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Extensions;
using RSSViewer.Services;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RSSViewer.ViewModels
{
    public class SessionViewModel : BaseViewModel, IDisposable
    {
        private CancelableTaskScheduler _searchScheduler = new CancelableTaskScheduler();
        private string _searchText = string.Empty;
        private RssItemGroupViewModel _selectedGroup;
        private Dictionary<(string, string), RssItemViewModel> _itemsIndexes;
        private List<(IPartialRssItem, RssItemState)> _stateChangesHook;
        private string _title;
        private readonly IViewerLogger _viewerLogger;
        public event EventHandler SessionStateChanged;

        public SessionViewModel()
        {
            var serviceProvider = App.RSSViewerHost.ServiceProvider;

            this._viewerLogger = serviceProvider.GetRequiredService<IViewerLogger>();

            serviceProvider.AddListener(EventNames.RssItemsStateChanged, this.OnRssItemsStateChanged);

            this.SourcesView.PropertyChanged += this.QueryOptionsViewModel_PropertyChanged;
            this.IncludeView.PropertyChanged += this.QueryOptionsViewModel_PropertyChanged;
            this.SortByView.PropertyChanged += this.QueryOptionsViewModel_PropertyChanged;
            this.PropertyChanged += this.QueryOptionsViewModel_PropertyChanged;
        }

        public void Dispose()
        {
            this.SourcesView.Dispose();

            var serviceProvider = App.RSSViewerHost.ServiceProvider;
            serviceProvider.RemoveListener(EventNames.RssItemsStateChanged, this.OnRssItemsStateChanged);
        }

        private async void QueryOptionsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, this) && e.PropertyName == nameof(this.SearchText))
            {
                await this.RefreshContentAsync(300);
            }
            else if (ReferenceEquals(sender, this.IncludeView))
            {
                await this.RefreshContentAsync(500);
            }
            else if (ReferenceEquals(sender, this.SortByView) || ReferenceEquals(sender, this.SourcesView))
            {
                await this.RefreshContentAsync(0);
            }
        }

        private void OnRssItemsStateChanged(object sender, IEnumerable<(IPartialRssItem, RssItemState)> e)
        {
            App.Current.Dispatcher.InvokeAsync(() =>
            {
                if (this._stateChangesHook != null)
                {
                    this._stateChangesHook.AddRange(e);
                    return;
                }

                if (this._itemsIndexes is null)
                {
                    // first run.
                    return;
                }

                this.OnRssItemsStateChangedInternal(e);
            });
        }

        private void OnRssItemsStateChangedInternal(IEnumerable<(IPartialRssItem, RssItemState)> e)
        {
            Debug.Assert(this._itemsIndexes != null);

            foreach (var (rssItem, state) in e)
            {
                var viewModel = this._itemsIndexes.GetValueOrDefault(rssItem.GetKey());
                if (viewModel != null)
                {
                    viewModel.RssItem.State = state;
                    viewModel.RefreshProperties();
                }
            }

            this.SessionStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public string SearchText
        {
            get => this._searchText;
            set => this.ChangeModelProperty(ref this._searchText, value);
        }

        public SourcesViewModel SourcesView { get; } = new();

        public IncludeViewModel IncludeView { get; } = new IncludeViewModel();

        public SortByViewModel SortByView { get; } = new SortByViewModel();

        public ObservableCollection<RssItemGroupViewModel> Groups { get; } = new();

        public ObservableCollection<RssItemViewModel> SelectedItems { get; } = new();

        public string Title
        {
            get => _title;
            set => this.ChangeModelProperty(ref _title, value);
        }

        public RssItemGroupViewModel SelectedGroup
        {
            get => this._selectedGroup;
            set => this.ChangeModelProperty(ref this._selectedGroup, value);
        }

        public bool Removable { get; set; }

        public async Task RefreshContentAsync(int delay = 300)
        {
            SearchInfo GetSearchInfo()
            {
                return new SearchInfo(
                    this.SearchText,
                    this.SourcesView.SelectedItem.FeedId,
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
                    await this._searchScheduler.RunAsync(token => this.RefreshContentCoreAsync(searchInfo, token));
                    this.SessionStateChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task RefreshContentCoreAsync(SearchInfo searchInfo, CancellationToken token)
        {
            if (this._stateChangesHook is null)
            {
                this._stateChangesHook = new();
            }

            var searchText = searchInfo.SearchText.Trim();

            var sc = App.RSSViewerHost.ServiceProvider.GetRequiredService<SyncService>();
            await sc.SyncAsync();

            token.ThrowIfCancellationRequested();

            var refreshStopwatch = Stopwatch.StartNew();

            var items = await App.RSSViewerHost.Query().SearchAsync(searchText, searchInfo.IncludeState, searchInfo.FeedId, token);
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

            var classifyService = App.RSSViewerHost.ServiceProvider.GetRequiredService<ClassifyService>();
            var classifiedItems = items
                .Select(z => new RssItemViewModel.ClassifyContext(z))
                .ToArray();

            // classify start

            var classifyStopwatch = Stopwatch.StartNew();
            await Task.Run(() => classifyService.Classify(classifiedItems, token), token);
            classifyStopwatch.Stop();
            this._viewerLogger.AddLine($"Classify {classifiedItems.Length} items cost {classifyStopwatch.Elapsed.TotalSeconds}s.");

            // classify end

            token.ThrowIfCancellationRequested();

            var groupList = new List<RssItemGroupViewModel>();

            // spec: all
            var allItemsGroup = new RssItemGroupViewModel { DisplayName = "ALL" };
            allItemsGroup.Items.AddRange(classifiedItems.Select(z => z.ViewModel));
            groupList.Add(allItemsGroup);

            // spec: no group
            var emptyGroupItemsGroup = new RssItemGroupViewModel { DisplayName = "NO_GROUP" };
            emptyGroupItemsGroup.Items.AddRange(classifiedItems.Where(z => z.GroupName == string.Empty).Select(z => z.ViewModel));
            groupList.Add(emptyGroupItemsGroup);

            // tags
            var taggedItems = classifiedItems.Where(z => z.Tags.Count > 0).ToList();
            var tagsGroupsMap = taggedItems
                .SelectMany(z => z.Tags)
                .Distinct()
                .ToDictionary(z => z, z => new RssItemGroupViewModel { DisplayName = z.ToString() });
            foreach (var item in taggedItems)
            {
                foreach (var tag in item.Tags)
                {
                    tagsGroupsMap[tag].Items.Add(item.ViewModel);
                }
            }
            var tagGroupWithoutTagMap = classifiedItems
                .SelectMany(z => z.TagGroupWithoutTag)
                .Distinct()
                .ToDictionary(z => z, z => new RssItemGroupViewModel { DisplayName = $"[{z}].<>" });
            foreach (var item in classifiedItems)
            {
                foreach (var tagGroup in item.TagGroupWithoutTag)
                {
                    tagGroupWithoutTagMap[tagGroup].Items.Add(item.ViewModel);
                }
            }
            groupList.AddRange(
                tagsGroupsMap
                    .Select(z => z.Value)
                    .Concat(tagGroupWithoutTagMap.Select(z => z.Value))
                    .OrderBy(z => z.DisplayName)                    
            );

            // groups
            groupList.AddRange(
                classifiedItems
                    .Where(z => z.GroupName != string.Empty)
                    .GroupBy(z => z.GroupName)
                    .OrderBy(z => z.Key)
                    .Select(z =>
                    {
                        var g = new RssItemGroupViewModel { DisplayName = $"{{{z.Key}}}" };
                        g.Items.AddRange(z.Select(z => z.ViewModel));
                        return g;
                    })
            );

            var itemsIndexes = classifiedItems.Select(z => z.ViewModel).ToDictionary(z => z.RssItem.GetKey());

            token.ThrowIfCancellationRequested();

            this.Groups.Clear();
            groupList.ForEach(this.Groups.Add);
            this.SelectedGroup = allItemsGroup;
            this._itemsIndexes = itemsIndexes;

            var changes = this._stateChangesHook;
            this._stateChangesHook = null;
            Debug.Assert(changes != null);
            this.OnRssItemsStateChangedInternal(changes);

            refreshStopwatch.Stop();

            var descState = string.Join(", ", searchInfo.IncludeState.Select(z => z.ToString().ToLower()));
            var desc = $"\"{searchText}\" from ({descState}) orderby ({searchInfo.SortBy.ToString().ToLower()})";
            this._viewerLogger.AddLine($"Query {desc} total cost {refreshStopwatch.Elapsed.TotalSeconds}s.");

            this.Title = searchText.Length == 0
                ? "*"
                : searchText;
        }

        public void UpdateSelectedItems(IEnumerable<RssItemGroupViewModel> groups)
        {
            if (groups is null)
                throw new ArgumentNullException(nameof(groups));

            this.SelectedItems.Clear();
            groups.SelectMany(z => z.Items).Distinct().ToList().ForEach(this.SelectedItems.Add);
        }

        public async Task HandleAsync(RssItemViewModel[] items, IRssItemHandler handler)
        {
            var rssItems = items.Select(z => ((IPartialRssItem)z.RssItem, z.RssItem.State)).ToArray();
            var changes = await handler.HandleAsync(rssItems).ToListAsync();
            if (changes.Count > 0)
            {
                var service = App.RSSViewerHost.Modify();
                var operationSession = service.CreateOperationSession(true);
                await operationSession.AcceptAsync(changes
                    .Where(z => z.Item2 == RssItemState.Accepted)
                    .Select(z => z.Item1)
                    .ToList());
                await operationSession.RejectAsync(changes
                    .Where(z => z.Item2 == RssItemState.Rejected)
                    .Select(z => z.Item1)
                    .ToList());
                await operationSession.ArchivedAsync(changes
                    .Where(z => z.Item2 == RssItemState.Archived)
                    .Select(z => z.Item1)
                    .ToList());
            }
        }

        public struct SearchInfo : IEquatable<SearchInfo>
        {
            public string SearchText { get; }

            public string FeedId { get; }

            public SearchInfo(string searchText, string feedId, RssItemState[] includeState, SortBy sortBy) : this()
            {
                this.SearchText = searchText;
                this.FeedId = feedId;
                this.IncludeState = includeState;
                this.SortBy = sortBy;
            }

            public RssItemState[] IncludeState { get; }

            public SortBy SortBy { get; }

            public bool Equals(SearchInfo other)
            {
                if (this.SearchText != other.SearchText)
                    return false;

                if (this.FeedId != other.FeedId)
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
