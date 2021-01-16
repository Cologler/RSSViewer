using Jasily.ViewModel;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.LocalDb;
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
        private List<(IRssItem, RssItemState)> _stateChangesHook;
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

        private void OnRssItemsStateChanged(object sender, IEnumerable<(IRssItem, RssItemState)> e)
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

        private void OnRssItemsStateChangedInternal(IEnumerable<(IRssItem, RssItemState)> e)
        {
            Debug.Assert(this._itemsIndexes != null);

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
                this._stateChangesHook = new List<(IRssItem, RssItemState)>();
            }

            var searchText = searchInfo.SearchText.Trim();

            var sc = App.RSSViewerHost.ServiceProvider.GetRequiredService<SyncService>();
            await sc.SyncAsync();

            token.ThrowIfCancellationRequested();

            var sw = Stopwatch.StartNew();

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

            var changes = this._stateChangesHook;
            this._stateChangesHook = null;
            Debug.Assert(changes != null);
            this.OnRssItemsStateChangedInternal(changes);

            sw.Stop();

            var descState = string.Join(", ", searchInfo.IncludeState.Select(z => z.ToString().ToLower()));
            var desc = $"\"{searchText}\" from ({descState}) orderby ({searchInfo.SortBy.ToString().ToLower()})";
            this._viewerLogger.AddLine($"Query {desc} takes {sw.Elapsed.TotalSeconds}s.");

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
            var rssItems = items.Select(z => ((IRssItem)z.RssItem, z.RssItem.State)).ToArray();
            var changes = await handler.Accept(rssItems).ToListAsync();
            if (changes.Count > 0)
            {
                var service = App.RSSViewerHost.Modify();
                var operationSession = service.CreateOperationSession(true);
                await operationSession.AcceptAsync(changes
                    .Where(z => z.Item2 == RssItemState.Accepted)
                    .Select(z => z.Item1)
                    .Cast<RssItem>()
                    .ToList());
                await operationSession.RejectAsync(changes
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
