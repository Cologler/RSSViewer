using Jasily.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.AcceptHandlers;
using RSSViewer.LocalDb;
using RSSViewer.Services;
using RSSViewer.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
            serviceProvider.GetRequiredService<AutoService>().AddedSingleRuleEffectedRssItemsStateChanged += 
                this.OnRssItemsStateChanged;
        }

        private void OnRssItemsStateChanged(IRssItemsStateChangedInfo obj)
        {
            _ = Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var state in new[] { RssItemState.Accepted , RssItemState.Rejected } )
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
            });
        }

        public string SearchText
        {
            get => this._searchText;
            set
            {
                if (this.ChangeModelProperty(ref this._searchText, value))
                {
                    _ = this.SearchAsync();
                }
            }
        }

        public IncludeViewModel IncludeView { get; } = new IncludeViewModel();

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

        public async Task SearchAsync()
        {
            var text = this.SearchText;
            await Task.Delay(300);
            if (text == this.SearchText)
            {
                await this._searchScheduler.RunAsync(token => this.SearchCoreAsync(text, token));
                this.Analytics.RefreshProperties();
            }
        }

        private async Task SearchCoreAsync(string searchText, CancellationToken token)
        {
            searchText = searchText.Trim();

            var sc = App.RSSViewerHost.ServiceProvider.GetRequiredService<SyncService>();
            await sc.SyncAsync();

            token.ThrowIfCancellationRequested();

            var sw = Stopwatch.StartNew();
            var states = this.IncludeView.GetStateValues();

            var items = await App.RSSViewerHost.Query().SearchAsync(searchText, states, token);
            token.ThrowIfCancellationRequested();
            items = items.OrderBy(z => z.Title).ToArray();

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
            this.LoggerMessage.AddLine($"Query \"{searchText}\" takes {sw.Elapsed.TotalSeconds}s.");
        }

        public async Task AcceptAsync(RssItemViewModel[] items, IAcceptHandler handler)
        {
            var rssItems = items.Select(z => z.RssItem).ToArray();
            if (await handler.Accept(rssItems))
            {
                await App.RSSViewerHost.Modify().AcceptAsync(rssItems);

                foreach (var item in items)
                {
                    // ensure notify updated whatever view is recreated or not.
                    var viewModel = this._itemsIndexes.GetValueOrDefault(item.RssItem.GetKey());
                    if (viewModel != null)
                    {
                        viewModel.RssItem.State = RssItemState.Accepted;
                        viewModel.RefreshProperties();
                    }
                }
                this.Analytics.RefreshProperties();
            }
        }

        public async Task RejectAsync(RssItemViewModel[] items)
        {
            var rssItems = items.Select(z => z.RssItem).ToArray();
            await App.RSSViewerHost.Modify().RejectAsync(rssItems);
            
            foreach (var item in items)
            {
                // ensure notify updated whatever view is recreated or not.
                var viewModel = this._itemsIndexes.GetValueOrDefault(item.RssItem.GetKey());
                if (viewModel != null)
                {
                    viewModel.RssItem.State = RssItemState.Rejected;
                    viewModel.RefreshProperties();
                }
            }
            this.Analytics.RefreshProperties();
        }
    }
}
