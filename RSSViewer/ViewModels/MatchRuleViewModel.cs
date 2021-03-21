using Jasily.ViewModel;
using RSSViewer.Configuration;
using RSSViewer.RulesDb;

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace RSSViewer.ViewModels
{
    public class MatchRuleViewModel : BaseViewModel
    {
        public static MatchRuleViewModel None = new(null, false);

        public MatchRule MatchRule { get; }

        public MatchRuleViewModel(MatchRule matchRule, bool isAdded = false)
        {
            this.MatchRule = matchRule;
            this.IsAdded = isAdded;
        }

        [ModelProperty]
        public string DisplayValue => this.DisplayPrefix + (this.MatchRule?.ToDebugString() ?? "< None >");

        public bool IsChanged { get; private set; }

        public void MarkChanged()
        {
            if (!this.IsAdded)
            {
                this.IsChanged = true;
            }
        }

        public bool IsAdded { get; private set; }

        public string DisplayPrefix { get; set; } = string.Empty;

        public int TreeLevel { get; set; } = 0;
    }
}
