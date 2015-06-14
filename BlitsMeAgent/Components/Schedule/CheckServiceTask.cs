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
                    Logger.Info("GwupeService has recovered and is available");
                    // As a temporary fault report, we need to see if the restart worked
                    // remove this after this bug is concluded fixed
                    ThreadPool.QueueUserWorkItem(
                        state =>
                            GwupeClientAppContext.CurrentAppContext.SubmitFaultReport(new FaultReport()
                            {
                                Subject = "Service restart success",
                                UserReport = "Not a fault, it was restarted ok."
                            }));
                }
            }
            catch (Exception e)
            {
                Logger.Error("GwupeService is not available : " + e.Message);
                if (_serviceAlert == null)
                {
                    // this is the first time we have seen this.
                    _serviceAlert = new Alert.Alert
                    {
                        Message = "Gwupe Service Unavailable",
                        ClickCommand = ClickRestartGwupeService
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
                                    Subject = "Service unavailable error",
                                    UserReport = "Detected gwupe service unavailable."
                                }));
                        // Lets try restart it
                        try
                        {
                            if (!GwupeClientAppContext.CurrentAppContext.RestartGwupeService())
                            {
                                throw new Exception("Automatic restart of Gwupe failed");
                            }
                            RunTask();
                        }
                        catch (Exception)
                        {
                            // crap, we couldn't restart it.
                            ThreadPool.QueueUserWorkItem(
                                state =>
                                    GwupeClientAppContext.CurrentAppContext.SubmitFaultReport(new FaultReport()
                                    {
                                        Subject = "Auto service restart failed",
                                        UserReport = "Failed to automatically restart gwupe service."
                                    }));
                        }
                    }
                }
            }
        }

        private void ClickRestartGwupeService()
        {
            if (GwupeClientAppContext.CurrentAppContext.RestartGwupeService())
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
