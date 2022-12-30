using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunDLL128
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ServiceEntry()
            };

#if DEBUG
            ((ServiceEntry)ServicesToRun[0]).DebugStart();
            Thread.Sleep(Timeout.Infinite);
#endif
            ServiceBase.Run(ServicesToRun);
        }
    }
}
