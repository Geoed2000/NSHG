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
using System.Net.Sockets;
using NSHG;

namespace Client
{
    /// <summary>
    /// Interaction logic for ConnectScreen.xaml
    /// </summary>
    public partial class ConnectScreen : Page
    {
        public static char[] nums = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        public MainWindow Parent;
        Socket ClientSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.IPv4);
        int attempts;
        public ConnectScreen(MainWindow Parent)
        {
            this.Parent = Parent;
            InitializeComponent();
        }

        private void Byte_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            if (int.TryParse(box.Text, out int value))
            {
                if (value > 255) value = 255;
                box.Text = value.ToString();
            }
            else
            {
                box.Text = new string(box.Text.Where(new Func<char, bool>(c => (nums.Contains(c)))).ToArray());
                if (!string.IsNullOrWhiteSpace(box.Text))
                {
                    value = int.Parse(box.Text);
                    if (value > 255) value = 255;
                    box.Text = value.ToString();
                }
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string ip = Byte1.Text + "." + Byte2.Text + "." + Byte3.Text + "." + Byte4.Text;
            attempts = 0;
            ClientSocket.BeginConnect(ip, Network.port, connectRecieveCallback, ClientSocket);
           
        }
        
        public void connectRecieveCallback(IAsyncResult asyncResult)
        {
            if (ClientSocket.Connected)
            {
                Parent.MainFrame.Content = new LoginScreen(Parent);
            }
            else
            {
                attempts++;
                if (attempts >= 3)
                {
                    OutLabel.Content = "Could not connect to server";
                }
                else
                {
                    OutLabel.Content = "Failed attempt "+ attempts + " to connect";
                }
            }
        }
    }
}
