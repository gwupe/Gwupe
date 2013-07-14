using System;
using System.Configuration.Install;
using System.Diagnostics;
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
            AppDomain.CurrentDomain.AssemblyResolve += EmbeddedAssemblyResolver;
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
                {
                    new BMService()
                };
            ServiceBase.Run(ServicesToRun);
        }

        private static Assembly EmbeddedAssemblyResolver(object sender, ResolveEventArgs args)
        {
            try
            {
                String resourceName = Assembly.GetExecutingAssembly().FullName.Split(',').First() + "." + new AssemblyName(args.Name).Name + ".dll";
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        Byte[] assemblyData = new Byte[stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        return Assembly.Load(assemblyData);
                    }
                }
            }
            catch (Exception ex)
            {
                // No where to log
            }
            return null;
        }
    }
}
