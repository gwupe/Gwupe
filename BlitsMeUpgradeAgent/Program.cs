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
            if (System.Environment.UserName.Equals("SYSTEM")) return;
            Process[] prs = Process.GetProcesses();

            try
            {
                foreach (Process pr in prs)
                {
                    if (pr.ProcessName == "BlitsMe.Agent" &&
                        (OsUtils.ProgramFilesx86 + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe")
                            .Equals(OsUtils.GetMainModuleFilepath(pr.Id)) &&
                        (Environment.UserDomainName + "\\" + Environment.UserName).Equals(
                            OsUtils.GetProcessOwner(pr.Id)))
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
                    if (pr.ProcessName == "Gwupe" &&
                        (OsUtils.ProgramFilesx86 + "\\Gwupe" + BuildMarker + "\\Gwupe.Agent.exe")
                            .Equals(OsUtils.GetMainModuleFilepath(pr.Id)) &&
                        (Environment.UserDomainName + "\\" + Environment.UserName).Equals(
                            OsUtils.GetProcessOwner(pr.Id)))
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
                Process.Start(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Gwupe.Agent.exe");
            }
            catch
            {
            }
        }


    }
}
