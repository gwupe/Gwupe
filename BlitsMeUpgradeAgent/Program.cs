using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;

namespace BlitsMe.Agent.Upgrade
{
    static class Program
    {
        private const string AppGuid = "BlitsMe.Agent.Upgrade.Application";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // never run as system user
            if (System.Environment.UserName.Equals("SYSTEM")) return;
            Process[] prs = Process.GetProcesses();

            foreach (Process pr in prs)
            {
                if (pr.ProcessName == "BlitsMe.Agent" && (Environment.UserDomainName + "\\" + Environment.UserName).Equals(GetProcessOwner(pr.Id)))
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
            // Now start BlitsMe
            try
            {
                Process.Start(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\BlitsMe.Agent.exe");
            }
            catch
            {
            }
        }

        public static string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    // return DOMAIN\user
                    return argList[1] + "\\" + argList[0];
                }
            }

            return "NO OWNER";
        }
    }
}
