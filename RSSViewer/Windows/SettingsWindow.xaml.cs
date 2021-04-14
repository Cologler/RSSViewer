using System.Windows;

using RSSViewer.ViewModels;

namespace RSSViewer.Windows
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            this.DataContext = new SettingsViewModel();
            this.ViewModel.Load();
        }

        public SettingsViewModel ViewModel => (SettingsViewModel)this.DataContext;

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.Save();
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
