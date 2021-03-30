using Microsoft.EntityFrameworkCore;

using System;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MatchRule>()
                .HasOne(z => z.Parent)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<MatchRule> MatchRules { get; set; }

        public int UpdateMatchRulesLifetime()
        {
            var now = DateTime.UtcNow;
            var changedCount = 0;

            foreach (var rule in this.MatchRules)
            {
                if (rule.AutoExpiredAfterLastMatched.HasValue)
                {
                    if (rule.LastMatched + rule.AutoExpiredAfterLastMatched.Value < now)
                    {
                        this.MatchRules.Remove(rule);
                        changedCount++;
                        continue;
                    }
                }

                if (!rule.IsDisabled && rule.AutoDisabledAfterLastMatched.HasValue)
                {
                    if (rule.LastMatched + rule.AutoDisabledAfterLastMatched.Value < now)
                    {
                        rule.IsDisabled = true;
                        changedCount++;
                        continue;
                    }
                }
            }

            return changedCount;
        }
    }
}
