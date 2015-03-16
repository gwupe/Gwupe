using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Gwupe.Agent.UI.WPF.API
{
    public abstract class GwupeModalUserControl : UserControl
    {
        private AutoResetEvent _userCompleted = new AutoResetEvent(false);
        public bool Cancelled = false;
        protected UiHelper UiHelper { get { return _uiHelper; } }
        private UiHelper _uiHelper;
        private string _processingWord = "Processing";
        public Control StartWithFocus;

        public String ProcessingWord
        {
            get { return _processingWord; }
            set { _processingWord = value; }
        }

        protected void InitGwupeModalUserControl(ContentPresenter disablerContentPresenter, TextBlock statusTextBlock, TextBlock errorTextBlock)
        {
            _uiHelper = new UiHelper(Dispatcher, disablerContentPresenter, null, errorTextBlock);
        }


        protected void CancelUserInput(object sender, RoutedEventArgs e)
        {
            _uiHelper.Validator.ResetStatus();
            Cancelled = true;
            _userCompleted.Set();
        }

        public bool PresentModal(int timeoutInSeconds = 0)
        {
            Reset();
            Dispatcher.Invoke(new Action(Show));
            if (StartWithFocus != null)
            {
                // can't focus immediately because it might not be setup correctly
                ThreadPool.QueueUserWorkItem(state =>
                {
                    Thread.Sleep(10);
                    Dispatcher.Invoke(new Action(() => StartWithFocus.Focus()));
                });
            }
            _userCompleted.Reset();
            var returnValue = false;
            while (WaitForUser(timeoutInSeconds) && !Cancelled)
            {
                if (CommitInput())
                {
                    returnValue = true;
                    break;
                }
            }
            Dispatcher.Invoke(new Action(Hide));
            return returnValue;
        }

        private bool WaitForUser(int timeoutInSeconds)
        {
            return (timeoutInSeconds > 0 ? _userCompleted.WaitOne(timeoutInSeconds) : _userCompleted.WaitOne());
        }

        protected void ProcessUserInput(object sender, RoutedEventArgs e)
        {
            UiHelper.Disabler.DisableInputs(true, ProcessingWord);
            if (ValidateInput())
            {
                Cancelled = false;
                _userCompleted.Set();
            }
            else
            {
                UiHelper.Disabler.DisableInputs(false);
            }
        }

        protected void ProcessUserInputOnEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ProcessUserInput(sender, e);
            }
        }

        public void Reset()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(Reset));
                return;
            }
            Cancelled = false;
            UiHelper.Disabler.DisableInputs(false);
            ResetInputs();
        }

        protected abstract bool ValidateInput();

        protected abstract void Show();

        protected abstract void Hide();

        protected abstract void ResetInputs();

        protected abstract bool CommitInput();

    }
}
