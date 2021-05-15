using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.Abstractions
{
    public interface IUndoable
    {
        void Undo(IServiceProvider serviceProvider);
    }
}
