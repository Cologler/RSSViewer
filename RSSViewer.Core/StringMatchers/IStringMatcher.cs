using System.Collections.Generic;
using System.Text;

namespace RSSViewer.StringMatchers
{
    public interface IStringMatcher
    {
        bool IsMatch(string value);
    }
}
