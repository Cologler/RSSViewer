using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Configuration;
using RSSViewer.LocalDb;
using RSSViewer.RulesDb;
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

        public void LoadFromConf(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            this.SelectedMatchStringMode = rule.Mode;
            this.MatchValueTextBox.Text = rule.Argument;
            switch (rule.Mode)
            {
                case MatchMode.Contains:
                case MatchMode.StartsWith:
                case MatchMode.EndsWith:
                    this.SelectedStringComparison = rule.OptionsAsStringComparison;
                    break;
                case MatchMode.Regex:
                    this.SelectedRegexOptions = rule.OptionsAsRegexOptions;
                    break;
            }
        }

        public void WriteToConf(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            rule.Mode = this.SelectedMatchStringMode;
            rule.Argument = this.MatchValueTextBox.Text;
            switch (rule.Mode)
            {
                case MatchMode.Contains:
                case MatchMode.StartsWith:
                case MatchMode.EndsWith:
                    rule.OptionsAsStringComparison = this.SelectedStringComparison;
                    break;
                case MatchMode.Regex:
                    rule.OptionsAsRegexOptions = this.SelectedRegexOptions;
                    break;
            }
        }

        private void SelectModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (string)((ComboBoxItem)this.SelectModeComboBox.SelectedItem).Content;

            this.RegexOptionsPanel.Visibility = Visibility.Collapsed;
            this.StringComparisonPanel.Visibility = Visibility.Collapsed;
            switch ((MatchMode)Enum.Parse(typeof(MatchMode), selected))
            {
                case MatchMode.Contains:
                case MatchMode.StartsWith:
                case MatchMode.EndsWith:
                    this.StringComparisonPanel.Visibility = Visibility.Visible;
                    break;

                case MatchMode.Regex:
                    this.RegexOptionsPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private MatchMode SelectedMatchStringMode
        {
            get
            {
                var selected = (string)((ComboBoxItem)this.SelectModeComboBox.SelectedItem).Content;
                return (MatchMode)Enum.Parse(typeof(MatchMode), selected);
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
            var rule = new MatchRule();
            this.WriteToConf(rule);
            var factory = App.RSSViewerHost.ServiceProvider.GetRequiredService<StringMatcherFactory>();

            try
            {
                return factory.Create(rule);
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

        internal static bool TryCreateConf(Window owner, out MatchRule rule)
        {
            rule = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>()
                .CreateMatchRule(MatchAction.Reject);

            return EditConf(owner, rule);
        }

        internal static bool EditConf(Window owner, MatchRule rule)
        {
            var win = new EditStringMatcherWindow { Owner = owner };
            win.LoadFromConf(rule);
            if (win.ShowDialog() == true)
            {
                win.WriteToConf(rule);
                return true;
            }
            return false;
        }
    }
}
