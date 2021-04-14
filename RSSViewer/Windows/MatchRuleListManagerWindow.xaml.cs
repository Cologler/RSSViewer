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
        }

        internal MatchRuleListManagerViewModel ViewModel => (MatchRuleListManagerViewModel)this.DataContext;

        private MatchRuleViewModel[] GetSelectedRules()
        {
            ListView activatedListView;
            if (this.RulesTabsPanel.SelectedItem == this.ActionRulesPanel)
            {
                activatedListView = this.ActionRulesListView;
            }
            else if (this.RulesTabsPanel.SelectedItem == this.ActionRulesPanel)
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

        bool OpenEditRuleWindow(MatchRule rule)
        {
            var win = new EditRuleWindow { Owner = this };
            if (rule.HandlerType == HandlerType.Action)
            {
                win.ViewModel.ParentSelectorView.ResetItems(this.ViewModel.ActionRulesViewModel.Items);
            }
            win.Rule = rule;
            return win.ShowDialog() == true;
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

            if (this.OpenEditRuleWindow(vm.MatchRule))
            {
                this.ViewModel.OnUpdateItem(vm);
                vm.MarkChanged();
                vm.RefreshProperties();
            }
        }

        private void Rules_Clone(object sender, RoutedEventArgs e)
        {
            var viewModel = this.GetSelectedRules().FirstOrDefault();
            if (viewModel == null)
                return;

            var serviceProvider = App.RSSViewerHost.ServiceProvider;
            var mapper = serviceProvider.GetRequiredService<IMapper>();

            var newMatchRule = mapper.Map<MatchRule>(viewModel.MatchRule);
            if (this.OpenEditRuleWindow(newMatchRule))
            {
                this.ViewModel.AddRule(newMatchRule);
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

        private void AddAutoRejectMatchButton_Click(object sender, RoutedEventArgs e)
        {
            var newRule = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>().CreateActionRule();
            if (this.OpenEditRuleWindow(newRule))
            {
                this.ViewModel.AddRule(newRule);
            }
        }

        private void AddSetTagRuleButton_Click(object sender, RoutedEventArgs e)
        {
            var newRule = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>().CreateSetTagRule();
            if (this.OpenEditRuleWindow(newRule))
            {
                this.ViewModel.AddRule(newRule);
            }
        }
    }
}
