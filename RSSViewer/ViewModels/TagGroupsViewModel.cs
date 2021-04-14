using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.RulesDb;
using RSSViewer.ViewModels.Bases;

namespace RSSViewer.ViewModels
{
    public class TagGroupsViewModel : ItemsViewModel<string>
    {
        public void ResetItemsFromDb()
        {
            using var scope = this.ServiceProvider.CreateScope();
            using var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

            this.ResetItems(ctx.Tags.AsQueryable().AsNoTracking().ToList());
        }

        public void ResetItems(IEnumerable<Tag> tags)
        {
            if (tags is null)
                throw new System.ArgumentNullException(nameof(tags));

            using var scope = this.ServiceProvider.CreateScope();
            using var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

            this.ResetItems(tags.Select(z => z.TagGroupName).Distinct().Where(z => !string.IsNullOrEmpty(z)));
        }
    }
}
