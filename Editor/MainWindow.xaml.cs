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
using System.Windows.Forms;
using NSHG;

namespace XMLEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool NetworkLoaded;
        Network network;
        bool saved;
        string file;
        
        public MainWindow()
        {
            InitializeComponent();
            network = Network.NewNet();
            NetworkLoaded = true;
            saved = true;
        }

        public void ReloadSystemPane()
        {
            List<BasicSys> basicSystems = new List<BasicSys>();
            foreach (KeyValuePair<uint, NSHG.System> kvp in network.Systems)
            {
                basicSystems.Add(new BasicSys() { ID = kvp.Key.ToString(), Type = kvp.Value.GetType().Name.ToString() });
            }
            Systems.ItemsSource = basicSystems;

            List<BasicConnection> basicConnections = new List<BasicConnection>();
            foreach (Tuple<uint, uint> tuple in network.Connections)
            {
                basicConnections.Add(new BasicConnection() { ID1 = tuple.Item1.ToString(), ID2 = tuple.Item2.ToString() });
            }
            Connections.ItemsSource = basicConnections;
        }


        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (saved)
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                DialogResult result = fileDialog.ShowDialog();
                switch (result)
                {
                    case System.Windows.Forms.DialogResult.OK:
                        file = fileDialog.FileName;
                        break;
                    case System.Windows.Forms.DialogResult.Cancel:
                    default:
                        return;
                }
                
                network = NetworkManagement.LoadNetwork(file);

                ReloadSystemPane();
                Frame.Content = null;

                NetworkLoaded = true;
                saved = false;
            }
        }

        private void NewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (saved)
            {
                network = Network.NewNet();

                ReloadSystemPane();
                Frame.Content = null;
                NetworkLoaded = true;
                saved = false;
            }
        }

        private void SaveAsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!NetworkLoaded)
            {
                return;
            }
            SaveFileDialog fileDialog = new SaveFileDialog();
            DialogResult result = fileDialog.ShowDialog();
            
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    file = fileDialog.FileName;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    return;
            }

            NetworkManagement.SaveNetwork(network, file);
            saved = true;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (file == "" || !NetworkLoaded)
            {
                return;
            }

            NetworkManagement.SaveNetwork(network, file);
            saved = true;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (saved)
            {
                network = new Network();
                NetworkLoaded = false;
                saved = true;

                Systems.ItemsSource = null;
                Connections.ItemsSource = null;
                Frame.Content = null;
            }
        }


        private void SelectSystemButton_Click(object sender, RoutedEventArgs e)
        {
            if (NetworkLoaded && Systems.SelectedItem != null)
            {
                BasicSys sys = (BasicSys)Systems.SelectedItem;
                NSHG.System s = network.Systems[uint.Parse(sys.ID)];
                Frame.Content = null;
                Frame.Content = new Editor(network, s, this);
            }
            
        }

        private void SelectConnectionButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class BasicSys
    {
        public string ID   { get; set; }
        public string Type { get; set; }
    }
    public class BasicConnection
    {
        public string ID1 { get; set; }
        public string ID2 { get; set; }
    }
}
