using System;
using System.Threading;
using System.Windows.Input;

namespace Gwupe.Agent.Components.Functions.API
{
    class AnswerTrueFalseCommand : ICommand
    {
        public String Message { get; set; }
        private readonly TrueFalseCommandHandler _commandHandler;

        internal AnswerTrueFalseCommand(TrueFalseCommandHandler commandHandler)
        {
            _commandHandler = commandHandler;
        }

        public void Execute(object parameter)
        {
            ThreadPool.QueueUserWorkItem(state => _commandHandler.Answer = (bool)parameter);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}