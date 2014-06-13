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
            private set { _answered = value; OnPropertyChanged("Answered"); }
        }

        public bool Answer
        {
            get { return _answer; }
            set
            {
                if (!Answered)
                {
                    _answer = value;
                    OnPropertyChanged("Answer");
                    Answered = true;
                    if (_answer)
                    {
                        OnAnswerTrue(EventArgs.Empty);
                    }
                    else
                    {
                        OnAnswerFalse(EventArgs.Empty);
                    }
                }
            }
        }

        public event EventHandler AnsweredTrue;

        private void OnAnswerTrue(EventArgs e)
        {
            EventHandler handler = AnsweredTrue;
            if (handler != null) handler(this, e);
        }

        public event EventHandler AnsweredFalse;

        private void OnAnswerFalse(EventArgs e)
        {
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
