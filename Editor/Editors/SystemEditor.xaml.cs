using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NSHG;

namespace XMLEditor
{
    /// <summary>
    /// Interaction logic for SystemEditor.xaml
    /// </summary>
    public partial class SystemEditor : Page
    {
        public MainWindow parent;
        public Network network;
        public NSHG.System sys;

        public SystemEditor(NSHG.Network network, NSHG.System s, MainWindow parent)
        {
            InitializeComponent();
            sys = s;
            this.network = network;
            this.parent = parent;
            UpdateView();
        }

        public void UpdateView()
        {
            parent.ReloadSystemPane();

            IDIn.Text = sys.ID.ToString();
            Adapters.ItemsSource = sys.NetworkInterfaces.ToArray();
            RTEIn.IsChecked = sys.respondToEcho;
        }

        private void IDIn_LostFocus(object sender, RoutedEventArgs e)
        {
            uint tmpId;
            if (uint.TryParse(IDIn.Text, out tmpId))
            {
                if (!network.Systems.ContainsKey(tmpId))
                {
                    network.Systems[tmpId] = network.Systems[sys.ID];
                    network.Systems.Remove(sys.ID);
                    sys.ID = tmpId;
                    foreach (Adapter a in sys.NetworkInterfaces.Values)
                    {
                        a.sysID = tmpId;
                    }
                    parent.ReloadSystemPane();
                }
                else
                {
                    IDIn.Text = "A system with that key already exists";
                }
            }
            else
            {
                IDIn.Text = "Invalid Syntax";
            }
        }

        private void RTEIn_Checked(object sender, RoutedEventArgs e)
        {
            sys.respondToEcho = (bool)RTEIn.IsChecked;
            parent.ReloadSystemPane();
        }

        private void SelectAdapter_Click(object sender, RoutedEventArgs e)
        {
            Adapter a = (Adapter)Adapters.SelectedItem;
            if (a != null)
            {
                AdapterEditor window = new AdapterEditor(ref a, this);
                window.Show();
                window.Focus();
            }

        }

        private void NewAdapter_Click(object sender, RoutedEventArgs e)
        {
            MAC m;
            do
            {
                m = MAC.Random();
            } while (network.TakenMacAddresses.Contains(m));

            sys.NetworkInterfaces.Add(m, new Adapter(m, sys.ID));

            network.TakenMacAddresses.Add(m);
            UpdateView();
        }

        private void DeleteAdapter_Click(object sender, RoutedEventArgs e)
        {
            if (Adapters.SelectedIndex != -1)
            {
                MAC m = sys.NetworkInterfaces.Values.ElementAt(Adapters.SelectedIndex).MyMACAddress;
                network.TakenMacAddresses.Remove(m);
                sys.NetworkInterfaces.Remove(m);
                UpdateView();
            }
        }
    }
}
