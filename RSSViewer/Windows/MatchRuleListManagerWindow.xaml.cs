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
            this.DataContext = new MatchRuleListManagerViewModel();
            this.ViewModel.Load();
        }

        internal MatchRuleListManagerViewModel ViewModel => (MatchRuleListManagerViewModel)this.DataContext;

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
            win.ViewModel.ResetTags(this.ViewModel.TagsViewModel.Values);

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
            var selectedViewModel = this.GetSelectedRules();
            if (selectedViewModel.Length == 0)
                return;

            if (selectedViewModel.Where(z => z.MatchRule.HandlerType != HandlerType.SetTag).Any())
                throw new InvalidOperationException();

            if (selectedViewModel.Where(z => z.Tag is null).Any())
                throw new NotImplementedException();

            var editGroupOnly = selectedViewModel.Length > 1;

            var mapper = this.ViewModel.ServiceProvider.GetRequiredService<IMapper>();

            var tags = selectedViewModel.Select(z => z.Tag).ToList();

            var editTagWin = new EditTagWindow { Owner = this };
            editTagWin.ViewModel.TagGroupsViewModel.ResetItems(this.ViewModel.TagsViewModel.Values.Select(z => z.Tag));

            if (editGroupOnly)
            {
                editTagWin.ViewModel.TagViewModel.TagGroupName = 
                    tags.Select(z => z.TagGroupName ?? string.Empty).Distinct().First();
                editTagWin.ViewModel.TagViewModel.RefreshProperties();
            }
            else
            {
                mapper.Map(tags[0], editTagWin.ViewModel.TagViewModel);
            }

            if (editTagWin.ShowDialog() == true)
            {
                if (editGroupOnly)
                {
                    foreach (var tag in tags)
                    {
                        tag.TagGroupName = string.IsNullOrWhiteSpace(editTagWin.ViewModel.TagViewModel.TagGroupName) 
                            ? null 
                            : editTagWin.ViewModel.TagViewModel.TagGroupName.Trim();
                    }
                }
                else
                {
                    mapper.Map(editTagWin.ViewModel.TagViewModel, tags[0]);
                } 

                foreach (var tag in tags)
                {
                    this.ViewModel.TagsViewModel[tag.Id].IsChanged = true;
                }

                foreach (var item in this.ViewModel.SetTagRulesViewModel.Items.Where(z => tags.Contains(z.Tag)))
                {
                    item.RefreshProperties();
                }

                this.ViewModel.SetTagRulesViewModel.ItemsView.Refresh();
            }
        }
    }
}
