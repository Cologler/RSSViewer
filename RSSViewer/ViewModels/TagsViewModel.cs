using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Extensions;
using RSSViewer.RulesDb;
using RSSViewer.ViewModels.Bases;

namespace RSSViewer.ViewModels
{
    public class TagsViewModel : ItemsViewModel<TagViewModel>
    {
        public void ResetItemsFromDb()
        {
            this.ResetItems(
                this.ServiceProvider.LoadMany<Tag>().Select(z => new TagViewModel(z))
            );
        }
    }
}
