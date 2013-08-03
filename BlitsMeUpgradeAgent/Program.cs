using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;

namespace BlitsMe.Agent.Upgrade
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
            if (System.Environment.UserName.Equals("SYSTEM")) return;
            Process[] prs = Process.GetProcesses();

            try
            {
                foreach (Process pr in prs)
                {
                    if (pr.ProcessName == "BlitsMe.Agent" &&
                        (Common.OsUtils.ProgramFilesx86 + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe")
                            .Equals(Common.OsUtils.GetMainModuleFilepath(pr.Id)) &&
                        (Environment.UserDomainName + "\\" + Environment.UserName).Equals(
                            Common.OsUtils.GetProcessOwner(pr.Id)))
                    {
                        try
                        {
                            if (!pr.WaitForExit(20000))
                            {
                                try
                                {
                                    pr.Kill();
                                }
                                catch (Exception e)
                                {
                                }
                            }
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            }catch (Exception e) {}
            // Now start BlitsMe
            try
            {
                Process.Start(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\BlitsMe.Agent.exe");
            }
            catch
            {
            }
        }


    }
}
