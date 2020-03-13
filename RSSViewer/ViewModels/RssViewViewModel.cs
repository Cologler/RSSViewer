using Jasily.ViewModel;
using RSSViewer.AcceptHandlers;
using RSSViewer.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (this.ChangeModelProperty(ref _searchText, value))
                {
                    _ = this.SearchAsync();
                }
            }
        }

        public IncludeViewModel IncludeView { get; } = new IncludeViewModel();

        public ObservableCollection<RssItemGroupViewModel> Groups { get; } = new ObservableCollection<RssItemGroupViewModel>();

        public RssItemGroupViewModel SelectedGroup 
        { 
            get => _selectedGroup;
            set => this.ChangeModelProperty(ref _selectedGroup, value);
        }

        public async Task SearchAsync()
        {
            var text = this.SearchText;
            await Task.Delay(300);
            if (text == this.SearchText)
            {
                await this._searchScheduler.RunAsync(token => this.SearchCoreAsync(text, token));
            }
        }

        private async Task SearchCoreAsync(string searchText, CancellationToken token)
        {
            await App.RSSViewerHost.SyncAsync();
            token.ThrowIfCancellationRequested();

            var states = this.IncludeView.GetStateValues();

            var items = await App.RSSViewerHost.Query().SearchAsync(searchText, states, token);
            token.ThrowIfCancellationRequested();

            var itemViewModels = items.Select(z => new RssItemViewModel(z)).ToArray();

            var groupAll = new RssItemGroupViewModel { DisplayName = "<ALL>" };
            groupAll.Items.AddRange(itemViewModels);

            var groupOther = new RssItemGroupViewModel { DisplayName = "<>" };

            var groups = new List<RssItemGroupViewModel>
            {
                groupAll,
                groupOther
            };

            this.Groups.Clear();
            groups.ForEach(this.Groups.Add);
            this.SelectedGroup = groupAll;
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
        }
    }
}
