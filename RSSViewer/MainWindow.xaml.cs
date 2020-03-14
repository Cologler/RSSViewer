using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.AcceptHandlers;
using RSSViewer.LocalDb;
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

            var sp = App.RSSViewerHost.ServiceProvider;
            foreach (var handler in sp.GetServices<IAcceptHandler>())
            {
                this.GroupsAcceptMenuItem.Items.Add(new MenuItem
                {
                    Header = handler.HandlerName,
                    Tag = handler
                });
                this.ItemsAcceptMenuItem.Items.Add(new MenuItem
                {
                    Header = handler.HandlerName,
                    Tag = handler
                });
            }

            this.DataContext = new RssViewViewModel();
            _ = this.ViewModel.SearchAsync();
        }

        public RssViewViewModel ViewModel => (RssViewViewModel) this.DataContext;

        private async void GroupsAcceptMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var handler = (IAcceptHandler)((MenuItem)e.OriginalSource).Tag;
            await this.ViewModel.AcceptAsync(
                this.GroupsListView.SelectedItems.OfType<RssItemGroupViewModel>().SelectMany(z => z.Items).ToArray(),
                handler);
        }

        private async void GroupsRejectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await this.ViewModel.RejectAsync(
                this.GroupsListView.SelectedItems.OfType<RssItemGroupViewModel>().SelectMany(z => z.Items).ToArray());
        }

        private async void ItemsAcceptMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var handler = (IAcceptHandler)((MenuItem)e.OriginalSource).Tag;
            await this.ViewModel.AcceptAsync(
                this.ItemsListView.SelectedItems.OfType<RssItemViewModel>().ToArray(),
                handler);
        }

        private async void ItemsRejectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await this.ViewModel.RejectAsync(
                this.ItemsListView.SelectedItems.OfType<RssItemViewModel>().ToArray());
        }

        private void ItemsCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.ItemsListView.SelectedItems.OfType<RssItemViewModel>().FirstOrDefault();
            if (vm is null) return;
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
    }
}
