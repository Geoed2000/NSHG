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

        static void Main(string[] args)
        {
            Dictionary<MAC,NSHG.System> Systems = new Dictionary<MAC, NSHG.System>();
            
            if (LoadNetwork())
            {

            }


        }


    }
}
