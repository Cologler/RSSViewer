using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public RssItemMatcher(MatchRule matchRule, IStringMatcher stringMatcher)
        {
            this._matchRule = matchRule ?? throw new ArgumentNullException(nameof(matchRule));
            this._stringMatcher = stringMatcher ?? throw new ArgumentNullException(nameof(stringMatcher));
            this.LastMatched = matchRule.LastMatched;
        }

        public DateTime LastMatched { get; set; }

        public int RuleId => this._matchRule.Id;

        public bool IsMatch(IPartialRssItem rssItem)
        {
            if (this._matchRule.OnFeedId is not null && this._matchRule.OnFeedId != rssItem.FeedId)
                return false;

            return this._stringMatcher.IsMatch(rssItem.Title);
        }

        public string HandlerId => this._matchRule.HandlerId;

        /// <summary>
        /// return <see langword="null"/> if not match.
        /// </summary>
        /// <param name="rssItem"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public ImmutableArray<MatchRule> TryFindMatchedRule(IPartialRssItem rssItem, DateTime now)
        {
            if (this._matchRule.OnFeedId is not null && this._matchRule.OnFeedId != rssItem.FeedId)
                return default;

            if (!this._stringMatcher.IsMatch(rssItem.Title))
                return default;

            this.LastMatched = now;
            this._matchRule.LastMatched = now;
            return ImmutableArray.Create(this._matchRule);
        }
    }
}
