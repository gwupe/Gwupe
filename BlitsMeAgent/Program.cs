using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MutexManager;
using log4net.Config;

namespace BlitsMe.Agent
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // don't want more than 1 started
            if (!SingleInstance.Start())
            {
                MessageBox.Show("BlitsMe Already Running", "BlitsMe", MessageBoxButtons.OK);
                return;
            }
            XmlConfigurator.Configure();
            // Make sure we load certain namespaces as resources (they are embedded dll's)
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
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
                    return null;
                }
            };

            Thread.CurrentThread.Name = "MAIN";
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                var applicationContext = new BlitsMeClientAppContext();
                Application.Run(applicationContext);
            }

            catch (Exception ex)
            {

                MessageBox.Show(ex.Message + (ex.InnerException != null ? " : " + ex.InnerException.Message : ""), "Program Terminated Unexpectedly",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
