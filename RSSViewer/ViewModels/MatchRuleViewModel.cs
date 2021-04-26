using Jasily.ViewModel;

using RSSViewer.Abstractions;
using RSSViewer.RssItemHandlers;
using RSSViewer.RulesDb;
using RSSViewer.ViewModels.Abstractions;

using System;
using System.Diagnostics;
using System.Linq;
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
        private readonly IMatchRuleViewModelDependencies _dependencies;

        public MatchRule MatchRule { get; }

        public MatchRuleViewModel(MatchRule matchRule, IMatchRuleViewModelDependencies dependencies)
        {
            this.MatchRule = matchRule ?? throw new ArgumentNullException(nameof(matchRule));
            this._dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));

            // id
            Debug.Assert(matchRule.Id >= 0);
            this.Id = matchRule.Id > 0 ? matchRule.Id : NewId();
            Debug.Assert(this.Id != 0);

            // 
            if (matchRule.HandlerType == HandlerType.SetTag)
            {
                if (matchRule.HandlerId is not null)
                {
                    this.Tag = dependencies.FindTag(matchRule.HandlerId);
                }
                else
                {
                    Debug.Assert(this.Id < 0); // new
                }
            }
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

                if (this.MatchRule.HandlerType == HandlerType.Action)
                {
                    sb.Append(this.DisplayPrefix);
                }

                if (!string.IsNullOrEmpty(this.MatchRule.DisplayName))
                {
                    sb.Append(this.MatchRule.DisplayName);
                }
                else
                {
                    if (this.MatchRule.HandlerType == HandlerType.Action)
                    {
                        if (this.MatchRule.Mode.IsStringMode())
                        {
                            sb.Append(this.MatchRule.ToDebugString());
                        }
                        else if (this.MatchRule.Mode == MatchMode.Tags) 
                        {
                            sb.Append(
                                string.Join(
                                    "+", 
                                    this.MatchRule
                                        .AsTagsMatch()
                                        .Select(z => this._dependencies.FindTag(z)?.ToShortString() ?? "[???]"))
                                );
                        }

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
                    else if (this.MatchRule.HandlerType == HandlerType.SetTag)
                    {
                        if (this.Tag is not null)
                        {
                            sb.Append(this.Tag.ToString()).Append("  ");
                        }

                        sb.Append(this.MatchRule.ToDebugString());
                    }
                }
                
                return sb.ToString();
            }
        }

        public IRssItemHandler Handler { get; set; }

        public Tag Tag { get; private set; }

        public bool IsChanged { get; private set; }

        public void SetTag(Tag tag)
        {
            if (tag is null)
                throw new ArgumentNullException(nameof(tag));
            if (this.MatchRule.HandlerType != HandlerType.SetTag)
                throw new InvalidOperationException();

            this.Tag = tag;
            this.MatchRule.HandlerId = tag.Id;
            this.MarkChanged();
        }

        public void MarkChanged()
        {
            if (!this.IsAdded)
            {
                this.IsChanged = true;
            }
        }

        public bool IsAdded => this.MatchRule is not null && this.MatchRule.Id == 0;

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
