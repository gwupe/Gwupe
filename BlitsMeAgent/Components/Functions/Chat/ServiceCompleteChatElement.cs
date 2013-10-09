using System;
using log4net;

namespace BlitsMe.Agent.Components.Functions.Chat
{
    public class ServiceCompleteChatElement : ChatElement
    {
        private readonly Engagement _engagement;
        private readonly String _interactionId;
        private static readonly ILog Logger = LogManager.GetLogger(typeof (ServiceCompleteChatElement));

        internal ServiceCompleteChatElement(Engagement engagement)
        {
            _engagement = engagement;
            _interactionId = engagement.Interactions.CurrentOrNewInteraction.Id;
        }

        public void SetRating(String ratingName, int rating)
        {
            _engagement.SetRating(_interactionId, ratingName, rating);
        }



        public new String ChatType
        {
            get { return "ChatServiceComplete"; }
        }
    }


}
