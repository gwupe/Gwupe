using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Deployment.WindowsInstaller;

namespace BlitsMeSetupCustomAction
{
    public class CustomActions
    {
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
            foreach(Process pr in prs)
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
                        catch(Exception e)
                        {
                            session.Log("Exception caught: Failed to kill process");    
                        }
                    }
                }
            }

            return ActionResult.Success;
        }
    }
}
