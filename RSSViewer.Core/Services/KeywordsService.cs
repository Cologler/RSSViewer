using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.KeywordsFinders;
using RSSViewer.LocalDb;
using RSSViewer.Utils;

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
        private readonly IViewerLogger _viewerLogger;
        private readonly RegexCache _regexCache;
        private ImmutableHashSet<string> _excludes;
        private ImmutableList<IKeywordsFinder> _finders;

        public KeywordsService(IServiceProvider serviceProvider, IViewerLogger viewerLogger, ConfigService config, RegexCache regexCache)
        {
            this._serviceProvider = serviceProvider;
            this._viewerLogger = viewerLogger;
            this._regexCache = regexCache;
            config.OnAppConfChanged += this.Reload;
            this.Reload(config.AppConf);
        }

        public void Reload(AppConf conf)
        {
            var section = conf.Keywords;

            var finders = new List<IKeywordsFinder>();
            finders.AddRange(this._serviceProvider.GetServices<IKeywordsFinder>());

            foreach (var match in section.Matches.Where(z => z is not null))
            {
                var regex = this._regexCache.TryGet(match, RegexOptions.IgnoreCase);
                if (regex is not null)
                {
                    finders.Add(new RegexKeywordsFinder(regex));
                }
            }

            lock (this._syncRoot)
            {
                this._finders = finders.ToImmutableList();
                this._excludes = section.Excludes.ToImmutableHashSet();
            }
        }

        public string[] GetKeywords(RssItem rssItem)
        {
            var finders = this._finders;
            var excludes = this._excludes;

            return finders.SelectMany(f => f.GetKeywords(rssItem))
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .Distinct()
                .Where(k => !excludes.Contains(k))
                .ToArray();
        }
    }
}
