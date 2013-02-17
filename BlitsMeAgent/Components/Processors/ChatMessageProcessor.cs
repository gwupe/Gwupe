using System;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Processors
{
    internal class ChatMessageProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChatMessageProcessor));
        private readonly BlitsMeClientAppContext _appContext;

        internal ChatMessageProcessor(BlitsMeClientAppContext appContext) : base(appContext)
        {
            _appContext = appContext;
        }

        internal override UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest req)
        {
            ChatMessageRq request = (ChatMessageRq) req;
            ChatMessageRs response = new ChatMessageRs();
            try
            {
                engagement.Chat.ReceiveChatMessage(request.message, request.chatId, request.shortCode);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process chat message : " + e.Message);
                response.error = "UNKNOWN_ERROR";
                response.errorMessage = "Failed to process Chat request";
            }
            return response;
        }
    }
}