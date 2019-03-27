using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG.Applications
{
    public abstract class Application
    {
        public abstract void OnTick(uint tick);
    }
}
