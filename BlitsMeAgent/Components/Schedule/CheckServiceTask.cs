using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Gwupe.Communication.P2P.Exceptions;
using log4net;

namespace Gwupe.Agent.Components.Schedule
{
    internal class CheckServiceTask : IScheduledTask
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CheckServiceTask));
        private readonly GwupeClientAppContext _appContext;
        private Alert.Alert _serviceAlert;
        private bool SentFaultReport = false;

        internal CheckServiceTask(GwupeClientAppContext appContext)
        {
            _appContext = appContext;
            LastCompleteTime = DateTime.MinValue;
            LastExecuteTime = DateTime.MinValue;
        }

        public void RunTask()
        {
            try
            {
                _appContext.GwupeService.Ping();
                if (_serviceAlert != null)
                {
                    _appContext.NotificationManager.DeleteAlert(_serviceAlert);
                    _serviceAlert = null;
                    Logger.Info("BlitsMeService has recovered and is available");
                }
            }
            catch (Exception e)
            {
                Logger.Error("BlitsMeService is not available : " + e.Message);
                if (_serviceAlert == null)
                {
                    _serviceAlert = new Alert.Alert
                    {
                        Message = "BlitsMe Service Unavailable",
                        ClickCommand = ClickRestartBlitsMeService
                    };
                    _appContext.NotificationManager.AddAlert(_serviceAlert);
                    SentFaultReport = false;
                }
                else
                {
                    // we only want to know the second time it happens (because of upgrades)
                    if (!SentFaultReport)
                    {
                        SentFaultReport = true;
                        ThreadPool.QueueUserWorkItem(
                            state =>
                                GwupeClientAppContext.CurrentAppContext.SubmitFaultReport(new FaultReport()
                                {
                                    UserReport = "Detected blitsme service unavailable."
                                }));
                    }
                }
            }
        }

        private void ClickRestartBlitsMeService()
        {
            if (GwupeClientAppContext.CurrentAppContext.RestartBlitsMeService())
            {
                RunTask();
            }
            else
            {
                Logger.Error("User manually attempted to restart via alert, failed");
            }
        }

        public string Name { get { return "CheckService"; } }
        public int PeriodSeconds { get; set; }
        public DateTime LastExecuteTime { get; set; }
        public DateTime LastCompleteTime { get; set; }
    }
}
