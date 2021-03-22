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
        public static MatchRuleViewModel None = new("< None >");
        public static MatchRuleViewModel NoParent = new("< None Parent >");

        private readonly string _displayValue;

        public MatchRule MatchRule { get; }

        public MatchRuleViewModel(MatchRule matchRule, bool isAdded = false)
        {
            this.MatchRule = matchRule;
            this.IsAdded = isAdded;
        }

        private MatchRuleViewModel(string displayValue)
        {
            this._displayValue = displayValue;
        }

        [ModelProperty]
        public string DisplayValue => this._displayValue ?? (this.DisplayPrefix + this.MatchRule?.ToDebugString());

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

        public void RefreshDisplayPrefix()
        {
            if (this.TreeLevel > 0)
            {
                // char copy from https://en.wikipedia.org/wiki/Box-drawing_character
                this.DisplayPrefix = new string(' ', this.TreeLevel) + "├ ";

            }
            else
            {
                this.DisplayPrefix = string.Empty;
            }
            this.RefreshProperties();
        }
    }
}
