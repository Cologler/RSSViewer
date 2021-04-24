using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.RulesDb;

namespace RSSViewer.Models
{
    public class ClassifyContext<T> where T : class
    {
        public ClassifyContext(T item)
        {
            this.Item = item ?? throw new ArgumentNullException(nameof(item));
        }

        public T Item { get; }

        public string GroupName { get; set; }

        public HashSet<Tag> Tags { get; } = new();

        public HashSet<string> TagIds = new();

        public HashSet<string> TagGroupWithoutTag = new();
    }
}
