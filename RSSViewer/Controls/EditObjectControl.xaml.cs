using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.ViewModels;
using RSSViewer.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RSSViewer.Controls
{
    /// <summary>
    /// EditObjectControl.xaml 的交互逻辑
    /// </summary>
    public partial class EditObjectControl : UserControl
    {
        public EditObjectControl()
        {
            InitializeComponent();
        }

        public IEnumerable<IObjectFactoryProvider> ObjectFactorys
        {
            set
            {
                this.FactoryComboBox.Items.Clear();
                foreach (var item in value.Select(z => new ObjectFactoryViewModel(z)))
                {
                    this.FactoryComboBox.Items.Add(item);
                }
            }
        }

        public IObjectFactoryProvider SelectedObjectFactory
        {
            get => (this.FactoryComboBox.SelectedItem as ObjectFactoryViewModel)?.ObjectFactory;
            set
            {
                var vm = this.FactoryComboBox.Items.OfType<ObjectFactoryViewModel>()
                    .FirstOrDefault(z => z.ObjectFactory == value);
                this.FactoryComboBox.SelectedItem = vm;
            }
        }

        public Dictionary<string, string> Variables
        {
            get
            {
                return this.VariablesPanel.Children
                    .OfType<TextBox>()
                    .ToDictionary(z => ((VariableInfo)z.Tag).VariableName, z => z.Text);
            }
            set
            {
                var copy = new Dictionary<string, string>(value);
                foreach (var item in this.VariablesPanel.Children.OfType<TextBox>())
                {
                    var vi = (VariableInfo)item.Tag;
                    if (copy.TryGetValue(vi.VariableName, out var v))
                    {
                        item.Text = v;
                        copy.Remove(vi.VariableName);
                    }
                }

                Debug.Assert(copy.Count == 0);
            }
        }

        private void FactoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.VariablesPanel.Children.Clear();
            var f = this.SelectedObjectFactory;
            if (f != null)
            {
                foreach (var v in f.GetVariableInfos())
                {
                    this.VariablesPanel.Children.Add(new TextBlock { Text = v.VariableName });
                    this.VariablesPanel.Children.Add(new TextBox { Tag = v });
                }
            }
        }

        public void WriteToConf(SyncSourceSection conf)
        {
            conf.Name = "";
            conf.ProviderName = this.SelectedObjectFactory.ProviderName;
            conf.Variables = this.Variables;
        }

        public bool Validate(AcceptableDialogWindow _)
        {
            foreach (var item in this.VariablesPanel.Children.OfType<TextBox>())
            {
                var vi = (VariableInfo)item.Tag;
                if (!vi.Validate(item.Text))
                {
                    return false;
                }
            }

            return true;
        }

        private static AcceptableDialogWindow CreateWindow(Window owner)
        {
            var win = new AcceptableDialogWindow
            {
                Owner = owner,
                ResizeMode = ResizeMode.NoResize,
                Width = 320,
                Height = 400,
            };
            return win;
        }

        public static bool CreateSyncSourceConf(Window owner, out SyncSourceSection conf)
        {
            var ctl = new EditObjectControl();
            var win = CreateWindow(owner);
            win.Body.Children.Add(ctl);
            win.Validator = ctl.Validate;

            var serviceProvider = App.RSSViewerHost.ServiceProvider;
            var ssProvider = serviceProvider.GetServices<ISyncSourceProvider>().ToArray();
            Debug.Assert(ssProvider.Length > 0);
            ctl.ObjectFactorys = ssProvider;
            ctl.SelectedObjectFactory = ssProvider[0];
            if (win.ShowDialog() == true)
            {
                conf = new SyncSourceSection();
                ctl.WriteToConf(conf);
                return true;
            }
            else
            {
                conf = null;
                return false;
            }
        }
    }
}
