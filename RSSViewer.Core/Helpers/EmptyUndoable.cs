using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.Abstractions;

namespace RSSViewer.Helpers
{
    class EmptyUndoable : IUndoable
    {
        public static readonly EmptyUndoable Default = new();

        public void Undo(IServiceProvider serviceProvider) { }
    }
}
