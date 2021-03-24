using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using RSSViewer.Abstractions;
using RSSViewer.RulesDb;
using RSSViewer.StringMatchers;

namespace RSSViewer.Helpers
{
    public class RuleMatchTree
    {
        private readonly object _syncRoot = new();
        private readonly StringMatcherFactory _stringMatcherFactory;
        private readonly List<RuleMatchTreeNode> _nodes = new();
        private ImmutableArray<RuleMatchTreeNode> _rootNodes;

        public RuleMatchTree(StringMatcherFactory stringMatcherFactory)
        {
            this._stringMatcherFactory = stringMatcherFactory ?? throw new ArgumentNullException(nameof(stringMatcherFactory));
        }

        private RuleMatchTreeNode CreateNode(MatchRule rule)
            => new RuleMatchTreeNode(rule, this._stringMatcherFactory.Create(rule));

        /// <summary>
        /// this should run inside a lock block.
        /// </summary>
        private void RebuildTree()
        {
            var nodesById = this._nodes.ToDictionary(z => z.Rule.Id);
            var rootNodes = new List<RuleMatchTreeNode>();
            foreach (var n in this._nodes)
            {
                if (n.Rule.ParentId is null)
                {
                    rootNodes.Add(n);
                }
                else
                {
                    nodesById.GetValueOrDefault(n.Rule.ParentId.Value)?.AddSubNode(n);
                }
            }
            this._rootNodes = rootNodes.ToImmutableArray();
        }

        public void AddOrUpdate(IEnumerable<MatchRule> rules)
        {
            if (rules is null)
                throw new ArgumentNullException(nameof(rules));

            lock (this._syncRoot)
            {
                var newRuleIds = rules.Select(z => z.Id).ToHashSet();
                var newNodes = this._nodes
                    .Where(z => !newRuleIds.Contains(z.Rule.Id))
                    .Select(z => z.DeepClone(false))
                    .Concat(rules.Select(this.CreateNode))
                    .ToList();
                this._nodes.Clear();
                this._nodes.AddRange(newNodes);

                // create tree.
                this.RebuildTree();
            }
        }

        public ImmutableArray<MatchRule> TryFindMatchedRule(IPartialRssItem rssItem, DateTime now)
        {
            if (rssItem is null)
                throw new ArgumentNullException(nameof(rssItem));

            var rootNodes = this._rootNodes; // make a clone
            foreach (var node in rootNodes)
            {
                var rulesChain = node.TryFindMatchedRule(rssItem, now, false);
                if (!rulesChain.IsDefault)
                {
                    Debug.Assert(!rulesChain.IsEmpty);
                    return rulesChain;
                }
            }

            return default;
        }

        public RuleMatchTree DeepClone()
        {
            RuleMatchTree newOne = new RuleMatchTree(this._stringMatcherFactory);
            lock (this._syncRoot)
            {
                // cannot use RuleMatchTreeNode.DeepClone, which will break the real _nodes reference.
                newOne.AddOrUpdate(this._nodes.Select(z => z.Rule));
            }
            newOne.RebuildTree();
            return newOne;
        }

        public void DisableAll()
        {
            lock (this._syncRoot)
            {
                foreach (var node in this._nodes)
                {
                    node.IsMatchable = false;
                }
            }
        }

        public void EnableFor(IEnumerable<MatchRule> rules)
        {
            if (rules is null)
                throw new ArgumentNullException(nameof(rules));

            lock (this._syncRoot)
            {
                var mapById = _nodes.ToDictionary(z => z.Rule.Id);
                foreach (var rule in rules)
                {
                    var node = mapById.GetValueOrDefault(rule.Id);
                    if (node is not null)
                    {
                        node.IsMatchable = true;
                    }
                }
            }
        }
    }
}
