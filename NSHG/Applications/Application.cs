using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG.Applications
{
    public abstract class Application
    {
        public List<string> Log;


        public Application()
        {
            Log = new List<string>();
        }


        public abstract void OnTick(uint tick);
        public abstract void Command(string commandstring);
    }
}
