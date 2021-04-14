using AutoMapper;

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
using RSSViewer.ViewModels.Bases;

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
        private MatchRule _rule;

        public EditRuleWindow()
        {
            this.InitializeComponent();
            this.DataContext = new DataContextViewModel();

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

            var serviceProvider = this.ViewModel.ServiceProvider;

            switch (rule.HandlerType)
            {
                case HandlerType.Action:
                    this.Title = "Edit Action Rule";
                    this.General_AddTag.Visibility = Visibility.Collapsed;

                    var handlersService = serviceProvider.GetRequiredService<RssItemHandlersService>();
                    this.ActionsList.ItemsSource = handlersService.GetRuleTargetHandlers();
                    this.ActionsList.SelectedItem = handlersService.GetRuleTargetHandler(rule.HandlerId);

                    this.LastMatchedText.Text = rule.LastMatched.ToLocalTime().ToLongDateString();
                    this.TotalMatchedCountText.Text = rule.TotalMatchedCount.ToString();
                    break;

                case HandlerType.SetTag:
                    this.Title = "Edit SetTag Rule";
                    this.General_Action.Visibility = Visibility.Collapsed;
                    this.ParentPanel.Visibility = Visibility.Collapsed;
                    this.LifetimePanel.Visibility = Visibility.Collapsed;
                    break;

                default:
                    throw new NotImplementedException();
            }

            // match
            this.SelectedMatchStringMode = rule.Mode;
            this.MatchValueTextBox.Text = rule.Argument;

            this.ViewModel.LoadAsync(rule);
        }

        public void WriteTo(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            switch (rule.HandlerType)
            {
                case HandlerType.Action:
                    rule.HandlerId = ((IRssItemHandler)this.ActionsList.SelectedItem).Id;
                    break;

                case HandlerType.SetTag:
                    break;

                default:
                    throw new NotImplementedException();
            }

            // match
            rule.Mode = this.SelectedMatchStringMode;
            rule.Argument = rule.Mode.IsStringMode() ? this.MatchValueTextBox.Text : string.Empty;

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
            if (this.Rule is null)
                throw new InvalidOperationException();

            switch (this.Rule.HandlerType)
            {
                case HandlerType.Action:
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
                    break;

                case HandlerType.SetTag:
                    break;

                default:
                    throw new NotImplementedException();
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
            var rule = new MatchRule { HandlerType = this.Rule.HandlerType };
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
            _ = win.ViewModel.ParentSelectorView.LoadActionRulesFromDbAsync();
            return win.ShowDialog() == true;
        }

        public static void ConfigureAutoMapperProfile(Profile profile)
        {
            profile.CreateMap<DataContextViewModel, MatchRule>()
                .AfterMap((v, m) =>
                {
                    if (m.DisplayName is not null)
                    {
                        m.DisplayName = m.DisplayName.Trim();
                        if (m.DisplayName.Length == 0)
                        {
                            m.DisplayName = null;
                        } 
                    }
                })
                .ReverseMap();
        }

        public class DataContextViewModel : ViewModels.Bases.BaseViewModel
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

            [ModelProperty]
            public string DisplayName { get; set; }

            public async void LoadAsync(MatchRule rule)
            {
                this.ServiceProvider.GetRequiredService<IMapper>().Map(rule, this);

                switch (rule.HandlerType)
                {
                    case HandlerType.Action:
                        this.SourcesView.SelectedItem = this.SourcesView.Items.FirstOrDefault(z => z.FeedId == rule.OnFeedId);
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
                        this.ParentSelectorView.SelectedItem = this.ParentSelectorView.Items
                            .Where(z => z.MatchRule == rule.Parent)
                            .FirstOrDefault();
                        break;

                    case HandlerType.SetTag:
                        this.TagsViewModel.SelectedItem = rule.HandlerId is null
                            ? null
                            : this.TagsViewModel.Items.FirstOrDefault(z => z.Tag.Id == rule.HandlerId);
                        break;

                    default:
                        throw new NotImplementedException();
                }

                this.RefreshProperties();
            }

            public void Write(MatchRule rule)
            {
                this.ServiceProvider.GetRequiredService<IMapper>().Map(this, rule);

                switch (rule.HandlerType)
                {
                    case HandlerType.Action:
                        rule.OnFeedId = this.SourcesView.SelectedItem?.FeedId;

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
                        rule.Parent = this.ParentSelectorView.SelectedItem?.MatchRule;
                        break;

                    case HandlerType.SetTag:
                        // set from manager window
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            public SourcesViewModel SourcesView { get; } = new(false);

            [ModelProperty]
            public TagsSelectorViewModel TagsViewModel { get; } = new();

            public bool IgnoreCase { get => _ignoreCase; set => this.ChangeModelProperty(ref _ignoreCase, value); }

            public ActionRuleParentSelectorViewModel ParentSelectorView { get; } = new();
        }

        public class TagsSelectorViewModel : TagsViewModel
        {
            public string TagName { get; set; }
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
