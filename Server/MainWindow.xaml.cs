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
using System.Net;
using System.Net.Sockets;


namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Timer TickTimer = new Timer(200);
        public List<string> SystemLog = new List<string>();
        public List<string> CommandLog = new List<string>();
        int upto = 0;
        bool TickInProgress = false;
        private object TIPLock = new object();
        private object LogLock;
        string filepath = "";
        Network network;
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
            //Log("Tick Start");
            tick++;
            //Log("The Elapsed event was raised at " + e.SignalTime.Hour + "." + e.SignalTime.Minute + "." + e.SignalTime.Second + "." + e.SignalTime.Millisecond);
            //Log("Tick " + tick + " Started at " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond );
            try
            {
                network.Tick(tick);
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
            finally
            {
                if (tick % 10 == 0)
                {
                    Log("tick " + tick);
                }

                lock (TIPLock)
                {
                    TickInProgress = false;
                }
            }
            //Log("Tick " + tick + " Ended at " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
            
            //Log("Tick fin");
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
                            if (network.SaveNetwork(tmpfilepath, Log)) Log("Save successfull");
                            else Log("Save unsuccessfull");
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
                        List<string> NetworkCommandList = new List<string>(commandlist);   
                        NetworkCommandList.RemoveAt(0);
                        network.asSystem(NetworkCommandList.ToArray());
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
                        
                    }
                    break;
                case "report":
                    try
                    {
                        tmpfilepath = commandlist[1];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Log("no filepath given");
                        break;
                    }
                    if (network.report(tmpfilepath)) Log("Successfull generated report at " + tmpfilepath);
                    else Log("Invalid filepath");
                    break;
                case "help":
                    Log("load filepath            -Loads a system from the specified filepath");
                    Log("tick                     -Ticks the network once");
                    Log("start                    -Starts the ticking of the network");
                    Log("stop                     -Stops the ticking of the network");
                    Log("tickrate MSBetweenTicks  -Sets the ms delay between each tick default 200 ms lower = more ticks per seccond");
                    Log("network                  -Displays each system in the network, its ID and its Type");
                    Log("as SystemID [command]    -Runs a command on selected system, use 'as [systemID] help' for more info");
                    Log("report filepath          -Generates a report of the users collected flags and times collected at");
                    Log("save filepath            -Saves the current network - Wanring doesn't save progress & should only be use for debugging, to see user progress use 'report'");
                    break;
                default:
                    Log("Unknown command: " + commandlist[0]);
                    Log("Use the 'help' command for a list of commands");
                    break;


            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string command = CommandsIn.Text;
            command = command.Trim();

            CommandsIn.Text = "";
            Log("=> " + command);
            CommandLog.Add(command);
            upto = CommandLog.Count;
            Command(command);
        }

        private void CommandsIn_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter||e.Key == Key.Return)
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
    }
}
