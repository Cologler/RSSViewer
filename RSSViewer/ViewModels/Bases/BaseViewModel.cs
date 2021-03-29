using System;

namespace RSSViewer.ViewModels.Bases
{
    public class BaseViewModel : Jasily.ViewModel.BaseViewModel
    {
        public virtual IServiceProvider ServiceProvider => App.RSSViewerHost.ServiceProvider;
    }
}
