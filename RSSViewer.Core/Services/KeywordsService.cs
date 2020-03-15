﻿using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.KeywordsFinders;
using RSSViewer.LocalDb;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace RSSViewer.Services
{
    public class KeywordsService
    {
        private readonly object _syncRoot = new object();
        private readonly IServiceProvider _serviceProvider;
        private ImmutableHashSet<string> _excludes;
        private ImmutableList<IKeywordsFinder> _finders;

        public KeywordsService(IServiceProvider serviceProvider, ConfigService config)
        {
            this._serviceProvider = serviceProvider;
            config.OnAppConfChanged += this.Reload;
            this.Reload(config.AppConf);
        }

        public void Reload(AppConf conf)
        {
            lock (this._syncRoot)
            {
                var section = conf.Keywords;

                var finders = new List<IKeywordsFinder>();
                finders.AddRange(this._serviceProvider.GetServices<IKeywordsFinder>());
                finders.AddRange(section.Matches
                    .Select(z => new Regex(z, RegexOptions.IgnoreCase))
                    .Select(z => new RegexKeywordsFinder(z)));
                this._finders = finders.ToImmutableList();

                this._excludes = section.Excludes.ToImmutableHashSet();
            }
        }

        public string[] GetKeywords(RssItem rssItem)
        {
            var finders = this._finders;
            var excludes = this._excludes;

            return finders.SelectMany(f => f.GetKeywords(rssItem))
                .Distinct()
                .Where(k => !excludes.Contains(k))
                .ToArray();
        }
    }
}
