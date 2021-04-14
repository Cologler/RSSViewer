using AutoMapper;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.RulesDb;
using RSSViewer.Services;
using RSSViewer.ViewModels;

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RSSViewer.Windows
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MatchRuleListManagerWindow : Window
    {
        public MatchRuleListManagerWindow()
        {
            InitializeComponent();
            this.DataContext = new MatchRuleListViewModel();
            this.ViewModel.Load();
        }

        internal MatchRuleListViewModel ViewModel => (MatchRuleListViewModel)this.DataContext;

        private MatchRuleViewModel[] GetSelectedRules()
        {
            ListView activatedListView;
            if (this.RulesTabsPanel.SelectedItem == this.ActionRulesPanel)
            {
                activatedListView = this.ActionRulesListView;
            }
            else if (this.RulesTabsPanel.SelectedItem == this.SetTagRulesPanel)
            {
                activatedListView = this.SetTagRulesListView;
            } 
            else
            {
                throw new NotImplementedException();
            }
            
            return activatedListView.SelectedItems
                .OfType<MatchRuleViewModel>()
                .ToArray();
        }

        bool OpenEditRuleWindow(MatchRuleViewModel viewModel)
        {
            var win = new EditRuleWindow { Owner = this };
            if (viewModel.MatchRule.HandlerType == HandlerType.Action)
            {
                win.ViewModel.ParentSelectorView.ResetItems(this.ViewModel.ActionRulesViewModel.Items);
            }
            else
            {
                win.ViewModel.TagsViewModel.ResetItems(this.ViewModel.TagsViewModel.Values);
            } 
            win.Rule = viewModel.MatchRule;

            if (win.ShowDialog() == true)
            {
                if (viewModel.MatchRule.HandlerType == HandlerType.SetTag)
                {
                    var tagsvm = win.ViewModel.TagsViewModel;
                    if (tagsvm.SelectedItem is null)
                    {
                        var tagViewModel = tagsvm.Items.FirstOrDefault(z => z.Tag.TagName == tagsvm.TagName);

                        if (tagViewModel is null)
                        {
                            var tag = new Tag
                            {
                                Id = Guid.NewGuid().ToString(),
                                TagName = tagsvm.TagName
                            };
                            tagViewModel = new TagViewModel(tag) { IsAdded = true };
                            this.ViewModel.TagsViewModel.Add(tag.Id, tagViewModel);
                        }

                        viewModel.SetTag(tagViewModel.Tag);
                    }
                    else
                    {
                        viewModel.SetTag(tagsvm.SelectedItem.Tag);
                    }
                }

                viewModel.MarkChanged();
                viewModel.RefreshProperties();

                return true;
            }
            return false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.Save();
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Rules_Edit(object sender, RoutedEventArgs e)
        {
            var vm = this.GetSelectedRules().FirstOrDefault();
            if (vm == null)
                return;

            if (this.OpenEditRuleWindow(vm))
            {
                this.ViewModel.OnUpdateItem(vm);
            }
        }

        private void Rules_Clone(object sender, RoutedEventArgs e)
        {
            var viewModel = this.GetSelectedRules().FirstOrDefault();
            if (viewModel == null)
                return;

            var serviceProvider = App.RSSViewerHost.ServiceProvider;
            var mapper = serviceProvider.GetRequiredService<IMapper>();

            var newRule = mapper.Map<MatchRule>(viewModel.MatchRule);
            var newViewModel = new MatchRuleViewModel(newRule);
            if (this.OpenEditRuleWindow(newViewModel))
            {
                this.ViewModel.AddRule(newViewModel);
            }
        }

        private void Rules_Combine(object sender, RoutedEventArgs e)
        {
            this.ViewModel.ActionRulesViewModel.Combine(this.GetSelectedRules());
        }

        private void Rules_Remove(object sender, RoutedEventArgs e)
        {
            var svm = this.ViewModel;
            foreach (var vm in this.GetSelectedRules())
            {
                svm.RemoveRule(vm);
            }
        }

        private void AddActionRuleButton_Click(object sender, RoutedEventArgs e)
        {
            var newRule = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>().CreateActionRule();
            var viewModel = new MatchRuleViewModel(newRule);
            if (this.OpenEditRuleWindow(viewModel))
            {
                this.ViewModel.AddRule(viewModel);
            }
        }

        private void AddSetTagRuleButton_Click(object sender, RoutedEventArgs e)
        {
            var newRule = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>().CreateSetTagRule();
            var viewModel = new MatchRuleViewModel(newRule);
            if (this.OpenEditRuleWindow(viewModel))
            {
                this.ViewModel.AddRule(viewModel);
            }
        }

        private void Rules_EditTag(object sender, RoutedEventArgs e)
        {
            var viewModel = this.GetSelectedRules().FirstOrDefault();
            if (viewModel == null)
                return;

            if (viewModel.MatchRule.HandlerType != HandlerType.SetTag)
                throw new InvalidOperationException();

            if (viewModel.Tag is null)
                throw new NotImplementedException();

            var mapper = this.ViewModel.ServiceProvider.GetRequiredService<IMapper>();

            var editTagWin = new EditTagWindow { Owner = this };
            editTagWin.ViewModel.TagGroupsViewModel.ResetItems(this.ViewModel.TagsViewModel.Values.Select(z => z.Tag));
            mapper.Map(viewModel.Tag, editTagWin.ViewModel.TagViewModel);
            if (editTagWin.ShowDialog() == true)
            {
                mapper.Map(editTagWin.ViewModel.TagViewModel, viewModel.Tag);
                var tagViewModel = this.ViewModel.TagsViewModel[viewModel.Tag.Id];
                tagViewModel.IsChanged = true;
                foreach (var item in this.ViewModel.SetTagRulesViewModel.Items.Where(z => z.Tag == viewModel.Tag))
                {
                    item.RefreshProperties();
                }
            }
        }
    }
}
