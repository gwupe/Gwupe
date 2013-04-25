using System;

namespace BlitsMe.Agent.Components.Schedule
{
    internal delegate RunTask RunTask();

    internal interface IScheduledTask
    {
        String Name { get; }
        int PeriodSeconds { get; }
        void RunTask();
        DateTime LastExecuteTime { get; set; }
        DateTime LastCompleteTime { get; set; }
    }
}
