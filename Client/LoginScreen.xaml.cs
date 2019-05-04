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
    /// Interaction logic for LoginScreen.xaml
    /// </summary>
    public partial class LoginScreen : Page
    {
        MainWindow Parent;
        Socket ClientSocket;
        byte[] Buffer = new byte[Network.buffersize];

        public LoginScreen(MainWindow Parent, Socket socket)
        {
            ClientSocket = socket;
            this.Parent = Parent;
            InitializeComponent();
            ClientSocket.BeginReceive(Buffer, 0, 0, SocketFlags.None, LoginRecieveCallback, ClientSocket);
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {

             ClientSocket.BeginSend()
        }

        private void NewUserButton_Click(object sender, RoutedEventArgs e)
        {

        }

        public void LoginRecieveCallback(IAsyncResult asyncResult)
        {
            int recieved;
            try
            {
                recieved = ClientSocket.EndReceive(asyncResult);
            }
            catch(SocketException)
            {
                Parent.MainFrame.Content = new ConnectScreen(Parent);
                ClientSocket.Close();
                return;
            }
            string data = Encoding.ASCII.GetString(Buffer, 0, recieved);

        }
    }
}
