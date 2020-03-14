using System.Collections.Generic;
using System.Text;

namespace RSSViewer.StringMatchers
{
    interface IStringMatcher
    {
        bool IsMatch(string value);
    }
}
