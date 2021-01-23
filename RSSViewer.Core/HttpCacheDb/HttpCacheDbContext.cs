using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace RSSViewer.HttpCacheDb
{
    public class HttpCacheDbContext : DbContext
    {
        public HttpCacheDbContext([NotNull] DbContextOptions<HttpCacheDbContext> options) : base(options)
        {
        }

        public DbSet<CachedRequest> Requests { get; set; }
    }
}
