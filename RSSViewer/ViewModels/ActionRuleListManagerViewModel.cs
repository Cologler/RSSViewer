using RSSViewer.Helpers;
using RSSViewer.RssItemHandlers;
using RSSViewer.RulesDb;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace RSSViewer.ViewModels
{
    public class ActionRuleListManagerViewModel : ActionRuleListViewModel
    {
        private string _searchText;

        public ActionRuleListManagerViewModel()
        {
            this.RulesView = new ListCollectionView(this.Items);
        }

        public string SearchText
        {
            get => this._searchText;
            set
            {
                if (this.ChangeModelProperty(ref this._searchText, value))
                {
                    this.UpdateSearchView();
                }
            }
        }

        private void UpdateSearchView()
        {
            var value = this._searchText;
            if (string.IsNullOrWhiteSpace(value))
            {
                this.RulesView.Filter = null;
            }
            else
            {
                var r = this.Search(value.Trim());
                this.RulesView.Filter = (v) => r.Contains((MatchRuleViewModel)v);
            }
        }

        public override void OnUpdateItem(MatchRuleViewModel item)
        {
            base.OnUpdateItem(item);
            this.UpdateSearchView();
        }

        public ListCollectionView RulesView { get; }

        public List<MatchRuleViewModel> RemovedRules { get; } = new List<MatchRuleViewModel>();

        internal void AddRule(MatchRuleViewModel viewModel)
        {
            Debug.Assert(viewModel.IsAdded);

            this.Items.Add(viewModel);
            base.OnUpdateItem(viewModel);
        }

        internal void RemoveRule(MatchRuleViewModel ruleViewModel)
        {
            if (ruleViewModel is null)
                throw new ArgumentNullException(nameof(ruleViewModel));
            this.RemovedRules.Add(ruleViewModel);
            this.Items.Remove(ruleViewModel);
        }

        public void Combine(IList<MatchRuleViewModel> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            if (items.Any(z => z.MatchRule.HandlerType != HandlerType.Action))
                throw new InvalidOperationException();

            if (items.Count < 2)
                return;

            // check
            if (items.Select(z => z.MatchRule.Parent).ToHashSet().Count > 1)
            {
                MessageBox.Show("Unable combine: some item did not has the same parent.");
                return;
            }
            if (items.Select(z => z.MatchRule.HandlerId ?? KnownHandlerIds.DefaultHandlerId).ToHashSet().Count > 1)
            {
                MessageBox.Show("Unable combine: some item did not has the same handler.");
                return;
            }
            if (items.Select(z => z.MatchRule.OnFeedId).ToHashSet().Count > 1)
            {
                MessageBox.Show("Unable combine: some item did not has the same target feed.");
                return;
            }
            if (items.Select(z => z.MatchRule.IgnoreCase).ToHashSet().Count > 1)
            {
                MessageBox.Show("Unable combine: some item did not has the same ignore case flag.");
                return;
            }

            var newValueParts = new List<string>();
            foreach (var item in items)
            {
                if (item.MatchRule.Mode.IsStringMode())
                {
                    newValueParts.Add(RegexHelper.ConvertToRegexPattern(item.MatchRule.Mode, item.MatchRule.Argument));
                }
                else if (item.MatchRule.Mode != MatchMode.All)
                {
                    throw new NotImplementedException();
                }
            }
            newValueParts = newValueParts.Distinct().ToList();
            var newValue = string.Join("|", newValueParts);
            if (newValue.Length > 0 && !RegexUtils.IsValidPattern(newValue))
            {
                MessageBox.Show("Unable combine.");
                return;
            }

            DateTime theMaxTime = default;
            var totalMatched = 0;
            foreach (var item in items)
            {
                if (theMaxTime < item.MatchRule.LastMatched)
                    theMaxTime = item.MatchRule.LastMatched;
                totalMatched += item.MatchRule.TotalMatchedCount;
            }

            var target = items.FirstOrDefault(z => !z.IsAdded) ?? items[0];
            target.MatchRule.Mode = newValue.Length > 0 ? MatchMode.Regex : MatchMode.All;
            target.MatchRule.Argument = newValue;
            target.MatchRule.LastMatched = theMaxTime;
            target.MatchRule.TotalMatchedCount = totalMatched;
            target.MarkChanged();
            var targetRange = this.GetRange(target).Value;

            var itemsToRemove = items.Where(z => z != target).ToList();
            var childs = new List<MatchRuleViewModel>();
            var childsOldIndexes = new List<int>();
            foreach (var range in itemsToRemove.Select(this.GetRange).Select(z => z.Value).OrderBy(z => z.Start.Value))
            {
                var (offset, length) = range.GetOffsetAndLength(this.Items.Count);
                var indexes = Enumerable.Range(offset + 1, length - 1).ToList();
                childs.AddRange(indexes.Select(i => this.Items[i]));
                childsOldIndexes.AddRange(indexes);
            }
            if (childs.Count > 0)
            {
                Debug.Assert(!target.IsAdded && target.MatchRule.Id > 0);
                childsOldIndexes.Reverse();
                foreach (var index in childsOldIndexes)
                {
                    this.Items.RemoveAt(index);
                }
                var childBeginIndex = targetRange.End.Value;
                for (var i = 0; i < childs.Count; i++)
                {
                    childs[i].MatchRule.Parent = target.MatchRule;
                    childs[i].MarkChanged();
                    this.Items.Insert(childBeginIndex + i, childs[i]);
                }
            }

            foreach (var item in itemsToRemove)
            {
                this.RemoveRule(item);
            }

            this.OnUpdateItem(target);
            target.RefreshProperties();
        }
    }
}
