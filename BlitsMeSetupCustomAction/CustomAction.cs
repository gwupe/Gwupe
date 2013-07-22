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
#if DEBUG
        public const String BuildMarker = "_Dev";
#else
        public const String BuildMarker = "";
#endif
        [CustomAction]
        public static ActionResult RequestBlitsMeAgentsRestart(Session session)
        {
            session.Log("Begin open BlitsMeAgentIfNotOpen");
            // send messages to all blitsme agents
            BlitsMe.Common.OsUtils.PostMessage((IntPtr)BlitsMe.Common.OsUtils.HWND_BROADCAST, BlitsMe.Common.OsUtils.WM_UPGRADEBM,
#if DEBUG
                            IntPtr.Zero, 
#else
                            new IntPtr(1),
#endif
                            IntPtr.Zero);
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult OpenBlitsMeAgentIfNotOpen(Session session)
        {
            session.Log("Begin open BlitsMeAgentIfNotOpen");
            Process pr = BlitsMe.Common.OsUtils.GetMyProcess("BlitsMe.Agent", null, BlitsMe.Common.OsUtils.ProgramFilesx86 + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe");
            if (pr == null)
            {
                // Now start BlitsMe
                try
                {
                    Process.Start(BlitsMe.Common.OsUtils.ProgramFilesx86 + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe");
                }
                catch (Exception e)
                {
                    session.Log("BlitsMeAgentIfNotOpen caught exception : " + e.Message + "\n" + e);
                    return ActionResult.Failure;
                }
                return ActionResult.Success;
            }
            return ActionResult.Success;

        }

        [CustomAction]
        public static ActionResult CloseBlitsMeAgents(Session session)
        {
            session.Log("Begin terminate BlitsMeAgent");
            // send messages to all blitsme agents
            BlitsMe.Common.OsUtils.PostMessage((IntPtr)BlitsMe.Common.OsUtils.HWND_BROADCAST, BlitsMe.Common.OsUtils.WM_SHUTDOWNBM,
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
                        (BlitsMe.Common.OsUtils.ProgramFilesx86 + "\\BlitsMe" + BuildMarker + "\\BlitsMe.Agent.exe")
                            .Equals(BlitsMe.Common.OsUtils.GetMainModuleFilepath(pr.Id)))
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
