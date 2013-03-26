using System;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

namespace BlitsMe.Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new BMService() 
			};
            ServiceBase.Run(ServicesToRun);
        }
    }
}
