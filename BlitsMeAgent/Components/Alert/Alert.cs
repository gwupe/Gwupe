using System;
using System.Windows;
using System.Windows.Input;
using Gwupe.Agent.Managers;

namespace Gwupe.Agent.Components.Alert
{
    internal class Alert
    {
        public NotificationManager Manager { get; set; }
        public string Message { get; set; }
        public ICommand Command { get { return _command; } }
        private ActionCommand _command;

        public Action ClickCommand
        {
            get { return _command != null ? _command.Action : null; }
            set
            {
                if (_command == null)
                {
                    _command = new ActionCommand();
                }
                _command.Action = value;
            }
        }

        public override string ToString()
        {
            return "[Alert: " + Message + " ]";
        }
    }

    public class ActionCommand : ICommand
    {
        public Action Action;

        public void Execute(object parameter)
        {
            if (Action != null)
            {
                Action();
            }
        }

        public bool CanExecute(object parameter)
        {
            return Action != null;
        }

        public event EventHandler CanExecuteChanged;
    }
}