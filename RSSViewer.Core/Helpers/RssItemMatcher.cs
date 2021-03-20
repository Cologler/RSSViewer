using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.Abstractions;
using RSSViewer.LocalDb;
using RSSViewer.RulesDb;
using RSSViewer.StringMatchers;

namespace RSSViewer.Helpers
{
    public class RssItemMatcher
    {
        private readonly MatchRule _matchRule;
        private readonly IStringMatcher _stringMatcher;
        private ImmutableArray<RssItemMatcher> _branchs;
        private readonly object _syncRoot = new();

        public RssItemMatcher(MatchRule matchRule, IStringMatcher stringMatcher)
        {
            this._matchRule = matchRule ?? throw new ArgumentNullException(nameof(matchRule));
            this._stringMatcher = stringMatcher ?? throw new ArgumentNullException(nameof(stringMatcher));
        }

        public DateTime LastMatched => this.Rule.LastMatched;

        public MatchRule Rule => this._matchRule;

        public bool IsMatch(IPartialRssItem rssItem)
        {
            if (this.Rule.OnFeedId is not null && this.Rule.OnFeedId != rssItem.FeedId)
                return false;

            return this._stringMatcher.IsMatch(rssItem.Title);
        }

        /// <summary>
        /// return <see langword="null"/> if not match.
        /// </summary>
        /// <param name="rssItem"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public ImmutableArray<MatchRule> TryFindMatchedRule(IPartialRssItem rssItem, DateTime now)
        {
            if (this.Rule.OnFeedId is not null && this.Rule.OnFeedId != rssItem.FeedId)
                return default;

            if (!this._stringMatcher.IsMatch(rssItem.Title))
                return default;

            // childs
            if (!this._branchs.IsDefault)
            {
                foreach (var child in this._branchs)
                {
                    var rulesChain = child.TryFindMatchedRule(rssItem, now);
                    if (!rulesChain.IsDefault)
                    {
                        Debug.Assert(!rulesChain.IsEmpty);
                        return ImmutableArray.Create(this.Rule).AddRange(rulesChain);
                    }
                }
            }

            this.Rule.LastMatched = now;
            return ImmutableArray.Create(this.Rule);
        }

        public void AddSubBranch(RssItemMatcher rssItemMatcher)
        {
            lock (this._syncRoot) this._branchs = this._branchs.Add(rssItemMatcher);
        }

        public RssItemMatcher FindSubBranch(int ruleId)
        {
            if (this.Rule.Id == ruleId)
                return this;

            if (this._branchs.IsDefault)
                return null;

            return this._branchs.Select(z => z.FindSubBranch(ruleId)).FirstOrDefault(z => z is not null);
        }
    }
}
