using RSSViewer.Abstractions;
using System.Collections.Generic;

namespace RSSViewer.Utils
{
    class NoneViewerLogger : IViewerLogger
    {
        public void AddLine(string line) { }

        public void AddLines(IEnumerable<string> lines) { }
    }
}
