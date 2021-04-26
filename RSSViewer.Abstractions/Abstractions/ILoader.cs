using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RSSViewer.Abstractions
{
    public interface ILoader<T>
    {
        T Load(CancellationToken token = default);
    }
}
