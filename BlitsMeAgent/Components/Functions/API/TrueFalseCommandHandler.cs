using System;
using System.ComponentModel;
using System.Windows.Input;
using Gwupe.Agent.Annotations;
using Gwupe.Agent.Components.Functions.Chat;

namespace Gwupe.Agent.Components.Functions.API
{
    public class TrueFalseCommandHandler : INotifyPropertyChanged
    {
        public bool IsAnswered
        {
            get { return _answered; }
            private set { _answered = value; OnPropertyChanged(nameof(IsAnswered)); }
        }

        public bool Answer
        {
            get { return _answer; }
            set
            {
                if (!IsAnswered)
                {
                    _answer = value;
                    OnPropertyChanged(nameof(IsAnswered));
                    if (_answer)
                    {
                        OnAnswerTrue(EventArgs.Empty);
                    }
                    else
                    {
                        OnAnswerFalse(EventArgs.Empty);
                    }
                    OnAnswered();
                }
            }
        }

        public event EventHandler AnsweredTrue;

        private void OnAnswerTrue(EventArgs e)
        {
            EventHandler handler = AnsweredTrue;
            handler?.Invoke(this, e);
        }

        public event EventHandler AnsweredFalse;

        private void OnAnswerFalse(EventArgs e)
        {
            EventHandler handler = AnsweredFalse;
            handler?.Invoke(this, e);
        }

        private ICommand _answerTrueFalseCommand;
        private bool _answer;
        private bool _answered;

        public ICommand AnswerTrueFalse => _answerTrueFalseCommand ?? (_answerTrueFalseCommand = new AnswerTrueFalseCommand(this));

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler Answered;

        protected virtual void OnAnswered()
        {
            Answered?.Invoke(this, EventArgs.Empty);
        }
    }
}