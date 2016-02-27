using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Gwupe.Agent.Components.Functions.API;

namespace Gwupe.Agent.Components.Functions.Chat.ChatElement
{
    public abstract class TrueFalseChatElement : BaseChatElement, INotifyPropertyChanged
    {
        private bool _isAnswered;
        public TrueFalseCommandHandler AnswerHandler { get; private set; }

        protected TrueFalseChatElement()
        {
            AnswerHandler = new TrueFalseCommandHandler();
            AnswerHandler.Answered += (sender, args) => IsAnswered = true;
        }

        public bool IsAnswered
        {
            set { _isAnswered = value; OnPropertyChanged(nameof(IsAnswered)); }
            get { return _isAnswered; }
        }
    }
}
