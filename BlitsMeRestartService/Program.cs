using System;
using System.Management;
using System.Security;
using System.Security.Authentication.ExtendedProtection;
using System.ServiceProcess;

namespace GwupeRestartService
{
    class Program
    {
#if DEBUG
        private static String ServiceName = "Gwupe_Dev Service";
        private static String CoreServiceName = "GwupeService_Dev";
#else
        private static String ServiceName = "Gwupe Service";
        private static String CoreServiceName = "GwupeService";
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

                EnableIfDisabled(CoreServiceName);

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

        public static void EnableIfDisabled(String serviceName)
        {
            try
            {
                var fullTrust = new PermissionSet(System.Security.Permissions.PermissionState.Unrestricted);
                fullTrust.Demand();
                using (var service = new ManagementObject(string.Format("Win32_Service.Name=\"{0}\"", serviceName)))
                /*
                string wmiQuery = @"SELECT * FROM Win32_Service WHERE Name='" + serviceName + @"'";
                var searcher = new ManagementObjectSearcher(wmiQuery);
                ManagementObjectCollection results = searcher.Get();
                foreach (ManagementObject service in results)*/
                {
                 
                    if (service["StartMode"].ToString() == "Disabled" || service["StartMode"].ToString() == "Manual")
                    {
                        Console.WriteLine("Enabling " + ServiceName + ".");
                        service.InvokeMethod("ChangeStartMode", new object[] { "Automatic" });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to check if service was enabled or enable it.");
            }
        }
    }
}
