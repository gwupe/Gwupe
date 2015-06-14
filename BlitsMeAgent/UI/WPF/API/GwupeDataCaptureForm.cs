using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Gwupe.Agent.Components;

namespace Gwupe.Agent.UI.WPF.API
{
    public abstract class GwupeDataCaptureForm : UserControl
    {
        private AutoResetEvent _userCompleted = new AutoResetEvent(false);
        public bool Cancelled = false;
        protected UiHelper UiHelper { get { return _uiHelper; } }
        private UiHelper _uiHelper;
        private string _processingWord = "Processing";
        public Control StartWithFocus;
        public event EventHandler CommitCancelled;
        public DataSubmitErrorArgs SubmissionErrors { get; protected set; }

        protected virtual void OnCommitCancelled()
        {
            EventHandler handler = CommitCancelled;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public event EventHandler CommitSucceed;

        protected virtual void OnCommitSucceed()
        {
            EventHandler handler = CommitSucceed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public event EventHandler<DataSubmitErrorArgs> CommitFail;

        protected virtual void OnCommitFail(DataSubmitErrorArgs args)
        {
            EventHandler<DataSubmitErrorArgs> handler = CommitFail;
            if (handler != null) handler(this, args);
        }

        public String ProcessingWord
        {
            get { return _processingWord; }
            set { _processingWord = value; }
        }

        protected void InitGwupeDataCaptureForm(ContentPresenter disablerContentPresenter, TextBlock statusTextBlock, TextBlock errorTextBlock)
        {
            _uiHelper = new UiHelper(Dispatcher, disablerContentPresenter, null, errorTextBlock);
        }

        protected void CancelUserInput(object sender, RoutedEventArgs e)
        {
            _uiHelper.Validator.ResetStatus();
            Cancelled = true;
            _userCompleted.Set();
            OnCommitCancelled();
        }

        protected void ProcessUserInput(object sender, RoutedEventArgs e)
        {
            ResetStatus();
            UiHelper.Disabler.DisableInputs(true, ProcessingWord);
            if (ValidateInput())
            {
                Cancelled = false;
                ThreadPool.QueueUserWorkItem(state =>
                {
                    if (CommitInput())
                    {
                        CommitSuccessful();
                        OnCommitSucceed();
                        ResetInputs();
                    }
                    else
                    {
                        CommitFailed();
                        OnCommitFail(SubmissionErrors);
                    }
                    Dispatcher.Invoke(new Action(() => UiHelper.Disabler.DisableInputs(false)));

                });
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
            ResetStatus();
        }

        protected abstract bool ValidateInput();

        protected abstract void ResetInputs();

        protected abstract bool CommitInput();

        protected abstract void CommitFailed();

        protected abstract void CommitSuccessful();

        protected abstract void ResetStatus();
    }
}
