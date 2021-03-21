using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RSSViewer.Abstractions;
using RSSViewer.LocalDb;
using RSSViewer.RulesDb;
using RSSViewer.StringMatchers;

namespace RSSViewer.Helpers
{
    public class RuleMatchTreeNode
    {
        private readonly object _syncRoot = new();
        private readonly IStringMatcher _stringMatcher;
        private ImmutableArray<RuleMatchTreeNode> _branchs;

        public RuleMatchTreeNode(MatchRule matchRule, IStringMatcher stringMatcher)
        {
            this.Rule = matchRule ?? throw new ArgumentNullException(nameof(matchRule));
            this._stringMatcher = stringMatcher ?? throw new ArgumentNullException(nameof(stringMatcher));
            this.LastMatched = matchRule.LastMatched;
        }

        public DateTime LastMatched { get; private set; }

        /// <summary>
        /// Get or set if this node is matchable, which use to only match for childs without this.
        /// </summary>
        public bool IsMatchable { get; set; } = true;

        /// <summary>
        /// Get whether any of childs or this is matchable.
        /// </summary>
        public bool IsActivated
        {
            get
            {
                var childs = this._branchs;
                return this.IsMatchable || !childs.IsDefaultOrEmpty && childs.Any(z => z.IsActivated);
            }
        }

        public MatchRule Rule { get; }

        public bool IsMatch(IPartialRssItem rssItem)
        {
            if (this.Rule.OnFeedId is not null && this.Rule.OnFeedId != rssItem.FeedId)
                return false;

            return this._stringMatcher.IsMatch(rssItem.Title);
        }

        /// <summary>
        /// return the chained rules, or <see langword="default"/> if not match.
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

            ImmutableArray<MatchRule> rulesChain = default;
            // childs
            if (!this._branchs.IsDefault)
            {
                foreach (var child in this._branchs)
                {
                    rulesChain = child.TryFindMatchedRule(rssItem, now);
                    if (!rulesChain.IsDefault)
                    {
                        rulesChain = ImmutableArray.Create(this.Rule).AddRange(rulesChain);
                        break;
                    }
                }
            }

            if (rulesChain.IsDefault && this.IsMatchable)
            {
                rulesChain = ImmutableArray.Create(this.Rule);
            }

            if (!rulesChain.IsDefault)
            {
                Debug.Assert(!rulesChain.IsEmpty);
                lock (this._syncRoot)
                {
                    if (now > this.LastMatched)
                        this.LastMatched = now;
                }
                return rulesChain;
            }

            return default;
        }

        /// <summary>
        /// add sub node as child to this node.
        /// </summary>
        /// <param name="node"></param>
        public void AddSubNode(RuleMatchTreeNode node)
        {
            if (node.Rule.ParentId != this.Rule.Id)
                throw new InvalidOperationException();
            lock (this._syncRoot) this._branchs = this._branchs.Add(node);
        }

        /// <summary>
        /// find node in entire tree, include this node.
        /// </summary>
        /// <param name="ruleId"></param>
        /// <returns></returns>
        public RuleMatchTreeNode FindNode(int ruleId)
        {
            if (this.Rule.Id == ruleId)
                return this;

            if (this._branchs.IsDefault)
                return null;

            return this._branchs.Select(z => z.FindNode(ruleId)).FirstOrDefault(z => z is not null);
        }

        public RuleMatchTreeNode DeepClone(bool includeChilds)
        {
            var newNode = new RuleMatchTreeNode(this.Rule, this._stringMatcher);
            lock (this._syncRoot)
            {
                newNode.LastMatched = this.LastMatched;
                if (includeChilds)
                {
                    newNode._branchs = this._branchs.Select(z => z.DeepClone(true)).ToImmutableArray();
                }
            }
            return newNode;
        }
    }
}
