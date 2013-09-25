using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlitsMe.Agent.Components.Processors;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Functions.Chat.Processors
{
    internal class ChatMessageProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChatMessageProcessor));

        internal ChatMessageProcessor(BlitsMeClientAppContext appContext)
            : base(appContext)
        {
        }

        internal override UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest req)
        {
            ChatMessageRq request = (ChatMessageRq)req;
            ChatMessageRs response = new ChatMessageRs();
            try
            {
                ((Function)engagement.Functions["Chat"]).ReceiveChatMessage(request.message, request.chatId, request.interactionId, request.shortCode);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process chat message : " + e.Message,e);
                response.error = "UNKNOWN_ERROR";
                response.errorMessage = "Failed to process Chat request";
            }
            return response;
        }
    }
}
