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
using NSHG;

namespace XMLEditor.Editors
{
    /// <summary>
    /// Interaction logic for AdapterEditor.xaml
    /// </summary>
    public partial class AdapterEditor : Window
    {
        public AdapterEditor(Adapter a)
        {
            InitializeComponent();
        }

        private void NameIn_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void MacIn_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void ConnectedIn_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void OtherEndIn_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void LocalIPIn_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void SubnetIn_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void DefaultGatewayIn_LostFocus(object sender, RoutedEventArgs e)
        {

        }
    }
}
