using RSSViewer.LocalDb;
using System.Collections.Generic;

namespace RSSViewer.ViewModels
{
    public class IncludeViewModel
    {
        public bool Undecided { get; set; } = true;

        public bool Accepted { get; set; }

        public bool Rejected { get; set; }

        public RssState[] GetStateValues() 
        {
            var states = new List<RssState>();
            if (this.Undecided)
                states.Add(RssState.Undecided);
            if (this.Accepted)
                states.Add(RssState.Accepted);
            if (this.Rejected)
                states.Add(RssState.Rejected);
            return states.ToArray();
        }
    }
}
