using RSSViewer.RulesDb;
using RSSViewer.ViewModels.Bases;

namespace RSSViewer.ViewModels
{
    public class MatchRuleListManagerViewModel : BaseViewModel
    {
        public ActionRuleListManagerViewModel ActionRulesViewModel { get; } = new();

        public void Save()
        {
            this.ActionRulesViewModel.Save();
        }

        public void OnUpdateItem(MatchRuleViewModel viewModel)
        {
            if (viewModel.MatchRule.HandlerType == HandlerType.Action)
            {
                this.ActionRulesViewModel.OnUpdateItem(viewModel);
            }
        }

        public void AddRule(MatchRule matchRule)
        {
            if (matchRule.HandlerType == HandlerType.Action)
            {
                this.ActionRulesViewModel.AddRule(matchRule);
            }
        }

        public void RemoveRule(MatchRuleViewModel viewModel)
        {
            if (viewModel.MatchRule.HandlerType == HandlerType.Action)
            {
                this.ActionRulesViewModel.RemoveRule(viewModel);
            }
        }
    }
}
