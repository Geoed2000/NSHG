using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSHG;


namespace Simulation
{
    class Program
    {
       
        private static bool LoadNetwork(string filepath)
        {
            

            return true;
        }

        static Dictionary<MAC,NSHG.System> Systems;
        static void Main(string[] args)
        {
            Systems = new Dictionary<MAC, NSHG.System>();

            if (!LoadNetwork("sys1.xml"))
            {

            }


    }


    }
}
