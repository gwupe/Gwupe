using System;
using log4net;

namespace BlitsMe.Agent.Components.Functions.RemoteDesktop.ChatElement
{
    public class ServiceCompleteChatElement : Chat.ChatElement.BaseChatElement
    {
        public override string Speaker { get { return "_SERVICE_COMPLETE"; } set { } }
        private readonly Engagement _engagement;
        private readonly String _interactionId;
        private static readonly ILog Logger = LogManager.GetLogger(typeof (ServiceCompleteChatElement));
        public byte[] Avatar { get { return _engagement.SecondParty.Person.Avatar; }  }

        internal ServiceCompleteChatElement(Engagement engagement)
        {
            _engagement = engagement;
            _interactionId = engagement.Interactions.CurrentOrNewInteraction.Id;
        }

        public void SetRating(String ratingName, int rating)
        {
            _engagement.SetRating(_interactionId, ratingName, rating);
        }
    }


}
