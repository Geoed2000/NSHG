﻿using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG.Applications
{
    public abstract class Application
    {
        public List<string> log;
        public Action<string> Log;

        public Application(Action<string> Log = null)
        {
            log = new List<string>();
            this.Log += Log;
            this.Log += log.Add;
        }
        
        public abstract void OnTick(uint tick);
        public abstract void Command(string commandstring);
    }
}
