﻿using System;
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
            ClientSocket.BeginReceive(Buffer, 0, Network.buffersize, SocketFlags.None, LoginRecieveCallback, ClientSocket);
        }

        public void log(string log)
        {
            Dispatcher.Invoke(new Action(delegate ()
               {
                   OutLabel.Content = log;
               }));
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            byte[] data = Encoding.ASCII.GetBytes("login " + UsernameIn.Text + " " + PasswordIn.Text);
            ClientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, null, null);
        }

        private void NewUserButton_Click(object sender, RoutedEventArgs e)
        {
            byte[] data = Encoding.ASCII.GetBytes("new " + UsernameIn.Text);
            ClientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, null, null);
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
                Dispatcher.Invoke(new Action(delegate ()
                {
                   Parent.MainFrame.Content = new ConnectScreen(Parent);
                }));
                ClientSocket.Close();
                return;
            }
            if (recieved == 0) return;

            string data = Encoding.ASCII.GetString(Buffer, 0, recieved);
            string[] packet = data.Split(' ');
            switch (packet[0].ToLower())
            {
                case "success":
                    if (packet.Length > 1)
                    {
                        log(data);
                    }
                    Dispatcher.Invoke(new Action(delegate ()
                    {
                        ContinueButton.IsEnabled = true;
                        ContinueButton.Visibility = Visibility.Visible;
                    }));
                    
                    break;
                case "error":
                    if(packet.Length > 1)
                    {
                        log(data);
                        ClientSocket.BeginReceive(Buffer, 0, Network.buffersize, SocketFlags.None, LoginRecieveCallback, ClientSocket);

                    }
                    break;
                default:
                    log("Invalid server packet: " + data);
                    ClientSocket.BeginReceive(Buffer, 0, Network.buffersize, SocketFlags.None, LoginRecieveCallback, ClientSocket);
                    break;
            }
            
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            Parent.MainFrame.Content = new PlayScreen(ClientSocket, Parent);
        }
    }
}
