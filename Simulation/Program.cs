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
            public Dictionary<MAC, NSHG.System> Systems;
            public List<Connection> Connections;
        }

        private static Network LoadNetwork(string filepath)
        {
            Dictionary<MAC,NSHG.System> Systems = new Dictionary<MAC, NSHG.System>();
            
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
                        break;

                    case "Connection":
                        Connections.Add(Connection.FromNode(node));
                        continue;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid Identifier " + s);
                        Console.ForegroundColor = ConsoleColor.Green;
                        continue;
                }
                Systems.Add(sys.MacAddress, sys);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }


    }
        static void Main(string[] args)
        {
            Systems = new Dictionary<MAC, NSHG.System>();

            if (!LoadNetwork("sys1.xml"))
            {

            }


    }


    }
}
