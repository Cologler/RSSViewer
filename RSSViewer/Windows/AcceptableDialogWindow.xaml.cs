using System;
using System.Windows;

namespace RSSViewer.Windows
{
    /// <summary>
    /// AcceptableDialogWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AcceptableDialogWindow : Window
    {
        public AcceptableDialogWindow()
        {
            InitializeComponent();
        }

        public Func<AcceptableDialogWindow, bool> Validator { get; set; }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Validator?.Invoke(this) != false)
            {
                this.DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
