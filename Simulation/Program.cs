using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NSHG;


namespace Simulation
{
    class Program
    {
        struct Network
        {
            public Dictionary<uint, NSHG.System> Systems;
            public List<Tuple<uint, uint>> Connections;
            public List<uint> OnlinePlayers;
            public List<uint> OfflinePlayers;
            public List<uint> UnallocatedPlayers;
        }

        private static bool Connect(Dictionary<uint, NSHG.System>systems, uint sysA, uint sysB)
        {
            Adapter a,b;
            if(systems[sysA].GetFreeAdapter(out a) && systems[sysB].GetFreeAdapter(out b))
            {
                a.Connect(b);
                b.Connect(a);
                a.Associated = true;
                b.Associated = true;
                return true;
            }

            return false;
        }
        private static bool Connect(Dictionary<uint, NSHG.System>systems, XmlNode Parent)
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
            return Connect(systems, sys1, sys2); 
        }

        private static Network LoadNetwork(string filepath)
        {
            Network network = new Network();
            
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);
            foreach (XmlNode node in doc.DocumentElement)
            {
                string s = node.Name;

                
                NSHG.System sys;

                switch (s)
                {
                    case "System":
                        sys = NSHG.System.FromNode(node);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Added System \n    ID:" + sys.ID);
                        Console.ForegroundColor = ConsoleColor.White;
                        network.Systems.Add(sys.ID, sys);
                        break;

                    case "Player":

                        break;
                    case "Connection":
                        if (Connect(network.Systems, node))
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
    
        static void Main(string[] args)
        {
            if (Console.ReadLine() != "")
            {
                
            }
            else
            {
                string filepath = "sys1.xml";
                Network network = LoadNetwork(filepath);

                Console.WriteLine(network.Systems[1].ID);
                Console.ReadLine();

            }
        }

    }
}
