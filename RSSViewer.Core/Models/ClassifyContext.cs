using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.RulesDb;

namespace RSSViewer.Models
{
    public class ClassifyContext<T>
    {
        public ClassifyContext(T item)
        {
            this.Item = item;
        }

        public T Item { get; }

        public string GroupName { get; set; }

        public HashSet<Tag> Tags { get; } = new();
    }
}
