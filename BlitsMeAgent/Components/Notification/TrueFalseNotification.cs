using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Gwupe.Agent.Components.Functions.API;

namespace Gwupe.Agent.Components.Notification
{
    internal class TrueFalseNotification : Notification
    {
        public TrueFalseCommandHandler AnswerHandler { get; private set; }

        public TrueFalseNotification()
        {
            AnswerHandler = new TrueFalseCommandHandler();
            AnswerHandler.Answered += (sender, args) => this.Manager.DeleteNotification(this);
        }
    }
}
