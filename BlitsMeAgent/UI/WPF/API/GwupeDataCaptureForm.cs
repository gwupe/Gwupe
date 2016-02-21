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
using Gwupe.Agent.Exceptions;

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
        public Exception SubmissionError { get; protected set; }

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

        protected virtual void OnCommitFail(Exception args)
        {
            EventHandler<DataSubmitErrorArgs> handler = CommitFail;
            handler?.Invoke(this, new DataSubmitErrorArgs() { SubmitErrors = args is DataSubmissionException ? ((DataSubmissionException)args).SubmitErrors : null, Exception = args });
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

        protected void ResetUserInput(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                ResetStatus();
                ResetInputs();
            });
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
                    try
                    {
                        SubmissionError = null;
                        CommitInput();
                        CommitSuccessful();
                        OnCommitSucceed();
                    }
                    catch (DataSubmissionException ex)
                    {
                        SubmissionError = ex;
                    }
                    catch (ElevationException ex)
                    {
                        SubmissionError = ex;
                    }
                    catch (Exception ex)
                    {
                        SubmissionError = ex;
                    }
                    finally
                    {
                        if (SubmissionError != null)
                        {
                            CommitFailed(SubmissionError);
                            OnCommitFail(SubmissionError);
                        }
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

        /// <summary>
        /// Is called after submission to validate the input
        /// </summary>
        /// <returns>true if the input is valid</returns>
        protected abstract bool ValidateInput();

        /// <summary>
        /// Called to reset the inputs to their pre-edited state
        /// </summary>
        protected abstract void ResetInputs();

        /// <summary>
        /// Called to commit the input
        /// </summary>
        /// <returns>true if the commit was successful</returns>
        protected abstract void CommitInput();

        /// <summary>
        /// Called if the commit failed.
        /// </summary>
        /// <param name="submissionError"></param>
        protected abstract void CommitFailed(Exception submissionError);

        /// <summary>
        /// Called if the commit was successful
        /// </summary>
        protected abstract void CommitSuccessful();

        /// <summary>
        /// Called to remove all the status' markings of pre submission successes and failures.
        /// </summary>
        protected abstract void ResetStatus();
    }
}
