using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gwupe.Agent.Components.Processors;
using Gwupe.Cloud.Messaging.API;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;
using log4net;

namespace Gwupe.Agent.Components.Functions.Chat.Processors
{
    internal class ChatMessageProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChatMessageProcessor));

        internal ChatMessageProcessor(GwupeClientAppContext appContext)
            : base(appContext)
        {
        }

        internal override UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest req)
        {
            ChatMessageRq request = (ChatMessageRq)req;
            ChatMessageRs response = new ChatMessageRs();
            try
            {
                ((Function)engagement.Functions["Chat"]).ReceiveChatMessage(request.message, request.chatId, request.interactionId, request.shortCode,request.username);
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
