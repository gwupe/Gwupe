using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Gwupe.Agent.Annotations;

namespace Gwupe.Agent.UI.WPF.API
{
    interface IGwupeUserControl
    {
    }

    public class GwupeDisablerContext : INotifyPropertyChanged
    {
        private readonly Dispatcher _dispatcher;
        private readonly ContentPresenter _disabler;

        private string _disableMessage;

        public string DisableMessage
        {
            get { return _disableMessage; }
            private set { _disableMessage = value; OnPropertyChanged("DisableMessage"); }
        }

        public GwupeDisablerContext(Dispatcher dispatcher, ContentPresenter disabler)
        {
            _dispatcher = dispatcher;
            _disabler = disabler;
            _disabler.Content = this;
        }

        public void DisableInputs(bool disabled, String message = null)
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.Invoke(new Action(() => DisableInputs(disabled, message)));
                return;
            }
            if (!String.IsNullOrEmpty(message))
            {
                DisableMessage = message;
            }
            _disabler.Visibility = disabled ? Visibility.Visible : Visibility.Hidden;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}