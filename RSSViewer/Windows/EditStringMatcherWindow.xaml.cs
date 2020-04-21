using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Configuration;
using RSSViewer.LocalDb;
using RSSViewer.Services;
using RSSViewer.StringMatchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
    /// EditStringMatcherWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EditStringMatcherWindow : Window
    {
        public EditStringMatcherWindow()
        {
            this.InitializeComponent();
        }

        public MatchStringConf CreateMatchStringConf()
        {
            var conf = new MatchStringConf();
            this.WriteToConf(conf);
            return conf;
        }

        public void LoadFromConf(MatchStringConf conf)
        {
            if (conf is null)
                throw new ArgumentNullException(nameof(conf));

            this.SelectedMatchStringMode = conf.MatchMode;
            this.MatchValueTextBox.Text = conf.MatchValue;
            switch (conf.MatchMode)
            {
                case MatchStringMode.Contains:
                case MatchStringMode.StartsWith:
                case MatchStringMode.EndsWith:
                    this.SelectedStringComparison = (StringComparison)conf.MatchOptions;
                    break;
                case MatchStringMode.Regex:
                    this.SelectedRegexOptions = (RegexOptions)conf.MatchOptions;
                    break;
            }

            if (conf.DisableAt != null)
            {
                this.LifeTimeControl.SelectedDisableAt = conf.DisableAt.Value;
            }

            if (conf.ExpiredAt != null)
            {
                this.LifeTimeControl.SelectedExpiredAt = conf.ExpiredAt.Value;
            }
        }

        public void WriteToConf(MatchStringConf conf)
        {
            conf.MatchMode = this.SelectedMatchStringMode;
            conf.MatchValue = this.MatchValueTextBox.Text;
            switch (conf.MatchMode)
            {
                case MatchStringMode.Contains:
                case MatchStringMode.StartsWith:
                case MatchStringMode.EndsWith:
                    conf.MatchOptions = (int)this.SelectedStringComparison;
                    break;
                case MatchStringMode.Regex:
                    conf.MatchOptions = (int)this.SelectedRegexOptions;
                    break;
            }

            if (this.LifeTimeControl.SelectedDisableAt is DateTime da)
            {
                conf.DisableAt = da;
            } 

            if (this.LifeTimeControl.SelectedExpiredAt is DateTime ea)
            {
                conf.ExpiredAt = ea;
            } 
        }

        private void SelectModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (string)((ComboBoxItem)this.SelectModeComboBox.SelectedItem).Content;

            this.RegexOptionsPanel.Visibility = Visibility.Collapsed;
            this.StringComparisonPanel.Visibility = Visibility.Collapsed;
            switch ((MatchStringMode)Enum.Parse(typeof(MatchStringMode), selected))
            {
                case MatchStringMode.Contains:
                case MatchStringMode.StartsWith:
                case MatchStringMode.EndsWith:
                    this.StringComparisonPanel.Visibility = Visibility.Visible;
                    break;

                case MatchStringMode.Regex:
                    this.RegexOptionsPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private MatchStringMode SelectedMatchStringMode
        {
            get
            {
                var selected = (string)((ComboBoxItem)this.SelectModeComboBox.SelectedItem).Content;
                return (MatchStringMode)Enum.Parse(typeof(MatchStringMode), selected);
            }
            set
            {
                var mode = value.ToString();
                this.SelectModeComboBox.SelectedItem = this.SelectModeComboBox.Items
                    .OfType<ComboBoxItem>()
                    .Single(z => ((string)z.Content) == mode);
            }
        }

        private StringComparison SelectedStringComparison
        {
            get
            {
                var selected = (string)this.StringComparisonPanel.Children
                    .OfType<RadioButton>()
                    .Single(z => z.IsChecked == true)
                    .Content;
                return (StringComparison)Enum.Parse(typeof(StringComparison), selected);
            }
            set
            {
                var mode = value.ToString();
                this.StringComparisonPanel.Children
                    .OfType<RadioButton>()
                    .Single(z => ((string)z.Content) == mode)
                    .IsChecked = true;
            }
        }

        private RegexOptions SelectedRegexOptions
        {
            get
            {
                return this.StringComparisonPanel.Children.OfType<CheckBox>()
                    .Where(z => z.IsChecked == true)
                    .Select(z => z.Content)
                    .Cast<string>()
                    .Select(z => (RegexOptions)Enum.Parse(typeof(RegexOptions), z))
                    .Aggregate(RegexOptions.None, (f1, f2) => f1 | f2);
            }
            set
            {
                foreach (var cb in this.StringComparisonPanel.Children.OfType<CheckBox>())
                {
                    var cbo = (RegexOptions)Enum.Parse(typeof(RegexOptions), (string)cb.Content);
                    cb.IsChecked = (value & cbo) == cbo;
                }
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.TryCreateStringMatcher() != null)
            {
                this.DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private IStringMatcher TryCreateStringMatcher()
        {
            var conf = this.CreateMatchStringConf();
            var factory = App.RSSViewerHost.ServiceProvider.GetRequiredService<StringMatcherFactory>();

            try
            {
                return factory.Create(conf);
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private async void RunTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.TryCreateStringMatcher() is IStringMatcher matcher)
            {
                var factory = App.RSSViewerHost.ServiceProvider.GetRequiredService<StringMatcherFactory>();
                var query = App.RSSViewerHost.Query();
                var items = await query.ListAsync(new[] { RssItemState.Undecided }, CancellationToken.None);
                items = items.Where(z => matcher.IsMatch(z.Title)).ToArray();
                this.MatchedRssItemsListView.Items.Clear();
                foreach (var item in items)
                {
                    this.MatchedRssItemsListView.Items.Add(item);
                }
            }
        }

        internal static bool TryCreateConf(Window owner, out MatchStringConf conf)
        {
            conf = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>()
                .CreateMatchStringConf();

            return EditConf(owner, conf);
        }

        internal static bool EditConf(Window owner, MatchStringConf conf)
        {
            var win = new EditStringMatcherWindow { Owner = owner };
            win.LoadFromConf(conf);
            if (win.ShowDialog() == true)
            {
                win.WriteToConf(conf);
                return true;
            }
            return false;
        }
    }
}
