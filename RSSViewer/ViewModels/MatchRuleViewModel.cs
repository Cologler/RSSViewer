using Jasily.ViewModel;

using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.RssItemHandlers;
using RSSViewer.RulesDb;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace RSSViewer.ViewModels
{
    public class MatchRuleViewModel : BaseViewModel
    {
        private static int _newIdSeed;

        private static int NewId() => Interlocked.Decrement(ref _newIdSeed);

        public static readonly MatchRuleViewModel None = new("< None >");
        public static readonly MatchRuleViewModel NoParent = new("< None Parent >");

        private readonly string _displayValue;

        public MatchRule MatchRule { get; }

        public MatchRuleViewModel(MatchRule matchRule, bool isAdded = false)
        {
            this.MatchRule = matchRule ?? throw new ArgumentNullException(nameof(matchRule));
            this.IsAdded = isAdded;

            Debug.Assert(matchRule.Id >= 0);
            this.Id = matchRule.Id > 0 ? matchRule.Id : NewId();
            Debug.Assert(this.Id != 0);
        }

        private MatchRuleViewModel(string displayValue)
        {
            this._displayValue = displayValue ?? throw new ArgumentNullException(nameof(displayValue));
            this.Id = NewId();
            Debug.Assert(this.Id != 0);
        }

        public int Id { get; }

        [ModelProperty]
        public string DisplayValue
        {
            get
            {
                if (this._displayValue is not null)
                    return this._displayValue;

                Debug.Assert(this.MatchRule is not null);

                var sb = new StringBuilder();
                sb.Append(this.DisplayPrefix);

                if (!string.IsNullOrEmpty(this.MatchRule.DisplayName))
                {
                    sb.Append(this.MatchRule.DisplayName);
                }
                else
                {
                    sb.Append(this.MatchRule.ToDebugString());
                    if (this.MatchRule.OnFeedId is not null)
                        sb.Append(" @").Append(this.MatchRule.OnFeedId);
                    if (this.Handler is IRssItemHandler handler)
                    {
                        if (handler.Id == KnownHandlerIds.EmptyHandlerId)
                        {
                            sb.Append(" (group) ");
                        }
                        else
                        {
                            sb.Append("  ->  ").Append(this.Handler.ShortDescription);
                        }
                    }
                } 
                
                return sb.ToString();
            }
        }

        public IRssItemHandler Handler { get; set; }

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
