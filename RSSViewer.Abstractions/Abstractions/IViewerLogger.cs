using System.Collections.Generic;

namespace RSSViewer.Abstractions
{
    public interface IViewerLogger
    {
        void AddLine(string line);

        void AddLines(IEnumerable<string> lines);
    }
}
