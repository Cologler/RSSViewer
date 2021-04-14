using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Controls;
using RSSViewer.RulesDb;
using RSSViewer.Services;
using RSSViewer.ViewModels;
using RSSViewer.Windows;

using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

            var viewModel = new RssViewViewModel();
            viewModel.SelectFirst();
            this.DataContext = viewModel;
            _ = viewModel.Items.First().RefreshContentAsync(0);
        }

        private void RefreshAcceptHandlers()
        {
            // clear
            var groupsHandlerContextMenu = (ContextMenu) this.SessionPanel.FindResource("GroupsHandlerContextMenu");
            var ItemsHandlerContextMenu = (ContextMenu)this.SessionPanel.FindResource("ItemsHandlerContextMenu");

            groupsHandlerContextMenu.Items.Clear();
            ItemsHandlerContextMenu.Items.OfType<MenuItem>()
                .Where(z => z.Tag is IRssItemHandler)
                .ToList()
                .ForEach(ItemsHandlerContextMenu.Items.Remove);

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
                groupsHandlerContextMenu.Items.Add(groupHandlerMenuItem);

                var itemHandlerMenuItem = new MenuItem
                {
                    Header = handler.HandlerName,
                    Tag = handler
                };
                itemHandlerMenuItem.Click += this.ItemsHandlerMenuItem_Click;
                ItemsHandlerContextMenu.Items.Add(itemHandlerMenuItem);
            }
        }

        public RssViewViewModel ViewModel => (RssViewViewModel)this.DataContext;

        public SessionViewModel CurrentSession => this.ViewModel.SelectedItem;

        private static IEnumerable GetSelectedTargets(MenuItem menuItem)
        {
            if (menuItem is null)
                throw new ArgumentNullException(nameof(menuItem));

            ContextMenu contextMenu;
            do
            {
                contextMenu = menuItem.Parent as ContextMenu;
            } while (contextMenu is null && menuItem.Parent is not null);

            Debug.Assert(contextMenu is not null);

            if (contextMenu.PlacementTarget is ListBox listSources)
            {
                return listSources.SelectedItems;
            }

            return Enumerable.Empty<object>();
        }

        private async void GroupHandlerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)e.OriginalSource;
            var selectedTargets = GetSelectedTargets(menuItem);
            var handler = (IRssItemHandler)menuItem.Tag;
            await this.CurrentSession.HandleAsync(
                selectedTargets.Cast<RssItemGroupViewModel>()
                    .SelectMany(z => z.Items)
                    .Distinct()
                    .ToArray(),
                handler);
        }

        private async void ItemsHandlerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)e.OriginalSource;
            var selectedTargets = GetSelectedTargets(menuItem);
            var handler = (IRssItemHandler)menuItem.Tag;
            await this.CurrentSession.HandleAsync(
                selectedTargets.Cast<RssItemViewModel>().ToArray(),
                handler);
        }

        private void ItemsCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)e.OriginalSource;
            var viewModel = GetSelectedTargets(menuItem).Cast<RssItemViewModel>().FirstOrDefault();
            if (viewModel is null) 
                return;

            var kws = App.RSSViewerHost.ServiceProvider.GetRequiredService<KeywordsService>();
            var kw = kws.GetKeywords(viewModel.RssItem);
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

        private async void AddAutoRuleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)e.OriginalSource;
            var viewModel = GetSelectedTargets(menuItem).Cast<RssItemViewModel>().FirstOrDefault();
            if (viewModel is null)
                return;

            var sp = App.RSSViewerHost.ServiceProvider;
            var kws = sp.GetRequiredService<KeywordsService>();
            var kw = kws.GetKeywords(viewModel.RssItem);
            if (StringsPickerWindow.TryPickString(this, kw, out var text))
            {
                var rule = sp.GetRequiredService<ConfigService>()
                    .CreateActionRule();

                var title = viewModel.RssItem.Title ?? string.Empty;
                if (title.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                {
                    rule.Mode = MatchMode.StartsWith;
                }
                else
                {
                    rule.Mode = MatchMode.Contains;
                }
                rule.IgnoreCase = true;
                rule.Argument = text;

                if (EditRuleWindow.Edit(this, rule))
                {
                    var cs = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>();
                    await cs.AddMatchRuleAsync(rule);
                }
            }
        }

        private void OpenRulesManagerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var win = new MatchRuleListManagerWindow
            {
                Owner = this
            };

            win.ShowDialog();
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
            await this.CurrentSession.RefreshContentAsync();
        }

        private void GroupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.CurrentSession.UpdateSelectedItems(((ListView)sender).SelectedItems.OfType<RssItemGroupViewModel>());

            this.ItemsListView_SelectionChanged(sender, e);
        }

        private void ItemsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ViewModel.AnalyticsView.SelectedCount = ((ListView)sender).SelectedItems.OfType<IRssItemsCount>()
                .Select(z => z.Count)
                .Sum();
        }

        private void UndoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _ = App.RSSViewerHost.Modify().UndoAsync();
        }

        private void RemoveTab_Click(object sender, RoutedEventArgs e)
        {
            var session = (SessionViewModel)((MenuItem)sender).DataContext;
            Debug.Assert(session is not null);

            if (session.Removable)
            {
                this.ViewModel.SelectFirst();
                this.ViewModel.ItemsView.Remove(session);
                session.Dispose();
            }
        }

        private void OpenSettingsWindowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow() { Owner = this }.ShowDialog();
        }
    }
}
