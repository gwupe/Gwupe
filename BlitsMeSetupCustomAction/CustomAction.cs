using System;
using System.Diagnostics;
using Microsoft.Deployment.WindowsInstaller;

namespace GwupeSetupCustomAction
{
    public class CustomActions
    {
#if DEBUG
        public const String BuildMarker = "_Dev";
#else
        public const String BuildMarker = "";
#endif
        [CustomAction]
        public static ActionResult RequestGwupeRestart(Session session)
        {
            session.Log("Begin open GwupeRestart");
            // send messages to all blitsme agents
            OsUtils.PostMessage((IntPtr)OsUtils.HWND_BROADCAST, OsUtils.WM_UPGRADEBM,
#if DEBUG
                            IntPtr.Zero, 
#else
                            new IntPtr(1),
#endif
                            IntPtr.Zero);
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult OpenGwupeIfNotOpen(Session session)
        {
            session.Log("Begin open GwupeIfNotOpen");
            Process pr = OsUtils.GetMyProcess("BlitsMe.Agent", null, OsUtils.ProgramFilesx86 + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe");
            Process pr2 = OsUtils.GetMyProcess("Gwupe", null, OsUtils.ProgramFilesx86 + "\\Gwupe" + BuildMarker + "\\Gwupe.Agent.exe");
            if (pr == null && pr2 == null)
            {
                // Now start Gwupe
                try
                {
                    Process.Start(OsUtils.ProgramFilesx86 + "\\Gwupe" + BuildMarker + "\\Gwupe.Agent.exe");
                }
                catch (Exception e)
                {
                    session.Log("GwupeIfNotOpen caught exception : " + e.Message + "\n" + e);
                    return ActionResult.Failure;
                }
                return ActionResult.Success;
            }
            return ActionResult.Success;

        }

        [CustomAction]
        public static ActionResult CloseGwupe(Session session)
        {
            session.Log("Begin terminate Gwupe");
            // send messages to all blitsme agents
            OsUtils.PostMessage((IntPtr)OsUtils.HWND_BROADCAST, OsUtils.WM_SHUTDOWNBM,
#if DEBUG
                            IntPtr.Zero, 
#else
                            new IntPtr(1),
#endif
                            IntPtr.Zero);

            // now we kill if necessary
            Process[] prs = Process.GetProcesses();
            foreach (Process pr in prs)
            {
                try
                {
                    if (pr.ProcessName == "BlitsMe.Agent" &&
                        (OsUtils.ProgramFilesx86 + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe")
                            .Equals(OsUtils.GetMainModuleFilepath(pr.Id)))
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
                                session.Log("CloseBlitsMeAgent caught exception : " + e.Message + "\n" + e);
                            }
                        }
                    }
                    if (pr.ProcessName == "Gwupe" &&
                        (OsUtils.ProgramFilesx86 + "\\Gwupe" + BuildMarker + "\\Gwupe.Agent.exe")
                            .Equals(OsUtils.GetMainModuleFilepath(pr.Id)))
                    {
                        session.Log("Gwupe is still alive and kicking so we gonna kill 'im after 5 seconds");
                        if (!pr.WaitForExit(5000))
                        {
                            session.Log("Gwupe: times up");
                            try
                            {
                                pr.Kill();
                            }
                            catch (Exception e)
                            {
                                session.Log("CloseGwupe caught exception : " + e.Message + "\n" + e);
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


    }
}
