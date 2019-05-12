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
using System.Net.Sockets;
namespace Client
{
    /// <summary>
    /// Interaction logic for FlagSubmit.xaml
    /// </summary>
    public partial class FlagSubmit : Window
    {
        private Socket client;
        public FlagSubmit(Socket clientSocket)
        {
            InitializeComponent();
            client = clientSocket;
        }

        public void Responce(string s)
        {
            Dispatcher.Invoke(new Action(delegate () 
            {
                ResponceOut.Content = s;
            }));
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(FlagIn.Text.Trim() == ""))
            {
                byte[] data = Encoding.ASCII.GetBytes("flag " + FlagIn.Text);
                byte[] wrappedData = NSHG.PacketProtocol.WrapMessage(data);
                client.BeginSend(wrappedData, 0, wrappedData.Length, SocketFlags.None, null, null);
            }
        }
    }
}
