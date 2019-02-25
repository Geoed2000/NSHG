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
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class Editor : Page
    {
        public Editor(NSHG.Network network, NSHG.System s, MainWindow parent)
        {
            InitializeComponent();

            switch (s.GetType().ToString())
            {
                case "NSHG.System":
                    Frame.Content = new Editors.SystemEditor(network, s, parent);
                    break;
                default:
                    Frame.Content = new Label() { Content = "Error Reading System Type" };
                    break;
            }
        }
    }
}
