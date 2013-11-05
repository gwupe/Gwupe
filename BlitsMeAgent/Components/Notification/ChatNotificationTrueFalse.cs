using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using BlitsMe.Agent.Components.Functions.Chat;
using BlitsMe.Agent.Managers;

namespace BlitsMe.Agent.Components.Notification
{
    class ChatNotificationTrueFalse : ChatElement
    {
        public event EventHandler AnsweredTrue;

        public void OnAnswerTrue(EventArgs e)
        {
            EventHandler handler = AnsweredTrue;
            if (handler != null) handler(this, e);
        }

        public event EventHandler AnsweredFalse;

        public void OnAnswerFalse(EventArgs e)
        {
            EventHandler handler = AnsweredFalse;
            if (handler != null) handler(this, e);
        }

        private ICommand _answerTrueFalseCommand;
        public ICommand AnswerChatNotificationTrueFalse
        {
            get { return _answerTrueFalseCommand ?? (_answerTrueFalseCommand = new AnswerChatNotificationTrueFalse(this.Manager, this)); }
        }
    }
}
