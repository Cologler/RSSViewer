using System;
using System.Collections.Generic;
using System.Linq;

using TreeCollections;

namespace RSSViewer.RulesDb
{
    public static class MatchRuleExtensions
    {
        private static Func<MatchRule, int> GetIdFunc = r => r?.Id ?? -1;

        public static MutableEntityTreeNode<int, MatchRule> BuildTree(this IList<MatchRule> rules, out List<MatchRule> unreachableItems)
        {
            if (rules is null)
                throw new ArgumentNullException(nameof(rules));

            var lookup = rules.ToLookup(z => z.ParentId);
            var root = new MutableEntityTreeNode<int, MatchRule>(GetIdFunc, null);
            void Walk(MutableEntityTreeNode<int, MatchRule> ruleNode)
            {
                foreach (var c in lookup[ruleNode.Item.Id])
                {
                    Walk(ruleNode.AddChild(c));
                }
            }
            foreach (var r in lookup[null])
            {
                Walk(root.AddChild(r));
            }
            unreachableItems = rules.Except(root.Select(z => z.Item)).ToList();
            return root;
        }
    }
}
