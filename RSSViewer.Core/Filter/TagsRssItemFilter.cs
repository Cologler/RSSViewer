using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.Abstractions;
using RSSViewer.Models;

namespace RSSViewer.Filter
{
    class TagsRssItemFilter : IRssItemFilter
    {
        private readonly string[] _tagIds;

        public TagsRssItemFilter(string[] tagIds)
        {
            this._tagIds = tagIds ?? throw new ArgumentNullException(nameof(tagIds));
        }

        public bool IsMatch(ClassifyContext<IPartialRssItem> context)
        {
            foreach (var tagId in this._tagIds)
            {
                if (!context.TagIds.Contains(tagId))
                    return false;
            }

            return true;
        }
    }
}
