using System.Windows;

using RSSViewer.ViewModels;
using RSSViewer.ViewModels.Bases;

namespace RSSViewer.Windows
{
    /// <summary>
    /// EditTagWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EditTagWindow : Window
    {
        public EditTagWindow()
        {
            InitializeComponent();
            this.DataContext = new EditTagViewModel();
        }

        public EditTagViewModel ViewModel => (EditTagViewModel)this.DataContext;

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        public class EditTagViewModel
        {
            public TagGroupsViewModel TagGroupsViewModel { get; } = new();

            public TagSnapshotViewModel TagViewModel { get; } = new();
        }
    }
}
