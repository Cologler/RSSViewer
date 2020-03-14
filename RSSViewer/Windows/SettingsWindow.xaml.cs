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
            vm.Load();
            this.DataContext = vm;
        }

        internal SettingsViewModel ViewModel => (SettingsViewModel)this.DataContext;

        private void RemoveAutoRejectMatchButton_Click(object sender, RoutedEventArgs e)
        {
            var svm = this.ViewModel;
            var vms = this.AutoRejectMatchesListView.SelectedItems
                .OfType<MatchStringConfViewModel>()
                .ToArray();
            foreach (var vm in vms)
            {
                svm.AutoRejectView.Matches.Remove(vm);
            }
        }

        private void AddAutoRejectMatchButton_Click(object sender, RoutedEventArgs e)
        {
            var conf = new MatchStringConf
            {
                MatchMode = MatchStringMode.Contains,
                AsStringComparison = StringComparison.OrdinalIgnoreCase,
                MatchValue = string.Empty
            };
            var win = new EditStringMatcherWindow { Owner = this };
            win.LoadFromConf(conf);
            if (win.ShowDialog() == true)
            {
                win.WriteToConf(conf);
                this.ViewModel.AutoRejectView.Add(conf);
            }
        }

        private void EditAutoRejectMatchButton_Click(object sender, RoutedEventArgs e)
        {
            var conf = this.AutoRejectMatchesListView.SelectedItems
                .OfType<MatchStringConfViewModel>()
                .FirstOrDefault()?.Conf;

            if (conf == null)
                return;

            var win = new EditStringMatcherWindow { Owner = this };
            win.LoadFromConf(conf);
            if (win.ShowDialog() == true)
            {
                win.WriteToConf(conf);
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
