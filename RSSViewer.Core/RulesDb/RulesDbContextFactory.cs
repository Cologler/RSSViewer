using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RSSViewer.RulesDb
{
    public class RulesDbContextFactory : IDesignTimeDbContextFactory<RulesDbContext>
    {
        public RulesDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RulesDbContext>();
            optionsBuilder.UseSqlite("Data Source=_.db");
            return new RulesDbContext(optionsBuilder.Options);
        }
    }
}
