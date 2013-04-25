using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace BlitsMe.Agent.Components.Schedule
{
    internal class CheckServiceTask : IScheduledTask
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (CheckServiceTask));
        private readonly BlitsMeClientAppContext _appContext;
        private Alert.Alert _serviceAlert;

        internal CheckServiceTask(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
            LastCompleteTime = DateTime.MinValue;
            LastExecuteTime = DateTime.MinValue;
        }

        public void RunTask()
        {
            try
            {
                _appContext.BlitsMeService.Ping();
                if (_serviceAlert != null)
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
                    _serviceAlert = new Alert.Alert() { Message = "BlitsMe Service Unavailable" };
                    _appContext.NotificationManager.AddAlert(_serviceAlert);
                    Logger.Error("BlitsMeService is not available : " + e.Message);
                }
            }
        }

        public string Name { get { return "CheckService"; } }
        public int PeriodSeconds { get; set; }
        public DateTime LastExecuteTime { get; set; }
        public DateTime LastCompleteTime { get; set; }
    }
}
