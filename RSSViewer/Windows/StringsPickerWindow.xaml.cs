using Jasily.ViewModel;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace RSSViewer.Windows
{
    /// <summary>
    /// StringsPickerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StringsPickerWindow : Window
    {
        public StringsPickerWindow()
        {
            this.InitializeComponent();
            this.DataContext = new KeywordPickerViewModel(this);
        }

        public KeywordPickerViewModel ViewModel => (KeywordPickerViewModel) this.DataContext;

        public class KeywordPickerViewModel : BaseViewModel
        {
            private ItemViewModel _selectedItem;
            private StringsPickerWindow _stringsPickerWindow;

            public KeywordPickerViewModel(StringsPickerWindow stringsPickerWindow) => this._stringsPickerWindow = stringsPickerWindow;

            public ObservableCollection<ItemViewModel> Items { get; } = new ObservableCollection<ItemViewModel>();

            public ItemViewModel SelectedItem
            {
                get => this._selectedItem;
                set
                {
                    if (this.ChangeModelProperty(ref this._selectedItem, value))
                    {
                        if (value != null) this._stringsPickerWindow.DialogResult = true;
                    }
                }
            }
        }

        public class ItemViewModel
        {
            public string DisplayValue { get; set; }

            public object Object { get; set; }
        }

        public static bool TryPickString(Window owner, IEnumerable<string> items, out string result)
        {
            result = default;
            var win = new StringsPickerWindow 
            { 
                Owner = owner 
            };
            var wvm = win.ViewModel;
            var vms = items.Select(z => new ItemViewModel { DisplayValue = z });
            foreach (var vm in vms)
            {
                wvm.Items.Add(vm);
            }
            if (win.ShowDialog() == true)
            {
                result = wvm.SelectedItem.DisplayValue;
                return true;
            }
            return false;
        }
    }
}
