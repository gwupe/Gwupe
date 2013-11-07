using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using BlitsMe.Agent.Components.Functions.Chat.ChatElement;

namespace BlitsMe.Agent.Components.Functions.Chat
{
    class AnswerTrueFalseCommand : ICommand
    {
        private readonly TrueFalseChatElement _chatElement;

        internal AnswerTrueFalseCommand(TrueFalseChatElement chatElement)
        {
            _chatElement = chatElement;
        }

        public void Execute(object parameter)
        {
            bool accept = (bool)parameter;
            Thread execThread;
            if (accept)
            {
                execThread = new Thread(() => _chatElement.OnAnswerTrue(EventArgs.Empty));
            }
            else
            {
                execThread = new Thread(() => _chatElement.OnAnswerFalse(EventArgs.Empty));
            }
            execThread.IsBackground = true;
            execThread.Start();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
