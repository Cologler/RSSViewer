using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RSSViewer.RulesDb
{
    public class RulesDbContext : DbContext
    {
        public RulesDbContext()
        {
        }

        public RulesDbContext([NotNull] DbContextOptions<RulesDbContext> options) : base(options)
        {
        }

        public DbSet<MatchRule> MatchRules { get; set; }
    }
}
