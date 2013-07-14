using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BlitsMe.Common
{
    public static class OsUtils
    {
        public const int HWND_BROADCAST = 0xFFFF;
        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
        [DllImport("user32")]
        public static extern int RegisterWindowMessage(string message);
        public static readonly int WM_SHOWBM = RegisterWindowMessage("WM_SHOWBM");
        public static readonly int WM_UPGRADEBM = RegisterWindowMessage("WM_RESTARTBM");
        public static readonly int WM_SHUTDOWNBM = RegisterWindowMessage("WM_SHUTDOWNBM");

        public static bool IsWinVistaOrHigher
        {
            get
            {
                {
                    OperatingSystem OS = Environment.OSVersion;
                    return (OS.Platform == PlatformID.Win32NT) && (OS.Version.Major >= 6);
                }
            }
        }

        public static string ProgramFilesx86
        {
            get
            {
                if (8 == IntPtr.Size
                    || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
                {
                    return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                }

                return Environment.GetEnvironmentVariable("ProgramFiles");
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

        public static string GetMainModuleFilepath(int processId)
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

        public static Process GetMyProcess(String processName, String pathRegex = null, String path = null)
        {
            Process[] prs = Process.GetProcesses();

            foreach (Process pr in prs)
            {
                if (pr.ProcessName == "BlitsMe.Agent" &&
                        (
                            (!String.IsNullOrWhiteSpace(pathRegex) && Regex.Match(GetMainModuleFilepath(pr.Id), pathRegex).Success) ||
                            (!String.IsNullOrWhiteSpace(path) && path.Equals(GetMainModuleFilepath(pr.Id)))
                        ) &&
                        (Environment.UserDomainName + "\\" + Environment.UserName).Equals(GetProcessOwner(pr.Id)))
                {
                    return pr;
                }
            }
            return null;
        }

        public static Process GetMyDoppleGangerProcess()
        {
            return GetMyProcess(Process.GetCurrentProcess().ProcessName, null, GetMainModuleFilepath(Process.GetCurrentProcess().Id));
        }
    }
}
