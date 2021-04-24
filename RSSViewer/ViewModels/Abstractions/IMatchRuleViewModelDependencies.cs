using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.RulesDb;

namespace RSSViewer.ViewModels.Abstractions
{
    public interface IMatchRuleViewModelDependencies
    {
        Tag FindTag(string tagId);
    }
}
