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
using System.Timers;
using NSHG;
using NSHG.Protocols.IPv4;


namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Timer TickTimer = new Timer(1000);
        public List<string> CommandLog = new List<string>();
        public List<string> SystemLog = new List<string>();
        string filepath = "";
        Network network = new Network();
        bool networkloaded = false;
        uint tick = 0;


        public MainWindow()
        {
            TickTimer.Elapsed += Tick;  
            InitializeComponent();
        }

        private void Tick(object source, ElapsedEventArgs e)
        {
            tick += 1;
            Log("The Elapsed event was raised at " + e.SignalTime.Hour + "." + e.SignalTime.Minute + "." + e.SignalTime.Second + "." + e.SignalTime.Millisecond);
            Log("Tick " + tick + " Started at " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond );
            foreach (NSHG.System s in network.Systems.Values)
            {
                s.Tick(tick);
            }
            Log("Tick " + tick + " Ended at " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            
        }

        public void Log(string log)
        {
            SystemLog.Add(log);
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                LogBox.Text += log + "\n";
                ScrollView.ScrollToBottom();
            }));
        }

        private void Command(string command)
        {
            command = command.Trim();

            CommandsIn.Text = "";
            Log(command);

            string[] commandlist = command.Split(' ');
            string tmpfilepath;

            switch (commandlist[0])
            {
                case "load":
                    try
                    {
                        tmpfilepath = commandlist[1];
                        network = Network.LoadNetwork(tmpfilepath,Log);
                        networkloaded = true;
                        filepath = tmpfilepath;
                    }
                    catch (Exception e)
                    {
                        Log(e.ToString());
                        break;
                    }
                    break;
                case "save":
                    if (!networkloaded)
                    {
                        Log("No network loded");
                    }
                    else
                    {
                        try
                        {
                            tmpfilepath = commandlist[1];
                            if (network.SaveNetwork(tmpfilepath)) Console.WriteLine("Save successfull");
                            else Console.WriteLine("Save unsuccessfull");
                        }
                        catch (Exception)
                        {
                            Log("no filepath given, or invalid filepath");
                            break;
                        }
                    }
                    break;
                case "start":
                    if (!networkloaded)
                    {
                        Log("No network loded");
                    }
                    else
                    {
                        Log("Started");
                        TickTimer.Enabled = true;
                    }
                    break;
                case "stop":
                    Log("Stoping");
                    TickTimer.Enabled = false;
                    break;
                case "help":
                    break;
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            Command(CommandsIn.Text);
        }

        private void CommandsIn_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter||e.Key == Key.Return)
            {
                string command = CommandsIn.Text;
                Command(command);
            }
        }
    }
}
