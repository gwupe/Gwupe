using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace BlitsMe.Agent
{
    static class Program
    {
#if DEBUG
        public const String BuildMarker = "_Dev";
#else
        public const String BuildMarker = "";
#endif
        private const string AppGuid = "BlitsMe.Agent" + BuildMarker + ".Application";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // never run as system user
            if (Environment.UserName.Equals("SYSTEM")) return;
            using (Mutex mutex = new Mutex(false, AppGuid))
            {
                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show("BlitsMe" + BuildMarker + " Is already running.");
                    return;
                }

                GC.Collect();
                // Make sure we load certain namespaces as resources (they are embedded dll's)
                AppDomain.CurrentDomain.AssemblyResolve += EmbeddedAssemblyResolver;
                try
                {
                    Thread.CurrentThread.Name = "MAIN";
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new BlitsMeClientAppContext());
                }

                catch
                    (Exception
                    ex)
                {

                    MessageBox.Show(ex.Message + (ex.InnerException != null ? " : " + ex.InnerException.Message : ""),
                                    "Program Terminated Unexpectedly",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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

                MessageBox.Show(ex.Message + (ex.InnerException != null ? " : " + ex.InnerException.Message : ""), "Program Failed to access Assembly " + args.Name,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null;
        }
    }
}
