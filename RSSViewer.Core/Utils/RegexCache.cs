using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using RSSViewer.Abstractions;

namespace RSSViewer.Utils
{
    public class RegexCache
    {
        private readonly IViewerLogger _viewerLogger;
        private readonly ConcurrentDictionary<(string, RegexOptions), WeakReference<Regex>> _cache = new();

        public RegexCache(IViewerLogger viewerLogger)
        {
            this._viewerLogger = viewerLogger;
        }

        /// <summary>
        /// Try get or create regex.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Regex TryGet(string pattern, RegexOptions options)
        {
            if (pattern is null)
                throw new ArgumentNullException(nameof(pattern));

            var key = (pattern, options);
            var wr = this._cache.GetValueOrDefault(key);
            if (wr is not null && wr.TryGetTarget(out var regex))
            {
                return regex;
            }

            try
            {
                regex = new Regex(pattern, options);
            }
            catch (ArgumentException)
            {
                this._viewerLogger.AddLine($"Unable convert \"{pattern}\" to regex.");
                return null;
            }
            
            if (wr is null)
            {
                this._cache.TryAdd(key, new WeakReference<Regex>(regex));
            }
            else
            {
                wr.SetTarget(regex);
            }

            return regex;
        }
    }
}
