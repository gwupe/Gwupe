using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;

namespace BlitsMe.Agent.Components.Schedule
{
    class CheckUpgradeTask : IScheduledTask
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (CheckUpgradeTask));
        private readonly BlitsMeClientAppContext _appContext;

        public CheckUpgradeTask(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
            LastCompleteTime = DateTime.MinValue;
            LastExecuteTime = DateTime.MinValue;
        }

        public string Name { get { return "CheckUpgradeTask"; } }
        public int PeriodSeconds { get; set; }


        public void RunTask()
        {
            try
            {
                var regVersion = _appContext.Reg.getRegValue("Version", true);
                //Logger.Debug("Checking for agent upgrade " + _appContext.StartupVersion + " vs " + regVersion);
                if (new Version(regVersion).CompareTo(new Version(_appContext.StartupVersion)) > 0)
                {
                    Logger.Info("My file version has changed " + _appContext.StartupVersion + " => " + regVersion +
                                ", closing to re-open as new version.");
                    try
                    {
                        Process.Start(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                      "\\BlitsMe.Agent.Upgrade.exe");
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to start the upgrade exe, but will stop myself anyway.");
                    }
                    _appContext.Shutdown();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to check version and act : " + e.Message, e);
            }
        }

        public DateTime LastExecuteTime { get; set; }
        public DateTime LastCompleteTime { get; set; }
    }
}
