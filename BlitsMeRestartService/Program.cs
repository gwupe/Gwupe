using System;
using System.Security.Authentication.ExtendedProtection;
using System.ServiceProcess;

namespace BlitsMeRestartService
{
    class Program
    {
#if DEBUG
        private static String ServiceName = "BlitsMe_Dev Service";
#else
        private static String ServiceName = "BlitsMe Service";
#endif
        private static int TimeoutMillis = 10000;

        static void Main(string[] args)
        {
            ServiceController service = new ServiceController(ServiceName);
            try
            {
                int millisec1 = Environment.TickCount;
                TimeSpan timeout = TimeSpan.FromMilliseconds(10000);
                if (service.Status.Equals(ServiceControllerStatus.Running))
                {
                    service.Stop();
                    Console.WriteLine("Waiting for " + ServiceName + " to stop.");
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                }

                // count the rest of the timeout
                int millisec2 = Environment.TickCount;
                timeout = TimeSpan.FromMilliseconds(10000 - (millisec2 - millisec1));

                Console.WriteLine("Starting " + ServiceName + ".");
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to restart Service " + ServiceName + " : " + e);
                Environment.Exit(1);
            }
        }
    }
}
