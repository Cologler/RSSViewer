using RSSViewer.LocalDb;
using System.Collections.Generic;

namespace RSSViewer.ViewModels
{
    public class IncludeViewModel
    {
        public bool Undecided { get; set; } = true;

        public bool Accepted { get; set; }

        public bool Rejected { get; set; }

        public RssItemState[] GetStateValues() 
        {
            var states = new List<RssItemState>();
            if (this.Undecided)
                states.Add(RssItemState.Undecided);
            if (this.Accepted)
                states.Add(RssItemState.Accepted);
            if (this.Rejected)
                states.Add(RssItemState.Rejected);
            return states.ToArray();
        }
    }
}
