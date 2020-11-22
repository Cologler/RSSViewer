using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.AcceptHandlers;
using RSSViewer.Configuration;
using RSSViewer.Controls;
using RSSViewer.LocalDb;
using RSSViewer.RulesDb;
using RSSViewer.Services;
using RSSViewer.ViewModels;
using RSSViewer.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RSSViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            App.RSSViewerHost.ServiceProvider.GetRequiredService<RssItemHandlersService>()
                .AcceptHandlersChanged += (_, __) => this.RefreshAcceptHandlers();
            this.RefreshAcceptHandlers();

            this.DataContext = new RssViewViewModel();
            _ = this.ViewModel.RefreshContentAsync(0);
        }

        private void RefreshAcceptHandlers()
        {
            // clear
            this.GroupHandlerMenuItems.Items.Clear();
            this.ItemContextMenu.Items.OfType<MenuItem>()
                .Where(z => z.Tag is IRssItemHandler)
                .ToList()
                .ForEach(this.ItemContextMenu.Items.Remove);

            // add
            var serviceProvider = App.RSSViewerHost.ServiceProvider;
            foreach (var handler in serviceProvider.GetRequiredService<RssItemHandlersService>().GetHandlers())
            {
                var groupHandlerMenuItem = new MenuItem
                {
                    Header = handler.HandlerName,
                    Tag = handler
                };
                groupHandlerMenuItem.Click += this.GroupHandlerMenuItem_Click;
                this.GroupHandlerMenuItems.Items.Add(groupHandlerMenuItem);

                var itemHandlerMenuItem = new MenuItem
                {
                    Header = handler.HandlerName,
                    Tag = handler
                };
                itemHandlerMenuItem.Click += this.ItemsHandlerMenuItem_Click;
                this.ItemContextMenu.Items.Add(itemHandlerMenuItem);
            }
        }

        public RssViewViewModel ViewModel => (RssViewViewModel) this.DataContext;

        private async void GroupHandlerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var handler = (IRssItemHandler)((MenuItem)e.OriginalSource).Tag;
            await this.ViewModel.HandleAsync(
                this.GroupsListView.SelectedItems.OfType<RssItemGroupViewModel>()
                    .SelectMany(z => z.Items)
                    .Distinct()
                    .ToArray(),
                handler);
        }

        private async void ItemsHandlerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var handler = (IRssItemHandler)((MenuItem)e.OriginalSource).Tag;
            await this.ViewModel.HandleAsync(
                this.ItemsListView.SelectedItems.OfType<RssItemViewModel>().ToArray(),
                handler);
        }

        private void ItemsCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.ItemsListView.SelectedItems.OfType<RssItemViewModel>().FirstOrDefault();
            if (vm is null) 
                return;

            var kws = App.RSSViewerHost.ServiceProvider.GetRequiredService<KeywordsService>();
            var kw = kws.GetKeywords(vm.RssItem);
            if (StringsPickerWindow.TryPickString(this, kw, out var text))
            {
                try
                {
                    Clipboard.SetText(text);
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Unable to copy: {exc}");
                }
            }
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow
            {
                Owner = this
            };

            win.ShowDialog();
        }

        private async void AddAutoRuleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.ItemsListView.SelectedItems.OfType<RssItemViewModel>().FirstOrDefault();
            if (vm is null)
                return;

            var sp = App.RSSViewerHost.ServiceProvider;
            var kws = sp.GetRequiredService<KeywordsService>();
            var kw = kws.GetKeywords(vm.RssItem);
            if (StringsPickerWindow.TryPickString(this, kw, out var text))
            {
                var rule = sp.GetRequiredService<ConfigService>()
                    .CreateMatchRule();
                rule.Mode = MatchMode.Contains;
                rule.OptionsAsStringComparison = StringComparison.OrdinalIgnoreCase;
                rule.Argument = text;

                if (EditRuleWindow.EditConf(this, rule))
                {
                    var cs = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>();
                    await cs.AddMatchRuleAsync(rule);
                }
            }
        }

        private void RunSyncSourceOnceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (EditObjectControl.CreateSyncSourceConf(this, out var conf))
            {

            }
        }

        private async void RunAllRulesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var mi = (MenuItem)e.OriginalSource;
            mi.IsEnabled = false;
            await App.RSSViewerHost.ServiceProvider.GetRequiredService<RunRulesService>()
                .RunAllRulesAsync();
            mi.IsEnabled = true;
            await this.ViewModel.RefreshContentAsync();
        }

        private void GroupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.UpdateSelectedItems(((ListView)sender).SelectedItems.OfType<RssItemGroupViewModel>());

            this.ItemsListView_SelectionChanged(sender, e);
        }

        private void ItemsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.Analytics.Selected = ((ListView)sender).SelectedItems.OfType<IRssItemsCount>()
                .Select(z => z.Count)
                .Sum();
        }
    }
}
