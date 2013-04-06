using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Deployment.WindowsInstaller;

namespace BlitsMeSetupCustomAction
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult OpenBlitsMeAgentIfNotOpen(Session session)
        {
            Process[] prs = Process.GetProcesses();

            foreach (Process pr in prs)
            {
                if (pr.ProcessName == "BlitsMe.Agent" &&
                    (Environment.UserDomainName + "\\" + Environment.UserName).Equals(GetProcessOwner(pr.Id)))
                {
                    return ActionResult.Success;
                }
            }
            // Now start BlitsMe
            try
            {
                Process.Start(ProgramFilesx86() + "\\BlitsMe\\BlitsMe.Agent.exe");
            }
            catch
            {
                return ActionResult.Failure;
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CloseBlitsMeAgents(Session session)
        {
            session.Log("Begin terminate BlitsMeAgent");
            Process[] prs = Process.GetProcesses();

            foreach (Process pr in prs)
            {
                if (pr.ProcessName == "BlitsMe.Agent")
                {
                    try
                    {
                        pr.CloseMainWindow();
                        pr.Close();
                    }
                    catch (Exception e)
                    {
                        session.Log("Exception caught: " + e.Message);
                    }
                }
            }

            prs = Process.GetProcesses();
            foreach (Process pr in prs)
            {
                if (pr.ProcessName == "BlitsMe.Agent")
                {
                    session.Log("Agent is still alive and kicking so we gonna kill 'im after 5 seconds");
                    if (!pr.WaitForExit(5000))
                    {
                        session.Log("Agent: times up");
                        try
                        {
                            pr.Kill();
                        }
                        catch (Exception e)
                        {
                            session.Log("Exception caught: Failed to kill process");
                        }
                    }
                }
            }

            return ActionResult.Success;
        }

        static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
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
