using Jasily.ViewModel;
using System.Collections.Generic;

namespace RSSViewer.ViewModels
{
    public class RssItemGroupViewModel : BaseViewModel
    {
        public string DisplayName { get; set; }

        public List<RssItemViewModel> Items { get; } = new List<RssItemViewModel>();
    }
}
