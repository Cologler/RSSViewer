using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RSSViewer.Search
{
    internal class SearchExpression
    {
        private static readonly Regex DoubleQuote = new("^\"(?<Text>[^\"]*)(?:\"(?: |$))");
        private static readonly Regex Word = new("^(?<Text>[^ ]+)( |$)");

        public List<ISearchPart> Parts { get; } = new();

        public static SearchExpression Parse(string searchText)
        {
            var expr = new SearchExpression();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                while (searchText.Length > 0)
                {
                    searchText = searchText.Trim();
                    Match match;

                    match = DoubleQuote.Match(searchText);
                    if (match.Success)
                    {
                        Debug.Assert(match.Length > 0);
                        var text = match.Groups["Text"].Value;
                        if (text.Length > 0)
                        {
                            expr.Parts.Add(new ContainsText(match.Groups["Text"].Value));
                        }
                        searchText = searchText[match.Length..];
                        continue;
                    }

                    match = Word.Match(searchText);
                    if (match.Success)
                    {
                        Debug.Assert(match.Length > 0);
                        expr.Parts.Add(new ContainsText(match.Groups["Text"].Value));
                        searchText = searchText[match.Length..];
                        continue;
                    }

                    if (searchText.Length > 0)
                    {
                        Debug.WriteLine("Unparsed block: " + searchText);
                        expr.Parts.Add(new ContainsText(searchText));
                        break;
                    }
                }

                Debug.WriteLine("SearchExpression: " + string.Join("|", expr.Parts));
            }

            return expr;
        }
    }
}
