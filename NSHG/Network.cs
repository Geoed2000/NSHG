using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NSHG
{
    public struct Network
    {
        public Dictionary<uint, System> Systems;
        public List<Tuple<uint, uint>> Connections;
        public List<uint> OnlinePlayers;
        public List<uint> OfflinePlayers;
        public List<uint> UnallocatedPlayers;

        public static Network NewNet()
        {
            Network n = new Network();
            n.Systems = new Dictionary<uint, System>();
            n.Connections = new List<Tuple<uint, uint>>();
            n.OnlinePlayers = new List<uint>();
            n.OfflinePlayers = new List<uint>();
            n.UnallocatedPlayers = new List<uint>();
            return n;
        }


    }

    public class NetworkManagement
    {
        public static bool Connect(Network network, uint sysA, uint sysB)
        {
            Adapter a,b;
            if(network.Systems[sysA].GetConnectedUnassociatedAdapter(out a, sysB) && network.Systems[sysB].GetConnectedUnassociatedAdapter(out b, sysA))
            {
                a.Connect(b);
                b.Connect(a);
                network.Connections.Add(new Tuple<uint, uint>(sysA, sysB));
                return true;
            }
            else if(network.Systems[sysA].GetFreeAdapter(out a) && network.Systems[sysB].GetFreeAdapter(out b))
            {
                a.Connect(b);
                b.Connect(a);
                network.Connections.Add(new Tuple<uint, uint>(sysA, sysB));
                return true;
            }
            
            return false;
        }
        public static bool Connect(Network network, XmlNode Parent)
        {

            uint sys1 = 0;
            uint sys2 = 0;
            List<uint> IDs = new List<uint>();
            foreach (XmlNode n in Parent.ChildNodes)
            {
                if (n.Name == "ID")
                {
                    if (sys1 != 0)
                    {
                        sys2 = uint.Parse(n.InnerText);
                    }
                    else
                    {
                        sys1 = uint.Parse(n.InnerText);
                    }
                }
            }
            return Connect(network, sys1, sys2); 
        }

        public static Network LoadNetwork(string filepath)
        {
            Network network = Network.NewNet();
            
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);
            foreach (XmlNode node in doc.DocumentElement)
            {
                string s = node.Name;

                
                NSHG.System sys;

                switch (s)
                {
                    case "System":
                        try
                        {
                            sys = NSHG.System.FromXML(node);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Reading System Failed");
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        }
                        try
                        {
                            
                            network.Systems.Add(sys.ID, sys);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Added System \n    ID:" + sys.ID);
                            Console.ForegroundColor = ConsoleColor.White;
                            
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("failed adding system to network");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;

                    case "Player":

                        break;
                    case "Connection":
                        if (Connect(network, node))
                        {
                            //success
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Addedd connection\n    {0}", node.InnerText);
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        else
                        {
                            //failure
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to add connection\n    {0}",node.InnerText);
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid Identifier " + s);
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }
            }
            return network;
        }
        public static bool SaveNetwork(Network network, string filepath)
        {
            try
            {
                XmlDocument file = new XmlDocument();
                XmlNode rootNode = file.CreateElement("root");
                

                foreach (KeyValuePair<uint, NSHG.System> entry in network.Systems)
                {
                    rootNode.AppendChild(entry.Value.ToXML(file));
                }

                foreach (Tuple<uint, uint> connection in network.Connections)
                {
                    XmlNode c1 = file.CreateElement("ID");
                    c1.InnerText = connection.Item1.ToString();
                    XmlNode c2 = file.CreateElement("ID");
                    c2.InnerText = connection.Item2.ToString();
                    XmlNode Connection = file.CreateElement("Connection");
                    Connection.AppendChild(c1);
                    Connection.AppendChild(c2);
                    rootNode.AppendChild(Connection);
                }
                file.AppendChild(rootNode);
                file.Save(filepath);
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
