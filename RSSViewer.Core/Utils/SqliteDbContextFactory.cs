using Microsoft.EntityFrameworkCore;

using System;
using System.Runtime.CompilerServices;

namespace RSSViewer.Utils
{
    public class SqliteDbContextFactory
    {
        protected DbContextOptions<TContext> Options<TContext>() where TContext : DbContext
        {
            var optionsBuilder = new DbContextOptionsBuilder<TContext>();
            optionsBuilder.UseSqlite($"Data Source={Guid.NewGuid().ToString().Replace("-", "")}.db");
            return optionsBuilder.Options;
        }
    }
}
