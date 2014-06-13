using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;
using BlitsMe.Agent.UI.WPF.API;
using BlitsMe.Agent.UI.WPF.Utils;
using log4net;

namespace BlitsMe.Agent.UI.WPF
{
    public class UiHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (UiHelper));
        private readonly Dispatcher _dispatcher;
        private readonly BlitsMeDisablerContext _disabler;
        private readonly InputValidator _validator;
        public InputValidator Validator { get { return _validator; }}
        public BlitsMeDisablerContext Disabler { get { return _disabler; } }

        public UiHelper(Dispatcher dispatcher, ContentPresenter disabler, TextBlock statusText, TextBlock errorText)
        {
            _dispatcher = dispatcher;
            _disabler = new BlitsMeDisablerContext(dispatcher,disabler);
            _validator = new InputValidator(statusText,errorText,dispatcher);
        }

        public void RunElevation(String disablerMessage, Action<String, String> successMethod, String actionDescription)
        {
            try
            {
                _disabler.DisableInputs(true, disablerMessage);
                String tokenId;
                String securityKey;
                if (BlitsMeClientAppContext.CurrentAppContext.Elevate(out tokenId, out securityKey))
                {
                    successMethod(tokenId, securityKey);
                }
                else
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
