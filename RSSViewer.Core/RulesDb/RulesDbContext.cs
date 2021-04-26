using Microsoft.EntityFrameworkCore;

using RSSViewer.Abstractions;
using RSSViewer.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

namespace RSSViewer.RulesDb
{
    public class RulesDbContext : DbContext, ILoader<IEnumerable<Tag>>
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

            modelBuilder.Entity<Tag>()
                .HasIndex(z => z.TagName)
                .IsUnique();
        }

        public DbSet<MatchRule> MatchRules { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public int UpdateMatchRulesLifetime()
        {
            var now = DateTime.UtcNow;
            var changedCount = 0;

            foreach (var rule in this.MatchRules.AsQueryable().Where(z => z.HandlerType == HandlerType.Action))
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

        IEnumerable<Tag> ILoader<IEnumerable<Tag>>.Load(CancellationToken token)
        {
            return this.Tags.AsQueryable().AsNoTracking().WithCancellation(token).ToList();
        }
    }
}
