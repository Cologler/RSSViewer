using Microsoft.EntityFrameworkCore.Design;

using RSSViewer.HttpCacheDb;

namespace RSSViewer.Utils
{
    public class DesignTimeHttpCacheDbContextFactory : SqliteDbContextFactory, IDesignTimeDbContextFactory<HttpCacheDbContext>
    {
        public HttpCacheDbContext CreateDbContext(string[] args)
            => new HttpCacheDbContext(this.Options<HttpCacheDbContext>());
    }
}
