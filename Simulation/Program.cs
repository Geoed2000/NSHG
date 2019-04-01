using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using NSHG;
using NSHG.NetworkInterfaces;
using NSHG.Applications;


namespace Simulation
{
    class Program
    {
        //static Network edit(Network network)
        //{
        //    string input;
        //    do
        //    {
        //        Console.Write("--> ");
        //        input = Console.ReadLine();
        //        switch (input.ToLower())
        //        {
        //            case "add":
        //                {
        //                    Console.WriteLine("What would you like to add?");
        //                    Console.Write("--> ");
        //                    string type = Console.ReadLine();
        //                    switch (type)
        //                    {
        //                        case "System":
        //                            {
        //                                Console.WriteLine("What would you like to add?");
        //                                Console.Write("--> ");
        //                                uint id;

        //                                while (!uint.TryParse(Console.ReadLine(), out id))
        //                                {
        //                                    Console.WriteLine("Error parsing int");
        //                                    Console.Write("--> ");
        //                                }

        //                                network.Systems.Add(id, new NSHG.System(id));
        //                            }
        //                            break;
                                
        //                    }

        //                }
        //                break;
        //        }


        //    } while (input.ToLower() != "exit");
        //}
    
        static void Main(string[] args)
        {
            //Dictionary<MAC, NetworkInterface> interfaces = new Dictionary<MAC, NetworkInterface>();
            //NetworkInterface ni;
            //ni = new GroupAdapter(MAC.Random(), 1);
            //interfaces.Add(ni.MyMACAddress, ni);

            //NSHG.System s = new NSHG.Router(1,interfaces);

            //foreach(Application app in s.Apps)Console.WriteLine(app.GetType());

            Network n = Network.LoadNetwork("sys1.xml", Console.WriteLine);
            Console.WriteLine(n.SaveNetwork("sys2.xml", Console.WriteLine));


            Console.ReadKey();

        }
    }
}
