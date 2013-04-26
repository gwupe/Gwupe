using System;
using System.Runtime.InteropServices;
using log4net;

namespace BlitsMe.Agent.Components.Schedule
{
    class DetectIdleTask : IScheduledTask
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DetectIdleTask));
        private readonly BlitsMeClientAppContext _appContext;

        public DetectIdleTask(BlitsMeClientAppContext appContext)
        {
            _appContext = appContext;
            LastCompleteTime = DateTime.MinValue;
            LastExecuteTime = DateTime.MinValue;
        }

        internal const int TimeToIdle = 600000; // 10 minutes
        internal const int TimeToExtendedIdle = 3600000; // 60 minutes

        public string Name { get { return "CheckIdle"; } }
        public int PeriodSeconds { get { return 1; } }
        public void RunTask()
        {
            var info = new LastInputInfo();
            info.cbSize = Marshal.SizeOf(info);
            GetLastInputInfo(ref info);
            uint tick = GetTickCount();
            uint msec = tick - info.dwTime;
            //Logger.Debug("Last input was " + (msec / 1000) + " secs ago");
            if (_appContext.IdleState != IdleState.ExtendedIdle && msec > TimeToExtendedIdle)
            {
                Logger.Debug("System is no longer " + _appContext.IdleState + ", now " + IdleState.ExtendedIdle);
                _appContext.IdleState = IdleState.ExtendedIdle;
            }
            else if (_appContext.IdleState == IdleState.InUse && msec > TimeToIdle)
            {
                Logger.Debug("System is no longer " + _appContext.IdleState + ", now " + IdleState.Idle);
                _appContext.IdleState = IdleState.Idle;
            } else if(_appContext.IdleState != IdleState.InUse && msec < TimeToIdle)
            {
                Logger.Debug("System is no longer " + _appContext.IdleState + ", now " + IdleState.InUse);
                _appContext.IdleState = IdleState.InUse;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public int cbSize;
            public readonly uint dwTime;
        }
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LastInputInfo info);
        [DllImport("kernel32.dll")]
        private static extern uint GetTickCount();

        public DateTime LastExecuteTime { get; set; }
        public DateTime LastCompleteTime { get; set; }
    }
}
