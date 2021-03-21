using RSSViewer.Configuration;
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
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            var vm = new SettingsViewModel();
            _ = vm.Load();
            this.Load(vm);
        }

        private void Load(SettingsViewModel settingsViewModel)
        {
            this.DataContext = settingsViewModel;
        }

        internal SettingsViewModel ViewModel => (SettingsViewModel)this.DataContext;

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

        private void AutoRules_Edit(object sender, RoutedEventArgs e)
        {
            var vm = this.SelectedAutoRules.FirstOrDefault();
            if (vm == null)
                return;

            if (EditRuleWindow.EditConf(this, vm.MatchRule))
            {
                this.ViewModel.AutoRulesView.OnUpdateItem(vm);
                vm.MarkChanged();
                vm.RefreshProperties();
            }
        }

        private void AutoRules_Combine(object sender, RoutedEventArgs e)
        {
            this.ViewModel.AutoRulesView.Combine(this.SelectedAutoRules);
        }

        private void AutoRules_Remove(object sender, RoutedEventArgs e)
        {
            var svm = this.ViewModel;
            foreach (var vm in this.SelectedAutoRules)
            {
                svm.AutoRulesView.RemoveRule(vm);
            }
        }

        private void AddAutoRejectMatchButton_Click(object sender, RoutedEventArgs e)
        {
            if (EditRuleWindow.TryCreateConf(this, out var conf))
            {
                this.ViewModel.AutoRulesView.AddRule(conf);
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
