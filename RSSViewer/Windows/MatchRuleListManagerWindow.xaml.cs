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

        private MatchRuleViewModel[] SelectedAutoRules
        {
            get
            {
                return this.AutoRejectMatchesListView.SelectedItems
                    .OfType<MatchRuleViewModel>()
                    .ToArray();
            }
            set
            {
                this.AutoRejectMatchesListView.SelectedItems.Clear();
                foreach (var item in value)
                {
                    this.AutoRejectMatchesListView.SelectedItems.Add(item);
                }
            }
        }

        bool OpenEditRuleWindow(MatchRule rule)
        {
            var win = new EditRuleWindow { Owner = this };
            win.ViewModel.ParentSelectorView.ResetItems(this.ViewModel.Items);
            win.Rule = rule;
            return win.ShowDialog() == true;
        }

        private void AutoRules_Edit(object sender, RoutedEventArgs e)
        {
            var vm = this.SelectedAutoRules.FirstOrDefault();
            if (vm == null)
                return;

            if (this.OpenEditRuleWindow(vm.MatchRule))
            {
                this.ViewModel.OnUpdateItem(vm);
                vm.MarkChanged();
                vm.RefreshProperties();
            }
        }

        private void AutoRules_Clone(object sender, RoutedEventArgs e)
        {
            if (this.AutoRejectMatchesListView.SelectedItem is MatchRuleViewModel viewModel)
            {
                var serviceProvider = App.RSSViewerHost.ServiceProvider;
                var mapper = serviceProvider.GetRequiredService<IMapper>();

                var newMatchRule = mapper.Map<MatchRule>(viewModel.MatchRule);
                if (this.OpenEditRuleWindow(newMatchRule))
                {
                    this.ViewModel.AddRule(newMatchRule);
                }
            }
        }

        private void AutoRules_Combine(object sender, RoutedEventArgs e)
        {
            this.ViewModel.Combine(this.SelectedAutoRules);
        }

        private void AutoRules_Remove(object sender, RoutedEventArgs e)
        {
            var svm = this.ViewModel;
            foreach (var vm in this.SelectedAutoRules)
            {
                svm.RemoveRule(vm);
            }
        }

        private void AddAutoRejectMatchButton_Click(object sender, RoutedEventArgs e)
        {
            var newRule = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>().CreateMatchRule();
            if (this.OpenEditRuleWindow(newRule))
            {
                this.ViewModel.AddRule(newRule);
            }
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
    }
}
