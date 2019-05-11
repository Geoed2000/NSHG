using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG.Applications
{
    public abstract class Application
    {
        public List<string> log;
        public Action<string> Log;
        public bool closed;

        public Application(Action<string> Log = null)
        {
            closed = false;
            log = new List<string>();
            this.Log += Log ?? Console.WriteLine;
            this.Log += log.Add;
        }
        
        public abstract void OnTick(uint tick);
        public abstract void Command(string commandstring);
    }
}
