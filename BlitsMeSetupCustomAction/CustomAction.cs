using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Deployment.WindowsInstaller;

namespace BlitsMeSetupCustomAction
{
    public class CustomActions
    {
        const uint WM_QUIT = 0x12;
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostThreadMessage(int idThread, uint Msg, IntPtr wParam, IntPtr lParam);
#if DEBUG
        public const String BuildMarker = "_Dev";
#else
        public const String BuildMarker = "";
#endif
        [CustomAction]
        public static ActionResult OpenBlitsMeAgentIfNotOpen(Session session)
        {
            Process[] prs = Process.GetProcesses();

            foreach (Process pr in prs)
            {
                try
                {
                    if (pr.ProcessName == "BlitsMe.Agent" &&
                        (ProgramFilesx86() + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe")
                            .Equals(GetMainModuleFilepath(pr.Id)) &&
                        (Environment.UserDomainName + "\\" + Environment.UserName).Equals(GetProcessOwner(pr.Id)))
                    {
                        return ActionResult.Success;
                    }
                }
                catch (Exception e)
                {
                    session.Log("Problem accessing " + pr.ProcessName + " : " + e.Message);
                }
            }
            // Now start BlitsMe
            try
            {
                Process.Start(ProgramFilesx86() + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe");
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
                try
                {
                    if (pr.ProcessName == "BlitsMe.Agent" &&
                        (ProgramFilesx86() + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe")
                            .Equals(GetMainModuleFilepath(pr.Id)))
                    {
                        try
                        {
                            foreach (ProcessThread thread in pr.Threads)
                            {
                                PostThreadMessage(thread.Id, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                            }
                            //pr.CloseMainWindow();
                            //pr.Close();
                        }
                        catch (Exception e)
                        {
                            session.Log("Exception caught: " + e.Message);
                        }
                    }
                }
                catch (Exception e)
                {
                    session.Log("Problem accessing " + pr.ProcessName + " : " + e.Message);
                }
            }

            prs = Process.GetProcesses();
            foreach (Process pr in prs)
            {
                try
                {
                    if (pr.ProcessName == "BlitsMe.Agent" &&
                        (ProgramFilesx86() + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe")
                            .Equals(GetMainModuleFilepath(pr.Id)))
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
                catch (Exception e)
                {
                    session.Log("Problem accessing " + pr.ProcessName + " : " + e.Message);
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

        private static string GetMainModuleFilepath(int processId)
        {
            string wmiQueryString = "SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE ProcessId = " + processId;
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            {
                using (var results = searcher.Get())
                {
                    ManagementObject mo = results.Cast<ManagementObject>().FirstOrDefault();
                    if (mo != null)
                    {
                        return (string)mo["ExecutablePath"];
                    }
                }
            }
            return null;
        }
    }
}
