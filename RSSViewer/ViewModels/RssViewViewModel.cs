using Jasily.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.AcceptHandlers;
using RSSViewer.Services;
using RSSViewer.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RSSViewer.ViewModels
{
    public class RssViewViewModel : BaseViewModel
    {
        private CancelableTaskScheduler _searchScheduler = new CancelableTaskScheduler();
        private string _searchText = string.Empty;
        private RssItemGroupViewModel _selectedGroup;
        private string _statusText;

        public RssViewViewModel()
        {
            this.Analytics = new AnalyticsViewModel(this);
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
            items = items.OrderBy(z => z.Title).ToArray();
            token.ThrowIfCancellationRequested();

            var groupService = App.RSSViewerHost.ServiceProvider.GetRequiredService<GroupService>();
            var groupsMap = await Task.Run(() => groupService.GetGroupsMap(items));
            token.ThrowIfCancellationRequested();

            var groups = new List<RssItemGroupViewModel>();
            var groupAll = new RssItemGroupViewModel { DisplayName = "<ALL>" };
            groups.Add(groupAll);

            await Task.Run(() =>
            {
                groupAll.Items.AddRange(items.Select(z => new RssItemViewModel(z)).ToArray());

                var groupEmpty = new RssItemGroupViewModel { DisplayName = "<>" };
                groups.Add(groupEmpty);

                groups.AddRange(groupsMap.Where(z =>
                {
                    if (z.Key == string.Empty)
                    {
                        groupEmpty.Items.AddRange(z.Value.Select(x => new RssItemViewModel(x)));
                        return false;
                    }
                    return true;
                }).Select(z =>
                {
                    var gvm = new RssItemGroupViewModel { DisplayName = z.Key };
                    gvm.Items.AddRange(z.Value.Select(x => new RssItemViewModel(x)));
                    return gvm;
                }).OrderBy(z => z.DisplayName));

                return groups;
            });
            token.ThrowIfCancellationRequested();

            this.Groups.Clear();
            groups.ForEach(this.Groups.Add);
            this.SelectedGroup = groupAll;

            sw.Stop();

            var st = "";
            if (sc.LastSyncElapsed.HasValue)
            {
                st += $"Last sync taked {sc.LastSyncElapsed.Value.TotalSeconds}s, ";
            }
            st += $"last query taked {sw.Elapsed.TotalSeconds}s, ";
            this.StatusText = st;
        }

        public async Task AcceptAsync(RssItemViewModel[] items, IAcceptHandler handler)
        {
            var rssItems = items.Select(z => z.RssItem).ToArray();
            if (await handler.Accept(rssItems))
            {
                await App.RSSViewerHost.Modify().AcceptAsync(rssItems);

                foreach (var item in items)
                {
                    item.RefreshProperties();
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
                item.RefreshProperties();
            }
            this.Analytics.RefreshProperties();
        }
    }
}
