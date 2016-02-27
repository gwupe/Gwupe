using System;
using System.Timers;

namespace Gwupe.Agent.Components.Functions.RemoteDesktop.ChatElement
{
    public class RdpRequestUnattendedChatElement : RdpRequestChatElement
    {
        public override string Speaker { get { return "_UNATTENDED_RDP_REQUEST"; } set { } }

        private readonly int _timeToOverride;
        private int _timeout;
        private readonly int _startTime;
        private readonly Timer _timer;

        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; OnPropertyChanged("Timeout"); }
        }

        public RdpRequestUnattendedChatElement(int timeToOverride)
        {
            AnswerHandler.Answered += (sender, args) => _timer.Stop();
            _timeToOverride = timeToOverride * 1000;
            Timeout = timeToOverride;
            _startTime = Environment.TickCount;
            _timer = new Timer {AutoReset = true, Interval = 1000};
            _timer.Elapsed += OnCountdown;
            _timer.Start();
        }

        private void OnCountdown(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var timed = Environment.TickCount - _startTime;
            if (timed > (_timeToOverride))
            {
                Timeout = 0;
                AnswerHandler.Answer = true;
            }
            else
            {
                Timeout = ((_timeToOverride) - timed)/1000;
            }

        }
    }
}