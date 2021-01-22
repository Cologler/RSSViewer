using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.Search
{
    internal class SearchExpression
    {
        public List<ISearchPart> Parts { get; } = new();

        public static SearchExpression Parse(string searchText)
        {
            var expr = new SearchExpression();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var parts = searchText.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                expr.Parts.AddRange(parts.Select(z => new ContainsText(z)));
            }
            return expr;
        }
    }
}
