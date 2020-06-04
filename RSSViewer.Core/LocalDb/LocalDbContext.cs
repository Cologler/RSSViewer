﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace RSSViewer.LocalDb
{
    public class LocalDbContext : DbContext
    {
        public LocalDbContext([NotNull] DbContextOptions<LocalDbContext> options) : base(options)
        {
        }

        public DbSet<RssItem> RssItems { get; set; }

        public DbSet<SyncSourceInfo> SyncSourceInfos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RssItem>()
                .HasKey(z => new { z.FeedId, z.RssId });

            base.OnModelCreating(modelBuilder);
        }

        public void AddOrIgnore(RssItem entity)
        {
            if (!this.RssItems.Any(e => e.FeedId == entity.FeedId && e.RssId == entity.RssId))
            {
                this.Add(entity);
            }
        }

        public void AddOrIgnoreRange(IEnumerable<RssItem> entities)
        {
            foreach (var entity in entities)
            {
                this.AddOrIgnore(entity);
            }
        }
    }
}
