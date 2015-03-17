using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Gwupe.Upgrade
{
    static class Program
    {
#if DEBUG
        private const String BuildMarker = "_Dev";
#else
        private const String BuildMarker = "";
#endif
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // never run as system user
            if (System.Environment.UserName.Equals("SYSTEM"))
            {
                Console.WriteLine("Cannot run as system user");
                return;
            }
            Process[] prs = Process.GetProcesses();

            try
            {
                foreach (Process pr in prs)
                {
                    //Console.WriteLine(pr.ProcessName);
                    if (pr.ProcessName == "Gwupe.Agent" || pr.ProcessName == "BlitsMe.Agent")
                    {
                        // If Gwupe was started by an elevated process we can't get the module path.

                        String path = OsUtils.GetMainModuleFilepath(pr.Id);
                        String owner = OsUtils.GetProcessOwner(pr.Id);
                        Console.WriteLine("Testing " + pr.ProcessName + ", " + path + " owned by " + owner);
                        if (path != null)
                        {
                            if (
                                ((OsUtils.ProgramFilesx86 + "\\Gwupe" + BuildMarker + "\\Gwupe.Agent.exe").Equals(path) ||
                                 (OsUtils.ProgramFilesx86 + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe").Equals(
                                     path)) &&
                                (Environment.UserDomainName + "\\" + Environment.UserName).Equals(owner))
                            {
                                Console.WriteLine("Killing " + pr.Id + "(" + pr.ProcessName + ")");
                                try
                                {
                                    if (!pr.WaitForExit(20000))
                                    {
                                        pr.Kill();
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed to kill " + pr.Id + " : " + e.Message);
                                }
                            }
                        }
                        else
                        {
                            // We have an elevated process, we need to wait for it to die (its quitting internally)
                            Console.WriteLine("Elevated process found, we will wait for it to die just in case");
                            try
                            {
                                pr.WaitForExit(20000);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Failed to wait for process to die : " + e.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to kill other instances of BlitsMe or Gwupe : " + e.Message);
            }
            // Now start Gwupe
            try
            {
                //string fileName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location.Replace("BlitsMe", "Gwupe")) + "\\Gwupe.Agent.exe";
                string fileName = OsUtils.ProgramFilesx86 + "\\Gwupe" + BuildMarker + "\\Gwupe.Agent.exe";
                Process.Start(fileName);
                Console.WriteLine("Started " + fileName);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to execute Gwupe.exe : " + e.Message);
            }
        }


    }
}
