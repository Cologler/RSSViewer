using Jasily.ViewModel;

using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.ViewModels
{
    public class RssItemGroupViewModel : BaseViewModel, IRssItemsCount
    {
        private readonly GroupBy _groupBy;

        public RssItemGroupViewModel(GroupBy groupBy = GroupBy.Group)
        {
            this._groupBy = groupBy;
        }

        public string DisplayName { get; set; }

        public List<RssItemViewModel> Items { get; } = new List<RssItemViewModel>();

        int IRssItemsCount.Count => this.Items.Count;

        public enum GroupBy
        {
            Group,
            Tag
        }

        public static IEnumerable<RssItemViewModel> Combine(List<RssItemGroupViewModel> groups)
        {
            if (groups is null)
                throw new System.ArgumentNullException(nameof(groups));

            if (groups.Count == 0)
                return Enumerable.Empty<RssItemViewModel>();

            if (groups.Count == 1)
                return groups[0].Items;

            var groupTypes = groups
                .Where(z => z._groupBy == GroupBy.Group)
                .ToList();

            var tagTypes = groups
                .Where(z => z._groupBy == GroupBy.Tag);

            var ret = groupTypes.Count > 0
                ? groupTypes.SelectMany(z => z.Items).Distinct()
                : groups[0].Items;

            foreach (var tagTypedGroup in tagTypes)
            {
                var set = tagTypedGroup.Items.ToHashSet();
                ret = ret.Where(z => set.Contains(z));
            }

            return ret;
        }
    }
}
