
using RSSViewer.Abstractions;

namespace RSSViewer.Utils
{
    public static class ViewerLoggerExtensions
    {
        public static ViewerLoggerEventTimer EnterEvent(this IViewerLogger viewerLogger, string eventName)
            => new ViewerLoggerEventTimer(viewerLogger, eventName);
    }
}
