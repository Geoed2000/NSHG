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
        private Timer TickTimer = new Timer(200);
        public List<string> CommandLog = new List<string>();
        int upto = 0;
        bool TickInProgress = false;
        private object TIPLock = new object();
        public List<string> SystemLog = new List<string>();
        private object LogLock;
        string filepath = "";
        Network network = new Network();
        bool networkloaded = false;
        uint tick = 0;


        public MainWindow()
        {
            LogLock = new object();
            TickTimer.Elapsed += Tick;  


            InitializeComponent();


            BrushConverter bc = new BrushConverter();
            Brush backgroudcol = (Brush)bc.ConvertFrom("#252526");
            backgroudcol.Freeze();
            Brush foregroundcol = (Brush)bc.ConvertFrom("#3e3e42");
            backgroudcol.Freeze();
            Brush fontcol = (Brush)bc.ConvertFrom("#f1f1f1");
            fontcol.Freeze();

            Grid.Background = backgroudcol;
            LogBox.Foreground = fontcol;
            CommandsIn.Foreground = fontcol;
            CommandsIn.Background = foregroundcol;
        }

        private void Tick(object source, ElapsedEventArgs e)
        {
            lock (TIPLock)
            {
                if (!TickInProgress)
                {
                    TickInProgress = true;
                }
                else return;
            }
            Log("Tick Start");
            tick++;
            //Log("The Elapsed event was raised at " + e.SignalTime.Hour + "." + e.SignalTime.Minute + "." + e.SignalTime.Second + "." + e.SignalTime.Millisecond);
            //Log("Tick " + tick + " Started at " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond );
            foreach (NSHG.System s in network.Systems.Values)
            {
                s.Tick(tick);
            }
            //Log("Tick " + tick + " Ended at " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
            if (tick % 10 == 0)
            {
                Log("tick " + tick);
            }

            lock (TIPLock)
            {
                TickInProgress = false;
            }
            Log("Tick fin");
        }
        
        public void Log(string log)
        {
            lock (LogLock)
            {
                SystemLog.Add(log);

                Dispatcher.Invoke(new Action(delegate ()
                {
                    LogBox.Text += log + "\n";
                    ScrollView.ScrollToBottom();
                }));
            }
        }

        private void Command(string command)
        {
            command = command.Trim();

            CommandsIn.Text = "";
            Log("=> " + command);
            CommandLog.Add(command);
            upto = CommandLog.Count;

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
                            if (network.SaveNetwork(tmpfilepath, Log)) Console.WriteLine("Save successfull");
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
                case "network":
                    if (!networkloaded)
                    {
                        Log("No network loded");
                    }
                    else
                    {
                        foreach (NSHG.System s in network.Systems.Values)
                        {
                            Log("ID: " + s.ID + "  TypeOf: " + s.GetType());
                        }
                    }
                    break;
                case "as":
                    if (!networkloaded)
                    {
                        Log("No network loded");
                    }
                    else
                    {
                        if(commandlist.Length > 1)
                        {
                            try
                            {
                                uint id = uint.Parse(commandlist[1]);
                                string newcommand = "";
                                for (int i = 2; i < commandlist.Length; i++)
                                {
                                    newcommand += commandlist[i] + " ";
                                }
                                network.Systems[id].Command(newcommand.Trim(' '));
                            }
                            catch (FormatException e)
                            {
                                Log("Error parsing ID");
                            }
                            catch (OverflowException e)
                            {
                                Log("Error, ID too large");
                            }
                        }
                        else
                        {
                            Log("Must specify a system to act as");
                        }
                    }
                    break;
                case "tick":
                    Tick(null,null);
                    break;
                case "tickrate":
                    try
                    {
                        TickTimer.Interval = double.Parse(commandlist[1]);
                    }
                    catch (Exception e)
                    {
                        Log(e.ToString());
                        
                    }break;

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
    }
}
