using Jasily.ViewModel;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.Filter;
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
        private MatchRule _rule;

        public EditRuleWindow()
        {
            this.InitializeComponent();
            this.DataContext = new DataContextViewModel();

            this._handlersService = App.RSSViewerHost.ServiceProvider.GetRequiredService<RssItemHandlersService>();

            this.ActionsList.ItemsSource = this._handlersService.GetRuleTargetHandlers();
            this.ActionsList.SelectedItem = this._handlersService.GetRuleTargetHandler(null);

            // build match mode
            foreach (var item in Enum.GetValues<MatchMode>())
            {
                if (item != MatchMode.None)
                {
                    this.SelectModeComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = item.ToString()
                    });
                }
            }
        }

        public DataContextViewModel ViewModel => (DataContextViewModel)this.DataContext;

        public MatchRule Rule
        {
            get => this._rule;
            set
            {
                if (this._rule != value)
                {
                    this._rule = value;
                    this.LoadFrom(value);
                }
            }
        }

        void LoadFrom(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            // general
            this.ActionsList.SelectedItem = this._handlersService.GetRuleTargetHandler(rule.HandlerId);

            // match
            this.SelectedMatchStringMode = rule.Mode;
            this.MatchValueTextBox.Text = rule.Argument;

            this.LastMatchedText.Text = rule.LastMatched.ToLocalTime().ToLongDateString();
            this.TotalMatchedCountText.Text = rule.TotalMatchedCount.ToString();

            this.ViewModel.LoadAsync(rule);
        }

        public void WriteTo(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            // general
            rule.HandlerId = ((IRssItemHandler)this.ActionsList.SelectedItem).Id;

            // match
            rule.Mode = this.SelectedMatchStringMode;
            if (rule.Mode.IsStringMode())
            {
                rule.Argument = this.MatchValueTextBox.Text;
            }
            else
            {
                rule.Argument = string.Empty;
            } 

            this.ViewModel.Write(rule);
        }

        private MatchMode SelectedMatchStringMode
        {
            get
            {
                return Enum.Parse<MatchMode>((string)((ComboBoxItem)this.SelectModeComboBox.SelectedItem).Content);
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

            // check parent
            var parent = this.ViewModel.ParentSelectorView.SelectedItem;
            if (parent is not null)
            {
                if (parent.MatchRule is not null && parent.MatchRule.Id <= 0)
                {
                    MessageBox.Show("Not implemented yet!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // check match
            if (this.TryCreateRssItemMatcher() == null)
                return;

            this.WriteTo(this.Rule);
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private RuleMatchTreeNode TryCreateRssItemMatcher()
        {
            var rule = new MatchRule();
            this.WriteTo(rule);
            var factory = App.RSSViewerHost.ServiceProvider.GetRequiredService<RssItemFilterFactory>();

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
                var factory = App.RSSViewerHost.ServiceProvider.GetRequiredService<RssItemFilterFactory>();
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

        /// <summary>
        /// use <see cref="EditRuleWindow"/> to edit a <see cref="MatchRule"/>.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        internal static bool Edit(Window owner, MatchRule rule)
        {
            var win = new EditRuleWindow { Owner = owner };
            win.Rule = rule;
            _ = win.ViewModel.ParentSelectorView.LoadItemsFromDbAsync();
            return win.ShowDialog() == true;
        }

        public class DataContextViewModel : BaseViewModel
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

            public async void LoadAsync(MatchRule rule)
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

                // parent
                await this.ParentSelectorView.Ready;
                this.ParentSelectorView.SelectedItem = 
                    this.ParentSelectorView.Items
                        .Where(z => z.MatchRule?.Id == rule.ParentId)
                        .FirstOrDefault();

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

                // parent
                if (this.ParentSelectorView.SelectedItem is not null)
                {
                    rule.ParentId = this.ParentSelectorView.SelectedItem.MatchRule?.Id;
                }
            }

            public SourcesViewModel SourcesView { get; } = new(false);

            public bool IgnoreCase { get => _ignoreCase; set => this.ChangeModelProperty(ref _ignoreCase, value); }

            public MatchRuleParentSelectorViewModel ParentSelectorView { get; } = new();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((TabControl)sender).SelectedItem == this.ParentPanel)
            {
                if (this.ParentList.SelectedItem is not null)
                {
                    this.ParentList.ScrollIntoView(this.ParentList.SelectedItem);
                }
            }
        }
    }
}
