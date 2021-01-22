using Jasily.ViewModel;

using RSSViewer.LocalDb;

using System.Collections.Generic;
using System.ComponentModel;

namespace RSSViewer.ViewModels
{
    public class IncludeViewModel : BaseViewModel
    {
        private bool _undecided = true;
        private bool _accepted;
        private bool _rejected;
        private bool _archived;

        public bool Undecided
        {
            get => this._undecided;
            set => this.ChangeModelProperty(ref this._undecided, value);
        }

        public bool Accepted
        {
            get => this._accepted;
            set => this.ChangeModelProperty(ref this._accepted, value);
        }

        public bool Rejected
        {
            get => this._rejected;
            set => this.ChangeModelProperty(ref this._rejected, value);
        }

        public bool Archived
        { 
            get => _archived; 
            set => this.ChangeModelProperty(ref this._archived, value); 
        }

        public RssItemState[] GetStateValues()
        {
            var states = new List<RssItemState>();
            if (this.Undecided)
                states.Add(RssItemState.Undecided);
            if (this.Accepted)
                states.Add(RssItemState.Accepted);
            if (this.Rejected)
                states.Add(RssItemState.Rejected);
            if (this.Archived)
                states.Add(RssItemState.Archived);
            return states.ToArray();
        }
    }
}
