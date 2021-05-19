﻿using Microsoft.EntityFrameworkCore;

using RSSViewer.Abstractions;

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

        public RssItem FindRssItem(IRssItemKey key) => this.RssItems.Find(key.FeedId, key.RssId);

        public bool AddOrIgnore(RssItem entity)
        {
            if (this.FindRssItem(entity) is null)
            {
                this.Add(entity);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns>the added items.</returns>
        public List<RssItem> AddOrIgnoreRange(IEnumerable<RssItem> entities)
        {
            return entities
                .Where(this.AddOrIgnore)
                .ToList();
        }

        public string[] GetFeedIds()
        {
            return this.RssItems.AsQueryable().Select(z => z.FeedId).Distinct().ToArray();
        }
    }
}
