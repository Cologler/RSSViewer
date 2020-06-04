using Microsoft.EntityFrameworkCore.Design;

using RSSViewer.LocalDb;

namespace RSSViewer.Utils
{
    public class DesignTimeLocalDbContextFactory : SqliteDbContextFactory, IDesignTimeDbContextFactory<LocalDbContext>
    {
        public LocalDbContext CreateDbContext(string[] args)
            => new LocalDbContext(this.Options<LocalDbContext>());
    }
}
