using System;
using log4net;

namespace BlitsMe.Agent.Components.Functions.Chat
{
    public class ServiceCompleteChatElement : ChatElement
    {
        private readonly Engagement _engagement;
        private readonly String _sessionId;
        private static readonly ILog Logger = LogManager.GetLogger(typeof (ServiceCompleteChatElement));

        internal ServiceCompleteChatElement(Engagement engagement)
        {
            _engagement = engagement;
            _sessionId = engagement.Interactions.CurrentOrNewInteraction.Id;
        }

        public void SetRating(String ratingName, int rating)
        {
            _engagement.SetRating(_sessionId, ratingName, rating);
        }



        public new String ChatType
        {
            get { return "ChatServiceComplete"; }
        }
    }


}
