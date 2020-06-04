using Jasily.ViewModel;
using RSSViewer.Configuration;
using RSSViewer.RulesDb;

namespace RSSViewer.ViewModels
{
    public class MatchRuleViewModel : BaseViewModel
    {
        public MatchRule MatchRule { get; }

        public MatchRuleViewModel(MatchRule matchRule)
        {
            this.MatchRule = matchRule;
        }

        [ModelProperty]
        public string DisplayValue
        {
            get
            {
                return $"({this.MatchRule.Mode}) {this.MatchRule.Argument}";
            }
        }
    }
}
