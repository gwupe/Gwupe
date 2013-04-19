using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;
using BlitsMe.Agent.Components.Alert;
using log4net;

namespace BlitsMe.Agent.Managers
{
    class MaintenanceManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (MaintenanceManager));
        private readonly BlitsMeClientAppContext _appContext;
        private readonly Timer _maintenanceTimer;
        private const int Interval = 60000;
        private Alert _serviceAlert;

        public MaintenanceManager(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
            _maintenanceTimer = new Timer(Interval);
        }

        public void Start()
        {
            _maintenanceTimer.Elapsed += UpgradeCheckTimerOnElapsed;
            _maintenanceTimer.Elapsed += BlitsMeServiceCheck;
            _maintenanceTimer.Enabled = true;
        }

        private void BlitsMeServiceCheck(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Logger.Debug("Checking if service is available");
            try
            {
                _appContext.BlitsMeService.Ping();
                if(_serviceAlert != null)
                {
                    _appContext.NotificationManager.DeleteAlert(_serviceAlert);
                    _serviceAlert = null;
                    Logger.Info("BlitsMeService is available");
                }
            }
            catch (Exception e)
            {
                if (_serviceAlert == null)
                {
                    _serviceAlert = new Alert() {Message = "BlitsMe Service Unavailable"};
                    _appContext.NotificationManager.AddAlert(_serviceAlert);
                    Logger.Error("BlitsMeService is not available : " + e.Message);
                }
            }
        }

        private void UpgradeCheckTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                var regVersion = _appContext.Reg.getRegValue("Version", true);
                Logger.Debug("Checking for agent upgrade " + _appContext.StartupVersion + " vs " + regVersion);
                if (new Version(regVersion).CompareTo(new Version(_appContext.StartupVersion)) != 0)
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
    }
}
