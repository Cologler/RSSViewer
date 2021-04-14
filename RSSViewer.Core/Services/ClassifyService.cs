using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.LocalDb;
using RSSViewer.Models;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class ClassifyService
    {
        private ImmutableList<Regex> _regexes;
        private readonly RegexCache _regexCache;

        public ClassifyService(ConfigService config, RegexCache regexCache)
        {
            this._regexCache = regexCache;
            config.OnAppConfChanged += this.Reload;
            this.Reload(config.AppConf);
        }

        private void Reload(AppConf config)
        {
            var regexes = new List<Regex>();

            foreach (var match in config.Group.Matches.Where(z => z is not null))
            {
                var regex = this._regexCache.TryGet(match, RegexOptions.IgnoreCase);
                if (regex is not null)
                {
                    regexes.Add(regex);
                }
            }

            this._regexes = regexes.ToImmutableList();
        }

        private static string GetGroupName(IImmutableList<Regex> regexes, IPartialRssItem rssItem)
        {
            foreach (var re in regexes)
            {
                var match = re.Match(rssItem.Title);
                if (match.Success)
                {
                    if (match.Groups.ContainsKey("name"))
                    {
                        return match.Groups["name"].Value.Trim();
                    }

                    if (match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value.Trim();
                    }

                    return match.Groups[0].Value;
                }
            }

            return string.Empty;
        }

        public void Classify(IEnumerable<ClassifyContext<IPartialRssItem>> source, System.Threading.CancellationToken token)
        {
            var regexes = this._regexes;

            source.AsParallel()
                .WithCancellation(token)
                .ForAll(item =>
                {
                    if (token.IsCancellationRequested)
                        return;
                    item.GroupName = GetGroupName(regexes, item.Item);
                });
        }
    }
}
