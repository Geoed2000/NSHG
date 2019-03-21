using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using NSHG;


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
            string filepath = "";
            Network network = new Network();
            bool networkloaded = false;

            string input = Console.ReadLine();
            if (input == "test")
            {
                filepath = "sys1.xml";
                network = Network.LoadNetwork(filepath, Console.WriteLine);
                networkloaded = true;
                Console.WriteLine(network.Systems[1].ID);
                Console.ReadLine();
            }

            do
            {
                Console.Write("--> ");
                input = Console.ReadLine();
                switch (input)
                {
                    case "load":
                        Console.WriteLine("Enter filepath \n--> ");
                        string tmpfilepath = Console.ReadLine();

                        network = Network.LoadNetwork(tmpfilepath);
                        networkloaded = true;
                        filepath = tmpfilepath;

                        break;
                    case "save":
                        if (!networkloaded)
                        {
                            Console.WriteLine("No network loded");
                        }
                        else
                        {
                            Console.Write("Please enter save filepath: ");
                            string savepath = Console.ReadLine();
                            if (network.SaveNetwork(savepath)) Console.WriteLine("Save successfull");
                            else Console.WriteLine("Save unsuccessfull");
                        }
                        break;
                    case "start":
                    case "help":
                        break;
                }
            } while (input != "exit");
        }
    }
}
