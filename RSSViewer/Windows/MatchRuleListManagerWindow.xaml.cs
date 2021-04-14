using AutoMapper;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Configuration;
using RSSViewer.RulesDb;
using RSSViewer.Services;
using RSSViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            else
            {
                win.ViewModel.TagsViewModel.ResetItemsFromDb();
                win.ViewModel.TagsViewModel.AddItems(this.ViewModel.NewTags.Values.Select(z => new TagViewModel(z)));
            } 
            win.Rule = viewModel.MatchRule;

            if (win.ShowDialog() == true)
            {
                if (viewModel.MatchRule.HandlerType == HandlerType.SetTag)
                {
                    var tagsvm = win.ViewModel.TagsViewModel;
                    if (tagsvm.SelectedItem is null)
                    {
                        if (!this.ViewModel.NewTags.TryGetValue(tagsvm.TagName, out var tag))
                        {
                            tag = new Tag
                            {
                                Id = Guid.NewGuid().ToString(),
                                TagName = tagsvm.TagName
                            };
                            this.ViewModel.NewTags.Add(tag.TagName, tag);
                        }

                        viewModel.MatchRule.HandlerId = tag.Id;
                    }
                    else
                    {
                        viewModel.MatchRule.HandlerId = tagsvm.SelectedItem.Tag.Id;
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
            var newViewModel = new MatchRuleViewModel(newRule, true);
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
            var viewModel = new MatchRuleViewModel(newRule, true);
            if (this.OpenEditRuleWindow(viewModel))
            {
                this.ViewModel.AddRule(viewModel);
            }
        }

        private void AddSetTagRuleButton_Click(object sender, RoutedEventArgs e)
        {
            var newRule = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>().CreateSetTagRule();
            var viewModel = new MatchRuleViewModel(newRule, true);
            if (this.OpenEditRuleWindow(viewModel))
            {
                this.ViewModel.AddRule(viewModel);
            }
        }
    }
}
