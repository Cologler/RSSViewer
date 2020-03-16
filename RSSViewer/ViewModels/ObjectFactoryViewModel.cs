using RSSViewer.Abstractions;

namespace RSSViewer.ViewModels
{
    internal class ObjectFactoryViewModel
    {
        public ObjectFactoryViewModel(IObjectFactoryProvider objectFactory)
        {
            this.ObjectFactory = objectFactory;
        }

        public string DisplayValue => this.ObjectFactory.ProviderName;

        public IObjectFactoryProvider ObjectFactory { get; }
    }
}
