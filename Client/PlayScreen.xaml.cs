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

namespace Client
{
    /// <summary>
    /// Interaction logic for PlayScreen.xaml
    /// </summary>
    public partial class PlayScreen : Page
    {
        MainWindow Parent;
        FlagSubmit Submit;
        Socket ClientSocket;
        List<string> SystemLog;
        Action<string> Log;
        List<string> ScenarioLog;
        byte[] buffer = new byte[NSHG.Network.buffersize];
        int upto;
        List<string> CommandLog;
        NSHG.PacketProtocol packetProtocol = new NSHG.PacketProtocol(NSHG.Network.buffersize);

        public PlayScreen(Socket clientSocket, MainWindow parent)
        {
            ScenarioLog = new List<string>();
            SystemLog = new List<string>();
            Parent = parent;
            Log += s => { SystemLog.Add(s); };
            Log += s => { SystemLogBox.Text += "\n" + s; };
            ClientSocket = clientSocket;
            CommandLog = new List<string>() { "" };
            packetProtocol.MessageArrived = ProcessPacket;
            InitializeComponent();
            clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, RecieveCallback, ClientSocket);
        }

        public void RecieveCallback(IAsyncResult asyncResult)
        {
            int recieved;
            try
            {
                recieved = ClientSocket.EndReceive(asyncResult);
            }
            catch (Exception e)
            {
                SystemLog.Add ("Client Forcefully Disconected " + e.Message);
                ClientSocket.Close();
                Parent.MainFrame.Content = new ConnectScreen(Parent);
                return;
            }
            if(recieved == 0)
            {
                return;
            }


            byte[] data = new byte[recieved];
            Array.Copy(buffer, data, recieved);

            packetProtocol.DataReceived(data);

            ClientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, RecieveCallback, ClientSocket);
        }
        public void SendCallback(IAsyncResult AR)
        {
            try
            {
                ClientSocket.EndSend(AR);
            }
            catch (SocketException)
            {
                ClientSocket.Close();
                Parent.MainFrame.Content = new ConnectScreen(Parent);
            }
        }

        public void ProcessPacket(byte[] Data)
        {
            string data = Encoding.ASCII.GetString(Data, 0, Data.Length);
            string[] commands = data.Split(' ');
            string message;
            switch (commands[0].ToLower())
            {
                case "flag":
                    message = data.Remove(0, commands[0].Length);
                    try
                    {
                        Submit.Responce(message);
                    }
                    catch { }
                    break;
                case "scenario":
                    message = data.Remove(0, commands[0].Length + 1);
                    Dispatcher.Invoke(new Action(delegate ()
                    {
                        ScenarioLog.Add(message);
                        ScenarioLogBox.Text += ("\n" + message + "\n");
                    }));
                    break;
                case "system":
                    message = data.Remove(0, commands[0].Length);
                    Dispatcher.Invoke(new Action(delegate ()
                    {
                        Log(message);
                    }));
                    break;
            }
        }

        private void FlagSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            Submit = new FlagSubmit(ClientSocket);
            Submit.Show();
        }
        
        private void Command(string command)
        {
            byte[] data = Encoding.ASCII.GetBytes("command " + command);
            byte[] wrappedData = NSHG.PacketProtocol.WrapMessage(data);
            
            ClientSocket.BeginSend(wrappedData, 0, wrappedData.Length, SocketFlags.None, SendCallback, ClientSocket);

        }

        private void CommandsIn_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                string command = CommandsIn.Text;
                command = command.Trim();

                CommandsIn.Text = "";
                Log("=> " + command);
                CommandLog.Add(command);
                upto = CommandLog.Count;
                Command(command);
            }
            else if (e.Key == Key.Up)
            {
                try
                {
                    CommandsIn.Text = CommandLog[--upto];
                }
                catch
                {
                    upto = 0;
                }
            }
            else if (e.Key == Key.Down)
            {
                try
                {
                    CommandsIn.Text = CommandLog[++upto];
                }
                catch
                {
                    upto = CommandLog.Count;
                }
            }
            else
            {
                upto = CommandLog.Count;
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
