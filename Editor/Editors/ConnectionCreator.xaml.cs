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
using System.Windows.Shapes;

namespace XMLEditor
{
    /// <summary>
    /// Interaction logic for ConnectionCreator.xaml
    /// </summary>
    public partial class ConnectionCreator : Window
    {
        List<Tuple<uint, uint>> Connections;
        MainWindow mainWindow;

        public ConnectionCreator(MainWindow mainWindow ,ref List<Tuple<uint,uint>> connections)
        {
            Connections = connections;
            this.mainWindow = mainWindow;
            InitializeComponent();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            uint tmpsys1;
            uint tmpsys2;
            bool ok = true;
            if (!uint.TryParse(System1.Text, out tmpsys1))
            {
                ok = false;
                System1.Text = "invalid system ID";
            }

            if (!uint.TryParse(System2.Text, out tmpsys2))
            {
                ok = false;
                System2.Text = "invalid system ID";
            }

            if (ok)
            {
                Connections.Add(new Tuple<uint, uint>(tmpsys1,tmpsys2));
                mainWindow.ReloadSystemPane();
                Close();
            }
            
        }
    }
}
