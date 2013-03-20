using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using log4net.Config;

namespace BlitsMe.Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            XmlConfigurator.Configure();
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new BMService() 
			};
            ServiceBase.Run(ServicesToRun);
        }
    }
}
