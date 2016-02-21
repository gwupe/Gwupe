using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using Gwupe.Agent.Components.Alert;
using Gwupe.Agent.Components.Schedule;
using log4net;
using Timer = System.Timers.Timer;

namespace Gwupe.Agent.Managers
{
    class ScheduleManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ScheduleManager));
        private readonly Timer _schedulerTimer;
        private const int Interval = 5000;
        private readonly List<IScheduledTask> _tasks;
        internal bool IsClosed { get; private set; }

        public ScheduleManager()
        {
            _schedulerTimer = new Timer(Interval);
            _tasks = new List<IScheduledTask>();
        }

        public void AddTask(IScheduledTask task)
        {
            task.LastExecuteTime = DateTime.Now;
            task.LastCompleteTime = DateTime.Now;
            lock (_tasks)
            {
                if (_tasks.Contains(task))
                {
                    Logger.Error("Cannot add task " + task + ", it is already there");
                }
                else
                {
                    _tasks.Add(task);
                }
            }
        }

        public void Start()
        {
            _schedulerTimer.Elapsed += RunTasks;
            _schedulerTimer.Enabled = true;
        }

        private void RunTasks(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            lock (_tasks)
            {
                foreach (var scheduledTask in _tasks)
                {
                    // make sure its not running and make sure it is due to run
                    if ((scheduledTask.LastExecuteTime.Ticks <= scheduledTask.LastCompleteTime.Ticks)
                        && (scheduledTask.LastExecuteTime.Ticks + (TimeSpan.FromSeconds(scheduledTask.PeriodSeconds).Ticks) < DateTime.Now.Ticks))
                    {
                        scheduledTask.LastExecuteTime = DateTime.Now;
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            try
                            {
                                //Logger.Debug("Running " + scheduledTask.Name);
                                scheduledTask.RunTask();
                                //Logger.Debug("Completed " + scheduledTask.Name);
                            }
                            catch (Exception e)
                            {
                                Logger.Error("Scheduled Task [" + scheduledTask.Name + "] failed : " + e.Message, e);
                            }
                            finally
                            {
                                scheduledTask.LastCompleteTime = DateTime.Now;
                            }
                        });

                    }
                }
            }
        }

        public void Close()
        {
            if (!IsClosed)
            {
                Logger.Debug("Closing ScheduleManager");
                IsClosed = true;
                if (_schedulerTimer != null && _schedulerTimer.Enabled)
                {
                    _schedulerTimer.Stop();
                }
            }
        }

    }

}
