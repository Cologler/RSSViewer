using Jasily.ViewModel;
using RSSViewer.Configuration;
using RSSViewer.RulesDb;

using System.ComponentModel;

namespace RSSViewer.ViewModels
{
    public class MatchRuleViewModel : BaseViewModel
    {
        public MatchRule MatchRule { get; }

        public MatchRuleViewModel(MatchRule matchRule, int index, bool isAdded = false)
        {
            this.MatchRule = matchRule;
            this.Index = index;
            this.IsAdded = isAdded;
        }

        [ModelProperty]
        public string DisplayValue
        {
            get
            {
                return $"[{this.Index}] ({this.MatchRule.Mode}) {this.MatchRule.Argument}";
            }
        }

        public int Index { get; set; }

        public bool IsChanged { get; private set; }

        public void MarkChanged()
        {
            if (!this.IsAdded)
            {
                this.IsChanged = true;
            }
        }

        public bool IsAdded { get; private set; }
    }
}
