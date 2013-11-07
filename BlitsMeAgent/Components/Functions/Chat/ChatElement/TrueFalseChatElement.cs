using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace BlitsMe.Agent.Components.Functions.Chat.ChatElement
{
    public abstract class TrueFalseChatElement : ChatElement
    {
        public bool Answered
        {
            get { return _answered; }
            set { _answered = value; OnPropertyChanged("Answered"); }
        }

        public bool Answer
        {
            get { return _answer; }
            set { _answer = value; OnPropertyChanged("Answer"); }
        }

        public event EventHandler AnsweredTrue;

        public void OnAnswerTrue(EventArgs e)
        {
            Answered = true;
            Answer = true;
            EventHandler handler = AnsweredTrue;
            if (handler != null) handler(this, e);
        }

        public event EventHandler AnsweredFalse;

        public void OnAnswerFalse(EventArgs e)
        {
            Answered = true;
            Answer = false;
            EventHandler handler = AnsweredFalse;
            if (handler != null) handler(this, e);
        }

        private ICommand _answerTrueFalseCommand;
        private bool _answer;
        private bool _answered;

        public ICommand AnswerTrueFalse
        {
            get { return _answerTrueFalseCommand ?? (_answerTrueFalseCommand = new AnswerTrueFalseCommand(this)); }
        }
    }
}
