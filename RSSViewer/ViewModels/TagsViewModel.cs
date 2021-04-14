using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.RulesDb;
using RSSViewer.ViewModels.Bases;

namespace RSSViewer.ViewModels
{
    public class TagsViewModel : ItemsViewModel<TagViewModel>
    {
        public void ResetItemsFromDb()
        {
            using var scope = this.ServiceProvider.CreateScope();
            using var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

            this.ResetItems(
                ctx.Tags.AsQueryable().AsNoTracking().ToList().Select(z => new TagViewModel(z))
            );
        }
    }
}
