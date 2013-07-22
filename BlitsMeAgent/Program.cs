using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
        static void Main(String[] args)
        {
            // never run as system user
            if (Environment.UserName.Equals("SYSTEM")) return;
            using (Mutex mutex = new Mutex(false, AppGuid))
            {
                if (!mutex.WaitOne(0, false))
                {
                    var process = Common.OsUtils.GetMyDoppleGangerProcess();
                    if (process != null)
                    {
                        var outcome = Common.OsUtils.PostMessage((IntPtr)Common.OsUtils.HWND_BROADCAST, Common.OsUtils.WM_SHOWBM, 
#if DEBUG
                            IntPtr.Zero, 
#else
                            new IntPtr(1), 
#endif
                            IntPtr.Zero);
                    }
                    else
                    {
                        MessageBox.Show("BlitsMe" + BuildMarker + " Is already running.");
                    }
                    return;
                }
                // Make sure we load certain namespaces as resources (they are embedded dll's)
                AppDomain.CurrentDomain.AssemblyResolve += EmbeddedAssemblyResolver;

                GC.Collect();
                try
                {
                    Thread.CurrentThread.Name = "MAIN";
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    var options = new List<BlitsMeOption>();
                    foreach (string argument in args)
                    {
                        if (argument.ToLower().Equals("/minimize"))
                        {
                            options.Add(BlitsMeOption.Minimize);
                        }
                    }
                    Application.Run(new BlitsMeClientAppContext(options));
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
