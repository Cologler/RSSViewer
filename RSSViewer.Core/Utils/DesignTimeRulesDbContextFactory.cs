using Microsoft.EntityFrameworkCore.Design;

using RSSViewer.RulesDb;

namespace RSSViewer.Utils
{
    public class DesignTimeRulesDbContextFactory : SqliteDbContextFactory, IDesignTimeDbContextFactory<RulesDbContext>
    {
        public RulesDbContext CreateDbContext(string[] args)
            => new RulesDbContext(this.Options<RulesDbContext>());
    }
}
