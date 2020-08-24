using Jasily.ViewModel;

using System.Collections.Generic;

namespace RSSViewer.ViewModels
{
    public class RssItemGroupViewModel : BaseViewModel, IRssItemsCount
    {
        public string DisplayName { get; set; }

        public List<RssItemViewModel> Items { get; } = new List<RssItemViewModel>();

        int IRssItemsCount.Count => this.Items.Count;
    }
}
