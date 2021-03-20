using Jasily.ViewModel;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.Helpers;
using RSSViewer.LocalDb;
using RSSViewer.RulesDb;
using RSSViewer.Services;
using RSSViewer.StringMatchers;
using RSSViewer.ViewModels;

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
    public partial class EditRuleWindow : Window
    {
        private readonly RssItemHandlersService _handlersService;

        public EditRuleWindow()
        {
            this.InitializeComponent();
            this.DataContext = new DataContextViewModel();

            this._handlersService = App.RSSViewerHost.ServiceProvider.GetRequiredService<RssItemHandlersService>();
            var handlers = this._handlersService.GetRuleTargetHandlers();

            this.ActionsList.ItemsSource = handlers;
            this.ActionsList.SelectedItem = this._handlersService.GetDefaultRuleTargetHandler();
        }

        DataContextViewModel ViewModel => (DataContextViewModel)this.DataContext;

        public void LoadFromConf(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            // general
            if (string.IsNullOrEmpty(rule.HandlerId))
            {
                this.ActionsList.SelectedItem = this._handlersService.GetDefaultRuleTargetHandler();
            }
            else
            {
                this.ActionsList.SelectedItem = this._handlersService.GetRuleTargetHandlers()
                    .FirstOrDefault(z => z.Id == rule.HandlerId);
            }

            // match
            this.SelectedMatchStringMode = rule.Mode;
            this.MatchValueTextBox.Text = rule.Argument;

            this.LastMatchedText.Text = rule.LastMatched.ToLocalTime().ToLongDateString();
            this.TotalMatchedCountText.Text = rule.TotalMatchedCount.ToString();

            this.ViewModel.From(rule);
        }

        public void WriteToConf(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            // general
            rule.HandlerId = ((IRssItemHandler)this.ActionsList.SelectedItem).Id;

            // match
            rule.Mode = this.SelectedMatchStringMode;
            rule.Argument = this.MatchValueTextBox.Text;

            this.ViewModel.Write(rule);
        }

        private void SelectModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            // check handler
            if (!(this.ActionsList.SelectedItem is IRssItemHandler))
            {
                MessageBox.Show("Please select a handler", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // check lifetime
            if (this.ViewModel.IsEnabledAutoDisabled && !int.TryParse(this.ViewModel.AutoDisabledAfterDaysText, out _))
            {
                MessageBox.Show("Auto disabled days is not a integer", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (this.ViewModel.IsEnabledAutoExpired && !int.TryParse(this.ViewModel.AutoExpiredAfterDaysText, out _))
            {
                MessageBox.Show("Auto expired days is not a integer", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // check match
            if (this.TryCreateRssItemMatcher() == null)
                return;

            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private RuleMatchTreeNode TryCreateRssItemMatcher()
        {
            var rule = new MatchRule();
            this.WriteToConf(rule);
            var factory = App.RSSViewerHost.ServiceProvider.GetRequiredService<StringMatcherFactory>();

            try
            {
                return new RuleMatchTreeNode(rule, factory.Create(rule));
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private async void RunTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.TryCreateRssItemMatcher() is RuleMatchTreeNode matcher)
            {
                var factory = App.RSSViewerHost.ServiceProvider.GetRequiredService<StringMatcherFactory>();
                var query = App.RSSViewerHost.Query();
                var items = await query.ListAsync(new[] { RssItemState.Undecided }, null, CancellationToken.None);
                items = items.Where(z => matcher.IsMatch(z)).ToArray();
                this.MatchedRssItemsListView.Items.Clear();
                foreach (var item in items)
                {
                    this.MatchedRssItemsListView.Items.Add(item);
                }
            }
        }

        internal static bool TryCreateConf(Window owner, out MatchRule rule)
        {
            rule = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>().CreateMatchRule();
            return EditConf(owner, rule);
        }

        internal static bool EditConf(Window owner, MatchRule rule)
        {
            var win = new EditRuleWindow { Owner = owner };
            win.LoadFromConf(rule);
            if (win.ShowDialog() == true)
            {
                win.WriteToConf(rule);
                return true;
            }
            return false;
        }

        class DataContextViewModel : BaseViewModel
        {
            private DateTime _lastMatched;
            private bool _isEnabledAutoDisabled;
            private bool _isEnabledAutoExpired;
            private string _autoDisabledAfterDaysText;
            private string _autoExpiredAfterDaysText;
            private bool _ignoreCase;

            public bool IsEnabledAutoDisabled
            {
                get => _isEnabledAutoDisabled;
                set
                {
                    if (this.ChangeModelProperty(ref _isEnabledAutoDisabled, value))
                    {
                        this.RefreshProperties(10);
                    }
                }
            }

            public string AutoDisabledAfterDaysText
            {
                get => _autoDisabledAfterDaysText;
                set
                {
                    if (this.ChangeModelProperty(ref _autoDisabledAfterDaysText, value))
                    {
                        this.RefreshProperties(10);
                    }
                }
            }

            [ModelProperty(Group = 10)]
            public string AutoDisabledAt
            {
                get
                {
                    if (!this.IsEnabledAutoDisabled)
                        return "Never";
                    if (int.TryParse(this.AutoDisabledAfterDaysText, out var days))
                        return (this._lastMatched + TimeSpan.FromDays(days)).ToLongDateString();
                    return "Unknown";
                }
            }

            public bool IsEnabledAutoExpired
            {
                get => _isEnabledAutoExpired;
                set
                {
                    if (this.ChangeModelProperty(ref _isEnabledAutoExpired, value))
                    {
                        this.RefreshProperties(11);
                    }
                }
            }

            public string AutoExpiredAfterDaysText
            {
                get => _autoExpiredAfterDaysText;
                set
                {
                    if (this.ChangeModelProperty(ref _autoExpiredAfterDaysText, value))
                    {
                        this.RefreshProperties(11);
                    }
                }
            }

            [ModelProperty(Group = 11)]
            public string AutoExpiredAt
            {
                get
                {
                    if (!this.IsEnabledAutoExpired)
                        return "Never";
                    if (int.TryParse(this.AutoExpiredAfterDaysText, out var days))
                        return (this._lastMatched + TimeSpan.FromDays(days)).ToLongDateString();
                    return "Unknown";
                }
            }

            public void From(MatchRule rule)
            {
                this.SourcesView.SelectedItem = this.SourcesView.Items.FirstOrDefault(z => z.FeedId == rule.OnFeedId);

                this.IgnoreCase = rule.IgnoreCase;

                this._lastMatched = rule.LastMatched.ToLocalTime();

                // lifetime: disabled
                this.IsEnabledAutoDisabled = rule.AutoDisabledAfterLastMatched.HasValue;
                if (rule.AutoDisabledAfterLastMatched.HasValue)
                {
                    this.AutoDisabledAfterDaysText = rule.AutoDisabledAfterLastMatched.Value.Days.ToString();
                }
                else
                {
                    this.AutoDisabledAfterDaysText = string.Empty;
                }
                // lifetime: expired
                this.IsEnabledAutoExpired = rule.AutoExpiredAfterLastMatched.HasValue;
                if (rule.AutoExpiredAfterLastMatched.HasValue)
                {
                    this.AutoExpiredAfterDaysText = rule.AutoExpiredAfterLastMatched.Value.Days.ToString();
                }
                else
                {
                    this.AutoExpiredAfterDaysText = string.Empty;
                }

                this.RefreshProperties();
            }

            public void Write(MatchRule rule)
            {
                rule.OnFeedId = this.SourcesView.SelectedItem?.FeedId;

                rule.IgnoreCase = this.IgnoreCase;

                // lifetime
                // lifetime: disabled
                if (this.IsEnabledAutoDisabled)
                {
                    rule.AutoDisabledAfterLastMatched = TimeSpan.FromDays(int.Parse(this.AutoDisabledAfterDaysText));
                }
                // lifetime: expired
                if (this.IsEnabledAutoExpired)
                {
                    rule.AutoExpiredAfterLastMatched = TimeSpan.FromDays(int.Parse(this.AutoExpiredAfterDaysText));
                }
            }

            public SourcesViewModel SourcesView { get; } = new(false);

            public bool IgnoreCase { get => _ignoreCase; set => this.ChangeModelProperty(ref _ignoreCase, value); }
        }
    }
}
