using System;
using System.Windows.Controls;
using System.Windows.Threading;
using Gwupe.Agent.Components;
using Gwupe.Agent.UI.WPF.API;
using Gwupe.Agent.UI.WPF.Utils;
using log4net;

namespace Gwupe.Agent.UI.WPF
{
    public class UiHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (UiHelper));
        private readonly Dispatcher _dispatcher;
        private readonly GwupeDisablerContext _disabler;
        private readonly InputValidator _validator;
        public InputValidator Validator { get { return _validator; }}
        public GwupeDisablerContext Disabler { get { return _disabler; } }

        public UiHelper(Dispatcher dispatcher, ContentPresenter disabler, TextBlock statusText, TextBlock errorText)
        {
            _dispatcher = dispatcher;
            _disabler = new GwupeDisablerContext(dispatcher,disabler);
            _validator = new InputValidator(statusText,errorText,dispatcher);
        }

        internal void RunElevation(String disablerMessage, Action<ElevateToken> successMethod, String actionDescription)
        {
            try
            {
                _disabler.DisableInputs(true, disablerMessage);
                try
                {
                    ElevateToken token = GwupeClientAppContext.CurrentAppContext.Elevate();
                    successMethod(token);
                }
                catch(Exception ex)
                {
                    _validator.SetError("Failed to authorise " + actionDescription + ".");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to elevate privileges for " + actionDescription + " : " + ex.Message, ex);
                _validator.SetError("Failed to elevate privileges for " + actionDescription + ".");
            }
            finally
            {
                _disabler.DisableInputs(false);
            }
        }

    }
}
