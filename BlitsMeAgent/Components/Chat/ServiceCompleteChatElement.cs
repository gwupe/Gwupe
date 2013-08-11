using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using log4net;

namespace BlitsMe.Agent.Components.Chat
{
    public class ServiceCompleteChatElement : ChatElement
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (ServiceCompleteChatElement));

        public ServiceCompleteChatElement()
        {
        }

        public void SetRating(String ratingName, int rating)
        {
            Logger.Debug("Rating " + ratingName + " set to " + rating);
        }

        public new String ChatType
        {
            get { return "ChatServiceComplete"; }
        }
    }


}
