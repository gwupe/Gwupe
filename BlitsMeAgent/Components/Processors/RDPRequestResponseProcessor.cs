using System;
using System.Diagnostics;
using BlitsMe.Agent.Components.Notification;
using BlitsMe.Agent.Components.RDP;
using BlitsMe.Cloud.Messaging.API;
using BlitsMe.Cloud.Messaging.Request;
using BlitsMe.Cloud.Messaging.Response;
using log4net;

namespace BlitsMe.Agent.Components.Processors
{
    internal class RDPRequestResponseProcessor : UserToUserProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RDPRequestResponseProcessor));
        private readonly BlitsMeClientAppContext _appContext;

        internal RDPRequestResponseProcessor(BlitsMeClientAppContext appContext)
            : base(appContext)
        {
            _appContext = appContext;
        }

        internal override UserToUserResponse ProcessWithEngagement(Engagement engagement, UserToUserRequest req)
        {
            RDPRequestResponseRq request = (RDPRequestResponseRq)req;
            RDPRequestResponseRs response = new RDPRequestResponseRs();
            try
            {
                if (request.accepted)
                {
                    engagement.Chat.LogSystemMessage(engagement.SecondParty.Name + " accepted your remote assistance request.");
                    try
                    {
                        int port = engagement.Client.Start();
                        Process.Start("c:\\Program Files\\TightVNC\\tvnviewer.exe", "127.0.0.1:" + port);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to start RDP client : " + e.Message, e);
                        throw e;
                    }
                }
                else
                {
                    engagement.Chat.LogSystemMessage(engagement.SecondParty.Name + " did not accept your remote assistance request.");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to process incoming RDP request response : " + e.Message, e);
                response.error = "INTERNAL_SERVER_ERROR";
                response.errorMessage = "Failed to process incoming RDP request response";
            }
            return response;
        }
    }
}